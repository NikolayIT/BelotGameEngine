"""Ownership export, public constraints, metrics and independent-label checks."""

from pathlib import Path
import math
import struct
import tempfile
import unittest

import torch

import fit
import fit_ownership


class OwnershipTests(unittest.TestCase):
    def test_checked_export_roundtrip(self):
        torch.manual_seed(81)
        model = fit.Network(11, 1, fit_ownership.SIZES)
        x = torch.randn(4, 600)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'trump.bin'
            fit.write_network(model, path)
            raw = path.read_bytes()
            self.assertEqual(len(raw), 182884)
            restored = fit_ownership.read_network(path, 11)
            torch.testing.assert_close(model(x), restored(x), atol=.0003, rtol=.005)
            corruptions = [raw[:-1], raw + b'x']
            for offset in range(0, 36, 4):
                corruptions.append(raw[:offset] + struct.pack('<i', 999) + raw[offset + 4:])
            corruptions.append(raw[:36] + struct.pack('<H', 0x7e00) + raw[38:])
            for corrupt in corruptions:
                path.write_bytes(corrupt)
                with self.assertRaises(ValueError):
                    fit_ownership.read_network(path, 11)

    def test_local_constraint_baseline_and_masked_metrics(self):
        model = fit.Network(11, 1, fit_ownership.SIZES)
        with torch.no_grad():
            for parameter in model.parameters():
                parameter.zero_()
        features = torch.zeros(1, 600)
        owners = torch.full((1, 32), -1, dtype=torch.long)
        owners[0, :3] = torch.tensor([0, 1, 2])
        features[0, 10 * 32 + 1] = 1  # Card 1 cannot be at next seat.
        features[0, 15 * 32 + 2] = 1  # Card 2 is known at previous seat.
        allowed = fit_ownership.allowed_owners(features)
        self.assertEqual(allowed[0, :3].tolist(), [[True, True, True], [False, True, True], [False, False, True]])
        measured = fit_ownership.metrics(model, features, owners, 1, 'cpu')
        self.assertEqual(measured['cards'], 3)
        self.assertAlmostEqual(measured['raw_nll'], math.log(3), places=6)
        self.assertAlmostEqual(measured['masked_nll'], (math.log(3) + math.log(2)) / 3, places=6)
        self.assertAlmostEqual(measured['uniform_legal_nll'], measured['masked_nll'], places=6)
        self.assertAlmostEqual(measured['uniform_legal_accuracy'], (1 / 3 + 1 / 2 + 1) / 3, places=6)
        owners[0, 1] = 0
        with self.assertRaises(ValueError):
            fit_ownership.metrics(model, features, owners, 1, 'cpu')

    def test_gradients_use_only_owner_targets_not_q_labels(self):
        logits = torch.randn(1, 32, 3, dtype=torch.double, requires_grad=True)
        owners = torch.full((1, 32), -1, dtype=torch.long)
        owners[0, :2] = torch.tensor([0, 2])
        self.assertTrue(torch.autograd.gradcheck(lambda value: fit_ownership.belief_loss(value, owners), (logits,)))
        gradient, = torch.autograd.grad(fit_ownership.belief_loss(logits, owners), logits)
        self.assertTrue(torch.all(gradient[:, 2:] == 0))


if __name__ == '__main__':
    torch.set_num_threads(1)
    unittest.main()
