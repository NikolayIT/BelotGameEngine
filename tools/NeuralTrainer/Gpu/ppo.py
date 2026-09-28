"""On-policy PPO with a training-only critic and point-valued managed card actors."""

import argparse
from dataclasses import dataclass
import hashlib
import json
import math
from pathlib import Path
import platform
import shutil
import struct
import subprocess
import time

import numpy as np
import torch
from torch import nn

import fit

FLOAT_MAGIC = 0x31464E42
ROLLOUT_MAGIC = 0x31505042
RECORD = struct.Struct('<iBIBfffiQH')
FEATURE = np.dtype([('index', '<u2'), ('value', '<f4')])


@dataclass
class Rollout:
    x: torch.Tensor
    private: torch.Tensor
    mask: torch.Tensor
    action: torch.Tensor
    old_log: torch.Tensor
    outcome: torch.Tensor
    old_q: torch.Tensor
    following: np.ndarray
    deals: np.ndarray
    seats: np.ndarray


def read_rollout(path, tag):
    raw = Path(path).read_bytes()
    if len(raw) < 28:
        raise ValueError('Truncated PPO header')
    magic, version, layout, actual_tag, inputs, privileged, n = struct.unpack_from('<7i', raw)
    if (magic, version, layout, actual_tag, inputs, privileged) != (ROLLOUT_MAGIC, 1, 1, tag, 600, 96):
        raise ValueError('Unsupported PPO header')
    if not 0 <= n <= 10_000_000 or n * RECORD.size > len(raw) - 28:
        raise ValueError('Invalid PPO record count')
    x = np.zeros((n, 600), np.float32)
    private = np.zeros((n, 96), np.float32)
    bits = np.empty(n, np.uint32)
    action = np.empty(n, np.int64)
    old_log = np.empty(n, np.float32)
    outcome = np.empty(n, np.float32)
    old_q = np.empty(n, np.float32)
    following = np.empty(n, np.int64)
    deals = np.empty(n, np.int32)
    seats = np.empty(n, np.int8)
    offset = 28
    for i in range(n):
        if offset + RECORD.size > len(raw):
            raise ValueError('Truncated PPO record')
        deal, seat, mask, chosen, logp, result, q, nxt, owners, count = RECORD.unpack_from(raw, offset)
        offset += RECORD.size
        if (deal < 0 or seat > 3 or mask.bit_count() < 2 or chosen >= 32 or not mask & (1 << chosen)
                or not all(math.isfinite(v) for v in (logp, result, q)) or logp > 0.00001
                or not (nxt == -1 or i < nxt < n) or not 1 <= count <= 600
                or offset + count * FEATURE.itemsize > len(raw)):
            raise ValueError('Invalid PPO record')
        sparse = np.frombuffer(raw, FEATURE, count, offset)
        indices, values = sparse['index'], sparse['value']
        if (indices >= 600).any() or not np.isfinite(values).all() or np.unique(indices).size != count:
            raise ValueError('Invalid PPO features')
        x[i, indices] = values
        offset += count * FEATURE.itemsize
        for card in range(32):
            owner = (owners >> (2 * card)) & 3
            if owner:
                private[i, (owner - 1) * 32 + card] = 1
        deals[i], seats[i], bits[i], action[i] = deal, seat, mask, chosen
        old_log[i], outcome[i], old_q[i], following[i] = logp, result, q, nxt
    if offset != len(raw):
        raise ValueError('Trailing PPO bytes')
    linked = np.flatnonzero(following >= 0)
    nxt = following[linked]
    if (np.any(deals[linked] != deals[nxt]) or np.any(seats[linked] != seats[nxt])
            or np.any(outcome[linked] != outcome[nxt]) or np.unique(nxt).size != nxt.size):
        raise ValueError('PPO trajectory crosses a deal/seat or merges trajectories')
    mask = (bits[:, None] & (np.uint32(1) << np.arange(32, dtype=np.uint32))) != 0
    return Rollout(*(torch.from_numpy(a) for a in (x, private, mask, action, old_log, outcome, old_q)),
                   following, deals, seats)


