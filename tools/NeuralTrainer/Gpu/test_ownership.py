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
    def test_history_sample_and_owner_headers_are_checked(self):
        # Include the last history column and a fractional value, preserving the
        # branch's uint16 feature count and float16 sparse feature representation.
        raw = (struct.pack('<4iH', 0x32504E42, 2, 32, 1, 3)
               + struct.pack('<HeHeHeIee', 0, 1.0, 600, .125, 663, .75, 3, 1.0, -1.0))
        owners = struct.pack('<4iQ', 0x314F5042, 2, 32, 1, 1 | (2 << 2) | (3 << 62))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.samples'
            labels = Path(directory) / 'data.samples.owners'
            path.write_bytes(raw)
            labels.write_bytes(owners)
            features = fit_ownership.read_features(path, 2)
            self.assertEqual(features.shape, (1, 664))
            self.assertEqual(features[0, [0, 600, 663]].tolist(), [1.0, .125, .75])
            self.assertEqual(fit_ownership.read_owners(labels, 1, 2)[0, [0, 1, 2, 31]].tolist(), [0, 1, -1, 2])
            with self.assertRaises(ValueError):
                fit_ownership.read_features(path, 1)
            with self.assertRaises(ValueError):
                fit_ownership.read_owners(labels, 1, 1)
            bad_features = [raw[:-1], raw + b'x', raw[:4] + struct.pack('<i', 1) + raw[8:],
                            raw[:18] + struct.pack('<H', 664) + raw[20:],
                            raw[:20] + struct.pack('<H', 0x7e00) + raw[22:]]
            for bad in bad_features:
                path.write_bytes(bad)
                with self.assertRaises(ValueError):
                    fit_ownership.read_features(path, 2)
            for bad in (owners[:-1], owners + b'x', owners[:4] + struct.pack('<i', 1) + owners[8:]):
                labels.write_bytes(bad)
                with self.assertRaises(ValueError):
                    fit_ownership.read_owners(labels, 1, 2)

    def test_history_export_and_zero_input_control(self):
        torch.manual_seed(84)
        model = fit.Network(12, 2, fit_ownership.sizes_for_layout(2))
        with torch.no_grad():
            model.layers[0].weight[:, 600:].zero_()
        inputs = torch.rand(3, 664)
        zero_history = inputs.clone()
        zero_history[:, 600:] = 0
        torch.testing.assert_close(model(inputs), model(zero_history), rtol=0, atol=0)
        target = torch.randint(0, 3, (3, 32))
        optimizer = torch.optim.Adam(model.parameters(), lr=.001)
        optimizer.zero_grad()
        fit_ownership.belief_loss(model(zero_history).reshape(-1, 32, 3), target).backward()
        self.assertTrue(torch.all(model.layers[0].weight.grad[:, 600:] == 0))
        optimizer.step()
        self.assertTrue(torch.all(model.layers[0].weight[:, 600:] == 0))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'history.bin'
            fit.write_network(model, path)
            self.assertEqual(path.stat().st_size, 199268)
            restored = fit_ownership.read_network(path, 12, 2)
            torch.testing.assert_close(restored(inputs), restored(zero_history), rtol=0, atol=0)
            torch.testing.assert_close(model(inputs), restored(inputs), atol=.0003, rtol=.005)
            with self.assertRaises(ValueError):
                fit_ownership.read_network(path, 12, 1)

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
