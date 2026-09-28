"""Matched warm-start ownership experiments: joint conditional loss versus CE.

Both objectives start from the same checked half-precision exports, use fresh
Adam states, and visit identical shuffled rows. The joint objective conditions
on capacities and stored public masks. Runtime declaration filters remain an
additional condition; evaluate untempered weights first (power=1, uniform mix=0).
"""

import argparse
import hashlib
import json
from pathlib import Path
import time

import numpy as np
import torch
from torch import nn

import fit
import fit_ownership
import joint_ownership


def read_dataset(path, actor_tag, layout, data_format):
    """Keep real public inputs and ownership labels, without synthesizing Q targets."""
    if data_format == 'ppo':
        if layout != 1:
            raise ValueError('PPO ownership files contain only feature layout 1')
        from ownership_rollout import read_ownership
        rollout = read_ownership(path, actor_tag)
        return rollout.x, rollout.owners
    if data_format != 'samples':
        raise ValueError('Unknown ownership data format')
    features = fit_ownership.read_features(path, layout)
    owners = fit_ownership.read_owners(str(path) + '.owners', len(features), layout)
    return features, owners


def validate_public_labels(features, owners, batch=8192):
    """Check target cards/capacities against public feature planes, before fitting.

    Planes 3..5 contain the other players' cards in completed tricks; 6..8 contain
    their cards in the current trick. Both use relative seat order. Counts reveal
    each remaining hand size independently of the hidden-card ownership labels.
    """
    if (features.ndim != 2 or features.shape[1] not in (600, 664)
            or owners.shape != (len(features), 32)
            or owners.dtype not in (torch.int32, torch.int64) or batch < 1):
        raise ValueError('Unexpected ownership feature or label shape')
    for start in range(0, len(features), batch):
        x, target = features[start:start + batch], owners[start:start + batch]
        planes = x[:, :9 * 32].reshape(-1, 9, 32)
        if not ((planes == 0) | (planes == 1)).all() or ((target < -1) | (target > 2)).any():
            raise ValueError('Invalid card planes or owner labels')
        seen = planes[:, 0] + planes[:, 2:9].sum(1)
        if (seen > 1).any() or not torch.equal(target >= 0, seen == 0):
            raise ValueError('Labelled unseen cards disagree with the public history')
        remaining = 8 - planes[:, 3:6].sum(2) - planes[:, 6:9].sum(2)
        counts = torch.stack([(target == owner).sum(1) for owner in range(3)], dim=1)
        if not torch.equal(counts, remaining):
            raise ValueError('Target hand sizes disagree with public remaining capacities')
        allowed = fit_ownership.allowed_owners(x)
        permitted = allowed.gather(2, target.clamp_min(0).long().unsqueeze(2)).squeeze(2)
        if (~permitted & (target >= 0)).any():
            raise ValueError('An ownership target violates public constraints')


@torch.no_grad()
def joint_metrics(model, features, owners, batch, device):
    """Report whole-world NLL and the pooled NLL per labelled card."""
    total, cards = 0.0, 0
    for start in range(0, len(features), batch):
        x = features[start:start + batch].to(device)
        target = owners[start:start + batch].to(device)
        logits = model(x).reshape(-1, 32, 3)
        total += float(joint_ownership.loss(logits, target, fit_ownership.allowed_owners(x), reduction='sum'))
        cards += int((target >= 0).sum())
    return dict(states=len(features), joint_nll=total / max(1, len(features)),
                joint_nll_per_card=total / max(1, cards))


def measure(model, features, owners, batch, device):
    return {**fit_ownership.metrics(model, features, owners, batch, device),
            **joint_metrics(model, features, owners, batch, device)}


def local_loss(logits, owners, normalization='card'):
    """Use the same state weighting as the joint objective, including empty rows."""
    entries = nn.functional.cross_entropy(logits.reshape(-1, 3), owners.reshape(-1),
                                          ignore_index=-1, reduction='none').reshape_as(owners)
    per_state = entries.sum(1)
    if normalization == 'card':
        per_state = per_state / (owners >= 0).sum(1).clamp_min(1)
    elif normalization != 'state':
        raise ValueError('Unknown normalization')
    return per_state.mean()


def train_epoch(model, optimizer, features, owners, order, batch, device, objective, normalization, allowed=None):
    total, weight = 0.0, 0
    order = torch.as_tensor(order, device=features.device, dtype=torch.long)
    for start in range(0, len(order), batch):
        selected = order[start:start + batch]
        x = features.index_select(0, selected).to(device)
        target = owners.index_select(0, selected).to(device)
        optimizer.zero_grad(set_to_none=True)
        logits = model(x).reshape(-1, 32, 3)
        if objective == 'joint':
            constraints = fit_ownership.allowed_owners(x) if allowed is None else allowed.index_select(0, selected).to(device)
            value = joint_ownership.loss(logits, target, constraints, normalization=normalization)
        elif objective == 'ce':
            value = local_loss(logits, target, normalization)
        else:
            raise ValueError('Unknown training objective')
        value.backward()
        nn.utils.clip_grad_norm_(model.parameters(), 1.0, error_if_nonfinite=True)
        optimizer.step()
        total += float(value.detach()) * len(selected)
        weight += len(selected)
    return total / max(1, weight)