def write_float_network(model, path):
    """No rounding between collection and the PPO probability-ratio calculation."""
    with Path(path).open('wb') as file:
        file.write(struct.pack('<5i', FLOAT_MAGIC, 1, model.tag, model.layout, len(model.layers)))
        file.write(struct.pack(f'<{len(model.sizes)}i', *model.sizes))
        for layer in model.layers:
            for tensor in (layer.weight.T, layer.bias):
                data = tensor.detach().cpu().numpy().astype('<f4')
                if not np.isfinite(data).all():
                    raise ValueError('Non-finite PPO snapshot')
                file.write(data.tobytes(order='C'))


def policy_log(q, mask, temperature):
    # q is in units of 26 game points, temperature is in game points.
    return torch.log_softmax((q * (fit.VALUE_SCALE / temperature)).masked_fill(~mask, -torch.inf), dim=-1)


def clipped_loss(new_log, old_log, advantage, clip):
    ratio = (new_log - old_log).exp()
    return -torch.minimum(ratio * advantage, ratio.clamp(1 - clip, 1 + clip) * advantage).mean()


def advantages(values, outcomes, following, lam=1.0):
    """Undiscounted deal reward, following the same seat's next non-forced decision.

    At lambda=1 this is exactly the observed terminal outcome minus the baseline.
    Neither a partner's nor an opponent's value is used as our next-state value.
    """
    values, outcomes = np.asarray(values), np.asarray(outcomes)
    result = np.zeros_like(values)
    if not 0 <= lam <= 1:
        raise ValueError('GAE lambda must be in [0,1]')
    for i in range(len(result) - 1, -1, -1):
        nxt = following[i]
        if nxt == -1:
            result[i] = outcomes[i] - values[i]
        elif i < nxt < len(result):
            result[i] = values[nxt] - values[i] + lam * result[nxt]
        else:
            raise ValueError('Invalid forward trajectory link')
    return result


class Critic(nn.Module):
    """Learns a residual over the actor's public expected Q; never exported to BNN1."""
    def __init__(self, widths=(256, 128)):
        super().__init__()
        if not 1 <= len(widths) <= 8 or any(not 1 <= width <= 4096 for width in widths):
            raise ValueError('Invalid helper widths')
        layers, previous = [], 696
        for width in widths:
            layers.extend((nn.Linear(previous, width), nn.ReLU()))
            previous = width
        layers.append(nn.Linear(previous, 1))
        self.net = nn.Sequential(*layers)
        nn.init.zeros_(self.net[-1].weight)
        nn.init.zeros_(self.net[-1].bias)

    def forward(self, public, private, base):
        return base + self.net(torch.cat((public, private), dim=1)).squeeze(-1)


def explained_variance(target, prediction):
    variance = target.var(unbiased=False)
    return float(1 - (target - prediction).var(unbiased=False) / variance) if float(variance) > 1e-12 else 0.0


