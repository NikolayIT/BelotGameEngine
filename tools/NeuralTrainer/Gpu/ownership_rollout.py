"""Read PPO collection files as ownership supervision, without policy/Q targets.

The actor's 600 public inputs are kept unchanged. Privileged owner planes become
labels only: -1 for own/played cards, or 0/1/2 for next seat/partner/previous seat.
Cards already use the actor's suit rotation, so no further rotation is needed.
The source PPO tags are 1/2/3; ownership exports use their separate tags 11/12/13.
This module does not write .samples files or synthesize action-value targets.
"""

from dataclasses import dataclass

import numpy as np
import torch

import ppo


@dataclass(frozen=True)
class OwnershipRollout:
    x: torch.Tensor
    owners: torch.Tensor
    deals: np.ndarray
    seats: np.ndarray


def decode_owners(private):
    """Convert checked owner-major planes [N,96] to encoded-card labels [N,32]."""
    if (private.ndim != 2 or private.shape[1] != 96
            or not private.is_floating_point() or not torch.isfinite(private).all()
            or not ((private == 0) | (private == 1)).all()):
        raise ValueError('Expected binary finite private ownership planes [N,96]')
    planes = private.reshape(len(private), 3, 32).transpose(1, 2)
    count = planes.sum(2)
    if (count > 1).any():
        raise ValueError('A card has multiple private owners')
    owners = planes.argmax(2)
    return owners.masked_fill(count == 0, -1)


def read_ownership(path, tag, validation_batch=8192):
    """Read one card-contract PPO file, returning only public inputs and labels.

    Deal/seat identifiers are retained for provenance and grouped diagnostics;
    they are metadata and must not become model inputs. The binary PPO reader
    still checks its complete record format, including trajectory consistency.
    The existing ownership validator then independently checks that label counts
    match public remaining hands, ignored cards match own/played cards, and every
    true owner satisfies public exclusions and known-card constraints.
    """
    if tag not in (1, 2, 3):
        raise ValueError('PPO ownership input requires a card actor tag 1, 2 or 3')
    if validation_batch < 1:
        raise ValueError('Ownership validation batch must be positive')
    rollout = ppo.read_rollout(path, tag)
    owners = decode_owners(rollout.private)
    # A local import lets the existing fine-tune runner adopt this reader later
    # without a module-initialization cycle; no active training code is changed.
    from finetune_ownership import validate_public_labels
    validate_public_labels(rollout.x, owners, validation_batch)
    return OwnershipRollout(rollout.x, owners, rollout.deals.copy(), rollout.seats.copy())
