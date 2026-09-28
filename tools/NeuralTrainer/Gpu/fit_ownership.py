"""Train a separate public-feature card-location model for managed sampling."""

import argparse
import hashlib
import json
from pathlib import Path
import struct
import time

import numpy as np
import torch
from torch import nn

import fit
from fit_belief import read_owners as read_legacy_owners, belief_loss

SIZES = (600, 128, 64, 96)
NAMES = ('trump', 'notrumps', 'alltrumps')


def sizes_for_layout(layout):
    if layout not in (1, 2):
        raise ValueError('Unsupported ownership feature layout')
    return (600 if layout == 1 else 664, *SIZES[1:])


def read_features(path, layout):
    if layout == 1:
        return fit.read_samples(path, 600, 32)[0]
    sizes_for_layout(layout)
    raw = Path(path).read_bytes()
    if len(raw) < 16:
        raise ValueError('Truncated history sample header')
    magic, version, outputs, count = struct.unpack_from('<4i', raw)
    if (magic, version, outputs) != (0x32504E42, 2, 32) or not 0 <= count <= 10_000_000:
        raise ValueError('Invalid history sample header')
    if count * 8 > len(raw) - 16:
        raise ValueError('History sample count exceeds available data')
    features = np.zeros((count, 664), np.float32)
    dtype = np.dtype([('index', '<u2'), ('value', '<f2')])
    offset = 16
    for sample in range(count):
        if offset + 2 > len(raw):
            raise ValueError('Truncated history record')
        active, = struct.unpack_from('<H', raw, offset)
        offset += 2
        if offset + 4 * active + 4 > len(raw):
            raise ValueError('Truncated history features')
        record = np.frombuffer(raw, dtype, count=active, offset=offset)
        indices, values = record['index'], record['value']
        if (indices >= 664).any() or not np.isfinite(values).all() or np.unique(indices).size != active:
            raise ValueError('Invalid history features')
        features[sample, indices] = values
        offset += 4 * active
        mask, = struct.unpack_from('<I', raw, offset)
        offset += 4
        if not mask:
            raise ValueError('Invalid history action mask')
        labels = mask.bit_count()
        if offset + 2 * labels > len(raw) or not np.isfinite(np.frombuffer(raw, '<f2', labels, offset)).all():
            raise ValueError('Invalid history labels')
        offset += 2 * labels
    if offset != len(raw):
        raise ValueError('Trailing history sample bytes')
    return torch.from_numpy(features)


def read_owners(path, count, layout=1):
    if layout == 1:
        return read_legacy_owners(path, count)
    sizes_for_layout(layout)
    raw = Path(path).read_bytes()
    if len(raw) != 16 + 8 * count or struct.unpack_from('<4i', raw) != (0x314F5042, 2, 32, count):
        raise ValueError('Invalid history ownership header or length')
    packed = np.frombuffer(raw, '<u8', offset=16)
    labels = ((packed[:, None] >> (2 * np.arange(32, dtype=np.uint64))) & 3).astype(np.int64) - 1
    return torch.from_numpy(labels)


def read_network(path, expected_tag, expected_layout=None):
    raw = Path(path).read_bytes()
    if len(raw) < 36 or expected_tag not in (11, 12, 13):
        raise ValueError('Truncated ownership header or invalid expected tag')
    layout, = struct.unpack_from('<i', raw, 12)
    sizes = sizes_for_layout(layout)
    if expected_layout is not None and layout != expected_layout:
        raise ValueError('Ownership feature layout mismatch')
    count = sum((a + 1) * b for a, b in zip(sizes, sizes[1:]))
    expected = (fit.NETWORK_MAGIC, 1, expected_tag, layout, 3, *sizes)
    if len(raw) != 36 + 2 * count or struct.unpack_from('<9i', raw) != expected:
        raise ValueError('Ownership network header, shape or length mismatch')
    parameters = np.frombuffer(raw, '<f2', offset=36).astype(np.float32)
    if not np.isfinite(parameters).all():
        raise ValueError('Non-finite ownership weights')
    model = fit.Network(expected_tag, layout, sizes)
    offset = 0
    with torch.no_grad():
        for layer, a, b in zip(model.layers, sizes, sizes[1:]):
            layer.weight.copy_(torch.from_numpy(parameters[offset:offset + a * b].reshape(a, b).T.copy()))
            offset += a * b
            layer.bias.copy_(torch.from_numpy(parameters[offset:offset + b]))
            offset += b
    return model


def allowed_owners(features):
    """Local public constraints only; exact joint hand sizes belong to the sampler."""
    excluded = features[:, 10 * 32:13 * 32].reshape(-1, 3, 32).transpose(1, 2) > 0
    known = features[:, 13 * 32:16 * 32].reshape(-1, 3, 32).transpose(1, 2) > 0
    return ~excluded & (~known.any(2, keepdim=True) | known)