def optimize(actor, critic, actor_optimizer, critic_optimizer, data, args, train_actor):
    device = next(actor.parameters()).device
    n = len(data.action)
    if n == 0:
        return {'samples': 0}
    x, private, mask, chosen, old_log, reward, stored_q = (
        tensor.to(device) for tensor in (data.x, data.private, data.mask, data.action, data.old_log, data.outcome, data.old_q))
    if args.critic == 'public':
        private = torch.zeros_like(private)
    with torch.no_grad():
        original = torch.cat([actor(batch) for batch in x.split(args.batch)])
        logs = policy_log(original, mask, args.temperature)
        probabilities = logs.exp()
        base = (probabilities * original).sum(1)
        baseline = torch.cat([critic(x[i:i + args.batch], private[i:i + args.batch], base[i:i + args.batch])
                              for i in range(0, n, args.batch)])
        log_error = float((logs.gather(1, chosen[:, None]).squeeze(1) - old_log).abs().max())
        q_error = float((original.gather(1, chosen[:, None]).squeeze(1) - stored_q).abs().max())
        if log_error > 0.001 or q_error > 0.0001:
            raise RuntimeError(f'C#/PyTorch actor mismatch: logp={log_error}, Q={q_error}')
        raw_adv = torch.from_numpy(advantages(baseline.cpu().numpy(), reward.cpu().numpy(), data.following, args.gae_lambda)).to(device)
        adv = (raw_adv - raw_adv.mean()) / raw_adv.std(unbiased=False).clamp_min(1e-8)
    # Baselines were predicted before fitting this batch: no action-dependent overfit in PPO advantages.
    stats = {'samples': n, 'log_parity_max': log_error, 'q_parity_max': q_error,
             'critic_ev_before': explained_variance(reward, baseline),
             'public_q_ev': explained_variance(reward, base),
             'advantage_std': float(raw_adv.std(unbiased=False)),
             'entropy_before': float(-(probabilities * logs.masked_fill(~mask, 0)).sum(1).mean())}
    for _ in range(args.critic_epochs):
        for indices in torch.randperm(n, device=device).split(args.batch):
            value = critic(x[indices], private[indices], base[indices])
            loss = 0.5 * (value - reward[indices]).square().mean()
            critic_optimizer.zero_grad(set_to_none=True)
            loss.backward()
            nn.utils.clip_grad_norm_(critic.parameters(), 1.0, error_if_nonfinite=True)
            critic_optimizer.step()
    batches = 0
    stopped = False
    if train_actor:
        for _ in range(args.epochs):
            for indices in torch.randperm(n, device=device).split(args.batch):
                q = actor(x[indices])
                all_log = policy_log(q, mask[indices], args.temperature)
                new_log = all_log.gather(1, chosen[indices, None]).squeeze(1)
                with torch.no_grad():
                    change = new_log - old_log[indices]
                    kl = float(((change.exp() - 1) - change).mean())
                if kl > args.target_kl:
                    stopped = True
                    break
                entropy = -(all_log.exp() * all_log.masked_fill(~mask[indices], 0)).sum(1).mean()
                point_loss = torch.nn.functional.huber_loss(q.gather(1, chosen[indices, None]).squeeze(1), reward[indices])
                loss = clipped_loss(new_log, old_log[indices], adv[indices], args.clip)
                loss = args.policy_weight * loss + args.q_weight * point_loss - args.entropy * entropy
                actor_optimizer.zero_grad(set_to_none=True)
                loss.backward()
                nn.utils.clip_grad_norm_(actor.parameters(), 0.5, error_if_nonfinite=True)
                actor_optimizer.step()
                batches += 1
            if stopped:
                break
    with torch.no_grad():
        current = torch.cat([actor(batch) for batch in x.split(args.batch)])
        updated = policy_log(current, mask, args.temperature)
        safe_delta = (logs - updated).masked_fill(~mask, 0)
        exact_kl = (probabilities * safe_delta).sum(1)
        old_best = original.masked_fill(~mask, -torch.inf).argmax(1)
        new_best = current.masked_fill(~mask, -torch.inf).argmax(1)
        value = torch.cat([critic(x[i:i + args.batch], private[i:i + args.batch], base[i:i + args.batch])
                           for i in range(0, n, args.batch)])
        stats.update(kl=float(exact_kl.mean()), actor_batches=batches, kl_stopped=stopped,
                     greedy_changed=float((old_best != new_best).float().mean()),
                     critic_ev_fitted=explained_variance(reward, value),
                     q_rmse_points=float((current.gather(1, chosen[:, None]).squeeze(1) - reward).square().mean().sqrt()) * fit.VALUE_SCALE)
    return stats


