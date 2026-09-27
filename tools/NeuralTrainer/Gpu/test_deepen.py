"""Function-preserving depth, export and trainability checks."""

from pathlib import Path
import tempfile
import unittest

import torch

import deepen
import fit


class DeepenTests(unittest.TestCase):
    def test_preserves_exported_function_and_new_layers_can_learn(self):
        torch.manual_seed(9711)
        original = fit.Network(1, 1, (600, 16, 8, 32))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'source.bin'
            fit.write_network(original, path)
            original = fit.read_network(path, 1)
            model = deepen.deepen(original, 2)
            self.assertEqual(model.sizes, (600, 16, 8, 8, 8, 32))
            x = torch.randn(64, 600)
            torch.testing.assert_close(model(x), original(x), rtol=0, atol=0)
            fit.write_network(model, path)
            restored = fit.read_network(path, 1)
            torch.testing.assert_close(restored(x), original(x), rtol=0, atol=0)
            restored(x).square().mean().backward()
            for layer in restored.layers[2:-1]:
                self.assertGreater(float(layer.weight.grad.abs().sum()), 0)
                self.assertGreater(float(layer.bias.grad.abs().sum()), 0)

    def test_refuses_missing_relu_or_unsupported_depth(self):
        with self.assertRaises(ValueError):
            deepen.deepen(fit.Network(1, 1, (600, 32)), 1)
        model = fit.Network(1, 1, (600, 8, 32))
        for depth in (-1, 0, 15):
            with self.assertRaises(ValueError):
                deepen.deepen(model, depth)


if __name__ == '__main__':
    torch.set_num_threads(1)
    unittest.main()
