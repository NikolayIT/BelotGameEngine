"""Fit C# search-teacher samples with PyTorch; export managed-player BNN1 weights."""

import argparse
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import time

os.environ.setdefault("CUBLAS_WORKSPACE_CONFIG", ":4096:8")

import numpy as np
import torch
from torch import nn

NAMES = ("bid", "trump", "notrumps", "alltrumps")
VALUE_SCALE = 26.0
NETWORK_MAGIC = 0x314E4E42
SAMPLE_MAGIC = 0x53504E42


def disable_power_throttling():
    """Same Windows opt-out as PowerThrottling.cs; affects this process only."""
    if os.name != "nt":
        return

    class State(ctypes.Structure):
        _fields_ = [("Version", ctypes.c_ulong), ("ControlMask", ctypes.c_ulong),
                    ("StateMask", ctypes.c_ulong)]

    api = ctypes.WinDLL("kernel32", use_last_error=True)
    api.GetCurrentProcess.restype = ctypes.c_void_p
    api.SetProcessInformation.argtypes = [ctypes.c_void_p, ctypes.c_int,
                                         ctypes.c_void_p, ctypes.c_ulong]
    state = State(1, 5, 0)
    if not api.SetProcessInformation(api.GetCurrentProcess(), 4,
                                    ctypes.byref(state), ctypes.sizeof(state)):
        print("Could not disable power throttling:", ctypes.get_last_error(), flush=True)


class Network(nn.Module):
    def __init__(self, tag, layout, sizes):
        super().__init__()
        self.tag, self.layout, self.sizes = tag, layout, tuple(sizes)
        self.layers = nn.ModuleList(nn.Linear(a, b) for a, b in zip(sizes, sizes[1:]))

    def forward(self, x):
        for layer in self.layers[:-1]:
            x = torch.relu(layer(x))
        return self.layers[-1](x)


class ResidualNetwork(nn.Module):
    """Train a small correction while preserving the pretrained branch exactly."""
    def __init__(self, base, hidden_sizes):
        super().__init__()
        if not hidden_sizes or len(hidden_sizes) != len(base.sizes) - 2 or any(size <= 0 for size in hidden_sizes):
            raise ValueError("Residual branch must have one positive width per existing hidden layer")
        self.base = base.requires_grad_(False)
        self.residual = Network(base.tag, base.layout, (base.sizes[0], *hidden_sizes, base.sizes[-1]))
        self.tag, self.layout = base.tag, base.layout
        self.sizes = (base.sizes[0], *(a + b for a, b in zip(base.sizes[1:-1], hidden_sizes)), base.sizes[-1])
        nn.init.zeros_(self.residual.layers[-1].weight)
        nn.init.zeros_(self.residual.layers[-1].bias)

    def forward(self, x):
        return self.base(x) + self.residual(x)

    @torch.no_grad()
    def merged(self):
        """Fold parallel branches into the runtime's ordinary block-diagonal MLP."""
        merged = Network(self.tag, self.layout, self.sizes)
        for index, (target, base, extra) in enumerate(zip(merged.layers, self.base.layers, self.residual.layers)):
            target.weight.zero_()
            target.bias.zero_()
            if index == 0:
                target.weight.copy_(torch.cat((base.weight, extra.weight)).cpu())
                target.bias.copy_(torch.cat((base.bias, extra.bias)).cpu())
            elif index == len(merged.layers) - 1:
                target.weight.copy_(torch.cat((base.weight, extra.weight), dim=1).cpu())
                target.bias.copy_((base.bias + extra.bias).cpu())
            else:
                outputs, inputs = base.weight.shape
                target.weight[:outputs, :inputs].copy_(base.weight.cpu())
                target.weight[outputs:, inputs:].copy_(extra.weight.cpu())
                target.bias.copy_(torch.cat((base.bias, extra.bias)).cpu())
        return merged


