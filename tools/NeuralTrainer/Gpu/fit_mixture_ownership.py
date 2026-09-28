"""Offline K4 likelihood pilot matched to an existing single-component control."""

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
import finetune_ownership
import mixture_ownership


@torch.no_grad()
def measure(model, features, owners, batch, device):
    """Actual exported joint likelihood and gate usage, overall and last five tricks."""
    count = 1 if model.sizes[-1] == 96 else mixture_ownership.COMPONENTS
    totals = {name: dict(states=0, cards=0, nll=0.0, gate_entropy=0.0,
                        responsibility_entropy=0.0, effective_gate=0.0,
                        effective_responsibility=0.0, gates=np.zeros(count),
                        responsibilities=np.zeros(count), component_nll=np.zeros(count))
              for name in ('overall', 'late_three_completed')}
    for start in range(0, len(features), batch):
        x = features[start:start + batch].to(device)
        target = owners[start:start + batch].to(device)
        output = model(x)
        if count == 1:
            logits, gates = output.reshape(-1, 1, 32, 3), output.new_zeros((len(x), 1))
        else:
            logits, gates = mixture_ownership.split(output)
        terms = mixture_ownership.terms(logits, gates, target, fit_ownership.allowed_owners(x))
        nll = -torch.logsumexp(terms, 1)
        log_gate, log_responsibility = torch.log_softmax(gates, 1), torch.log_softmax(terms, 1)
        gate, responsibility = log_gate.exp(), log_responsibility.exp()
        gate_entropy = -(gate * log_gate).sum(1)
        responsibility_entropy = -(responsibility * log_responsibility).sum(1)
        component_nll = log_gate - terms
        late = x[:, 512:520].argmax(1) >= 3
        for name, selected in (('overall', torch.ones(len(x), dtype=torch.bool, device=device)),
                               ('late_three_completed', late)):
            total = totals[name]
            total['states'] += int(selected.sum())
            total['cards'] += int((target[selected] >= 0).sum())
            total['nll'] += float(nll[selected].sum())
            total['gate_entropy'] += float(gate_entropy[selected].sum())
            total['responsibility_entropy'] += float(responsibility_entropy[selected].sum())
            total['effective_gate'] += float(gate_entropy[selected].exp().sum())
            total['effective_responsibility'] += float(responsibility_entropy[selected].exp().sum())
            total['gates'] += gate[selected].sum(0).cpu().numpy()
            total['responsibilities'] += responsibility[selected].sum(0).cpu().numpy()
            total['component_nll'] += component_nll[selected].sum(0).cpu().numpy()
    report = {}
    for name, total in totals.items():
        states = max(1, total['states'])
        report[name] = dict(states=total['states'], cards=total['cards'],
                            joint_nll=total['nll'] / states,
                            joint_nll_per_card=total['nll'] / max(1, total['cards']),
                            gate_entropy=total['gate_entropy'] / states,
                            responsibility_entropy=total['responsibility_entropy'] / states,
                            effective_gate_components=total['effective_gate'] / states,
                            effective_responsibility_components=total['effective_responsibility'] / states,
                            mean_gates=(total['gates'] / states).tolist(),
                            mean_responsibilities=(total['responsibilities'] / states).tolist(),
                            component_joint_nll=(total['component_nll'] / states).tolist())
    return report


def train_epoch(model, optimizer, features, owners, allowed, order, batch, device):
    order = torch.as_tensor(order, device=features.device, dtype=torch.long)
    total = 0.0
    for start in range(0, len(order), batch):
        selected = order[start:start + batch]
        x = features.index_select(0, selected).to(device)
        target = owners.index_select(0, selected).to(device)
        constraints = allowed.index_select(0, selected).to(device)
        optimizer.zero_grad(set_to_none=True)
        logits, gates = mixture_ownership.split(model(x))
        value = mixture_ownership.loss(logits, gates, target, constraints, normalization='card')
        value.backward()
        nn.utils.clip_grad_norm_(model.parameters(), 1.0, error_if_nonfinite=True)
        optimizer.step()
        total += float(value.detach()) * len(selected)
    return total / max(1, len(order))


def check_control(args, control):
    for name in ('input', 'data', 'validation_data', 'epochs', 'batch', 'learning_rate', 'seed'):
        if control['arguments'][name] != getattr(args, name):
            raise ValueError('Mixture/control setting differs: ' + name)
    if control['arguments']['layout'] != 1 or control['arguments']['normalization'] != 'card':
        raise ValueError('The matched control must use layout1 and per-state card normalization')


