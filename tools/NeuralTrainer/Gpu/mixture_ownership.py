"""A mixture of separately normalized, capacity-conditioned ownership models."""

from pathlib import Path
import struct

import numpy as np
import torch

import fit
import joint_ownership

COMPONENTS = 4
OUTPUTS = COMPONENTS * 96 + COMPONENTS
SIZES = (600, 128, 64, OUTPUTS)
TAGS = (14, 15, 16)
FILE_BYTES = 36 + 2 * sum((a + 1) * b for a, b in zip(SIZES, SIZES[1:]))


def terms(logits, gates, owners, allowed):
    """Log mixture terms log pi_k - NLL_k, retaining each exact partition.

    logits[B,K,32,3] and gates[B,K] use only public model outputs. Capacities
    inside joint_ownership are checked against public features by the dataset
    loader. Normalization per card belongs AFTER mixing complete-world masses.
    """
    if (logits.ndim != 4 or logits.shape[0] == 0 or logits.shape[1] < 1
            or logits.shape[2:] != (32, 3) or gates.shape != logits.shape[:2]
            or not gates.is_floating_point() or gates.device != logits.device
            or not torch.isfinite(gates).all()):
        raise ValueError('Expected finite component logits [B,K,32,3] and gates [B,K]')
    batch, count = logits.shape[:2]
    if owners.shape != (batch, 32) or allowed.shape != (batch, 32, 3):
        raise ValueError('Ownership labels and masks do not match the mixture batch')
    component_nll = joint_ownership.loss(
        logits.reshape(batch * count, 32, 3),
        owners[:, None].expand(-1, count, -1).reshape(batch * count, 32),
        allowed[:, None].expand(-1, count, -1, -1).reshape(batch * count, 32, 3),
        reduction='none', normalization='state').reshape(batch, count)
    gates = gates if gates.dtype in (torch.float32, torch.float64) else gates.float()
    return torch.log_softmax(gates, 1) - component_nll


def loss(logits, gates, owners, allowed, reduction='mean', normalization='state'):
    if reduction not in ('none', 'mean', 'sum') or normalization not in ('state', 'card'):
        raise ValueError('Unknown mixture reduction or normalization')
    result = -torch.logsumexp(terms(logits, gates, owners, allowed), 1)
    if normalization == 'card':
        result = result / (owners >= 0).sum(1).clamp_min(1)
    return result if reduction == 'none' else result.mean() if reduction == 'mean' else result.sum()


def split(outputs):
    if outputs.ndim != 2 or outputs.shape[1] != OUTPUTS:
        raise ValueError('Expected 388 mixture outputs')
    return outputs[:, :COMPONENTS * 96].reshape(-1, COMPONENTS, 32, 3), outputs[:, COMPONENTS * 96:]


def warm_start(base, seed, perturbation=.05):
    """Clone a 600-input CE model, then break symmetry with card-owner biases.

    Perturbations are centered over components, cards and owners. Card-only and
    owner-only logit shifts cancel from a capacity-conditioned distribution and
    cannot split components. The remaining tensor is scaled to the requested
    population RMS. Gates start uniform; the actor is untouched.
    """
    if (base.tag not in (11, 12, 13) or base.layout != 1
            or base.sizes != (600, 128, 64, 96) or not np.isfinite(perturbation) or perturbation < 0):
        raise ValueError('Expected a checked layout-1 ownership model and nonnegative perturbation')
    model = fit.Network(base.tag + 3, 1, SIZES)
    with torch.no_grad():
        for original, destination in zip(base.layers[:-1], model.layers[:-1]):
            destination.weight.copy_(original.weight)
            destination.bias.copy_(original.bias)
        head = model.layers[-1]
        head.weight[:COMPONENTS * 96].copy_(base.layers[-1].weight.repeat(COMPONENTS, 1))
        head.bias[:COMPONENTS * 96].copy_(base.layers[-1].bias.repeat(COMPONENTS))
        head.weight[COMPONENTS * 96:].zero_()
        head.bias[COMPONENTS * 96:].zero_()
        if perturbation:
            generator = torch.Generator().manual_seed(seed)
            noise = torch.randn((COMPONENTS, 32, 3), generator=generator)
            for axis in (2, 1, 0):
                noise -= noise.mean(axis, keepdim=True)
            noise *= perturbation / noise.square().mean().sqrt()
            head.bias[:COMPONENTS * 96].add_(noise.flatten())
    return model


def read_network(path, expected_tag):
    """Strict BNN1 tag14/15/16, layout1, 600->128->64->388 half export."""
    raw = Path(path).read_bytes()
    expected = (fit.NETWORK_MAGIC, 1, expected_tag, 1, 3, *SIZES)
    if (expected_tag not in TAGS or len(raw) != FILE_BYTES
            or struct.unpack_from('<9i', raw) != expected):
        raise ValueError('Mixture ownership header, shape or length mismatch')
    parameters = np.frombuffer(raw, '<f2', offset=36).astype(np.float32)
    if not np.isfinite(parameters).all():
        raise ValueError('Non-finite mixture ownership parameters')
    model = fit.Network(expected_tag, 1, SIZES)
    offset = 0
    with torch.no_grad():
        for layer, a, b in zip(model.layers, SIZES, SIZES[1:]):
            layer.weight.copy_(torch.from_numpy(parameters[offset:offset + a * b].reshape(a, b).T.copy()))
            offset += a * b
            layer.bias.copy_(torch.from_numpy(parameters[offset:offset + b]))
            offset += b
    return model