def run(args):
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.use_deterministic_algorithms(True)
    device = torch.device(args.device)
    if device.type == 'cuda' and not torch.cuda.is_available():
        raise RuntimeError('CUDA is unavailable')
    objectives = ('ce', 'joint') if args.objective == 'both' else (args.objective,)
    data_format = getattr(args, 'data_format', 'samples')
    if data_format not in ('samples', 'ppo') or (data_format == 'ppo' and args.layout != 1):
        raise ValueError('Invalid data format or unsupported PPO feature layout')
    output = Path(args.out)
    destinations = [output / objective for objective in objectives]
    destinations += [output / objective / f'epoch-{epoch:03}'
                     for objective in objectives for epoch in range(1, args.epochs + 1)]
    if any(path.resolve() == Path(args.input).resolve() for path in destinations):
        raise ValueError('Output would overwrite the warm-start folder')
    output.mkdir(parents=True, exist_ok=True)
    report = dict(arguments=vars(args), torch=torch.__version__, sources={}, networks={})
    if data_format == 'ppo':
        report['collections'] = {}
        for prefix in (args.data, args.validation_data):
            manifest = Path(prefix + '.ppo.json')
            if manifest.exists():
                report['collections'][str(manifest)] = json.loads(manifest.read_text())
                report['sources'][str(manifest)] = hashlib.sha256(manifest.read_bytes()).hexdigest()
    print(json.dumps(report), flush=True)
    for index, name in enumerate(fit_ownership.NAMES):
        tag = 11 + index
        source = Path(args.input) / (name + '.bin')
        extension = '.ppo' if data_format == 'ppo' else '.samples'
        training_path = Path(args.data + '.' + name + extension)
        validation_path = Path(args.validation_data + '.' + name + extension)
        if training_path.resolve() == validation_path.resolve():
            raise ValueError('Training and validation must use separate files')
        training, owners = read_dataset(training_path, index + 1, args.layout, data_format)
        validation, validation_owners = read_dataset(validation_path, index + 1, args.layout, data_format)
        if not len(training) or not len(validation):
            raise ValueError('Both datasets must contain samples')
        validate_public_labels(training, owners)
        validate_public_labels(validation, validation_owners)
        source_files = [source, training_path, validation_path]
        if data_format == 'samples':
            source_files += [Path(str(training_path) + '.owners'), Path(str(validation_path) + '.owners')]
        for path in source_files:
            report['sources'][str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()

        allowed = None
        if args.cache_device:
            allowed = fit_ownership.allowed_owners(training)
            tensors = (training, validation, owners, validation_owners, allowed)
            required_bytes = sum(tensor.numel() * tensor.element_size() for tensor in tensors)
            if device.type == 'cuda':
                free_bytes, _ = torch.cuda.mem_get_info(device)
                reserve_bytes = args.cache_reserve_mib * 1024 * 1024
                if free_bytes < required_bytes + reserve_bytes:
                    raise RuntimeError('Insufficient free GPU memory for the dataset and requested training reserve')
            training, validation, owners, validation_owners, allowed = (tensor.to(device) for tensor in tensors)
            print(name, 'cached_bytes', required_bytes, 'device', str(device), flush=True)

        entries = {}
        report['networks'][name] = entries
        for objective in objectives:
            model = fit_ownership.read_network(source, tag, args.layout).to(device)
            optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
            rng = np.random.default_rng(args.seed + tag)
            initial = measure(model, validation, validation_owners, args.batch, device)
            print(name, objective, 'initial', json.dumps(initial), flush=True)
            epochs = []
            entries[objective] = dict(initial=initial, epochs=epochs)
            for epoch in range(1, args.epochs + 1):
                started = time.perf_counter()
                rate = args.learning_rate * (0.3 if epoch > args.epochs * .7 else 1)
                optimizer.param_groups[0]['lr'] = rate
                order = rng.permutation(len(training))
                training_loss = train_epoch(model, optimizer, training, owners, order, args.batch,
                                            device, objective, args.normalization, allowed)
                path = output / objective / f'epoch-{epoch:03}' / (name + '.bin')
                fit.write_network(model, path)
                restored = fit_ownership.read_network(path, tag, args.layout).to(device)
                validation_metrics = measure(restored, validation, validation_owners, args.batch, device)
                entry = dict(epoch=epoch, seconds=time.perf_counter() - started,
                             learning_rate=rate, training_loss=training_loss,
                             row_order_sha256=hashlib.sha256(order.tobytes()).hexdigest(),
                             validation=validation_metrics,
                             export_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
                epochs.append(entry)
                (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
                print(name, objective, json.dumps(entry), flush=True)
            fit.write_network(model, output / objective / (name + '.bin'))
            del model, restored, optimizer
            if device.type == 'cuda':
                torch.cuda.empty_cache()
        (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        del training, validation, owners, validation_owners, allowed
        if device.type == 'cuda':
            torch.cuda.empty_cache()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--data-format', choices=('samples', 'ppo'), default='samples')
    parser.add_argument('--out', required=True)
    parser.add_argument('--layout', type=int, choices=(1, 2), default=1)
    parser.add_argument('--objective', choices=('both', 'ce', 'joint'), default='both')
    parser.add_argument('--normalization', choices=('state', 'card'), default='card')
    parser.add_argument('--epochs', type=int, default=4)
    parser.add_argument('--batch', type=int, default=512)
    parser.add_argument('--cache-device', action='store_true', help='Cache this contract on the training device after CPU validation')
    parser.add_argument('--cache-reserve-mib', type=int, default=1536,
                        help='Required free memory after caching, covering DP activations and at least 768 MiB headroom; size after benchmarking')
    parser.add_argument('--learning-rate', type=float, default=.0001)
    parser.add_argument('--seed', type=int, default=9833)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if args.epochs < 1 or args.batch < 1 or not 0 < args.learning_rate <= 1 or args.cache_reserve_mib < 768:
        parser.error('Invalid epochs, batch or learning rate')
    run(args)