def run(args):
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.use_deterministic_algorithms(True)
    device = torch.device(args.device)
    if device.type == 'cuda' and not torch.cuda.is_available():
        raise RuntimeError('CUDA is unavailable')
    control_path = Path(args.control_report)
    control = json.loads(control_path.read_text(encoding='utf-8'))
    check_control(args, control)
    output = Path(args.out)
    destinations = [output, output / 'initial'] + [output / f'epoch-{epoch:03}' for epoch in range(1, args.epochs + 1)]
    protected = [Path(args.input).resolve(), (control_path.parent / 'joint').resolve()]
    if any(path.resolve() in protected for path in destinations):
        raise ValueError('Output would overwrite the warm start or control')
    output.mkdir(parents=True, exist_ok=True)
    report = dict(arguments=vars(args), torch=torch.__version__, sizes=mixture_ownership.SIZES,
                  components=mixture_ownership.COMPONENTS, file_bytes=mixture_ownership.FILE_BYTES,
                  control_report_sha256=hashlib.sha256(control_path.read_bytes()).hexdigest(),
                  sources={}, networks={})
    print(json.dumps(report), flush=True)
    for index, name in enumerate(fit_ownership.NAMES):
        base_tag, tag = 11 + index, 14 + index
        source = Path(args.input) / (name + '.bin')
        training_path = Path(args.data + '.' + name + '.samples')
        validation_path = Path(args.validation_data + '.' + name + '.samples')
        if training_path.resolve() == validation_path.resolve():
            raise ValueError('Training and validation must use separate files')
        training = fit_ownership.read_features(training_path, 1)
        validation = fit_ownership.read_features(validation_path, 1)
        owners = fit_ownership.read_owners(str(training_path) + '.owners', len(training))
        validation_owners = fit_ownership.read_owners(str(validation_path) + '.owners', len(validation))
        if not len(training) or not len(validation):
            raise ValueError('Both datasets must contain samples')
        finetune_ownership.validate_public_labels(training, owners)
        finetune_ownership.validate_public_labels(validation, validation_owners)
        for path in (source, training_path, Path(str(training_path) + '.owners'),
                     validation_path, Path(str(validation_path) + '.owners')):
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            if control['sources'].get(str(path)) != digest:
                raise ValueError('Mixture/control source differs: ' + str(path))
            report['sources'][str(path)] = digest
        allowed = fit_ownership.allowed_owners(training)
        if args.cache_device:
            tensors = (training, validation, owners, validation_owners, allowed)
            required = sum(tensor.numel() * tensor.element_size() for tensor in tensors)
            if device.type == 'cuda':
                free, _ = torch.cuda.mem_get_info(device)
                if free < required + args.cache_reserve_mib * 1024 * 1024:
                    raise RuntimeError('Insufficient free GPU memory for dataset and mixture reserve')
            training, validation, owners, validation_owners, allowed = (tensor.to(device) for tensor in tensors)
            print(name, 'cached_bytes', required, flush=True)

        base = fit_ownership.read_network(source, base_tag, 1)
        initial_seed = args.seed + 10000 + base_tag
        model = mixture_ownership.warm_start(base, initial_seed, args.perturbation).to(device)
        fit.write_network(model, output / 'initial' / (name + '.bin'))
        initial_export = mixture_ownership.read_network(output / 'initial' / (name + '.bin'), tag).to(device)
        initial = measure(initial_export, validation, validation_owners, args.batch, device)
        reference_path = control_path.parent / 'joint' / (name + '.bin')
        reference = fit_ownership.read_network(reference_path, base_tag, 1).to(device)
        reference_digest = hashlib.sha256(reference_path.read_bytes()).hexdigest()
        expected_digest = control['networks'][name]['joint']['epochs'][-1]['export_sha256']
        if reference_digest != expected_digest:
            raise ValueError('Final control weights disagree with their recorded hash')
        reference_metrics = measure(reference, validation, validation_owners, args.batch, device)
        del initial_export, reference, base
        epochs = []
        report['networks'][name] = dict(initial_seed=initial_seed, initial=initial,
                                        reference_sha256=reference_digest,
                                        reference_validation=reference_metrics, epochs=epochs)
        print(name, 'initial', json.dumps(initial), flush=True)
        print(name, 'control', json.dumps(reference_metrics), flush=True)
        optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
        rng = np.random.default_rng(args.seed + base_tag)
        for epoch in range(1, args.epochs + 1):
            started = time.perf_counter()
            rate = args.learning_rate * (0.3 if epoch > args.epochs * .7 else 1)
            optimizer.param_groups[0]['lr'] = rate
            order = rng.permutation(len(training))
            order_digest = hashlib.sha256(order.tobytes()).hexdigest()
            control_epoch = control['networks'][name]['joint']['epochs'][epoch - 1]
            if order_digest != control_epoch['row_order_sha256'] or rate != control_epoch['learning_rate']:
                raise ValueError('Mixture/control row order or learning rate differs')
            training_loss = train_epoch(model, optimizer, training, owners, allowed, order, args.batch, device)
            path = output / f'epoch-{epoch:03}' / (name + '.bin')
            fit.write_network(model, path)
            restored = mixture_ownership.read_network(path, tag).to(device)
            measured = measure(restored, validation, validation_owners, args.batch, device)
            entry = dict(epoch=epoch, seconds=time.perf_counter() - started,
                         learning_rate=rate, training_loss=training_loss,
                         row_order_sha256=order_digest, validation=measured,
                         export_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
            epochs.append(entry)
            (output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
            print(name, json.dumps(entry), flush=True)
            del restored
        fit.write_network(model, output / (name + '.bin'))
        del model, optimizer, training, validation, owners, validation_owners, allowed
        if device.type == 'cuda':
            torch.cuda.empty_cache()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True)
    parser.add_argument('--data', required=True)
    parser.add_argument('--validation-data', required=True)
    parser.add_argument('--control-report', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--epochs', type=int, default=4)
    parser.add_argument('--batch', type=int, default=2048)
    parser.add_argument('--learning-rate', type=float, default=.0001)
    parser.add_argument('--seed', type=int, default=9833)
    parser.add_argument('--perturbation', type=float, default=.05)
    parser.add_argument('--cache-device', action='store_true')
    parser.add_argument('--cache-reserve-mib', type=int, default=1536)
    parser.add_argument('--device', choices=('cpu', 'cuda'), default='cuda')
    args = parser.parse_args()
    if (args.epochs < 1 or args.batch < 1 or not 0 < args.learning_rate <= 1
            or not np.isfinite(args.perturbation) or args.perturbation < 0 or args.cache_reserve_mib < 768):
        parser.error('Invalid epochs, batch, learning rate, perturbation or memory reserve')
    run(args)
