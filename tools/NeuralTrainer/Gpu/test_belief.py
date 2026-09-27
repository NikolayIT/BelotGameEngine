"""Privileged-label format, auxiliary gradients and policy-only export checks."""

from pathlib import Path
import struct
import tempfile
import unittest

import torch

import fit
import fit_belief


class BeliefTests(unittest.TestCase):
    def test_owner_format_and_ignored_cards(self):
        packed = (1 << 2) | (2 << 4) | (3 << 62)
        raw = struct.pack('<4iQ', 0x314F5042, 1, 32, 1, packed)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.owners'
            path.write_bytes(raw)
            owners = fit_belief.read_owners(path, 1)
            self.assertEqual(owners[0, [0, 1, 2, 31]].tolist(), [-1, 0, 1, 2])
            for bad in (raw[:-1], raw + b'x', struct.pack('<i', 0) + raw[4:],
                        raw[:4] + struct.pack('<i', 2) + raw[8:]):
                path.write_bytes(bad)
                with self.assertRaises(ValueError):
                    fit_belief.read_owners(path, 1)

    def test_auxiliary_gradients_and_ignored_card_mask(self):
        logits = torch.randn(2, 32, 3, dtype=torch.double, requires_grad=True)
        owners = torch.full((2, 32), -1, dtype=torch.long)
        owners[:, [1, 3, 5]] = torch.tensor([0, 1, 2])
        self.assertTrue(torch.autograd.gradcheck(lambda x: fit_belief.belief_loss(x, owners), (logits,)))
        gradient, = torch.autograd.grad(fit_belief.belief_loss(logits, owners), logits)
        self.assertTrue(torch.all(gradient[owners == -1] == 0))
        empty = fit_belief.belief_loss(logits, torch.full_like(owners, -1))
        self.assertEqual(float(empty.detach()), 0)
        self.assertTrue(torch.all(torch.autograd.grad(empty, logits)[0] == 0))

    def test_warmup_preserves_base_and_export_discards_privileged_head(self):
        torch.manual_seed(17)
        base = fit.Network(1, 1, (600, 8, 32))
        original = [p.detach().clone() for p in base.parameters()]
        model = fit_belief.BeliefModel(base)
        x = torch.randn(3, 600)
        owners = torch.randint(0, 3, (3, 32))
        base.requires_grad_(False)
        optimizer = torch.optim.Adam(model.head.parameters(), lr=0.01)
        optimizer.zero_grad()
        fit_belief.belief_loss(model(x)[1], owners).backward()
        optimizer.step()
        for before, after in zip(original, base.parameters()):
            torch.testing.assert_close(before, after, rtol=0, atol=0)
        base.requires_grad_(True)
        model.zero_grad()
        fit_belief.belief_loss(model(x)[1], owners).backward()
        self.assertGreater(float(base.layers[0].weight.grad.abs().sum()), 0)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'network.bin'
            fit.write_network(base, path)
            restored = fit.read_network(path, 1)
            torch.testing.assert_close(model(x)[0], restored(x), rtol=0.01, atol=0.001)
            self.assertEqual(path.stat().st_size, 32 + 2 * (600 * 8 + 8 + 8 * 32 + 32))


if __name__ == '__main__':
    torch.set_num_threads(1)
    unittest.main()
