"""Fit a small final-four-trick correction; keep the managed base actor frozen."""

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import time

import fit
import torch
from torch import nn

SIZES = (600, 64, 32)
FILE_BYTES = 81120
NAMES = ('trump', 'notrumps', 'alltrumps')


def make_model(tag):
    if tag not in (21, 22, 23):
        raise ValueError('Late correction tag must be 21, 22 or 23')
    model = fit.Network(tag, 1, SIZES)
    nn.init.zeros_(model.layers[-1].weight)
    nn.init.zeros_(model.layers[-1].bias)
    return model


def read_correction(path, tag):
    if tag not in (21, 22, 23) or Path(path).stat().st_size != FILE_BYTES:
        raise ValueError('Late correction tag or file length mismatch')
    with Path(path).open('rb') as stream:
        header = struct.unpack('<8i', stream.read(32))
    if header != (fit.NETWORK_MAGIC, 1, tag, 1, 2, *SIZES):
        raise ValueError('Late correction header mismatch')
    model = fit.read_network(path, tag)
    if model.sizes != SIZES:
        raise ValueError('Late correction architecture mismatch')
    return model


def late_mask(features):
    """Layout 1 has the completed-trick one-hot at columns 512..519."""
    if features.ndim != 2 or features.shape[1] != 600:
        raise ValueError('Late correction requires layout 1 with 600 inputs')
    phase = features[:, 512:520]
    if not bool((((phase == 0) | (phase == 1)).all()) & (phase.sum(1) == 1).all()):
        raise ValueError('Invalid completed-trick one-hot')
    return phase[:, 4:].sum(1) == 1


def center(values, mask):
    count = mask.sum(1, keepdim=True)
    if bool((count == 0).any()):
        raise ValueError('Every decision must contain a legal action')
    mean = (values * mask).sum(1, keepdim=True) / count
    return torch.where(mask, values - mean, 0)


def delta_targets(base, teacher, mask):
    return center(teacher - base, mask)


def correction_loss(prediction, target, mask):
    return fit.action_loss(prediction, target, mask, value_weight=0)


def apply_correction(base, correction, features, mask):
    """Preserve earlier rows and illegal entries exactly, as the C# gate does."""
    result = base.clone()
    eligible = late_mask(features) & (mask.sum(1) > 1)
    delta = center(correction, mask)
    changed = eligible[:, None] & mask & (delta != 0)
    result[changed] = result[changed] + delta[changed]
    return result


def read_late_samples(path, max_samples=0, seed=0):
    data = fit.read_samples(path, 600, 32)
    selected = torch.where(late_mask(data[0]) & (data[2].sum(1) > 1))[0]
    if max_samples > 0 and len(selected) > max_samples:
        generator = torch.Generator().manual_seed(seed)
        selected = selected[torch.randperm(len(selected), generator=generator)[:max_samples]]
    if not len(selected):
        raise ValueError(f'No late non-forced positions in {path}')
    return tuple(tensor[selected].contiguous() for tensor in data)


@torch.no_grad()
def base_values(model, features, batch, device):
    result = torch.empty((len(features), 32))
    for start in range(0, len(features), batch):
        result[start:start + batch] = model(features[start:start + batch].to(device)).cpu()
    return result


@torch.no_grad()
def metrics(model, data, base, batch, device):
    squared = regret = baseline_regret = best = actions = 0.0
    for start in range(0, len(base), batch):
        features, teacher, mask = (item[start:start + batch].to(device) for item in data)
        original = base[start:start + batch].to(device)
        corrected = apply_correction(original, model(features), features, mask)
        error = center(corrected - teacher, mask)
        chosen = corrected.masked_fill(~mask, -torch.inf).argmax(1)
        old_chosen = original.masked_fill(~mask, -torch.inf).argmax(1)
        maximum = teacher.masked_fill(~mask, -torch.inf).max(1).values
        loss = maximum - teacher.gather(1, chosen[:, None]).squeeze(1)
        old_loss = maximum - teacher.gather(1, old_chosen[:, None]).squeeze(1)
        squared += float((error.square() * mask).sum())
        actions += int(mask.sum())
        regret += float(loss.sum())
        baseline_regret += float(old_loss.sum())
        best += int((loss == 0).sum())
    return dict(samples=len(base), actions=int(actions),
                centered_rmse=fit.VALUE_SCALE * (squared / actions) ** 0.5,
                teacher_regret=fit.VALUE_SCALE * regret / len(base),
                base_teacher_regret=fit.VALUE_SCALE * baseline_regret / len(base),
                teacher_best_fraction=best / len(base))