def read_network(path, expected_tag):
    data = Path(path).read_bytes()
    if len(data) < 20:
        raise ValueError("Truncated network header")
    magic, version, tag, layout, layers = struct.unpack_from("<5i", data)
    if (magic, version, tag, layout) != (NETWORK_MAGIC, 1, expected_tag, 1):
        raise ValueError("Unsupported network magic, version, tag or feature layout")
    if not 1 <= layers <= 16 or len(data) < 20 + 4 * (layers + 1):
        raise ValueError("Invalid network layer count")
    sizes = struct.unpack_from(f"<{layers + 1}i", data, 20)
    if any(not 1 <= size <= 16384 for size in sizes):
        raise ValueError("Invalid network width")
    expected = (97, 9) if tag == 0 else (600, 32)
    if (sizes[0], sizes[-1]) != expected:
        raise ValueError("Network dimensions do not match feature layout 1")
    offset = 20 + 4 * (layers + 1)
    expected_length = offset + 2 * sum((a + 1) * b for a, b in zip(sizes, sizes[1:]))
    if len(data) != expected_length:
        raise ValueError("Truncated network parameters or trailing bytes")
    model = Network(tag, layout, sizes)
    with torch.no_grad():
        for layer, a, b in zip(model.layers, sizes, sizes[1:]):
            parameters = np.frombuffer(data, dtype="<f2", count=(a + 1) * b, offset=offset).astype(np.float32)
            if not np.isfinite(parameters).all():
                raise ValueError("Non-finite network parameters")
            layer.weight.copy_(torch.from_numpy(parameters[:a * b].reshape(a, b).T.copy()))
            layer.bias.copy_(torch.from_numpy(parameters[a * b:]))
            offset += 2 * (a + 1) * b
    return model


def write_network(model, path):
    if isinstance(model, ResidualNetwork):
        model = model.merged()
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as file:
        file.write(struct.pack("<5i", NETWORK_MAGIC, 1, model.tag, model.layout, len(model.layers)))
        file.write(struct.pack(f"<{len(model.sizes)}i", *model.sizes))
        for layer in model.layers:
            for tensor in (layer.weight.T, layer.bias):
                values = tensor.detach().cpu().numpy()
                if not np.isfinite(values).all() or np.abs(values).max() > 65504:
                    raise ValueError("Network cannot be represented by finite 16-bit weights")
                file.write(values.astype("<f2").tobytes(order="C"))


def read_samples(path, inputs, outputs):
    data = Path(path).read_bytes()
    if len(data) < 12:
        raise ValueError("Truncated sample header")
    magic, width, count = struct.unpack_from("<3i", data)
    if magic != SAMPLE_MAGIC or width != outputs or not 0 <= count <= 10_000_000:
        raise ValueError("Invalid sample magic, output count or record count")
    if count * 7 > len(data) - 12:
        raise ValueError("Sample count exceeds available data")
    x = np.zeros((count, inputs), np.float32)
    y = np.zeros((count, outputs), np.float32)
    masks = np.zeros((count, outputs), bool)
    feature_dtype = np.dtype([("index", "<u2"), ("value", "<f2")])
    offset = 12
    for sample in range(count):
        if offset >= len(data):
            raise ValueError("Truncated sample record")
        features = data[offset]
        offset += 1
        if offset + 4 * features + 4 > len(data):
            raise ValueError("Truncated sample features")
        record = np.frombuffer(data, feature_dtype, count=features, offset=offset)
        indices, values = record["index"], record["value"]
        if (indices >= inputs).any() or not np.isfinite(values).all() or np.unique(indices).size != features:
            raise ValueError("Invalid sample features")
        x[sample, indices] = values
        offset += 4 * features
        mask, = struct.unpack_from("<I", data, offset)
        offset += 4
        if mask == 0 or mask >= 1 << outputs:
            raise ValueError("Invalid sample action mask")
        actions = [action for action in range(outputs) if mask & (1 << action)]
        if offset + 2 * len(actions) > len(data):
            raise ValueError("Truncated sample labels")
        targets = np.frombuffer(data, "<f2", count=len(actions), offset=offset)
        if not np.isfinite(targets).all():
            raise ValueError("Non-finite sample labels")
        y[sample, actions] = targets
        masks[sample, actions] = True
        offset += 2 * len(actions)
    if offset != len(data):
        raise ValueError("Trailing sample bytes")
    return tuple(torch.from_numpy(array) for array in (x, y, masks))


