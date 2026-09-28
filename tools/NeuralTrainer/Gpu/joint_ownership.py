"""Exact likelihood for capacity- and public-mask-conditioned ownership weights.

For a world w, its unnormalized weight is exp(sum_card logits[card, w[card]]).
We condition on public hand sizes, known cards, and exclusions. The objective is
log Z - the true world's log weight, rather than independent card cross-entropy.
The gradient is the constrained owner marginal minus the true owner indicator.

The three residual hand sizes are counted from the labels. These counts are
public information; callers loading a dataset should independently check them
against the public feature planes. Labels do not determine any other constraint.
Own and played cards have owner -1 and do not participate in the distribution.

The DP matches the runtime weighted sampler before additional declaration
filtering. Public feature masks can also omit runtime deductions of plain-carre
ranks, because the stored feature layout lacks declaration multiplicities.

Runtime weighting may temper or mix probabilities. This loss models untempered
product weights; use matching runtime settings when evaluating that objective.
"""

import torch
from torch.nn import functional as functional


def _safe_logsumexp(candidates):
    """Avoid undefined gradients from logsumexp(-inf, -inf, -inf)."""
    reachable = torch.isfinite(candidates).any(dim=0)
    safe = torch.where(reachable.unsqueeze(0), candidates, 0.0)
    summed = torch.logsumexp(safe, dim=0)
    return summed.masked_fill(~reachable, -torch.inf)


def loss(logits, owners, allowed, reduction='mean', normalization='state'):
    """Return exact conditional negative log likelihood of each complete world.

    Args:
        logits: Floating tensor [B, 32, 3], in encoded-card / relative-owner order.
        owners: Integer tensor [B, 32], with -1 ignored and owners 0, 1 or 2.
        allowed: Boolean [B, 32, 3] public constraints. Known cards have one owner.
        reduction: 'none' returns [B]; 'mean' (default) or 'sum' reduces states.
        normalization: 'state' (default) scores a complete assignment. 'card'
            divides each state's loss by its own number of active cards before
            reduction; it is a mean of per-state averages, not a pooled average.

    Each capacity is at most eight. The DP tracks owners 0 and 1 in a 9x9 table;
    owner 2's count follows from the number of active cards processed. All 32
    positions are visited so batches may contain different ignored-card masks.
    Impossible DP cells have exactly zero probability and zero gradient.
    Float16/bfloat16 logits are accumulated in float32; float64 is preserved.
    """
    if (logits.ndim != 3 or logits.shape[1:] != (32, 3)
            or logits.shape[0] == 0 or not logits.is_floating_point()):
        raise ValueError('Expected nonempty floating logits [B, 32, 3]')
    if (owners.shape != logits.shape[:2] or owners.dtype not in (torch.int32, torch.int64)
            or allowed.shape != logits.shape or allowed.dtype != torch.bool):
        raise ValueError('Expected integer owners [B, 32] and boolean allowed [B, 32, 3]')
    if owners.device != logits.device or allowed.device != logits.device:
        raise ValueError('Logits, owners and constraints must share a device')
    if reduction not in ('none', 'mean', 'sum') or normalization not in ('state', 'card'):
        raise ValueError('Unknown reduction or normalization')
    if not torch.isfinite(logits).all() or ((owners < -1) | (owners > 2)).any():
        raise ValueError('Non-finite logits or invalid owner labels')

    owners = owners.to(dtype=torch.long)
    active = owners >= 0
    targets = owners.clamp_min(0)
    capacities = torch.stack([(owners == owner).sum(1) for owner in range(3)], dim=1)
    if (capacities > 8).any():
        raise ValueError('A residual hand has more than eight cards')
    if (~allowed.gather(2, targets.unsqueeze(2)).squeeze(2) & active).any():
        raise ValueError('A true owner violates the public constraints')

    values = logits if logits.dtype in (torch.float32, torch.float64) else logits.float()
    # A common per-card shift cancels from the conditional likelihood. Shifting
    # by the target logit makes the true path's weight one, so the result is log Z
    # directly. A unique feasible world then has exactly zero loss and gradient.
    relative = values - values.gather(2, targets.unsqueeze(2))
    relative = relative.masked_fill(~allowed, -torch.inf)
    batch = logits.shape[0]
    rows = torch.arange(9, device=logits.device).reshape(1, 9, 1)
    columns = torch.arange(9, device=logits.device).reshape(1, 1, 9)
    in_capacity = ((rows <= capacities[:, 0, None, None])
                   & (columns <= capacities[:, 1, None, None]))
    table = values.new_full((batch, 9, 9), -torch.inf)
    table[:, 0, 0] = 0
    processed = torch.zeros(batch, device=logits.device, dtype=torch.long)
    for card in range(32):
        processed = processed + active[:, card]
        third = processed[:, None, None] - rows - columns
        valid = in_capacity & (third >= 0) & (third <= capacities[:, 2, None, None])
        scores = relative[:, card]
        next_first = functional.pad(table[:, :-1, :], (0, 0, 1, 0), value=-torch.inf)
        next_second = functional.pad(table[:, :, :-1], (1, 0, 0, 0), value=-torch.inf)
        candidates = torch.stack((next_first + scores[:, 0, None, None],
                                  next_second + scores[:, 1, None, None],
                                  table + scores[:, 2, None, None]))
        updated = _safe_logsumexp(candidates).masked_fill(~valid, -torch.inf)
        table = torch.where(active[:, card, None, None], updated, table)

    result = table[torch.arange(batch, device=logits.device), capacities[:, 0], capacities[:, 1]]
    if normalization == 'card':
        result = result / active.sum(1).clamp_min(1)
    if reduction == 'sum':
        return result.sum()
    if reduction == 'mean':
        return result.mean()
    return result
