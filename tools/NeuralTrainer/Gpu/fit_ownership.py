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
from fit_belief import read_owners, belief_loss

SIZES = (600, 128, 64, 96)
NAMES = ('trump', 'notrumps', 'alltrumps')


def read_network(path, expected_tag):
    raw = Path(path).read_bytes()
    count = sum((a + 1) * b for a, b in zip(SIZES, SIZES[1:]))
    expected = (fit.NETWORK_MAGIC, 1, expected_tag, 1, 3, *SIZES)
    if len(raw) != 36 + 2 * count or struct.unpack_from('<9i', raw) != expected:
        raise ValueError('Ownership network header, shape or length mismatch')
    parameters = np.frombuffer(raw, '<f2', offset=36).astype(np.float32)
    if not np.isfinite(parameters).all():
        raise ValueError('Non-finite ownership weights')
    model = fit.Network(expected_tag, 1, SIZES)
    offset = 0
    with torch.no_grad():
        for layer, a, b in zip(model.layers, SIZES, SIZES[1:]):
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
    report = dict(arguments=vars(args), torch=torch.__version__, sizes=SIZES,
                  sources={}, networks={})
    print(json.dumps(report), flush=True)
    for index, name in enumerate(NAMES):
        tag = 11 + index
        model = fit.Network(tag, 1, SIZES).to(device)
        training_path = args.data + '.' + name + '.samples'
        validation_path = args.validation_data + '.' + name + '.samples'
        if Path(training_path).resolve() == Path(validation_path).resolve():
            raise ValueError('Training and validation must be separate files')
        training = fit.read_samples(training_path, 600, 32)[0]
        validation = fit.read_samples(validation_path, 600, 32)[0]
        owners = read_owners(training_path + '.owners', len(training))
        validation_owners = read_owners(validation_path + '.owners', len(validation))
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
            restored = read_network(path, tag).to(device)
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
    parser.add_argument('--epochs', type=int, default=12)
    parser.add_argument('--batch', type=int, default=1024)
    parser.add_argument('--learning-rate', type=float, default=.001)
    parser.add_argument('--seed', type=int, default=9821)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if args.epochs < 1 or args.batch < 1 or not 0 < args.learning_rate <= 1:
        parser.error('Invalid epochs, batch or learning rate')
    run(args)
