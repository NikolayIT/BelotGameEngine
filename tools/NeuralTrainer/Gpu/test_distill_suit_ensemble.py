"""Independent suit-map, target, public-input and matched-run distillation checks."""

from argparse import Namespace
import contextlib
import io
import itertools
import json
from pathlib import Path
import struct
import tempfile
import unittest

import torch

import distill_suit_ensemble as distill
import fit


class DistillSuitEnsembleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        torch.set_num_threads(1)

    @staticmethod
    def features():
        x = torch.zeros((16, 600))
        for fixed in range(16):
            for plane in range(16):
                x[fixed, plane * 32 + (plane * 7 + fixed) % 32] = (plane + 1) / 17
            x[fixed, 32:64] = 0
            x[fixed, [32, 34, 44, 55]] = 1
            for suit in range(4):
                if fixed & (1 << suit):
                    x[fixed, 534 + 8 * ((fixed + suit) % 4) + suit] = 1
            x[fixed, 538] = 1
            x[fixed, 594:] = torch.arange(6) / 10
        return x

    @staticmethod
    def write_samples(path, features, targets):
        raw = bytearray(struct.pack('<3i', fit.SAMPLE_MAGIC, 32, len(features)))
        for x, target in zip(features, targets):
            indices = x.nonzero().flatten().tolist()
            raw.extend(struct.pack('<B', len(indices)))
            for index in indices:
                raw.extend(struct.pack('<He', index, float(x[index])))
            actions = x[32:64].nonzero().flatten().tolist()
            raw.extend(struct.pack('<I', sum(1 << action for action in actions)))
            for action in actions:
                raw.extend(struct.pack('<e', float(target[action])))
        path.write_bytes(raw)

    def test_fixed_masks_include_every_seats_suit_bids_and_only_trump_slot(self):
        x = self.features()
        for tag in (1, 2, 3):
            self.assertEqual(distill.fixed_masks(x, tag).tolist(), [value | (tag == 1) for value in range(16)])
        x[:, 534:566] = 0
        for seat in range(4):
            x[:, 534 + seat * 8 + 4:534 + seat * 8 + 8] = 1
        self.assertEqual(distill.fixed_masks(x, 2).tolist(), [0] * 16)
        with self.assertRaises(ValueError):
            distill.fixed_masks(x[:, :599], 2)
        with self.assertRaises(ValueError):
            distill.card_maps(16)

    def test_groups_are_complete_bijections_fixing_every_auction_suit(self):
        for fixed in range(16):
            maps = distill.card_maps(fixed)
            expected = 1
            for number in range(1, 5 - fixed.bit_count()):
                expected *= number
            self.assertEqual(len(maps), expected)
            self.assertEqual(maps[0], tuple(range(32)))
            self.assertEqual(len(set(maps)), len(maps))
            for mapping in maps:
                self.assertEqual(sorted(mapping), list(range(32)))
                for card in range(32):
                    self.assertEqual(mapping[card] % 8, card % 8)
                    if fixed & (1 << (card // 8)):
                        self.assertEqual(mapping[card], card)

    def test_teacher_matches_independent_source_to_destination_dense_reference(self):
        torch.manual_seed(479)
        model = fit.Network(2, 1, (600, 17, 32)).eval()
        x = self.features()
        for tag in (1, 2, 3):
            baseline, actual = distill.teacher_values(model, x, tag)
            torch.testing.assert_close(baseline, model(x))
            expected = []
            for row in range(len(x)):
                fixed = row | (1 if tag == 1 else 0)
                total, views = torch.zeros(32, dtype=torch.float64), 0
                for candidate in itertools.product(range(4), repeat=4):
                    if len(set(candidate)) != 4 or any(fixed & (1 << s) and candidate[s] != s for s in range(4)):
                        continue
                    transformed = x[row].clone()
                    for plane in range(16):
                        for card in range(32):
                            destination = candidate[card // 8] * 8 + card % 8
                            transformed[plane * 32 + destination] = x[row, plane * 32 + card]
                    prediction = model(transformed[None])[0]
                    total += torch.stack([prediction[candidate[c // 8] * 8 + c % 8] for c in range(32)]).double()
                    views += 1
                expected.append((total / views).float())
            torch.testing.assert_close(actual, torch.stack(expected), atol=1e-7, rtol=1e-6)
            for row in (7, 11, 13, 14, 15):
                torch.testing.assert_close(actual[row], baseline[row], atol=0, rtol=0)

    def test_centered_legal_targets_keep_teacher_differences_and_baseline_mean(self):
        torch.manual_seed(481)
        baseline, ensemble = torch.randn((3, 32)), torch.randn((3, 32))
        legal = torch.zeros((3, 32), dtype=torch.bool)
        legal[0, [0, 1]], legal[1, [2, 7, 17]], legal[2, 31] = True, True, True
        target = distill.anchored_targets(baseline, ensemble, legal)
        counts = legal.sum(1)
        torch.testing.assert_close((target * legal).sum(1) / counts, (baseline * legal).sum(1) / counts)
        for row in range(3):
            values = legal[row].nonzero().flatten()
            torch.testing.assert_close(target[row, values] - target[row, values[0]],
                                       ensemble[row, values] - ensemble[row, values[0]])
        torch.testing.assert_close(target[~legal], baseline[~legal], atol=0, rtol=0)
        self.assertAlmostEqual(target[2, 31].item(), baseline[2, 31].item(), delta=1e-6)
        with self.assertRaises(ValueError):
            distill.anchored_targets(baseline, ensemble, torch.zeros_like(legal))

    def test_old_monte_carlo_targets_do_not_affect_public_inputs_or_teacher(self):
        x = self.features()[:2]
        with tempfile.TemporaryDirectory() as directory:
            first, second = Path(directory) / 'first.samples', Path(directory) / 'second.samples'
            self.write_samples(first, x, torch.zeros((2, 32)))
            self.write_samples(second, x, torch.full((2, 32), 1000.0))
            a, b = distill.read_public(first), distill.read_public(second)
            for left, right in zip(a, b):
                torch.testing.assert_close(left, right, atol=0, rtol=0)
            raw = bytearray(first.read_bytes())
            count = raw[12]
            offset = 13
            for index in range(count):
                if struct.unpack_from('<H', raw, offset + 4 * index)[0] == 32:
                    struct.pack_into('<e', raw, offset + 4 * index + 2, .5)
            first.write_bytes(raw)
            with self.assertRaisesRegex(ValueError, 'legal-card plane'):
                distill.read_public(first)

    def test_metrics_use_game_points_and_teacher_changed_subset(self):
        model = fit.Network(1, 1, (600, 32))
        with torch.no_grad():
            model.layers[0].weight.zero_()
            model.layers[0].bias.zero_()
            model.layers[0].bias[0] = 1
        x = torch.zeros((2, 600))
        baseline = model(x).detach()
        target = baseline.clone()
        target[0, 0], target[0, 1] = 0, 1
        legal = torch.zeros((2, 32), dtype=torch.bool)
        legal[:, :2] = True
        result = distill.metrics(model, (x, baseline, target, legal), 1)
        self.assertEqual(result['ensemble_regret_game_points'], 13)
        self.assertEqual(result['changed_state_regret_game_points'], 26)
        self.assertEqual(result['teacher_changed_states'], 1)
        self.assertEqual(result['baseline_mean_drift_rmse_game_points'], 0)

    def test_tiny_run_preserves_bid_weights_orders_and_checked_exports(self):
        torch.manual_seed(483)
        x = self.features()[[0, 3, 15]]
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / 'source', root / 'output'
            source.mkdir()
            fit.write_network(fit.Network(0, 1, (97, 8, 9)), source / 'bid.bin')
            original = {}
            for tag, name in enumerate(distill.NAMES, 1):
                fit.write_network(fit.Network(tag, 1, distill.SIZES), source / (name + '.bin'))
                original[name] = distill.digest(source / (name + '.bin'))
                self.write_samples(root / f'train.{name}.samples', x, torch.zeros((3, 32)))
                self.write_samples(root / f'valid.{name}.samples', x[:2], torch.ones((2, 32)))
            args = Namespace(input=str(source), data=str(root / 'train'), validation_data=str(root / 'valid'),
                             out=str(output), epochs=1, batch=2, teacher_batch=2, learning_rate=1e-5,
                             seed=9851, device='cpu')
            with contextlib.redirect_stdout(io.StringIO()):
                distill.run(args)
            report = json.loads((output / 'report.json').read_text())
            for tag, name in enumerate(distill.NAMES, 1):
                self.assertEqual(distill.digest(source / (name + '.bin')), original[name])
                entries = report['networks'][name]['objectives']
                self.assertEqual(entries['control'][0]['row_order_sha256'], entries['ensemble'][0]['row_order_sha256'])
                for objective in ('control', 'ensemble'):
                    for folder in (output / objective, output / objective / 'epoch-001'):
                        self.assertEqual((folder / 'bid.bin').read_bytes(), (source / 'bid.bin').read_bytes())
                        fit.read_network(folder / (name + '.bin'), tag)
                    self.assertEqual(distill.digest(output / objective / (name + '.bin')), entries[objective][0]['export_sha256'])


if __name__ == '__main__':
    unittest.main()