@torch.no_grad()
def metrics(model, features, owners, batch, device):
    totals = np.zeros(6, np.float64)
    for start in range(0, len(features), batch):
        x = features[start:start + batch].to(device)
        target = owners[start:start + batch].to(device)
        valid = target >= 0
        logits = model(x).reshape(-1, 32, 3)
        allowed = allowed_owners(x)
        if not allowed[valid].gather(1, target[valid, None]).all():
            raise ValueError('An ownership label violates public constraints')
        logits, target, allowed = logits[valid], target[valid], allowed[valid]
        masked = logits.masked_fill(~allowed, -torch.inf)
        count = allowed.sum(1)
        totals += (len(target),
                   float(nn.functional.cross_entropy(logits, target, reduction='sum')),
                   float((logits.argmax(1) == target).sum()),
                   float(nn.functional.cross_entropy(masked, target, reduction='sum')),
                   float((masked.argmax(1) == target).sum()),
                   float(count.float().log().sum()))
    n = max(1, totals[0])
    # A uniform local baseline has expected accuracy 1/number-of-allowed-owners,
    # avoiding an arbitrary class-index tie break.
    baseline_correct = 0.0
    for start in range(0, len(features), batch):
        valid = owners[start:start + batch] >= 0
        count = allowed_owners(features[start:start + batch]).sum(2)[valid]
        baseline_correct += float((1.0 / count).sum())
    return dict(cards=int(totals[0]), raw_nll=totals[1] / n, raw_accuracy=totals[2] / n,
                masked_nll=totals[3] / n, masked_accuracy=totals[4] / n,
                uniform_legal_nll=totals[5] / n, uniform_legal_accuracy=baseline_correct / n)


def run(args):
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.use_deterministic_algorithms(True)
    device = torch.device(args.device)
    if device.type == 'cuda' and not torch.cuda.is_available():
        raise RuntimeError('CUDA is unavailable')
    output = Path(args.out)
    output.mkdir(parents=True, exist_ok=True)
    sizes = sizes_for_layout(args.layout)
    report = dict(arguments=vars(args), torch=torch.__version__, sizes=sizes,
                  sources={}, networks={})
    print(json.dumps(report), flush=True)
    for index, name in enumerate(NAMES):
        tag = 11 + index
        model = fit.Network(tag, args.layout, sizes).to(device)
        if args.layout == 2:
            with torch.no_grad():
                model.layers[0].weight[:, 600:].zero_()
        training_path = args.data + '.' + name + '.samples'
        validation_path = args.validation_data + '.' + name + '.samples'
        if Path(training_path).resolve() == Path(validation_path).resolve():
            raise ValueError('Training and validation must be separate files')
        training = read_features(training_path, args.layout)
        validation = read_features(validation_path, args.layout)
        owners = read_owners(training_path + '.owners', len(training), args.layout)
        validation_owners = read_owners(validation_path + '.owners', len(validation), args.layout)
        if args.history_features == 'zero':
            training[:, 600:] = 0
            validation[:, 600:] = 0
        if not len(training) or not len(validation):
            raise ValueError('Both datasets must contain samples')
        for path in (training_path, training_path + '.owners', validation_path, validation_path + '.owners'):
            report['sources'][path] = hashlib.sha256(Path(path).read_bytes()).hexdigest()
        optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
        rng = np.random.default_rng(args.seed + tag)
        entries = []
        initial = metrics(model, validation, validation_owners, args.batch, device)
        print(name, 'initial', json.dumps(initial), flush=True)
        for epoch in range(1, args.epochs + 1):
            started = time.perf_counter()
            optimizer.param_groups[0]['lr'] = args.learning_rate * (0.3 if epoch > args.epochs * .7 else 1)
            order = rng.permutation(len(training))
            loss_sum, labelled = 0.0, 0
            for start in range(0, len(order), args.batch):
                selected = order[start:start + args.batch]
                x = training[selected].to(device)
                target = owners[selected].to(device)
                optimizer.zero_grad(set_to_none=True)
                loss = belief_loss(model(x).reshape(-1, 32, 3), target)
                loss.backward()
                nn.utils.clip_grad_norm_(model.parameters(), 1.0, error_if_nonfinite=True)
                optimizer.step()
                count = int((target >= 0).sum())
                loss_sum += float(loss.detach()) * count
                labelled += count
            path = output / f'epoch-{epoch:03}' / (name + '.bin')
            fit.write_network(model, path)
            # Report the actual half-precision export, not only its float training copy.
            restored = read_network(path, tag, args.layout).to(device)
            measured = metrics(restored, validation, validation_owners, args.batch, device)
            entry = dict(epoch=epoch, seconds=time.perf_counter() - started,
                         training_nll=loss_sum / max(1, labelled), validation=measured)
            entries.append(entry)
            print(name, json.dumps(entry), flush=True)
        fit.write_network(model, output / (name + '.bin'))
        report['networks'][name] = dict(initial=initial, epochs=entries)
        (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        del model, restored, optimizer, training, validation, owners, validation_owners
        if device.type == 'cuda':
            torch.cuda.empty_cache()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--layout', type=int, choices=(1, 2), default=1)
    parser.add_argument('--history-features', choices=('full', 'zero'), default='full')
    parser.add_argument('--epochs', type=int, default=12)
    parser.add_argument('--batch', type=int, default=1024)
    parser.add_argument('--learning-rate', type=float, default=.001)
    parser.add_argument('--seed', type=int, default=9821)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if args.epochs < 1 or args.batch < 1 or not 0 < args.learning_rate <= 1:
        parser.error('Invalid epochs, batch or learning rate')
    if args.layout == 1 and args.history_features != 'full':
        parser.error('History feature ablation requires layout 2')
    run(args)
