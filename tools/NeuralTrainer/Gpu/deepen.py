"""Add identity ReLU layers to a warm start, preserving its initial function."""

import argparse
from pathlib import Path
import shutil

import torch

import fit


def deepen(source, additional_layers):
    if additional_layers < 1 or len(source.layers) + additional_layers > 16:
        raise ValueError('Need positive added depth and at most 16 total layers')
    if len(source.layers) < 2:
        raise ValueError('An identity ReLU layer needs an existing hidden ReLU')
    width = source.sizes[-2]
    sizes = (*source.sizes[:-1], *((width,) * additional_layers), source.sizes[-1])
    result = fit.Network(source.tag, source.layout, sizes)
    with torch.no_grad():
        for old, new in zip(source.layers[:-1], result.layers):
            new.weight.copy_(old.weight)
            new.bias.copy_(old.bias)
        for layer in result.layers[len(source.layers) - 1:-1]:
            layer.weight.copy_(torch.eye(width))
            layer.bias.zero_()
        result.layers[-1].weight.copy_(source.layers[-1].weight)
        result.layers[-1].bias.copy_(source.layers[-1].bias)
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--in', dest='input', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--layers', type=int, default=2)
    args = parser.parse_args()
    fit.disable_power_throttling()
    torch.set_num_threads(2)
    output = Path(args.out)
    output.mkdir(parents=True, exist_ok=True)
    for tag, name in enumerate(fit.NAMES):
        source = Path(args.input) / (name + '.bin')
        if tag == 0:
            shutil.copyfile(source, output / source.name)
        else:
            model = deepen(fit.read_network(source, tag), args.layers)
            fit.write_network(model, output / source.name)
            print(name, model.sizes, flush=True)
