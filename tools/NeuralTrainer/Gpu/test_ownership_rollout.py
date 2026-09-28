"""Real PPO binary ownership extraction and public/private separation tests."""

from dataclasses import fields
from pathlib import Path
import struct
import tempfile
import unittest

import torch

import ownership_rollout
import ppo


def position(completed=0, partial=False):
    x = torch.zeros(600)
    owners = torch.full((32,), -1, dtype=torch.long)
    for relative in range(4):
        first = relative * 8
        played = list(range(first, first + completed))
        remaining = list(range(first + completed, first + 8))
        for card in played:
            x[(2 + relative) * 32 + card] = 1
        if partial and relative in (2, 3):
            card = remaining.pop(0)
            x[(5 + relative) * 32 + card] = 1
        if relative == 0:
            x[remaining] = 1
            x[32 + torch.tensor(remaining)] = 1
        else:
            owners[remaining] = relative - 1
    x[512 + completed] = 1
    x[520 + (2 if partial else 0)] = 1
    return x, owners


def write_rollout(path, rows, tag=1, action_offset=0):
    raw = bytearray(struct.pack('<7i', ppo.ROLLOUT_MAGIC, 1, 1, tag, 600, 96, len(rows)))
    for index, (x, owners) in enumerate(rows):
        legal = torch.nonzero(x[32:64], as_tuple=True)[0].tolist()
        mask = sum(1 << card for card in legal)
        packed = sum((int(owner) + 1) << (2 * card) for card, owner in enumerate(owners) if owner >= 0)
        indices = torch.nonzero(x, as_tuple=True)[0].tolist()
        raw.extend(ppo.RECORD.pack(41, 2, mask, legal[action_offset % len(legal)], -0.7,
                                   0.5, 0.2 + action_offset, -1, packed, len(indices)))
        for feature in indices:
            raw.extend(struct.pack('<Hf', feature, float(x[feature])))
    Path(path).write_bytes(raw)


class OwnershipRolloutTests(unittest.TestCase):
    def test_owner_major_layout_and_ignored_cards(self):
        private = torch.zeros(2, 96)
        private[0, 31] = 1
        private[0, 32 + 7] = 1
        private[0, 64 + 0] = 1
        owners = ownership_rollout.decode_owners(private)
        self.assertEqual(owners.dtype, torch.long)
        self.assertEqual(owners.shape, (2, 32))
        self.assertEqual(owners[0, [31, 7, 0, 1]].tolist(), [0, 1, 2, -1])
        self.assertTrue(torch.all(owners[1] == -1))

    def test_real_files_keep_inputs_and_metadata_for_all_contract_tags(self):
        rows = [position(), position(2, partial=True)]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.ppo'
            for tag in (1, 2, 3):
                write_rollout(path, rows, tag)
                loaded = ownership_rollout.read_ownership(path, tag, validation_batch=1)
                torch.testing.assert_close(loaded.x, torch.stack([row[0] for row in rows]), rtol=0, atol=0)
                torch.testing.assert_close(loaded.owners, torch.stack([row[1] for row in rows]), rtol=0, atol=0)
                self.assertEqual(loaded.deals.tolist(), [41, 41])
                self.assertEqual(loaded.seats.tolist(), [2, 2])
                self.assertEqual([field.name for field in fields(loaded)], ['x', 'owners', 'deals', 'seats'])
                self.assertEqual(loaded.x.shape[1], 600)

    def test_hidden_assignment_and_action_targets_do_not_change_public_inputs(self):
        x, owners = position()
        swapped = owners.clone()
        swapped[8], swapped[16] = 1, 0
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.ppo'
            write_rollout(path, [(x, owners)])
            original = ownership_rollout.read_ownership(path, 1)
            write_rollout(path, [(x, swapped)], action_offset=1)
            changed = ownership_rollout.read_ownership(path, 1)
            torch.testing.assert_close(original.x, changed.x, rtol=0, atol=0)
            self.assertEqual(changed.owners[0, [8, 16]].tolist(), [1, 0])
            self.assertFalse(torch.equal(original.owners, changed.owners))
            self.assertFalse(hasattr(changed, 'private'))
            self.assertFalse(hasattr(changed, 'old_q'))
            self.assertFalse(hasattr(changed, 'outcome'))
            self.assertFalse(hasattr(changed, 'action'))

    def test_public_capacities_seen_cards_and_known_owners_are_checked(self):
        x, owners = position()
        bad_capacity = owners.clone()
        bad_capacity[8] = 1
        bad_seen = owners.clone()
        bad_seen[0], bad_seen[8] = 0, -1
        known = x.clone()
        known[13 * 32 + 8] = 1
        bad_known = owners.clone()
        bad_known[8], bad_known[16] = 1, 0
        excluded = x.clone()
        excluded[10 * 32 + 8] = 1
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.ppo'
            for features, labels in ((x, bad_capacity), (x, bad_seen), (known, bad_known), (excluded, owners)):
                write_rollout(path, [(features, labels)])
                with self.assertRaises(ValueError):
                    ownership_rollout.read_ownership(path, 1)

    def test_private_planes_reject_nonbinary_duplicate_or_invalid_shapes(self):
        base = torch.zeros(1, 96)
        for value in (.5, -1, float('nan'), float('inf')):
            bad = base.clone()
            bad[0, 0] = value
            with self.assertRaises(ValueError):
                ownership_rollout.decode_owners(bad)
        duplicate = base.clone()
        duplicate[0, 0] = duplicate[0, 32] = 1
        for bad in (duplicate, base[:, :95], base.reshape(1, 3, 32), base.long()):
            with self.assertRaises(ValueError):
                ownership_rollout.decode_owners(bad)

    def test_binary_reader_guards_and_empty_dataset_are_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.ppo'
            write_rollout(path, [position()])
            valid = path.read_bytes()
            for bad in (valid[:-1], valid + b'x', valid[:8] + struct.pack('<i', 2) + valid[12:]):
                path.write_bytes(bad)
                with self.assertRaises(ValueError):
                    ownership_rollout.read_ownership(path, 1)
            path.write_bytes(valid)
            for tag in (0, 2, 11):
                with self.assertRaises(ValueError):
                    ownership_rollout.read_ownership(path, tag)
            with self.assertRaises(ValueError):
                ownership_rollout.read_ownership(path, 1, validation_batch=0)
            write_rollout(path, [])
            loaded = ownership_rollout.read_ownership(path, 1)
            self.assertEqual(loaded.x.shape, (0, 600))
            self.assertEqual(loaded.owners.shape, (0, 32))


if __name__ == '__main__':
    torch.set_num_threads(1)
    unittest.main()
