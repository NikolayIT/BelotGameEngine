"""Frozen-base, phase gate, centered gradients and checked correction export."""

from pathlib import Path
import contextlib
import io
import json
import struct
import tempfile
import unittest
from unittest import mock

import fit
import fit_late_correction as late
import torch


class LateCorrectionTests(unittest.TestCase):
    def test_zero_output_initialization_and_hard_phase_gate(self):
        torch.manual_seed(887)
        model = late.make_model(21)
        features = torch.rand(8, 600)
        features[:, 512:520] = torch.eye(8)
        base = torch.randn(8, 32)
        base[0, 0] = -0.0
        mask = torch.rand(8, 32) > 0.2
        mask[:, 0] = True
        self.assertTrue(torch.all(model(features) == 0))
        result = late.apply_correction(base, model(features), features, mask)
        self.assertTrue(torch.equal(base.view(torch.int32), result.view(torch.int32)))
        changed = late.apply_correction(base, torch.randn(8, 32), features, mask)
        self.assertTrue(torch.equal(base[:4].view(torch.int32), changed[:4].view(torch.int32)))
        self.assertTrue(torch.equal(base[~mask], changed[~mask]))
        torch.testing.assert_close((changed * mask).sum(1), (base * mask).sum(1), rtol=1e-5, atol=2e-6)

    def test_targets_are_teacher_minus_base_advantages(self):
        teacher = torch.tensor([[3.0, 7.0, 91.0], [2.0, -3.0, 11.0]])
        base = torch.tensor([[1.0, -1.0, -4.0], [7.0, 5.0, -3.0]])
        mask = torch.tensor([[True, True, False], [True, False, True]])
        target = late.delta_targets(base, teacher, mask)
        torch.testing.assert_close(target, late.center(teacher, mask) - late.center(base, mask))
        torch.testing.assert_close(target.sum(1), torch.zeros(2))
        shifted = late.delta_targets(base + 12, teacher - 7, mask)
        torch.testing.assert_close(target, shifted)

    def test_centered_loss_gradients_match_finite_differences(self):
        torch.manual_seed(888)
        prediction = torch.randn(3, 5, dtype=torch.double, requires_grad=True) * 0.2
        target = torch.randn(3, 5, dtype=torch.double) * 0.2
        mask = torch.tensor([[True, True, False, True, False],
                             [True, False, True, True, False],
                             [False, True, True, False, True]])
        self.assertTrue(torch.autograd.gradcheck(lambda value: late.correction_loss(value, target, mask), (prediction,)))
        gradient, = torch.autograd.grad(late.correction_loss(prediction, target, mask), prediction)
        self.assertTrue(torch.all(gradient[~mask] == 0))
        torch.testing.assert_close(gradient.sum(1), torch.zeros(3, dtype=torch.double), atol=1e-15, rtol=0)

    def test_training_changes_only_correction_and_reduces_the_objective(self):
        torch.manual_seed(889)
        base = fit.Network(1, 1, (600, 8, 32)).requires_grad_(False)
        frozen = [parameter.clone() for parameter in base.parameters()]
        correction = late.make_model(21)
        features = torch.rand(12, 600)
        mask = torch.ones(12, 32, dtype=torch.bool)
        original = late.base_values(base, features, 5, 'cpu')
        teacher = original + torch.arange(32)[None, :] / 100
        target = late.delta_targets(original, teacher, mask)
        initial = float(late.correction_loss(correction(features), target, mask).detach())
        optimizer = torch.optim.Adam(correction.parameters(), lr=0.001)
        for _ in range(8):
            optimizer.zero_grad()
            late.correction_loss(correction(features), target, mask).backward()
            optimizer.step()
        self.assertLess(float(late.correction_loss(correction(features), target, mask).detach()), initial)
        for before, after in zip(frozen, base.parameters()):
            self.assertTrue(torch.equal(before, after))
            self.assertIsNone(after.grad)

    def test_loader_accepts_only_checked_half_precision_format(self):
        torch.manual_seed(890)
        model = late.make_model(23)
        with torch.no_grad():
            model.layers[-1].weight.normal_(0, 0.1)
        features = torch.randn(4, 600)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'alltrumps.bin'
            fit.write_network(model, path)
            raw = path.read_bytes()
            self.assertEqual(len(raw), late.FILE_BYTES)
            loaded = late.read_correction(path, 23)
            torch.testing.assert_close(model(features), loaded(features), atol=0.0005, rtol=0.005)
            corruptions = [raw[:-1], raw + b'x']
            for offset in range(0, 32, 4):
                corruptions.append(raw[:offset] + struct.pack('<i', 999) + raw[offset + 4:])
            corruptions.append(raw[:32] + struct.pack('<H', 0x7e00) + raw[34:])
            for corrupt in corruptions:
                path.write_bytes(corrupt)
                with self.assertRaises(ValueError):
                    late.read_correction(path, 23)

    def test_only_late_nonforced_samples_are_retained(self):
        rows = []
        for phase in range(8):
            mask = 1 if phase == 7 else 3
            row = struct.pack('<BHeI', 1, 512 + phase, 1.0, mask)
            row += struct.pack('<' + 'e' * mask.bit_count(), *([0.5] * mask.bit_count()))
            rows.append(row)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.samples'
            path.write_bytes(struct.pack('<3i', fit.SAMPLE_MAGIC, 32, 8) + b''.join(rows))
            features, labels, masks = late.read_late_samples(path)
            self.assertEqual(features.shape, (3, 600))
            self.assertEqual(features[:, 512:520].argmax(1).tolist(), [4, 5, 6])
            self.assertEqual(labels.shape, (3, 32))
            self.assertEqual(masks.sum(1).tolist(), [2, 2, 2])
        with self.assertRaises(ValueError):
            late.late_mask(torch.zeros(1, 600))
        with self.assertRaises(ValueError):
            late.late_mask(torch.ones(1, 664))

    def test_entrypoint_exports_all_contracts_and_keeps_base_files_unchanged(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            base_folder = root / 'base'
            for index, name in enumerate(late.NAMES):
                fit.write_network(fit.Network(index + 1, 1, (600, 8, 32)), base_folder / f'{name}.bin')
                for prefix, phase in [('train', 4), ('validation', 5)]:
                    row = struct.pack('<BHeIee', 1, 512 + phase, 1.0, 3, 0.5, -0.5)
                    (root / f'{prefix}.{name}.samples').write_bytes(struct.pack('<3i', fit.SAMPLE_MAGIC, 32, 4) + row * 4)
            hashes = {name: late.sha256(base_folder / f'{name}.bin') for name in late.NAMES}
            arguments = ['fit_late_correction.py', '--in', str(base_folder), '--data', str(root / 'train'),
                         '--validation-data', str(root / 'validation'), '--out', str(root / 'output'),
                         '--epochs', '1', '--batch', '3', '--threads', '1', '--device', 'cpu']
            with mock.patch('sys.argv', arguments), contextlib.redirect_stdout(io.StringIO()):
                late.main()
            report = json.loads((root / 'output' / 'report.json').read_text())
            self.assertEqual(set(report['networks']), set(late.NAMES))
            for index, name in enumerate(late.NAMES):
                self.assertEqual(late.sha256(base_folder / f'{name}.bin'), hashes[name])
                self.assertEqual(report['networks'][name]['train_samples'], 4)
                self.assertEqual(report['networks'][name]['validation_samples'], 4)
                self.assertEqual(late.read_correction(root / 'output' / f'{name}.bin', 21 + index).sizes, late.SIZES)
                self.assertTrue((root / 'output' / 'epoch-001' / f'{name}.bin').is_file())


if __name__ == '__main__':
    torch.set_num_threads(1)
    unittest.main()
