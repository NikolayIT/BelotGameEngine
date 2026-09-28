"""Public-label validation and a matched partial-batch fine-tuning experiment."""

from argparse import Namespace
import contextlib
import hashlib
import io
import json
from pathlib import Path
import struct
import tempfile
import unittest

import torch

import fit
import fit_ownership
import finetune_ownership
from test_ownership_rollout import position, write_rollout


class FinetuneOwnershipTests(unittest.TestCase):
    @staticmethod
    def example():
        features = torch.zeros((3, 600))
        features[:, :2] = 1
        features[:, 32:34] = 1
        for seat in range(4):
            for card in range(8 + 6 * seat, 14 + 6 * seat):
                features[:, (2 + seat) * 32 + card] = 1
        owners = torch.full((3, 32), -1, dtype=torch.long)
        owners[:, 2:8] = torch.tensor([0, 0, 1, 1, 2, 2])
        owners[1, 3], owners[1, 5] = owners[1, 5].clone(), owners[1, 3].clone()
        features[:, 11 * 32 + 2] = 1
        features[:, 14 * 32 + 4] = 1
        return features, owners

    @staticmethod
    def write_data(path, features, owners):
        raw = bytearray(struct.pack('<3i', fit.SAMPLE_MAGIC, 32, len(features)))
        labels = bytearray(struct.pack('<4i', 0x314F5042, 1, 32, len(features)))
        for x, target in zip(features, owners):
            indices = x.nonzero().flatten().tolist()
            raw.extend(struct.pack('<B', len(indices)))
            for index in indices:
                raw.extend(struct.pack('<He', index, x[index].item()))
            raw.extend(struct.pack('<Iee', 3, 0, 0))
            packed = sum((owner + 1) << (2 * card) for card, owner in enumerate(target.tolist()))
            labels.extend(struct.pack('<Q', packed))
        path.write_bytes(raw)
        Path(str(path) + '.owners').write_bytes(labels)

    def test_targets_are_checked_against_public_cards_capacities_and_masks(self):
        features, owners = self.example()
        finetune_ownership.validate_public_labels(features, owners, batch=2)
        wrong_count = owners.clone()
        wrong_count[0, 3] = 1
        with self.assertRaisesRegex(ValueError, 'capacities'):
            finetune_ownership.validate_public_labels(features, wrong_count)
        wrong_card = owners.clone()
        wrong_card[0, 0], wrong_card[0, 2] = 0, -1
        with self.assertRaisesRegex(ValueError, 'history'):
            finetune_ownership.validate_public_labels(features, wrong_card)
        impossible = features.clone()
        impossible[0, 10 * 32 + 2] = 1
        with self.assertRaisesRegex(ValueError, 'constraints'):
            finetune_ownership.validate_public_labels(impossible, owners)
        duplicate = features.clone()
        duplicate[0, 3 * 32 + 8] = 1
        with self.assertRaisesRegex(ValueError, 'history'):
            finetune_ownership.validate_public_labels(duplicate, owners)

    def test_current_trick_cards_count_toward_public_remaining_hands(self):
        features, owners = self.example()
        features[:, 3 * 32 + 14] = 0
        features[:, 6 * 32 + 14] = 1
        finetune_ownership.validate_public_labels(features, owners)
        features[:, 6 * 32 + 14] = 0
        with self.assertRaises(ValueError):
            finetune_ownership.validate_public_labels(features, owners)

    def test_ce_control_uses_the_joint_objectives_per_state_weighting(self):
        logits = torch.zeros((3, 32, 3), requires_grad=True)
        owners = torch.full((3, 32), -1, dtype=torch.long)
        owners[1, :2] = 0
        owners[2, :6] = 0
        expected = torch.log(torch.tensor(3.0)) * 2 / 3
        actual = finetune_ownership.local_loss(logits, owners)
        torch.testing.assert_close(actual, expected)
        actual.backward()
        torch.testing.assert_close(logits.grad[1, :2].sum(0), logits.grad[2, :6].sum(0))
        self.assertTrue((logits.grad[0] == 0).all())

    def test_matched_exported_warm_starts_orders_and_partial_batches(self):
        torch.manual_seed(491)
        features, owners = self.example()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / 'source', root / 'output'
            source.mkdir()
            before = {}
            for index, name in enumerate(fit_ownership.NAMES):
                path = source / (name + '.bin')
                fit.write_network(fit.Network(11 + index, 1, fit_ownership.SIZES), path)
                before[name] = hashlib.sha256(path.read_bytes()).hexdigest()
                self.write_data(root / f'train.{name}.samples', features, owners)
                self.write_data(root / f'valid.{name}.samples', features[:2], owners[:2])
            args = Namespace(input=str(source), data=str(root / 'train'), validation_data=str(root / 'valid'),
                             out=str(output), layout=1, objective='both', normalization='card',
                             epochs=2, batch=2, learning_rate=.0001, seed=197, device='cpu',
                             cache_device=False, cache_reserve_mib=1536)
            with contextlib.redirect_stdout(io.StringIO()):
                finetune_ownership.run(args)
            report = json.loads((output / 'report.json').read_text())
            for index, name in enumerate(fit_ownership.NAMES):
                entries = report['networks'][name]
                self.assertEqual(entries['ce']['initial'], entries['joint']['initial'])
                self.assertEqual([x['row_order_sha256'] for x in entries['ce']['epochs']],
                                 [x['row_order_sha256'] for x in entries['joint']['epochs']])
                self.assertEqual(before[name], hashlib.sha256((source / (name + '.bin')).read_bytes()).hexdigest())
                for objective in ('ce', 'joint'):
                    for folder in (output / objective, output / objective / 'epoch-001', output / objective / 'epoch-002'):
                        restored = fit_ownership.read_network(folder / (name + '.bin'), 11 + index, 1)
                        self.assertTrue(torch.isfinite(restored(features)).all())
                    self.assertNotEqual(before[name], entries[objective]['epochs'][-1]['export_sha256'])
            self.assertEqual(len(report['sources']), 15)

            args.cache_device = True
            args.out = str(root / 'cached')
            with contextlib.redirect_stdout(io.StringIO()):
                finetune_ownership.run(args)
            for name in fit_ownership.NAMES:
                for objective in ('ce', 'joint'):
                    for epoch in ('', 'epoch-001', 'epoch-002'):
                        relative = Path(objective) / epoch / (name + '.bin')
                        self.assertEqual((output / relative).read_bytes(), (Path(args.out) / relative).read_bytes())

    def test_ppo_reader_path_trains_from_real_labels_and_hashes_actual_files(self):
        torch.manual_seed(493)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output = root / 'source', root / 'output'
            source.mkdir()
            for index, name in enumerate(fit_ownership.NAMES):
                fit.write_network(fit.Network(11 + index, 1, fit_ownership.SIZES), source / (name + '.bin'))
                write_rollout(root / f'train.{name}.ppo', [position(2), position(5, partial=True), position(6)], index + 1)
                write_rollout(root / f'valid.{name}.ppo', [position(3), position(6)], index + 1)
            (root / 'train.ppo.json').write_text(json.dumps({'Seed': 7, 'Deals': 3}))
            args = Namespace(input=str(source), data=str(root / 'train'), validation_data=str(root / 'valid'),
                             out=str(output), layout=1, data_format='ppo', objective='ce', normalization='card',
                             epochs=1, batch=2, learning_rate=.0001, seed=197, device='cpu',
                             cache_device=False, cache_reserve_mib=1536)
            with contextlib.redirect_stdout(io.StringIO()):
                finetune_ownership.run(args)
            report = json.loads((output / 'report.json').read_text())
            self.assertEqual(len(report['sources']), 10)
            self.assertEqual(report['collections'][str(root / 'train.ppo.json')]['Seed'], 7)
            self.assertFalse(any(path.endswith('.samples') or path.endswith('.owners') for path in report['sources']))
            for index, name in enumerate(fit_ownership.NAMES):
                self.assertEqual(report['networks'][name]['ce']['initial']['states'], 2)
                fit_ownership.read_network(output / 'ce' / (name + '.bin'), 11 + index, 1)
                path = root / f'train.{name}.ppo'
                self.assertEqual(report['sources'][str(path)], hashlib.sha256(path.read_bytes()).hexdigest())
            with self.assertRaises(ValueError):
                finetune_ownership.read_dataset(root / 'train.trump.ppo', 1, 2, 'ppo')


if __name__ == '__main__':
    unittest.main()
