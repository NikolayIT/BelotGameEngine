"""Distil the exact frozen suit-average teacher into unchanged card networks."""

import argparse
import hashlib
import itertools
import json
from pathlib import Path
import shutil
import time

import numpy as np
import torch

import fit

SIZES = (600, 512, 256, 128, 32)
NAMES = ('trump', 'notrumps', 'alltrumps')


def fixed_masks(features, tag):
    if features.ndim != 2 or features.shape[1] != 600 or tag not in (1, 2, 3):
        raise ValueError('Expected layout1 card features and tag1,2,3')
    bids = features[:, 534:566].reshape(-1, 4, 8)[:, :, :4]
    fixed = (bids != 0).any(1)
    result = (fixed.to(torch.int64) * features.new_tensor([1, 2, 4, 8], dtype=torch.int64)).sum(1)
    return result | (1 if tag == 1 else 0)


def card_maps(fixed):
    """C# source-to-destination permutation order, identity first."""
    if not 0 <= fixed <= 15:
        raise ValueError('Fixed-suit mask must fit four bits')
    return tuple(tuple(8 * order[card // 8] + card % 8 for card in range(32))
                 for order in itertools.permutations(range(4))
                 if all(not (fixed & (1 << suit)) or order[suit] == suit for suit in range(4)))


def permute_features(features, mapping):
    """Map every card plane source->destination; scalar auction/history stays fixed."""
    inverse = torch.argsort(mapping)
    planes = features[:, :512].reshape(-1, 16, 32).index_select(2, inverse).flatten(1)
    return torch.cat((planes, features[:, 512:]), 1)


@torch.no_grad()
def teacher_values(model, features, tag):
    """Raw normalized Q, matching managed double accumulation before the x26 scale."""
    features = features.float()
    masks = fixed_masks(features, tag)
    baseline, ensemble = features.new_empty((len(features), 32)), features.new_empty((len(features), 32))
    for fixed in range(16):
        rows = (masks == fixed).nonzero().flatten()
        if not len(rows):
            continue
        selected = features.index_select(0, rows)
        original = model(selected)
        maps = card_maps(fixed)
        total = original.double()
        for mapping in maps[1:]:
            mapping = torch.tensor(mapping, device=features.device)
            outputs = model(permute_features(selected, mapping))
            total += outputs.index_select(1, mapping).double()
        baseline.index_copy_(0, rows, original)
        ensemble.index_copy_(0, rows, (total / len(maps)).float())
    return baseline, ensemble


def anchored_targets(baseline, ensemble, legal):
    counts = legal.sum(1, keepdim=True)
    if (baseline.shape != ensemble.shape or baseline.shape != legal.shape
            or legal.dtype != torch.bool or (counts == 0).any()):
        raise ValueError('Expected equal Q tables and a nonempty legal set per state')
    offset = ((baseline - ensemble) * legal).sum(1, keepdim=True) / counts
    return torch.where(legal, ensemble + offset, baseline)


def read_public(path):
    features, old_targets, legal = fit.read_samples(path, 600, 32)
    del old_targets
    if not len(features) or not torch.equal(features[:, 32:64], legal.float()):
        raise ValueError('Action masks must exactly match the public legal-card plane')
    return features, legal


@torch.no_grad()
def prepare(teacher, features, legal, tag, batch, device):
    features = features.to(device=device, dtype=torch.float16)
    legal = legal.to(device)
    baseline = torch.empty((len(features), 32), device=device)
    targets = torch.empty_like(baseline)
    for start in range(0, len(features), batch):
        original, average = teacher_values(teacher, features[start:start + batch], tag)
        baseline[start:start + batch] = original
        targets[start:start + batch] = anchored_targets(original, average, legal[start:start + batch])
    return features, baseline, targets, legal


@torch.no_grad()
def metrics(model, data, batch):
    """Game-point errors/regret; top1 ties use canonical indices from the corpus.

    The corpus omits the original trump rotation, so canonical top1 agreement
    is not an exact physical-card action-agreement metric on tied values.
    ensemble_optimal_choice_rate accepts every action with zero teacher regret.
    """
    features, baseline, targets, legal = data
    totals = dict(states=0, labels=0, centered_squared=0.0, mean_squared=0.0,
                  regret=0.0, agreement=0, optimal=0, changed=0, changed_regret=0.0, changed_agreement=0)
    for start in range(0, len(features), batch):
        x, base, target, mask = (tensor[start:start + batch] for tensor in data)
        prediction = model(x.float())
        count = mask.sum(1)
        error = prediction - target
        mean = (error * mask).sum(1) / count
        centered = error - mean[:, None]
        choice = prediction.masked_fill(~mask, -torch.inf).argmax(1)
        teacher_choice = target.masked_fill(~mask, -torch.inf).argmax(1)
        base_choice = base.masked_fill(~mask, -torch.inf).argmax(1)
        regret = target.gather(1, teacher_choice[:, None]).squeeze(1) - target.gather(1, choice[:, None]).squeeze(1)
        changed = teacher_choice != base_choice
        mean_drift = ((prediction - base) * mask).sum(1) / count
        totals['states'] += len(x)
        totals['labels'] += int(count.sum())
        totals['centered_squared'] += float((centered.square() * mask).sum())
        totals['mean_squared'] += float(mean_drift.square().sum())
        totals['regret'] += float(regret.sum())
        totals['agreement'] += int((choice == teacher_choice).sum())
        totals['optimal'] += int((regret == 0).sum())
        totals['changed'] += int(changed.sum())
        totals['changed_regret'] += float(regret[changed].sum())
        totals['changed_agreement'] += int(((choice == teacher_choice) & changed).sum())
    return dict(states=totals['states'], legal_actions=totals['labels'],
                centered_rmse_game_points=26 * (totals['centered_squared'] / totals['labels']) ** .5,
                baseline_mean_drift_rmse_game_points=26 * (totals['mean_squared'] / totals['states']) ** .5,
                ensemble_regret_game_points=26 * totals['regret'] / totals['states'],
                ensemble_top1_agreement=totals['agreement'] / totals['states'],
                ensemble_optimal_choice_rate=totals['optimal'] / totals['states'],
                teacher_changed_states=totals['changed'],
                changed_state_regret_game_points=26 * totals['changed_regret'] / max(1, totals['changed']),
                changed_state_top1_agreement=totals['changed_agreement'] / max(1, totals['changed']))


def train_epoch(model, optimizer, data, objective, order, batch):
    features, baseline, targets, legal = data
    target = targets if objective == 'ensemble' else baseline
    total, labels = 0.0, 0
    order = torch.as_tensor(order, device=features.device, dtype=torch.long)
    for start in range(0, len(order), batch):
        selected = order[start:start + batch]
        x, y, mask = (tensor.index_select(0, selected) for tensor in (features, target, legal))
        optimizer.zero_grad(set_to_none=True)
        loss = fit.action_loss(model(x.float()), y, mask, value_weight=1.0)
        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0, error_if_nonfinite=True)
        optimizer.step()
        size = int(mask.sum())
        total += float(loss.detach()) * size
        labels += size
    return total / labels


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def run(args):
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.use_deterministic_algorithms(True)
    device = torch.device(args.device)
    if device.type == 'cuda' and not torch.cuda.is_available():
        raise RuntimeError('CUDA unavailable')
    output = Path(args.out)
    source = Path(args.input)
    if output.resolve() == source.resolve() or source.resolve() in output.resolve().parents:
        raise ValueError('Output must be outside the frozen teacher directory')
    output.mkdir(parents=True, exist_ok=True)
    report = dict(arguments=vars(args), torch=torch.__version__, sources={}, networks={})
    bid = source / 'bid.bin'
    fit.read_network(bid, 0)
    report['sources'][str(bid)] = digest(bid)
    for objective in ('control', 'ensemble'):
        for folder in [output / objective] + [output / objective / f'epoch-{i:03}' for i in range(1, args.epochs + 1)]:
            folder.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(bid, folder / 'bid.bin')
    print(json.dumps(report), flush=True)
    started_all = time.perf_counter()
    for tag, name in enumerate(NAMES, 1):
        model_path = source / (name + '.bin')
        teacher = fit.read_network(model_path, tag)
        if teacher.layout != 1 or teacher.sizes != SIZES:
            raise ValueError('This pilot requires unchanged layout1 600->512->256->128->32 cards')
        teacher = teacher.to(device).requires_grad_(False).eval()
        train_path = Path(args.data + '.' + name + '.samples')
        valid_path = Path(args.validation_data + '.' + name + '.samples')
        if train_path.resolve() == valid_path.resolve():
            raise ValueError('Training and heldout data must be separate')
        for path in (model_path, train_path, valid_path):
            report['sources'][str(path)] = digest(path)
        preparation = time.perf_counter()
        x, legal = read_public(train_path)
        training = prepare(teacher, x, legal, tag, args.teacher_batch, device)
        del x, legal
        x, legal = read_public(valid_path)
        validation = prepare(teacher, x, legal, tag, args.teacher_batch, device)
        del x, legal
        fixed = fixed_masks(training[0], tag)
        groups = {str(mask): int((fixed == mask).sum()) for mask in range(16) if (fixed == mask).any()}
        entry = dict(preparation_seconds=time.perf_counter() - preparation, train_states=len(training[0]),
                     validation_states=len(validation[0]), fixed_mask_counts=groups,
                     initial=metrics(teacher, validation, args.batch), objectives={})
        report['networks'][name] = entry
        print(name, 'teacher', json.dumps({key: value for key, value in entry.items() if key != 'objectives'}), flush=True)
        for objective in ('control', 'ensemble'):
            model = fit.read_network(model_path, tag).to(device)
            optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
            rng = np.random.default_rng(args.seed + tag)
            epochs = []
            entry['objectives'][objective] = epochs
            for epoch in range(1, args.epochs + 1):
                started = time.perf_counter()
                rate = args.learning_rate * (.3 if epoch > args.epochs * .7 else 1)
                optimizer.param_groups[0]['lr'] = rate
                order = rng.permutation(len(training[0]))
                loss = train_epoch(model, optimizer, training, objective, order, args.batch)
                path = output / objective / f'epoch-{epoch:03}' / (name + '.bin')
                fit.write_network(model, path)
                restored = fit.read_network(path, tag).to(device)
                epoch_record = dict(epoch=epoch, learning_rate=rate, training_loss_normalized_q=loss,
                                    row_order_sha256=hashlib.sha256(order.tobytes()).hexdigest(),
                                    export_sha256=digest(path), validation=metrics(restored, validation, args.batch),
                                    seconds=time.perf_counter() - started)
                if objective == 'ensemble':
                    control = entry['objectives']['control'][epoch - 1]
                    if control['row_order_sha256'] != epoch_record['row_order_sha256'] or control['learning_rate'] != rate:
                        raise RuntimeError('Matched control order/rate differs')
                epochs.append(epoch_record)
                (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
                print(name, objective, json.dumps(epoch_record), flush=True)
                del restored
            fit.write_network(model, output / objective / (name + '.bin'))
            del model, optimizer
        del training, validation, teacher, fixed
        if device.type == 'cuda':
            torch.cuda.empty_cache()
    for objective in ('control', 'ensemble'):
        for folder in [output / objective] + [output / objective / f'epoch-{i:03}' for i in range(1, args.epochs + 1)]:
            if digest(folder / 'bid.bin') != report['sources'][str(bid)]:
                raise RuntimeError('Bidding must remain byte-identical')
    report['total_seconds'] = time.perf_counter() - started_all
    (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('completed', report['total_seconds'], flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--epochs', type=int, default=4)
    parser.add_argument('--batch', type=int, default=2048)
    parser.add_argument('--teacher-batch', type=int, default=4096)
    parser.add_argument('--learning-rate', type=float, default=.00001)
    parser.add_argument('--seed', type=int, default=9851)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if args.epochs < 1 or args.batch < 1 or args.teacher_batch < 1 or not 0 < args.learning_rate <= .001:
        parser.error('Invalid epochs, batch or learning rate')
    run(args)