def action_loss(prediction, target, mask, value_weight=-1.0, huber=1.0):
    """ActionValueLoss.cs objective, averaged over labelled actions in the batch."""
    count = mask.sum(dim=1, keepdim=True).clamp_min(1)
    error = prediction - target
    mean = (error * mask).sum(dim=1, keepdim=True) / count

    def penalty(value):
        size = value.abs()
        return torch.where(size <= huber, 0.5 * value.square(), huber * (size - 0.5 * huber))

    if value_weight < 0:
        loss = (penalty(error) * mask).sum()
    else:
        loss = (penalty(error - mean) * mask).sum()
        loss = loss + value_weight * (count * penalty(mean)).sum()
    return loss / mask.sum().clamp_min(1)


def fitting_loss(prediction, target, mask, value_weight=-1.0, policy_temperature=0.0):
    """Optional policy distillation, retaining a separately calibrated mean Q value."""
    if policy_temperature <= 0:
        return action_loss(prediction, target, mask, value_weight)
    temperature = policy_temperature / VALUE_SCALE
    teacher_log = torch.log_softmax((target / temperature).masked_fill(~mask, -torch.inf), dim=1)
    student_log = torch.log_softmax((prediction / temperature).masked_fill(~mask, -torch.inf), dim=1)
    # Clear masked log probabilities before multiplying: 0 * (-inf - -inf) is NaN.
    divergence = (teacher_log.exp() * (teacher_log.masked_fill(~mask, 0)
                                      - student_log.masked_fill(~mask, 0))).sum(1)
    count = mask.sum(1)
    mean = ((prediction - target) * mask).sum(1) / count
    common_loss = torch.where(mean.abs() <= 1, 0.5 * mean.square(), mean.abs() - 0.5)
    return ((temperature ** 2 * divergence + value_weight * common_loss) * count).sum() / count.sum()


@torch.no_grad()
def diagnostics(model, data, slots, batch, device, value_weight, policy_temperature=0.0):
    totals = np.zeros(6, np.float64)
    for start in range(0, len(slots), batch):
        selection = slots[start:start + batch]
        x, y, mask = (tensor[selection].to(device) for tensor in data)
        prediction = model(x)
        error = prediction - y
        labels = mask.sum()
        mean = (error * mask).sum(1, keepdim=True) / mask.sum(1, keepdim=True)
        chosen = prediction.masked_fill(~mask, -torch.inf).argmax(1)
        regret = y.masked_fill(~mask, -torch.inf).max(1).values - y.gather(1, chosen[:, None]).squeeze(1)
        totals += np.array([
            float((error.square() * mask).sum()),
            float(((error - mean).square() * mask).sum()),
            float(regret.sum()), float((regret == 0).sum()), float(labels),
            float(fitting_loss(prediction, y, mask, value_weight, policy_temperature)) * float(labels),
        ])
    square, centred, regret, best, labels, loss = totals
    return dict(samples=len(slots), loss=loss / max(1, labels),
                rmse=VALUE_SCALE * (square / max(1, labels)) ** 0.5,
                centred_rmse=VALUE_SCALE * (centred / max(1, labels)) ** 0.5,
                teacher_regret=VALUE_SCALE * regret / max(1, len(slots)),
                optimal_choices=best / max(1, len(slots)))


