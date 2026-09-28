"""Independent likelihood, gradient, symmetry and half-export mixture checks."""

from argparse import Namespace
import contextlib
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
import fit_mixture_ownership
import joint_ownership
import mixture_ownership
import test_finetune_ownership
import test_joint_ownership


class MixtureOwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        torch.set_num_threads(1)

    @staticmethod
    def example():
        base, owners, allowed = test_joint_ownership.JointOwnershipTests.example()
        generator = torch.Generator().manual_seed(379)
        logits = base[:, None] + torch.randn((4, 4, 32, 3), generator=generator, dtype=torch.float64)
        gates = torch.randn((4, 4), generator=generator, dtype=torch.float64)
        return logits, gates, owners, allowed

    def test_normalized_components_match_independent_enumeration(self):
        logits, gates, owners, allowed = self.example()
        components = torch.stack([test_joint_ownership.brute_loss(logits[:, k], owners, allowed) for k in range(4)], 1)
        expected = -torch.logsumexp(torch.log_softmax(gates, 1) - components, 1)
        actual = mixture_ownership.loss(logits, gates, owners, allowed, reduction='none')
        torch.testing.assert_close(actual, expected, atol=1e-12, rtol=1e-12)
        expected_per_card = expected / (owners >= 0).sum(1).clamp_min(1)
        torch.testing.assert_close(mixture_ownership.loss(logits, gates, owners, allowed,
                                                        normalization='card'), expected_per_card.mean())

    def test_one_component_matches_joint_loss_and_gradients(self):
        logits, _, owners, allowed = self.example()
        first = logits[:, :1].clone().requires_grad_()
        second = first[:, 0].detach().clone().requires_grad_()
        gates = torch.randn((4, 1), dtype=torch.float64, requires_grad=True)
        actual = mixture_ownership.loss(first, gates, owners, allowed, normalization='card')
        expected = joint_ownership.loss(second, owners, allowed, normalization='card')
        actual.backward()
        expected.backward()
        torch.testing.assert_close(actual, expected)
        torch.testing.assert_close(first.grad[:, 0], second.grad)
        self.assertTrue((gates.grad == 0).all())

    def test_cloned_components_collapse_to_one_even_with_unequal_gates(self):
        logits, gates, owners, allowed = self.example()
        clones = logits[:, :1].expand(-1, 4, -1, -1).clone().requires_grad_()
        gates.requires_grad_()
        actual = mixture_ownership.loss(clones, gates, owners, allowed, reduction='sum')
        expected = joint_ownership.loss(clones[:, 0], owners, allowed, reduction='sum')
        torch.testing.assert_close(actual, expected, atol=1e-12, rtol=1e-12)
        actual.backward()
        torch.testing.assert_close(gates.grad, torch.zeros_like(gates), atol=1e-14, rtol=0)

    def test_component_permutation_and_card_owner_gauges_do_not_change_density(self):
        logits, gates, owners, allowed = self.example()
        expected = mixture_ownership.loss(logits, gates, owners, allowed, reduction='none')
        order = [2, 0, 3, 1]
        torch.testing.assert_close(mixture_ownership.loss(logits[:, order], gates[:, order], owners,
                                                        allowed, reduction='none'), expected)
        generator = torch.Generator().manual_seed(487)
        card = torch.randn((4, 4, 32, 1), generator=generator, dtype=torch.float64) * 10
        owner = torch.randn((4, 4, 1, 3), generator=generator, dtype=torch.float64) * 10
        torch.testing.assert_close(mixture_ownership.loss(logits + card + owner, gates, owners,
                                                        allowed, reduction='none'), expected,
                                   atol=1e-12, rtol=1e-12)

    def test_component_and_gate_derivatives_match_finite_differences(self):
        logits, gates, owners, allowed = self.example()
        logits.requires_grad_()
        gates.requires_grad_()
        mixture_ownership.loss(logits, gates, owners, allowed, reduction='sum').backward()
        for target, indices in ((logits, [(1, 0, 0, 0), (2, 3, 11, 1), (3, 1, 13, 2)]),
                                (gates, [(1, 0), (2, 3), (0, 1)])):
            for index in indices:
                plus, minus = target.detach().clone(), target.detach().clone()
                plus[index] += 1e-5
                minus[index] -= 1e-5
                arguments = (owners, allowed)
                if target is logits:
                    difference = (mixture_ownership.loss(plus, gates, *arguments, reduction='sum')
                                  - mixture_ownership.loss(minus, gates, *arguments, reduction='sum')) / 2e-5
                else:
                    difference = (mixture_ownership.loss(logits, plus, *arguments, reduction='sum')
                                  - mixture_ownership.loss(logits, minus, *arguments, reduction='sum')) / 2e-5
                self.assertAlmostEqual(difference.item(), target.grad[index].item(), delta=1e-8)

    def test_unique_and_empty_worlds_have_zero_loss_and_gradients(self):
        logits, gates, owners, _ = self.example()
        logits.requires_grad_()
        gates.requires_grad_()
        allowed = torch.zeros((4, 32, 3), dtype=torch.bool)
        row, card = (owners >= 0).nonzero(as_tuple=True)
        allowed[row, card, owners[row, card]] = True
        loss = mixture_ownership.loss(logits, gates, owners, allowed)
        self.assertAlmostEqual(loss.item(), 0, delta=1e-14)
        loss.backward()
        torch.testing.assert_close(logits.grad, torch.zeros_like(logits), atol=1e-14, rtol=0)
        torch.testing.assert_close(gates.grad, torch.zeros_like(gates), atol=1e-14, rtol=0)

    def test_warm_start_preserves_trunk_and_breaks_only_distributional_symmetry(self):
        torch.manual_seed(431)
        base = fit.Network(11, 1, fit_ownership.SIZES)
        clone = mixture_ownership.warm_start(base, 791, 0)
        perturbed = mixture_ownership.warm_start(base, 791, .05)
        repeat = mixture_ownership.warm_start(base, 791, .05)
        for left, right in zip(base.layers[:-1], perturbed.layers[:-1]):
            torch.testing.assert_close(left.weight, right.weight, atol=0, rtol=0)
            torch.testing.assert_close(left.bias, right.bias, atol=0, rtol=0)
        for left, right in zip(perturbed.parameters(), repeat.parameters()):
            torch.testing.assert_close(left, right, atol=0, rtol=0)
        noise = (perturbed.layers[-1].bias[:384] - clone.layers[-1].bias[:384]).reshape(4, 32, 3)
        self.assertAlmostEqual(noise.square().mean().sqrt().item(), .05, delta=1e-7)
        for axis in (0, 1, 2):
            torch.testing.assert_close(noise.mean(axis), torch.zeros_like(noise.mean(axis)), atol=1e-8, rtol=0)
        self.assertTrue((perturbed.layers[-1].weight[384:] == 0).all())
        self.assertTrue((perturbed.layers[-1].bias[384:] == 0).all())
        x = torch.randn((3, 600))
        component_logits, gates = mixture_ownership.split(clone(x))
        torch.testing.assert_close(component_logits, base(x).reshape(3, 1, 32, 3).expand(-1, 4, -1, -1))
        self.assertTrue((gates == 0).all())
        for invalid in (-1, float('nan'), float('inf')):
            with self.assertRaises(ValueError):
                mixture_ownership.warm_start(base, 1, invalid)

    def test_checked_half_exports_refuse_each_header_mismatch_length_and_nonfinite(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'mixture.bin'
            for tag in mixture_ownership.TAGS:
                model = mixture_ownership.warm_start(fit.Network(tag - 3, 1, fit_ownership.SIZES), 41)
                fit.write_network(model, path)
                raw = path.read_bytes()
                self.assertEqual(len(raw), 220844)
                restored = mixture_ownership.read_network(path, tag)
                expected = fit.Network(tag, 1, mixture_ownership.SIZES)
                with torch.no_grad():
                    for before, after in zip(model.parameters(), expected.parameters()):
                        after.copy_(before.half().float())
                x = torch.randn((3, 600))
                torch.testing.assert_close(restored(x), expected(x), atol=0, rtol=0)
                for offset in range(0, 36, 4):
                    changed = bytearray(raw)
                    struct.pack_into('<i', changed, offset, struct.unpack_from('<i', changed, offset)[0] + 1)
                    path.write_bytes(changed)
                    with self.assertRaises(ValueError):
                        mixture_ownership.read_network(path, tag)
                for invalid in (raw[:-1], raw + b'\0', b'', raw[:4]):
                    path.write_bytes(invalid)
                    with self.assertRaises(ValueError):
                        mixture_ownership.read_network(path, tag)
                for value in (float('nan'), float('inf')):
                    changed = bytearray(raw)
                    struct.pack_into('<e', changed, 36, value)
                    path.write_bytes(changed)
                    with self.assertRaises(ValueError):
                        mixture_ownership.read_network(path, tag)

    def test_invalid_shapes_gates_and_reductions_are_refused(self):
        logits, gates, owners, allowed = self.example()
        for arguments in ((logits[:, :0], gates[:, :0], owners, allowed),
                          (logits[:, :, :31], gates, owners, allowed),
                          (logits, gates[:, :3], owners, allowed),
                          (logits, gates, owners[:1], allowed),
                          (logits, gates, owners, allowed[:1])):
            with self.assertRaises(ValueError):
                mixture_ownership.loss(*arguments)
        for value in (float('nan'), float('inf')):
            invalid = gates.clone()
            invalid[0, 0] = value
            with self.assertRaises(ValueError):
                mixture_ownership.loss(logits, invalid, owners, allowed)
        for keyword in ('reduction', 'normalization'):
            with self.assertRaises(ValueError):
                mixture_ownership.loss(logits, gates, owners, allowed, **{keyword: 'invalid'})
        with self.assertRaises(ValueError):
            mixture_ownership.split(torch.zeros((2, 96)))

    def test_runner_checks_control_sources_orders_and_actual_half_metrics(self):
        torch.manual_seed(433)
        features, owners = test_finetune_ownership.FinetuneOwnershipTests.example()
        features[:, 512 + 6] = 1
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'source'
            source.mkdir()
            for index, name in enumerate(fit_ownership.NAMES):
                fit.write_network(fit.Network(11 + index, 1, fit_ownership.SIZES), source / (name + '.bin'))
                test_finetune_ownership.FinetuneOwnershipTests.write_data(root / f'train.{name}.samples', features, owners)
                test_finetune_ownership.FinetuneOwnershipTests.write_data(root / f'valid.{name}.samples', features[:2], owners[:2])
            control_args = Namespace(input=str(source), data=str(root / 'train'), validation_data=str(root / 'valid'),
                                     out=str(root / 'control'), layout=1, objective='joint', normalization='card',
                                     epochs=1, batch=2, learning_rate=.0001, seed=197, device='cpu',
                                     cache_device=False, cache_reserve_mib=1536)
            with contextlib.redirect_stdout(io.StringIO()):
                finetune_ownership.run(control_args)
            args = Namespace(**{key: value for key, value in vars(control_args).items()
                                if key not in ('layout', 'objective', 'normalization')})
            args.out = str(root / 'mixture')
            args.control_report = str(root / 'control' / 'report.json')
            args.perturbation = .05
            with contextlib.redirect_stdout(io.StringIO()):
                fit_mixture_ownership.run(args)
            report = json.loads((Path(args.out) / 'report.json').read_text())
            control = json.loads(Path(args.control_report).read_text())
            for index, name in enumerate(fit_ownership.NAMES):
                entry = report['networks'][name]['epochs'][0]
                self.assertEqual(entry['row_order_sha256'], control['networks'][name]['joint']['epochs'][0]['row_order_sha256'])
                restored = mixture_ownership.read_network(Path(args.out) / (name + '.bin'), 14 + index)
                measured = fit_mixture_ownership.measure(restored, features[:2], owners[:2], 2, 'cpu')
                self.assertEqual(measured, entry['validation'])
                self.assertEqual(measured['overall'], measured['late_three_completed'])
                self.assertAlmostEqual(sum(measured['overall']['mean_gates']), 1, delta=1e-6)
            args.seed += 1
            with self.assertRaisesRegex(ValueError, 'setting differs: seed'):
                fit_mixture_ownership.check_control(args, control)


if __name__ == '__main__':
    unittest.main()
