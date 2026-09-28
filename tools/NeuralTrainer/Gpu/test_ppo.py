"""PPO math, information separation and checked training-file tests."""

import copy
from pathlib import Path
import struct
import tempfile
from types import SimpleNamespace
import unittest

import numpy as np
import torch

import fit
import ppo


class PpoTests(unittest.TestCase):
    def test_clipped_objective_gradient_matches_finite_differences(self):
        logp = torch.tensor([0.0, np.log(1.4), np.log(0.6), np.log(1.4), np.log(0.6)],
                            dtype=torch.float64, requires_grad=True)
        old = torch.zeros_like(logp)
        advantage = torch.tensor([1.0, 2.0, -3.0, -1.0, 4.0], dtype=torch.float64)
        loss = ppo.clipped_loss(logp, old, advantage, 0.2)
        derivative, = torch.autograd.grad(loss, logp)
        for i in range(len(logp)):
            high, low = logp.detach().clone(), logp.detach().clone()
            high[i] += 1e-6
            low[i] -= 1e-6
            finite = (ppo.clipped_loss(high, old, advantage, 0.2) - ppo.clipped_loss(low, old, advantage, 0.2)) / 2e-6
            self.assertAlmostEqual(float(derivative[i]), float(finite), places=8)
        self.assertEqual(float(derivative[1]), 0)
        self.assertEqual(float(derivative[2]), 0)
        self.assertGreater(float(derivative[3]), 0)
        self.assertLess(float(derivative[4]), 0)

    def test_gae_keeps_seats_separate_and_lambda_one_is_monte_carlo(self):
        values = np.array([0.1, -0.2, 0.4, -0.5], np.float32)
        outcomes = np.array([1, -1, 1, -1], np.float32)
        following = np.array([2, 3, -1, -1])
        np.testing.assert_allclose(ppo.advantages(values, outcomes, following), outcomes - values, atol=1e-7)
        np.testing.assert_allclose(ppo.advantages(values, outcomes, following, 0.5),
                                   [0.6, -0.55, 0.6, -0.5], atol=1e-7)
        with self.assertRaises(ValueError):
            ppo.advantages(values, outcomes, [0, 3, -1, -1])

    def test_masked_policy_uses_point_units_and_ignores_illegal_values(self):
        q = torch.tensor([[1, 1 + 1 / 26, 1000]], dtype=torch.float64, requires_grad=True)
        mask = torch.tensor([[True, True, False]])
        logs = ppo.policy_log(q, mask, 1)
        self.assertAlmostEqual(float(logs[0, 0].detach()), -np.log1p(np.e), places=12)
        self.assertEqual(float(logs[0, 2].detach().exp()), 0)
        (-logs[0, 0]).backward()
        self.assertEqual(float(q.grad[0, 2]), 0)

    def test_reader_checks_boundaries_and_separates_hidden_cards(self):
        header = struct.pack('<7i', ppo.ROLLOUT_MAGIC, 1, 1, 1, 600, 96, 2)
        # Card 7 is held by the teammate relative to this seat; it is not an actor feature.
        one = ppo.RECORD.pack(4, 0, 3, 0, -0.7, 0.5, 0.2, 1, 2 << 14, 2)
        one += struct.pack('<HfHf', 0, 1, 590, 0.1)
        two = ppo.RECORD.pack(4, 0, 3, 1, -0.7, 0.5, 0.3, -1, 3 << 14, 1)
        two += struct.pack('<Hf', 1, 1)
        raw = header + one + two
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'data.ppo'
            path.write_bytes(raw)
            data = ppo.read_rollout(path, 1)
            self.assertEqual(tuple(data.x.shape), (2, 600))
            self.assertEqual(float(data.private[0, 32 + 7]), 1)
            self.assertEqual(float(data.private[1, 64 + 7]), 1)
            self.assertEqual(float(data.x[0, 7]), 0)
            self.assertEqual(float(data.x[0, 590]), np.float32(0.1))
            with self.assertRaises(ValueError):
                ppo.read_rollout(path, 2)
            for bad in (raw[:20], raw[:-1], raw + b'x', raw[:8] + struct.pack('<i', 2) + raw[12:]):
                path.write_bytes(bad)
                with self.assertRaises(ValueError):
                    ppo.read_rollout(path, 1)
            bad = bytearray(raw)
            bad[28 + len(one) + 4] = 1  # The next decision cannot belong to another seat.
            path.write_bytes(bad)
            with self.assertRaises(ValueError):
                ppo.read_rollout(path, 1)

    def test_full_precision_snapshot_and_half_export_have_distinct_checked_formats(self):
        torch.manual_seed(3)
        actor = fit.Network(1, 1, (600, 8, 32))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'actor.f32'
            ppo.write_float_network(actor, path)
            raw = path.read_bytes()
            self.assertEqual(struct.unpack_from('<5i', raw), (ppo.FLOAT_MAGIC, 1, 1, 1, 2))
            weights = np.frombuffer(raw, '<f4', 600 * 8, 32).reshape(600, 8)
            np.testing.assert_array_equal(weights.T, actor.layers[0].weight.detach().numpy())
            with self.assertRaises(ValueError):
                fit.read_network(path, 1)
            fit.write_network(actor, Path(directory) / 'actor.bin')
            restored = fit.read_network(Path(directory) / 'actor.bin', 1)
            self.assertEqual(restored.sizes, (600, 8, 32))

    def test_small_update_is_finite_and_public_control_does_not_mutate_private_inputs(self):
        torch.manual_seed(7)
        torch.set_num_threads(1)
        actor = fit.Network(1, 1, (600, 8, 32))
        x = torch.rand(16, 600)
        hidden = torch.rand(16, 96)
        mask = torch.ones(16, 32, dtype=torch.bool)
        chosen = torch.arange(16)
        with torch.no_grad():
            q = actor(x)
            logs = ppo.policy_log(q, mask, 2).gather(1, chosen[:, None]).squeeze(1)
        data = ppo.Rollout(x, hidden, mask, chosen, logs, torch.linspace(-1, 1, 16),
                           q.gather(1, chosen[:, None]).squeeze(1), np.full(16, -1),
                           np.arange(16), np.zeros(16))
        args = SimpleNamespace(critic='public', batch=8, temperature=2, gae_lambda=1,
                               critic_epochs=2, epochs=2, target_kl=1, clip=0.2,
                               policy_weight=1, q_weight=1, entropy=0.001)
        original_hidden = hidden.clone()
        original_actor = copy.deepcopy(actor.state_dict())
        critic = ppo.Critic()
        stats = ppo.optimize(actor, critic, torch.optim.Adam(actor.parameters(), lr=1e-5),
                             torch.optim.Adam(critic.parameters(), lr=1e-3), data, args, True)
        self.assertTrue(torch.equal(hidden, original_hidden))
        self.assertGreater(stats['actor_batches'], 0)
        self.assertLess(stats['log_parity_max'], 1e-6)
        self.assertTrue(any(not torch.equal(original_actor[k], v) for k, v in actor.state_dict().items()))
        self.assertTrue(all(np.isfinite(v) for v in stats.values()))

    def test_checkpoint_restores_parameters_rng_and_requested_rates(self):
        torch.manual_seed(17)
        actors = [fit.Network(tag, 1, (600, 4, 32)) for tag in (1, 2, 3)]
        critics = [ppo.Critic() for _ in actors]
        actor_optimizers = [torch.optim.Adam(m.parameters(), lr=1e-6) for m in actors]
        critic_optimizers = [torch.optim.Adam(m.parameters(), lr=3e-4) for m in critics]
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'bid.bin').write_bytes(b'frozen bid')
            for actor, name in zip(actors, fit.NAMES[1:]):
                fit.write_network(actor, root / (name + '.bin'))
            args = SimpleNamespace(input=str(root), device='cpu', critic='privileged',
                                   temperature=1, gae_lambda=1, seed=7, warmup=2, deals=32,
                                   threads=2, actor_lr=1e-6, critic_lr=3e-4)
            ppo.checkpoint(root / 'checkpoint', actors, critics, actor_optimizers, critic_optimizers, 4, args)
            expected_random = torch.rand(5)
            expected_weight = actors[0].layers[0].weight.detach().clone()
            with torch.no_grad():
                actors[0].layers[0].weight.zero_()
            args.actor_lr = 3e-6
            iteration = ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                                    actor_optimizers, critic_optimizers, args)
            self.assertEqual(iteration, 4)
            self.assertTrue(torch.equal(torch.rand(5), expected_random))
            self.assertTrue(torch.equal(actors[0].layers[0].weight, expected_weight))
            self.assertEqual(actor_optimizers[0].param_groups[0]['lr'], 3e-6)
            self.assertEqual((root / 'checkpoint/bid.bin').read_bytes(), b'frozen bid')
            args.temperature = 2
            with self.assertRaises(ValueError):
                ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)
            args.temperature = 1
            args.critic_sizes = '512,256,128'
            with self.assertRaisesRegex(ValueError, 'helper architecture'):
                ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)
            args.critic_sizes = '256,128'
            args.opponents = 'smart,sharpbelot,belot206'
            with self.assertRaisesRegex(ValueError, 'Resume changes opponents'):
                ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)
            args.opponents = ''
            args.opponent_chance = 0.5
            with self.assertRaisesRegex(ValueError, 'Resume changes opponent_chance'):
                ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)
            args.opponent_chance = 0.0
            (root / 'bid.bin').write_bytes(b'changed bid')
            with self.assertRaisesRegex(ValueError, 'source weights changed'):
                ppo.restore(root / 'checkpoint/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)

            # Frozen neural opponents live in the collector assemblies, independently
            # of the actor's source folder. A changed build must not silently replace them.
            args.opponents = 'neural'
            args.opponent_chance = 1
            args.trainer = str(root / 'NeuralTrainer.dll')
            assembly = root / 'Belot.AI.ClaudePlayer.dll'
            assembly.write_bytes(b'frozen opponent')
            ppo.checkpoint(root / 'external', actors, critics, actor_optimizers, critic_optimizers, 4, args)
            ppo.restore(root / 'external/training.pt', actors, critics,
                        actor_optimizers, critic_optimizers, args)
            assembly.write_bytes(b'changed opponent')
            with self.assertRaisesRegex(ValueError, 'opponent assemblies changed'):
                ppo.restore(root / 'external/training.pt', actors, critics,
                            actor_optimizers, critic_optimizers, args)

    def test_helper_capacity_starts_at_the_same_public_value(self):
        torch.manual_seed(19)
        public, private, base = torch.rand(5, 600), torch.rand(5, 96), torch.rand(5)
        for widths in ((256, 128), (512, 256, 128)):
            critic = ppo.Critic(widths)
            self.assertTrue(torch.equal(critic(public, private, base), base))
            self.assertEqual(critic.net[0].in_features, 696)
            self.assertEqual(critic.net[-1].out_features, 1)
        for widths in ((), (0,), (4097,), (32,) * 9):
            with self.assertRaises(ValueError):
                ppo.Critic(widths)


if __name__ == '__main__':
    unittest.main()