def run(args):
    disable_power_throttling()
    torch.set_num_threads(2)
    torch.manual_seed(args.seed)
    torch.use_deterministic_algorithms(True)
    if args.device == "cuda" and not torch.cuda.is_available():
        raise RuntimeError("CUDA requested but unavailable; no silent CPU fallback")
    device = torch.device(args.device)
    print(json.dumps(dict(arguments=vars(args), torch=torch.__version__,
                          device=str(device), gpu=torch.cuda.get_device_name() if device.type == "cuda" else None)), flush=True)
    output = Path(args.out)
    output.mkdir(parents=True, exist_ok=True)
    sources = {}
    for tag, name in enumerate(NAMES):
        source = Path(args.input) / (name + ".bin")
        model = read_network(source, tag)
        if tag != 0 and args.residual_sizes:
            model = ResidualNetwork(model, [int(width) for width in args.residual_sizes.split(",")])
        model = model.to(device)
        sample_path = args.data + "." + name + ".samples"
        data = read_samples(sample_path, model.sizes[0], model.sizes[-1])
        sources[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
        sources[sample_path] = hashlib.sha256(Path(sample_path).read_bytes()).hexdigest()
        count = len(data[0])
        if count == 0:
            for destination in [output] + [output / f"epoch-{epoch:03}" for epoch in range(1, args.epochs + 1)]:
                destination.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, destination / (name + ".bin"))
            print(name, "no samples; keeping warm start unchanged", flush=True)
            continue
        rng = np.random.default_rng(args.seed + tag)
        shuffled = rng.permutation(count)
        if args.validation_data:
            validation_path = args.validation_data + "." + name + ".samples"
            validation_data = read_samples(validation_path, model.sizes[0], model.sizes[-1])
            validation = np.arange(len(validation_data[0]))
            training = shuffled
            sources[validation_path] = hashlib.sha256(Path(validation_path).read_bytes()).hexdigest()
        else:
            validation_data = data
            validation = shuffled[:max(1, count // 20)]
            training = shuffled[len(validation):]
        if not len(training) or not len(validation):
            raise ValueError(f"{name}: need non-empty training and validation sets")
        weight = -1 if tag == 0 else args.card_value_weight
        temperature = 0 if tag == 0 else args.policy_temperature
        print(name, "initial", json.dumps(diagnostics(model, validation_data, validation, args.batch, device, weight, temperature)), flush=True)
        optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
        # Only one contract's data is resident on the GPU at once.
        device_data = tuple(tensor.to(device) for tensor in data)
        for epoch in range(1, args.epochs + 1):
            rate = args.learning_rate * (0.3 if epoch > args.epochs * 0.7 else 1)
            optimizer.param_groups[0]["lr"] = rate
            order = torch.tensor(rng.permutation(training), device=device)
            if device.type == "cuda":
                torch.cuda.synchronize()
            started = time.perf_counter()
            train_loss, label_count = 0.0, 0
            for start in range(0, len(order), args.batch):
                selection = order[start:start + args.batch]
                x, y, mask = (tensor[selection] for tensor in device_data)
                optimizer.zero_grad(set_to_none=True)
                loss = fitting_loss(model(x), y, mask, weight, temperature)
                loss.backward()
                nn.utils.clip_grad_norm_(model.parameters(), 1.0)
                optimizer.step()
                labels = int(mask.sum())
                train_loss += float(loss.detach()) * labels
                label_count += labels
            if device.type == "cuda":
                torch.cuda.synchronize()
            elapsed = time.perf_counter() - started
            metrics = diagnostics(model, validation_data, validation, args.batch, device, weight, temperature)
            print(name, "epoch", epoch, json.dumps(dict(seconds=elapsed, rate=rate,
                  train_loss=train_loss / label_count, validation=metrics)), flush=True)
            write_network(model, output / f"epoch-{epoch:03}" / (name + ".bin"))
        write_network(model, output / (name + ".bin"))
        del device_data, optimizer, model, data, validation_data
        if device.type == "cuda":
            torch.cuda.empty_cache()
    (output / "sources.json").write_text(json.dumps(sources, indent=2), encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--in", dest="input", required=True)
    parser.add_argument("--data", required=True)
    parser.add_argument("--validation-data", default="")
    parser.add_argument("--out", required=True)
    parser.add_argument("--epochs", type=int, default=8)
    parser.add_argument("--batch", type=int, default=1024)
    parser.add_argument("--learning-rate", type=float, default=5e-5)
    parser.add_argument("--card-value-weight", type=float, default=-1)
    parser.add_argument("--policy-temperature", type=float, default=0,
                        help="Positive game-point temperature replaces advantage Huber with policy KL")
    parser.add_argument("--residual-sizes", default="",
                        help="Freeze the input network and learn an additive branch, e.g. 128,64,64")
    parser.add_argument("--device", choices=("cpu", "cuda"), default="cuda")
    parser.add_argument("--seed", type=int, default=401)
    arguments = parser.parse_args()
    if arguments.epochs < 1 or arguments.batch < 1 or arguments.learning_rate <= 0:
        parser.error("epochs, batch and learning-rate must be positive")
    if arguments.policy_temperature < 0 or (arguments.policy_temperature > 0 and arguments.card_value_weight < 0):
        parser.error("policy-temperature needs a nonnegative card-value-weight")
    run(arguments)
