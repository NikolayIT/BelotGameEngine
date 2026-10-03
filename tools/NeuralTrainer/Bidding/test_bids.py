"""Tests of the bidding tools: python -m unittest discover -s tools/NeuralTrainer/Bidding -v"""
import unittest

import numpy as np

from bids import RECORD, FEATURES, KINDS, class_name, choose, predict, read_model
from embed_model import render
from fit_bids import fit, ridge


def records(kind, situation, cells, gains, features=None):
    out = np.zeros(len(gains), dtype=RECORD)
    out['row'] = np.arange(1, len(gains) + 1)
    out['kind'] = kind
    out['cls'] = situation
    out['slot'] = 6 if kind == 2 else 7 if kind == 3 else 1
    out['cell'] = cells
    out['y'] = gains
    if features is not None:
        out['f'] = features
    out['f'][:, 0] = 1
    return out


class RidgeTests(unittest.TestCase):
    def test_recovers_a_linear_rule_and_leaves_unused_numbers_at_zero(self):
        rng = np.random.default_rng(1)
        X = np.zeros((5000, FEATURES))
        X[:, 0] = 1
        X[:, 1] = rng.integers(0, 2, 5000)
        X[:, 2] = rng.integers(0, 3, 5000)
        y = -2 + 3 * X[:, 1] + 0.5 * X[:, 2]
        w = ridge(X, y, 1e-6)
        self.assertAlmostEqual(w[0], -2, places=4)
        self.assertAlmostEqual(w[1], 3, places=4)
        self.assertAlmostEqual(w[2], 0.5, places=4)
        self.assertTrue(np.all(w[3:] == 0))


class FitTests(unittest.TestCase):
    def test_rare_situations_bid_only_in_cells_that_clearly_pay(self):
        situation = 10  # partner-NT
        spread = np.tile([20.0, -20.0], 200)  # a standard error of 1 game point over 400 bids
        cells = np.repeat([1, 3, 8], 400)
        gains = np.concatenate([-14 + spread, 1 + spread, 9 + spread])
        feats = records(2, situation, cells, gains)
        lines = fit(feats, np.ones(len(feats), dtype=bool), 30, 0, 1, 60000, log=lambda _: None)
        cells_kept = sorted(int(line.split()[3]) for line in lines if line.startswith('cell at partner-NT'))
        self.assertEqual([8], cells_kept)  # cell 3's +1 is within 1.5 standard errors of 0

    def test_a_situation_with_no_good_cell_never_bids(self):
        feats = records(3, 15, np.zeros(500, dtype=int), np.full(500, -30.0))
        lines = fit(feats, np.ones(500, dtype=bool), 30, 0, 1, 60000, log=lambda _: None)
        self.assertEqual(['gate double opp-AT 0'], lines)


class ModelTests(unittest.TestCase):
    def test_the_text_round_trips_and_predicts_like_the_player(self):
        weights = ' '.join(['-1', '2'] + ['0'] * (FEATURES - 2))
        model = read_model(f'suit open-none {weights}\ncell at partner-NT 3 2.5\ngate double opp-AT 0\n')
        self.assertIn((0, 0), model[0])
        self.assertEqual(2.5, model[2][(2, 10)][3])
        self.assertEqual(0, model[1][(3, 15)])
        f = np.zeros((2, FEATURES))
        f[:, 0] = 1
        f[1, 1] = 1
        feats = records(0, 0, np.zeros(2, dtype=int), np.zeros(2), f)
        gain = predict(model, feats, feats['row'].astype(np.int64) - 1, 2)
        self.assertEqual(-1, gain[0, 1])
        self.assertEqual(1, gain[1, 1])

    def test_a_bid_needs_more_than_the_margin_and_redoubles_stay(self):
        gain = np.array([[0, 0.4, -np.inf, -np.inf, -np.inf, -np.inf, 1.2, -np.inf, -np.inf],
                         [0, 0.4, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf],
                         [0, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf, -np.inf]])
        values = np.zeros((3, 9))
        chosen = np.array([0, 0, 8])
        self.assertEqual([6, 0, 8], list(choose(gain, values, chosen, margin=0.5)))

    def test_the_embedded_file_keeps_every_line(self):
        weights = ' '.join(['-1.23456', '2'] + ['0'] * (FEATURES - 2))
        text = render(f'suit open-none {weights}\ncell at partner-NT 3 2.5\n', 'Test.')
        self.assertIn('suit open-none -1.235 2 0', text)
        self.assertIn('cell at partner-NT 3 2.5', text)
        self.assertIn('Test.', text)

    def test_situation_names_match_the_player(self):
        self.assertEqual('open-none', class_name(0))
        self.assertEqual('partner-NT', class_name(10))
        self.assertEqual('opp-AT', class_name(15))
        self.assertEqual(['suit', 'nt', 'at', 'double'], KINDS)


if __name__ == '__main__':
    unittest.main()
