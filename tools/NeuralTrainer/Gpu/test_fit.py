"""Format, loss and decision-metric checks; run with unittest on CPU."""

from argparse import Namespace
import contextlib
import io
from pathlib import Path
import struct
import tempfile
import unittest

import numpy as np
import torch

import fit


class FitTests(unittest.TestCase):
    def test_partial_batch_and_empty_contract_export(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / "source", root / "output"
            source.mkdir()
            for tag, name in enumerate(fit.NAMES):
                inputs, outputs = (97, 9) if tag == 0 else (600, 32)
                fit.write_network(fit.Network(tag, 1, (inputs, 8, outputs)), source / (name + ".bin"))
                header = struct.pack("<3i", fit.SAMPLE_MAGIC, outputs, 2 if tag == 1 else 0)
                # Two samples: one held out, one in the final partial training batch.
                record = struct.pack("<BHeIee", 1, 0, 1.0, 3, 0.25, -0.25)
                (root / ("data." + name + ".samples")).write_bytes(header + (record * 2 if tag == 1 else b""))
            args = Namespace(input=str(source), data=str(root / "data"), validation_data="",
                             out=str(output), epochs=2, batch=1024, learning_rate=0.001,
                             card_value_weight=0.05, policy_temperature=0, residual_sizes="",
                             suit_augmentation=False, anchor_mean=False, device="cpu", seed=7)
            with contextlib.redirect_stdout(io.StringIO()):
                fit.run(args)
            for name in ("bid", "notrumps", "alltrumps"):
                original = (source / (name + ".bin")).read_bytes()
                for path in (output, output / "epoch-001", output / "epoch-002"):
                    self.assertEqual(original, (path / (name + ".bin")).read_bytes())
            self.assertNotEqual((source / "trump.bin").read_bytes(), (output / "trump.bin").read_bytes())
            self.assertTrue((output / "sources.json").is_file())
            fit.read_network(output / "trump.bin", 1)

    def test_network_round_trip_preserves_half_weights_and_input_major_order(self):
        torch.manual_seed(2)
        model = fit.Network(1, 1, (600, 8, 32))
        with tempfile.TemporaryDirectory() as directory:
            first, second = (Path(directory) / name for name in ("a.bin", "b.bin"))
            fit.write_network(model, first)
            restored = fit.read_network(first, 1)
            fit.write_network(restored, second)
            self.assertEqual(first.read_bytes(), second.read_bytes())
            raw = first.read_bytes()
            offset = 20 + 4 * 3
            weights = np.frombuffer(raw, "<f2", 600 * 8, offset).astype(np.float32).reshape(600, 8)
            x = np.random.default_rng(3).normal(size=(4, 600)).astype(np.float32)
            expected = x @ weights + restored.layers[0].bias.detach().numpy()
            expected = np.maximum(expected, 0) @ restored.layers[1].weight.detach().numpy().T
            expected += restored.layers[1].bias.detach().numpy()
            actual = restored(torch.from_numpy(x)).detach().numpy()
            np.testing.assert_allclose(expected, actual, rtol=1e-5, atol=1e-5)

            for corrupted in (raw[:10], raw[:-1], raw + b"x",
                              raw[:4] + struct.pack("<i", 99) + raw[8:],
                              raw[:12] + struct.pack("<i", 99) + raw[16:]):
                second.write_bytes(corrupted)
                with self.assertRaises(ValueError):
                    fit.read_network(second, 1)
            with self.assertRaises(ValueError):
                fit.read_network(first, 2)

    def test_frozen_residual_branch_merges_into_existing_format(self):
        torch.manual_seed(2)
        base = fit.Network(1, 1, (600, 16, 8, 4, 32))
        original = [parameter.detach().clone() for parameter in base.parameters()]
        model = fit.ResidualNetwork(base, (8, 4, 4))
        x = torch.randn(6, 600)
        torch.testing.assert_close(model(x), base(x), rtol=0, atol=0)
        optimizer = torch.optim.Adam(model.parameters(), lr=0.01)
        for _ in range(3):
            optimizer.zero_grad()
            model(x).square().mean().backward()
            optimizer.step()
        for before, after in zip(original, base.parameters()):
            torch.testing.assert_close(before, after, rtol=0, atol=0)
        self.assertFalse(torch.equal(model(x), base(x)))
        torch.testing.assert_close(model(x), model.merged()(x), rtol=1e-5, atol=1e-6)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "merged.bin"
            fit.write_network(model, path)
            restored = fit.read_network(path, 1)
            self.assertEqual(restored.sizes, (600, 24, 12, 8, 32))
            torch.testing.assert_close(model(x), restored(x), rtol=0.01, atol=0.001)
        with self.assertRaises(ValueError):
            fit.ResidualNetwork(base, (8, 4))

    def test_non_finite_or_overflowed_export_is_refused(self):
        model = fit.Network(1, 1, (600, 32))
        with tempfile.TemporaryDirectory() as directory:
            for bad in (float("nan"), float("inf"), 1e6):
                with torch.no_grad():
                    model.layers[0].bias[0] = bad
                with self.assertRaises(ValueError):
                    fit.write_network(model, Path(directory) / "bad.bin")

    def test_sparse_sample_layout_and_validation(self):
        raw = (struct.pack("<iiiB", fit.SAMPLE_MAGIC, 32, 1, 2)
               + struct.pack("<HeHeIee", 3, 1.0, 599, -0.5, (1 << 1) | (1 << 31), 0.25, -0.25))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "samples"
            path.write_bytes(raw)
            x, y, mask = fit.read_samples(path, 600, 32)
            self.assertEqual(x.shape, (1, 600))
            self.assertEqual(float(x[0, 3]), 1)
            self.assertEqual(float(x[0, 599]), -0.5)
            self.assertEqual(float(y[0, 31]), -0.25)
            self.assertEqual(int(mask.sum()), 2)
            self.assertTrue(bool(mask[0, 1]))
            for corrupted in (raw[:10], raw[:-1], raw + b"x",
                              struct.pack("<i", 0) + raw[4:]):
                path.write_bytes(corrupted)
                with self.assertRaises(ValueError):
                    fit.read_samples(path, 600, 32)
            path.write_bytes(raw)
            with self.assertRaises(ValueError):
                fit.read_samples(path, 599, 32)
            with self.assertRaises(ValueError):
                fit.read_samples(path, 600, 9)

    def test_loss_gradients_against_finite_differences(self):
        prediction = torch.tensor([[0.4, 1.5, -0.7, 10], [-2, 0.2, 0.8, 4]],
                                  dtype=torch.float64, requires_grad=True)
        target = torch.tensor([[0.1, -0.3, 0.2, 4], [0.2, 0, 0.1, 2]], dtype=torch.float64)
        mask = torch.tensor([[True, True, True, False], [True, False, True, True]])
        for weight in (-1, 0, 0.05, 1):
            self.assertTrue(torch.autograd.gradcheck(
                lambda x: fit.action_loss(x, target, mask, weight, 0.7), (prediction,)))
            gradient, = torch.autograd.grad(fit.action_loss(prediction, target, mask, weight, 0.7), prediction)
            self.assertTrue(torch.all(gradient[~mask] == 0))

    def test_common_offset_has_no_action_gradient(self):
        prediction = torch.tensor([[4.0, 5.0, 9.0]], requires_grad=True)
        target = torch.tensor([[1.0, 2.0, -100.0]])
        mask = torch.tensor([[True, True, False]])
        loss = fit.action_loss(prediction, target, mask, 0)
        loss.backward()
        self.assertEqual(float(loss.detach()), 0)
        self.assertTrue(torch.all(prediction.grad == 0))

    def test_policy_distillation_gradients_masks_and_common_offset(self):
        prediction = torch.tensor([[0.2, -0.1, 99.0], [0.3, 10.0, 0.1]], dtype=torch.double, requires_grad=True)
        target = torch.tensor([[0.1, 0.2, -10.0], [0.4, -99.0, 0.2]], dtype=torch.double)
        mask = torch.tensor([[True, True, False], [True, False, True]])
        for weight in (0, 0.05):
            self.assertTrue(torch.autograd.gradcheck(
                lambda x: fit.fitting_loss(x, target, mask, weight, 2), (prediction,)))
        loss = fit.fitting_loss(prediction, target, mask, 0, 2)
        gradient, = torch.autograd.grad(loss, prediction)
        self.assertTrue(torch.all(gradient[~mask] == 0))
        self.assertAlmostEqual(float(loss.detach()),
                               float(fit.fitting_loss(prediction + 100, target - 30, mask, 0, 2).detach()), places=10)
        equal = fit.fitting_loss(target + 1, target, mask, 0, 2)
        self.assertAlmostEqual(float(equal), 0, places=10)

    def test_suit_augmentation_keeps_auction_and_moves_every_card_plane(self):
        for tag in (1, 2, 3):
            features = torch.zeros(64, 600)
            features[:, :512] = torch.arange(512)
            features[:, 580:] = 0.5
            target = torch.arange(32).float().repeat(64, 1)
            mask = (target.long() % 3) == 0
            maps = fit.suit_maps(tag, "cpu")
            torch.manual_seed(9)
            x, y, m = fit.augment_suits(features, target, mask, tag, maps)
            torch.testing.assert_close(x[:, 512:], features[:, 512:])
            for plane in range(16):
                torch.testing.assert_close(x[:, 32 * plane:32 * (plane + 1)], y + 32 * plane)
            self.assertTrue(torch.equal(m, (y.long() % 3) == 0))
            self.assertFalse(torch.equal(x, features))
            if tag == 1:
                torch.testing.assert_close(y[:, :8], target[:, :8])
            # A bid in an affected suit disables permutation for that position.
            features[:, 535] = 1
            x, y, m = fit.augment_suits(features, target, mask, tag, maps)
            self.assertTrue(torch.equal(x, features))
            self.assertTrue(torch.equal(y, target))
            self.assertTrue(torch.equal(m, mask))

    def test_mean_anchor_preserves_action_gaps_and_original_validation_labels(self):
        model = fit.Network(1, 1, (600, 32))
        x = torch.randn(3, 600)
        y = torch.randn(3, 32)
        mask = torch.zeros(3, 32, dtype=torch.bool)
        mask[:, :3] = True
        original = y.clone()
        _, anchored, _ = fit.anchor_targets(model, (x, y, mask), 2, "cpu")
        torch.testing.assert_close(anchored[:, :3].mean(1), model(x)[:, :3].mean(1))
        torch.testing.assert_close(anchored[:, 0] - anchored[:, 2], y[:, 0] - y[:, 2])
        self.assertTrue(torch.equal(anchored[~mask], y[~mask]))
        self.assertTrue(torch.equal(y, original))

    def test_diagnostics_use_legal_actions_and_game_points(self):
        model = fit.Network(1, 1, (600, 32))
        with torch.no_grad():
            model.layers[0].weight.zero_()
            model.layers[0].bias.fill_(100)
            model.layers[0].bias[0] = 1
            model.layers[0].bias[1] = 3
        x = torch.zeros((1, 600))
        y = torch.zeros((1, 32))
        mask = torch.zeros((1, 32), dtype=torch.bool)
        y[0, 0], y[0, 1] = 3, 1
        mask[0, :2] = True
        metrics = fit.diagnostics(model, (x, y, mask), np.array([0]), 10, torch.device("cpu"), -1)
        self.assertEqual(metrics["samples"], 1)
        self.assertEqual(metrics["rmse"], 52)
        self.assertEqual(metrics["centred_rmse"], 52)
        self.assertEqual(metrics["teacher_regret"], 52)
        self.assertEqual(metrics["optimal_choices"], 0)


if __name__ == "__main__":
    torch.set_num_threads(1)
    unittest.main()