def invoke(command, log):
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, encoding='utf-8', errors='replace')
    Path(log).write_text(result.stdout, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(f'Command failed ({result.returncode}), see {log}: {result.stdout[-3000:]}')
    return result.stdout


def checkpoint(directory, actors, critics, actor_optimizers, critic_optimizers, iteration, args):
    directory.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(Path(args.input) / 'bid.bin', directory / 'bid.bin')
    for model, name in zip(actors, fit.NAMES[1:]):
        fit.write_network(model, directory / (name + '.bin'))
    torch.save({'iteration': iteration, 'args': vars(args), 'source_hashes': source_hashes(args.input),
                'opponent_assemblies': opponent_hashes(args.trainer) if getattr(args, 'opponent_chance', 0) > 0 else {},
                'actors': [m.state_dict() for m in actors], 'critics': [m.state_dict() for m in critics],
                'actor_optimizers': [o.state_dict() for o in actor_optimizers],
                'critic_optimizers': [o.state_dict() for o in critic_optimizers],
                'rng': torch.get_rng_state(),
                'cuda_rng': torch.cuda.get_rng_state_all() if torch.cuda.is_available() else []}, directory / 'training.pt')


def source_hashes(directory):
    return {name: hashlib.sha256((Path(directory) / (name + '.bin')).read_bytes()).hexdigest()
            for name in fit.NAMES}


def opponent_hashes(trainer):
    return {path.name: hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted(Path(trainer).parent.glob('Belot*.dll'))}


def restore(path, actors, critics, actor_optimizers, critic_optimizers, args):
    saved = torch.load(path, map_location=args.device, weights_only=True)
    for key in ('critic', 'temperature', 'gae_lambda', 'seed', 'warmup', 'deals', 'threads'):
        if saved['args'][key] != getattr(args, key):
            raise ValueError(f'Resume changes {key}')
    if saved['args'].get('critic_sizes', '256,128') != getattr(args, 'critic_sizes', '256,128'):
        raise ValueError('Resume changes helper architecture')
    for key, default in (('opponents', ''), ('opponent_chance', 0.0)):
        if saved['args'].get(key, default) != getattr(args, key, default):
            raise ValueError(f'Resume changes {key}')
    if Path(saved['args']['input']).resolve() != Path(args.input).resolve():
        raise ValueError('Resume changes the frozen input reference')
    if 'source_hashes' in saved and saved['source_hashes'] != source_hashes(args.input):
        raise ValueError('Resume source weights changed')
    if (saved.get('opponent_assemblies') is not None and getattr(args, 'opponent_chance', 0) > 0
            and saved['opponent_assemblies'] != opponent_hashes(args.trainer)):
        raise ValueError('Resume opponent assemblies changed')
    for objects, key in ((actors, 'actors'), (critics, 'critics'),
                         (actor_optimizers, 'actor_optimizers'), (critic_optimizers, 'critic_optimizers')):
        for target, state in zip(objects, saved[key]):
            target.load_state_dict(state)
    # Loading optimizer state otherwise silently restores the old command's rates.
    for optimizers, rate in ((actor_optimizers, args.actor_lr), (critic_optimizers, args.critic_lr)):
        for optimizer in optimizers:
            for group in optimizer.param_groups:
                group['lr'] = rate
    torch.set_rng_state(saved['rng'].cpu())
    if saved['cuda_rng'] and torch.cuda.is_available():
        torch.cuda.set_rng_state_all([state.cpu() for state in saved['cuda_rng']])
    return saved['iteration']


def run(args):
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.backends.cuda.matmul.allow_tf32 = False
    torch.backends.cudnn.allow_tf32 = False
    torch.use_deterministic_algorithms(True)
    if (args.updates < 0 or args.warmup < 0 or args.updates + args.warmup <= 0
            or args.deals <= 0 or args.threads <= 0 or args.batch <= 0 or args.save_every <= 0
            or args.temperature <= 0 or args.actor_lr <= 0 or args.critic_lr <= 0
            or args.epochs <= 0 or args.critic_epochs <= 0 or args.target_kl <= 0
            or not 0 < args.clip < 1 or not 0 <= args.gae_lambda <= 1
            or not 0 <= args.opponent_chance <= 1 or (args.opponent_chance > 0 and not args.opponents.strip())):
        raise ValueError('Invalid PPO settings')
    root = Path(args.out)
    root.mkdir(parents=True, exist_ok=True)
    if (root / 'progress.jsonl').exists() and not args.resume:
        raise ValueError('Output already contains a run; use a new directory or --resume')
    actors = [fit.read_network(Path(args.input) / (name + '.bin'), tag).to(args.device)
              for tag, name in enumerate(fit.NAMES[1:], 1)]
    critics = [Critic(tuple(map(int, args.critic_sizes.split(',')))).to(args.device) for _ in actors]
    actor_optimizers = [torch.optim.Adam(m.parameters(), lr=args.actor_lr, eps=1e-5) for m in actors]
    critic_optimizers = [torch.optim.Adam(m.parameters(), lr=args.critic_lr, eps=1e-5) for m in critics]
    start = 0
    if args.resume:
        start = restore(args.resume, actors, critics, actor_optimizers, critic_optimizers, args)
        if (root / 'progress.jsonl').exists():
            last = json.loads((root / 'progress.jsonl').read_text(encoding='utf-8').splitlines()[-1])
            if last['iteration'] != start:
                raise ValueError('Resume would duplicate or skip existing progress; use a new output directory')
        if start >= args.warmup + args.updates:
            raise ValueError('Resume needs a later requested final iteration')
    snapshot = root / 'actor-f32'
    snapshot.mkdir(exist_ok=True)
    shutil.copyfile(Path(args.input) / 'bid.bin', snapshot / 'bid.bin')
    clock = time.monotonic()
    (root / 'settings.json').write_text(json.dumps(vars(args), indent=2), encoding='utf-8')
    provenance = {'args': vars(args), 'python': platform.python_version(), 'torch': str(torch.__version__),
                  'cuda': torch.version.cuda,
                  'device': torch.cuda.get_device_name(args.device) if str(args.device).startswith('cuda') else 'cpu',
                  'source_hashes': source_hashes(args.input),
                  'trainer_sha256': hashlib.sha256(Path(args.trainer).read_bytes()).hexdigest(),
                  'opponent_assemblies': opponent_hashes(args.trainer),
                  'python_sources': {Path(path).name: hashlib.sha256(Path(path).read_bytes()).hexdigest()
                                     for path in (__file__, fit.__file__)}}
    (root / f'run-{start:04}.json').write_text(json.dumps(provenance, indent=2), encoding='utf-8')
    for step in range(start, args.warmup + args.updates):
        iteration_clock = time.monotonic()
        for actor, name in zip(actors, fit.NAMES[1:]):
            write_float_network(actor, snapshot / (name + '.f32'))
        prefix = root / 'batch'
        seed = (args.seed + step * 100003) % 2147483647
        collection = invoke(['dotnet', args.trainer, 'record-ppo', '--in', str(snapshot), '--ppo-float', 'true',
                             '--data', str(prefix), '--deals', str(args.deals), '--threads', str(args.threads),
                             '--temperature', str(args.temperature), '--seed', str(seed),
                             '--ppo-opponents', args.opponents, '--ppo-opponent-chance', str(args.opponent_chance)], root / 'collect.log')
        print(collection.strip(), flush=True)
        manifest = json.loads(Path(str(prefix) + '.ppo.json').read_text())
        hashes = [hashlib.sha256((snapshot / (name + ('.bin' if tag == 0 else '.f32'))).read_bytes()).hexdigest().upper()
                  for tag, name in enumerate(fit.NAMES)]
        if manifest['Hashes'] != hashes or manifest['Temperature'] != args.temperature:
            raise RuntimeError('Collector snapshot does not match PPO policy')
        if (manifest.get('PpoOpponents', '') != args.opponents
                or manifest.get('PpoOpponentChance', 0.0) != args.opponent_chance):
            raise RuntimeError('Collector opponents do not match PPO settings')
        stats = []
        for tag, name in enumerate(fit.NAMES[1:], 1):
            data = read_rollout(str(prefix) + '.' + name + '.ppo', tag)
            stats.append(optimize(actors[tag - 1], critics[tag - 1], actor_optimizers[tag - 1],
                                  critic_optimizers[tag - 1], data, args, step >= args.warmup))
        iteration = step + 1
        record = {'iteration': iteration, 'phase': 'warmup' if step < args.warmup else 'ppo',
                  'seed': seed, 'deals': args.deals, 'seconds': time.monotonic() - iteration_clock,
                  'elapsed': time.monotonic() - clock, 'networks': stats,
                  'opponent_deals': manifest.get('OpponentDeals', [])}
        with (root / 'progress.jsonl').open('a', encoding='utf-8') as file:
            file.write(json.dumps(record) + '\n')
        print(json.dumps(record), flush=True)
        if iteration % args.save_every == 0 or iteration == args.warmup + args.updates or iteration == args.warmup:
            directory = root / f'iteration-{iteration:04}'
            checkpoint(directory, actors, critics, actor_optimizers, critic_optimizers, iteration, args)
            if args.eval_pairs > 0 and step >= args.warmup:
                result = invoke(['dotnet', args.trainer, 'validate', '--in', str(directory),
                                 '--opponent', args.input, '--pairs', str(args.eval_pairs),
                                 '--threads', str(args.eval_threads), '--seed', str(args.eval_seed)], directory / 'baseline.log')
                print(result.strip().splitlines()[-1], flush=True)
    print('PPO run complete; exported actors require independent greedy/no-search evaluation.', flush=True)


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--in', dest='input', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--trainer', required=True, help='Copied Release NeuralTrainer.dll')
    parser.add_argument('--device', default='cuda')
    parser.add_argument('--critic', choices=('privileged', 'public'), default='privileged')
    parser.add_argument('--critic-sizes', default='256,128', help='Training-only hidden widths')
    parser.add_argument('--updates', type=int, default=16)
    parser.add_argument('--warmup', type=int, default=8)
    parser.add_argument('--deals', type=int, default=8192)
    parser.add_argument('--threads', type=int, default=12)
    parser.add_argument('--seed', type=int, default=4401)
    parser.add_argument('--temperature', type=float, default=1.0)
    parser.add_argument('--opponents', default='', help='Comma-separated named opposing teams, e.g. smart,sharpbelot,belot206')
    parser.add_argument('--opponent-chance', type=float, default=0.0, help='Fraction of deals using an external team; PPO records only current-policy seats')
    parser.add_argument('--epochs', type=int, default=3)
    parser.add_argument('--critic-epochs', type=int, default=3)
    parser.add_argument('--batch', type=int, default=2048)
    parser.add_argument('--actor-lr', type=float, default=1e-6)
    parser.add_argument('--critic-lr', type=float, default=3e-4)
    parser.add_argument('--clip', type=float, default=0.2)
    parser.add_argument('--entropy', type=float, default=0.001)
    parser.add_argument('--q-weight', type=float, default=1.0)
    parser.add_argument('--policy-weight', type=float, default=1.0)
    parser.add_argument('--target-kl', type=float, default=0.01)
    parser.add_argument('--gae-lambda', type=float, default=1.0)
    parser.add_argument('--save-every', type=int, default=4)
    parser.add_argument('--eval-pairs', type=int, default=2000)
    parser.add_argument('--eval-threads', type=int, default=16)
    parser.add_argument('--eval-seed', type=int, default=521)
    parser.add_argument('--resume', default='')
    return parser.parse_args()


if __name__ == '__main__':
    run(arguments())
