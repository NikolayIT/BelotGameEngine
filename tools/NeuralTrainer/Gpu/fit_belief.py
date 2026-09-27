"""Controlled joint Q-value/card-location experiment; exports the original managed MLP."""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import time

import numpy as np
import torch
from torch import nn

import fit


def read_owners(path, count):
    raw = Path(path).read_bytes()
    if len(raw) != 16 + 8 * count or struct.unpack_from('<4i', raw) != (0x314F5042, 1, 32, count):
        raise ValueError('Invalid ownership label header or length')
    packed = np.frombuffer(raw, '<u8', offset=16)
    labels = ((packed[:, None] >> (2 * np.arange(32, dtype=np.uint64))) & 3).astype(np.int64) - 1
    return torch.from_numpy(labels)


class BeliefModel(nn.Module):
    def __init__(self, base):
        super().__init__()
        self.base = base
        self.head = nn.Linear(base.sizes[-2], 32 * 3)

    def forward(self, x):
        for layer in self.base.layers[:-1]:
            x = torch.relu(layer(x))
        return self.base.layers[-1](x), self.head(x).reshape(-1, 32, 3)


def belief_loss(logits, owners):
    valid = owners >= 0
    if not valid.any():
        return logits.sum() * 0
    return nn.functional.cross_entropy(logits[valid], owners[valid])


@torch.no_grad()
def belief_metrics(model, features, owners, batch, device):
    total, correct, loss = 0, 0, 0.0
    for start in range(0, len(features), batch):
        target = owners[start:start + batch].to(device)
        logits = model(features[start:start + batch].to(device))[1]
        valid = target >= 0
        count = int(valid.sum())
        total += count
        correct += int(((logits.argmax(2) == target) & valid).sum())
        loss += float(belief_loss(logits, target)) * count
    return dict(cards=total, cross_entropy=loss / max(1, total), accuracy=correct / max(1, total))


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
    sources = {}
    print(json.dumps(dict(arguments=vars(args), torch=torch.__version__)), flush=True)
    for tag, name in enumerate(fit.NAMES):
        source = Path(args.input) / (name + '.bin')
        sources[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
        if tag == 0:
            for destination in [output] + [output / f'epoch-{e:03}' for e in range(1, args.epochs + 1)]:
                destination.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, destination / (name + '.bin'))
            continue
        base = fit.read_network(source, tag)
        model = BeliefModel(base).to(device)
        prefix = args.data + '.' + name + '.samples'
        data = fit.read_samples(prefix, 600, 32)
        owners = read_owners(prefix + '.owners', len(data[0]))
        validation_path = args.validation_data + '.' + name + '.samples'
        validation = fit.read_samples(validation_path, 600, 32)
        validation_owners = (read_owners(validation_path + '.owners', len(validation[0]))
                             if Path(validation_path + '.owners').is_file() else None)
        for path in (prefix, prefix + '.owners', validation_path):
            sources[path] = hashlib.sha256(Path(path).read_bytes()).hexdigest()
        if validation_owners is not None:
            sources[validation_path + '.owners'] = hashlib.sha256(Path(validation_path + '.owners').read_bytes()).hexdigest()
        if not len(data[0]) or not len(validation[0]):
            raise ValueError('Training and independent validation must contain samples')
        rng = np.random.default_rng(args.seed + tag)
        valid_slots = np.arange(len(validation[0]))
        print(name, 'initial', json.dumps(fit.diagnostics(base, validation, valid_slots, args.batch, device, args.card_value_weight)), flush=True)
        # Warm up the random auxiliary head while preserving the entire playing network.
        base.requires_grad_(False)
        warm_optimizer = torch.optim.Adam(model.head.parameters(), lr=0.001)
        for start in range(0, len(data[0]), args.batch):
            x = data[0][start:start + args.batch].to(device)
            target = owners[start:start + args.batch].to(device)
            warm_optimizer.zero_grad(set_to_none=True)
            loss = belief_loss(model(x)[1], target)
            loss.backward()
            warm_optimizer.step()
        del warm_optimizer
        base.requires_grad_(True)
        optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
        for epoch in range(1, args.epochs + 1):
            started = time.perf_counter()
            optimizer.param_groups[0]['lr'] = args.learning_rate * (0.3 if epoch > args.epochs * 0.7 else 1)
            order = rng.permutation(len(data[0]))
            totals = np.zeros(3)
            for start in range(0, len(order), args.batch):
                selected = order[start:start + args.batch]
                x, y, mask = (tensor[selected].to(device) for tensor in data)
                target = owners[selected].to(device)
                optimizer.zero_grad(set_to_none=True)
                q, logits = model(x)
                q_loss = fit.action_loss(q, y, mask, args.card_value_weight)
                auxiliary = belief_loss(logits, target)
                (q_loss + args.belief_weight * auxiliary).backward()
                nn.utils.clip_grad_norm_(model.parameters(), 1)
                optimizer.step()
                totals += (float(q_loss.detach()) * len(selected), float(auxiliary.detach()) * len(selected), len(selected))
            metrics = fit.diagnostics(base, validation, valid_slots, args.batch, device, args.card_value_weight)
            belief = (belief_metrics(model, validation[0], validation_owners, args.batch, device)
                      if validation_owners is not None else None)
            print(name, 'epoch', epoch, json.dumps(dict(seconds=time.perf_counter() - started,
                  q_loss=totals[0] / totals[2], belief_loss=totals[1] / totals[2], validation=metrics,
                  belief_validation=belief)), flush=True)
            fit.write_network(base, output / f'epoch-{epoch:03}' / (name + '.bin'))
        fit.write_network(base, output / (name + '.bin'))
        del optimizer, model, base, data, owners, validation
        if device.type == 'cuda':
            torch.cuda.empty_cache()
    (output / 'sources.json').write_text(json.dumps(sources, indent=2), encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--in', dest='input', required=True)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--belief-weight', type=float, default=0.01)
    parser.add_argument('--epochs', type=int, default=4)
    parser.add_argument('--batch', type=int, default=1024)
    parser.add_argument('--learning-rate', type=float, default=1e-5)
    parser.add_argument('--card-value-weight', type=float, default=0.05)
    parser.add_argument('--seed', type=int, default=1401)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if args.belief_weight < 0 or args.epochs < 1 or args.batch < 1 or args.learning_rate <= 0:
        parser.error('Invalid weight, epoch, batch or learning rate')
    run(args)
