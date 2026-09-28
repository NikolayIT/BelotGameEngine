"""Independent enumeration and derivative checks for exact ownership likelihood."""

import itertools
import math
import unittest

import torch

import joint_ownership


def brute_loss(logits, owners, allowed):
    """Enumerate all 3^N assignments before applying capacity and public masks."""
    result = []
    for row in range(len(logits)):
        cards = (owners[row] >= 0).nonzero().flatten().tolist()
        counts = [(owners[row] == owner).sum().item() for owner in range(3)]
        scores = []
        for assignment in itertools.product(range(3), repeat=len(cards)):
            if ([assignment.count(owner) for owner in range(3)] != counts
                    or any(not allowed[row, card, owner] for card, owner in zip(cards, assignment))):
                continue
            scores.append(sum((logits[row, card, owner] for card, owner in zip(cards, assignment)),
                              logits[row].sum() * 0))
        truth = sum((logits[row, card, owners[row, card]] for card in cards), logits[row].sum() * 0)
        result.append(torch.logsumexp(torch.stack(scores), 0) - truth)
    return torch.stack(result)


class JointOwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        torch.set_num_threads(1)

    @staticmethod
    def example():
        generator = torch.Generator().manual_seed(1979)
        logits = torch.randn((4, 32, 3), generator=generator, dtype=torch.float64)
        owners = torch.full((4, 32), -1, dtype=torch.long)
        owners[1, [0, 4, 31]] = torch.tensor([0, 1, 2])
        owners[2, [1, 8, 11, 18, 23]] = torch.tensor([0, 1, 0, 1, 2])
        owners[3, [0, 4, 8, 13, 21, 30]] = torch.tensor([0, 1, 2, 0, 1, 2])
        allowed = torch.ones_like(logits, dtype=torch.bool)
        allowed[2, 11] = torch.tensor([True, False, False])
        allowed[2, 18, 2] = False
        allowed[3, 0, 2] = False
        allowed[3, 13, 1] = False
        allowed[0] = False
        return logits, owners, allowed

    def test_variable_card_counts_match_independent_assignment_enumeration(self):
        logits, owners, allowed = self.example()
        expected = brute_loss(logits, owners, allowed)
        actual = joint_ownership.loss(logits, owners, allowed, reduction='none')
        torch.testing.assert_close(actual, expected, atol=1e-12, rtol=1e-12)
        self.assertEqual(actual[0].item(), 0)
        torch.testing.assert_close(joint_ownership.loss(logits, owners, allowed), expected.mean())
        torch.testing.assert_close(joint_ownership.loss(logits, owners, allowed, reduction='sum'), expected.sum())
        expected = expected / (owners >= 0).sum(1).clamp_min(1)
        torch.testing.assert_close(joint_ownership.loss(logits, owners, allowed, normalization='card'), expected.mean())

    def test_gradients_match_independent_world_marginals(self):
        logits, owners, allowed = self.example()
        actual_logits = logits.clone().requires_grad_()
        expected_logits = logits.clone().requires_grad_()
        joint_ownership.loss(actual_logits, owners, allowed, reduction='sum').backward()
        brute_loss(expected_logits, owners, allowed).sum().backward()
        self.assertTrue(torch.isfinite(actual_logits.grad).all())
        torch.testing.assert_close(actual_logits.grad, expected_logits.grad, atol=1e-12, rtol=1e-12)
        torch.testing.assert_close(actual_logits.grad.sum(2), torch.zeros((4, 32), dtype=torch.float64), atol=1e-12, rtol=0)
        self.assertTrue((actual_logits.grad[owners < 0] == 0).all())
        self.assertTrue((actual_logits.grad[~allowed] == 0).all())

    def test_selected_gradients_match_centered_finite_differences(self):
        logits, owners, allowed = self.example()
        logits.requires_grad_()
        joint_ownership.loss(logits, owners, allowed, reduction='sum').backward()
        for index in [(1, 0, 0), (1, 4, 2), (2, 11, 1), (2, 18, 0), (3, 13, 2), (3, 31, 0)]:
            plus, minus = logits.detach().clone(), logits.detach().clone()
            plus[index] += 1e-5
            minus[index] -= 1e-5
            difference = (joint_ownership.loss(plus, owners, allowed, reduction='sum')
                          - joint_ownership.loss(minus, owners, allowed, reduction='sum')) / 2e-5
            self.assertAlmostEqual(difference.item(), logits.grad[index].item(), delta=1e-8)

    def test_common_card_and_owner_shifts_cancel_under_fixed_capacities(self):
        logits, owners, allowed = self.example()
        generator = torch.Generator().manual_seed(1987)
        card_shift = torch.randn((4, 32, 1), generator=generator, dtype=torch.float64) * 100
        owner_shift = torch.randn((4, 1, 3), generator=generator, dtype=torch.float64) * 10
        expected = joint_ownership.loss(logits, owners, allowed, reduction='none')
        torch.testing.assert_close(joint_ownership.loss(logits + card_shift, owners, allowed, reduction='none'), expected, atol=1e-12, rtol=1e-12)
        # Every feasible assignment has the same count for each owner as well.
        torch.testing.assert_close(joint_ownership.loss(logits + owner_shift, owners, allowed, reduction='none'), expected, atol=1e-12, rtol=1e-12)

    def test_uniform_full_deal_partition_is_multinomial_world_count(self):
        logits = torch.zeros((1, 32, 3), dtype=torch.float64, requires_grad=True)
        owners = torch.tensor([[0] * 8 + [1] * 8 + [2] * 8 + [-1] * 8])
        allowed = torch.ones_like(logits, dtype=torch.bool)
        result = joint_ownership.loss(logits, owners, allowed)
        expected = math.log(math.factorial(24) // math.factorial(8) ** 3)
        self.assertAlmostEqual(result.item(), expected, delta=1e-12)
        result.backward()
        expected_gradient = torch.full((24, 3), 1 / 3, dtype=torch.float64)
        expected_gradient[torch.arange(24), owners[0, :24]] -= 1
        torch.testing.assert_close(logits.grad[0, :24], expected_gradient, atol=1e-12, rtol=1e-12)

    def test_capacity_and_known_cards_can_force_a_unique_world(self):
        logits = torch.arange(96, dtype=torch.float64).reshape(1, 32, 3).requires_grad_()
        owners = torch.full((1, 32), -1, dtype=torch.long)
        owners[0, [3, 7, 14, 28]] = torch.tensor([0, 0, 1, 2])
        allowed = torch.zeros_like(logits, dtype=torch.bool)
        allowed[0, 3, 0] = True
        allowed[0, 7, 0] = True
        allowed[0, 14, :2] = True
        allowed[0, 28, 1:] = True
        result = joint_ownership.loss(logits, owners, allowed)
        self.assertEqual(result.item(), 0)
        result.backward()
        self.assertTrue(torch.isfinite(logits.grad).all())
        self.assertTrue((logits.grad == 0).all())

    def test_extreme_logits_have_finite_loss_and_gradients(self):
        logits, owners, allowed = self.example()
        logits = (logits * 1000).float().requires_grad_()
        actual = joint_ownership.loss(logits, owners, allowed, reduction='sum')
        expected = brute_loss(logits.double(), owners, allowed).sum()
        self.assertTrue(torch.isfinite(actual))
        self.assertAlmostEqual(actual.item(), expected.item(), delta=.002)
        actual.backward()
        self.assertTrue(torch.isfinite(logits.grad).all())

    def test_low_precision_accumulates_in_float32(self):
        logits, owners, allowed = self.example()
        logits = logits.half().requires_grad_()
        result = joint_ownership.loss(logits, owners, allowed)
        self.assertEqual(result.dtype, torch.float32)
        result.backward()
        self.assertTrue(torch.isfinite(logits.grad).all())

    def test_empty_world_has_zero_loss_and_gradient(self):
        logits = torch.randn((2, 32, 3), requires_grad=True)
        owners = torch.full((2, 32), -1, dtype=torch.long)
        allowed = torch.zeros_like(logits, dtype=torch.bool)
        result = joint_ownership.loss(logits, owners, allowed, normalization='card')
        self.assertEqual(result.item(), 0)
        result.backward()
        self.assertTrue((logits.grad == 0).all())

    def test_invalid_inputs_and_impossible_truth_are_rejected(self):
        logits, owners, allowed = self.example()
        cases = [(logits[:, :31], owners, allowed), (logits[:0], owners[:0], allowed[:0]),
                 (logits, owners.float(), allowed), (logits, owners, allowed.float()),
                 (logits, owners[:, :31], allowed)]
        for args in cases:
            with self.subTest(shapes=[x.shape for x in args]):
                with self.assertRaises(ValueError):
                    joint_ownership.loss(*args)
        for value in (float('nan'), float('inf')):
            invalid = logits.clone()
            invalid[0, 0, 0] = value
            with self.assertRaises(ValueError):
                joint_ownership.loss(invalid, owners, allowed)
        for value in (-2, 3):
            invalid = owners.clone()
            invalid[0, 0] = value
            with self.assertRaises(ValueError):
                joint_ownership.loss(logits, invalid, allowed)
        too_many = owners.clone()
        too_many[0, :9] = 0
        with self.assertRaises(ValueError):
            joint_ownership.loss(logits, too_many, torch.ones_like(allowed))
        impossible = allowed.clone()
        impossible[1, 0, 0] = False
        with self.assertRaises(ValueError):
            joint_ownership.loss(logits, owners, impossible)
        for keyword in ('reduction', 'normalization'):
            with self.assertRaises(ValueError):
                joint_ownership.loss(logits, owners, allowed, **{keyword: 'invalid'})


if __name__ == '__main__':
    unittest.main()