def sha256(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--in', dest='base', required=True)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--epochs', type=int, default=12)
    parser.add_argument('--batch', type=int, default=1024)
    parser.add_argument('--learning-rate', type=float, default=0.001)
    parser.add_argument('--max-samples', type=int, default=0)
    parser.add_argument('--seed', type=int, default=11921)
    parser.add_argument('--threads', type=int, default=2)
    parser.add_argument('--device', default='cuda')
    args = parser.parse_args()
    if (args.epochs < 1 or args.batch < 1 or args.threads < 1 or args.max_samples < 0
            or not math.isfinite(args.learning_rate) or args.learning_rate <= 0):
        parser.error('Invalid fitting limits or learning rate')
    if Path(args.data).resolve() == Path(args.validation_data).resolve():
        parser.error('Training and validation prefixes must be independent')
    if Path(args.base).resolve() == Path(args.out).resolve():
        parser.error('Correction output must not overwrite the frozen base')
    fit.disable_power_throttling()
    torch.set_num_threads(args.threads)
    torch.use_deterministic_algorithms(True)
    torch.backends.cuda.matmul.allow_tf32 = False
    torch.backends.cudnn.allow_tf32 = False
    output = Path(args.out)
    output.mkdir(parents=True, exist_ok=True)
    started = time.monotonic()
    report = dict(arguments=vars(args), architecture=SIZES, layout=1, phase='TricksPlayed >= 4',
                  objective='centered Huber of teacher-minus-frozen-base advantage', networks={})
    for index, name in enumerate(NAMES):
        torch.manual_seed(args.seed + index)
        base_path = Path(args.base) / f'{name}.bin'
        base_hash = sha256(base_path)
        base = fit.read_network(base_path, index + 1).to(args.device).requires_grad_(False).eval()
        train_path = Path(f'{args.data}.{name}.samples')
        validation_path = Path(f'{args.validation_data}.{name}.samples')
        train = read_late_samples(train_path, args.max_samples, args.seed + index)
        validation = read_late_samples(validation_path)
        train_base = base_values(base, train[0], args.batch, args.device)
        validation_base = base_values(base, validation[0], args.batch, args.device)
        targets = delta_targets(train_base, train[1], train[2])
        model = make_model(21 + index).to(args.device)
        optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
        measured = metrics(model, validation, validation_base, args.batch, args.device)
        history = [dict(epoch=0, **measured)]
        best_regret = measured['teacher_regret']
        best_epoch = 0
        fit.write_network(model, output / f'{name}.bin')
        generator = torch.Generator().manual_seed(args.seed + index)
        for epoch in range(1, args.epochs + 1):
            order = torch.randperm(len(train[0]), generator=generator)
            for start in range(0, len(order), args.batch):
                selection = order[start:start + args.batch]
                features, mask = train[0][selection].to(args.device), train[2][selection].to(args.device)
                target = targets[selection].to(args.device)
                optimizer.zero_grad(set_to_none=True)
                loss = correction_loss(model(features), target, mask)
                if not bool(torch.isfinite(loss)):
                    raise ValueError('Non-finite correction loss')
                loss.backward()
                optimizer.step()
            checkpoint = output / f'epoch-{epoch:03d}' / f'{name}.bin'
            fit.write_network(model, checkpoint)
            # Select using the actual exported half-precision weights.
            exported = read_correction(checkpoint, 21 + index).to(args.device)
            measured = metrics(exported, validation, validation_base, args.batch, args.device)
            history.append(dict(epoch=epoch, **measured))
            print(json.dumps(dict(network=name, epoch=epoch, **measured)), flush=True)
            if measured['teacher_regret'] < best_regret:
                best_regret, best_epoch = measured['teacher_regret'], epoch
                fit.write_network(exported, output / f'{name}.bin')
        if sha256(base_path) != base_hash:
            raise RuntimeError('Frozen base changed during correction fitting')
        report['networks'][name] = dict(train_samples=len(train[0]), validation_samples=len(validation[0]),
                                       base_sha256=base_hash, train_sha256=sha256(train_path),
                                       validation_sha256=sha256(validation_path), best_epoch=best_epoch,
                                       best_teacher_regret=best_regret, history=history,
                                       output_sha256=sha256(output / f'{name}.bin'))
        report['elapsed_seconds'] = time.monotonic() - started
        (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
