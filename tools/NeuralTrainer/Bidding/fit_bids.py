"""Fits the learned bidding (LearnedBidding.cs) to bid lab data; see README.md.

For each kind of bid and situation with plenty of data the gain over passing is a ridge regression on
BidFeatures (a point count over the cards and the auction). Rare situations, all trumps over the partner's
no trumps (a rare bid, however many hands could make it) and every double bid by cells instead: the cell's average gain, kept only when the average less 1.5 standard errors clears the
bar (--tau for bids, --double-tau for doubles). Prints the one-step improvement over the policy that
made the data, on held-out deals: each decision changed, everything after it as it was.
"""
import argparse
import time

import numpy as np
import pandas as pd

from bids import KINDS, SLOTS, VALUES, class_name, held_out, load, predict, read_model, choose


def ridge(X, y, alpha, w=None):
    """Weighted least squares with an L2 penalty on every weight but the bias (column 0); unused columns stay 0."""
    w = np.ones(len(y)) if w is None else w
    Xw = X * w[:, None]
    A = X.T @ Xw
    penalty = np.full(X.shape[1], alpha)
    penalty[0] = 0
    used = np.abs(X).sum(axis=0) > 0
    penalty[~used] = 1e9
    weights = np.linalg.solve(A + np.diag(penalty), Xw.T @ y)
    weights[~used] = 0
    return weights


def cell_stats(cells, gains, w=None):
    """Per cell: the bids, the (weighted) average gain and its standard error, and the average less 1.5 of them."""
    w = np.ones(len(gains)) if w is None else w
    frame = pd.DataFrame({'cell': cells, 'y': gains, 'w': w, 'wy': w * gains, 'wyy': w * gains * gains})
    sums = frame.groupby('cell').agg(count=('y', 'size'), w=('w', 'sum'), wy=('wy', 'sum'), wyy=('wyy', 'sum'))
    stats = pd.DataFrame(index=sums.index)
    stats['count'] = sums['count']
    stats['mean'] = sums.wy / sums.w
    variance = (sums.wyy / sums.w - stats['mean'] ** 2).clip(lower=0) * sums['count'] / (sums['count'] - 1).clip(lower=1)
    stats['sem'] = np.sqrt(variance / sums['count'])
    stats.loc[sums['count'] < 2, 'sem'] = 99
    stats['low'] = stats['mean'] - 1.5 * stats['sem']
    return stats


def fit(feats, train, alpha, tau, double_tau, rare, log=print, sample_weights=None, by_cells=('at:partner-NT',)):
    """The model's lines (the trainer's text format). by_cells: kind:situation pairs always decided by cells."""
    lines = []
    sample_weights = np.ones(len(feats)) if sample_weights is None else sample_weights
    X_all = feats['f'].astype(np.float64)
    y_all = feats['y'].astype(np.float64)
    cells = feats['cell'].astype(np.int64)
    for kind in range(len(KINDS)):
        for situation in range(16):
            sel = (feats['kind'] == kind) & (feats['cls'] == situation) & train
            if sel.sum() < 200:
                continue
            name = class_name(situation)
            stats = cell_stats(cells[sel], y_all[sel], sample_weights[sel])
            if kind != 3 and sel.sum() >= rare and f'{KINDS[kind]}:{name}' not in by_cells:
                weights = ridge(X_all[sel], y_all[sel], alpha, sample_weights[sel])
                lines.append(f'{KINDS[kind]} {name} ' + ' '.join(f'{v:.5g}' for v in weights))
                log(f'{KINDS[kind]:6} {name:13} points over {sel.sum()} bids, average gain {y_all[sel].mean():+.2f}')
                continue
            bar = double_tau if kind == 3 else tau
            good = stats[(stats['count'] >= 30) & (stats['low'] >= bar)]
            for cell, row in good.iterrows():
                lines.append(f'cell {KINDS[kind]} {name} {cell} {row["mean"]:.4g}')
            if len(good) == 0:
                lines.append(f'gate {KINDS[kind]} {name} 0')
            log(f'{KINDS[kind]:6} {name:13} by cells over {sel.sum()} bids: '
                + (', '.join(f'{c}: {r["mean"]:+.1f} (n={int(r["count"])})' for c, r in good.iterrows()) or 'never'))
    return lines


def one_step(model_text, decisions, feats, rows, mask, margin=0.0):
    """Game points a deal-leg the model's choices gain over the data's own choices, on the masked decisions."""
    values = decisions[VALUES].to_numpy(dtype=float)
    chosen = np.array([SLOTS.index(c) for c in decisions.chosen])
    gain = predict(read_model(model_text), feats, rows, len(decisions))
    choice = choose(gain, values, chosen, margin)
    index = np.arange(len(decisions))
    delta = values[index, choice] - values[index, chosen]
    report = {}
    for source in [None] + list(decisions.source.unique()):
        m = mask if source is None else mask & (decisions.source.to_numpy() == source)
        legs = len(decisions[m][['source', 'deal', 'leg']].drop_duplicates())
        report[source or 'all'] = (np.nansum(delta[m]) / max(1, legs), (choice != chosen)[m].mean())
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--out', required=True, help='the model text to write')
    parser.add_argument('--alpha', type=float, default=30)
    parser.add_argument('--tau', type=float, default=0, help="a rare situation's cell bids when its gain clears this")
    parser.add_argument('--double-tau', type=float, default=1)
    parser.add_argument('--rare', type=int, default=60000, help='fewer training bids than this: bid by cells')
    parser.add_argument('--all', action='store_true', help='fit on every deal (default: hold out 20%%)')
    parser.add_argument('--heavy', default='', help='lab files whose name contains this count --heavy-weight times')
    parser.add_argument('--heavy-weight', type=float, default=1)
    parser.add_argument('--report', default='', help='report the one-step improvement only on lab files whose name contains this')
    parser.add_argument('data', nargs='+', help='bid lab prefixes (with .bids.csv and .feat)')
    args = parser.parse_args()
    start = time.time()
    decisions, feats, rows = load(args.data)
    test_rows = held_out(decisions.deal.to_numpy())
    train = np.ones(len(feats), dtype=bool) if args.all else ~test_rows[rows]
    print(f'{len(decisions)} decisions, {len(feats)} bids ({time.time() - start:.0f} s)')
    heavy = np.array([bool(args.heavy) and args.heavy in source for source in decisions.source])
    sample_weights = np.where(heavy[rows], args.heavy_weight, 1.0)
    lines = fit(feats, train, args.alpha, args.tau, args.double_tau, args.rare, sample_weights=sample_weights)
    text = '\n'.join(lines) + '\n'
    with open(args.out, 'w') as handle:
        handle.write(text)
    mask = test_rows if not args.all else np.ones(len(decisions), dtype=bool)
    if args.report:
        mask = mask & np.array([args.report in source for source in decisions.source])
    for source, (points, changed) in one_step(text, decisions, feats, rows, mask).items():
        print(f'one-step improvement {source:>24}: {points:+.3f} game points a deal-leg, {changed:.1%} of decisions changed')


if __name__ == '__main__':
    main()
