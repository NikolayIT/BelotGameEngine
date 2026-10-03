"""The tables of HEURISTIC_PLAYER.md's bidding section, from bid lab data; see README.md.

  bid_tables.py made PREFIX...      how the subject's own bids did, per lab file (gain over passing on the
                                    same cards, how often worse than passing, failed, doubled)
  bid_tables.py holdings PREFIX...  what each bid gains over passing by the holdings a person looks at first
                                    (every natural bid evaluated, whatever the subject chose), per situation
  bid_tables.py own PREFIX...       the subject's own bids by kind, situation and cell (BidFeatures.Cell),
                                    and every cell where they lost to passing
"""
import sys

import numpy as np
import pandas as pd

from bids import KINDS, RECORD, SLOTS, VALUES, class_name, load

CONTRACT_CODES = {1: 'C', 2: 'D', 4: 'H', 8: 'S', 16: 'N', 32: 'A'}


def parse_auction(bids):
    """Who holds the contract (relative seat, -1 nobody) and its kind, from the lab's auction text."""
    holder, contract = -1, ''
    for i in range(0, len(bids), 2):
        seat, code = int(bids[i]), bids[i + 1]
        if code not in 'PXR':
            holder, contract = seat, code
    side = 'open' if holder < 0 else 'mine' if holder == 0 else 'partner' if holder == 2 else 'opp'
    kind = '-' if not contract else 'suit' if contract in 'CDHS' else contract
    return side, kind


def made(prefixes):
    situations = [
        ('open a suit', 'open', '-', 'suit'),
        ('compete: higher suit over their suit', 'opp', 'suit', 'suit'),
        ('compete: no trumps over their suit', 'opp', 'suit', 'N'),
        ('compete: all trumps over their suit', 'opp', 'suit', 'A'),
        ('compete: all trumps over their no trumps', 'opp', 'N', 'A'),
        ("all trumps over the partner's suit", 'partner', 'suit', 'A'),
        ("all trumps over the partner's no trumps", 'partner', 'N', 'A'),
        ("a suit over the partner's suit", 'partner', 'suit', 'suit'),
        ('double', 'opp', None, 'X'),
    ]
    for prefix in prefixes:
        df = pd.read_csv(prefix + '.bids.csv', dtype={'bids': str, 'chosen': str})
        df['bids'] = df['bids'].fillna('')
        parsed = [parse_auction(b) for b in df.bids]
        df['side'] = [p[0] for p in parsed]
        df['ckind'] = [p[1] for p in parsed]
        values = df[VALUES].to_numpy(dtype=float)
        chosen = np.array([SLOTS.index(c) for c in df.chosen])
        df['gain'] = values[np.arange(len(df)), chosen] - df.v_pass.to_numpy()
        legs = len(df[['deal', 'leg']].drop_duplicates())
        print(f'== {prefix.replace(chr(92), "/").split("/")[-1]}: {len(df)} decisions, {legs} deal-legs')
        for label, side, ckind, bid in situations:
            kind_of = df.chosen.map(lambda c: 'suit' if c in 'CDHS' else c)
            sel = (df.side == side) & (kind_of == bid)
            if ckind is not None:
                sel &= df.ckind == ckind
            g = df[sel]
            if len(g) < 30:
                continue
            own = g[(g.declarer == 0) & np.array([CONTRACT_CODES.get(c & 63) == ch for c, ch in zip(g.contract, g.chosen)], dtype=bool)]
            failed = (own.made == 0).mean() if len(own) else float('nan')
            doubled = ((own.contract & 64) != 0).mean() if len(own) else float('nan')
            se = g.gain.std(ddof=1) / np.sqrt(len(g))
            print(f'  {label:44} {100 * len(g) / legs:5.2f} a 100 deals  gain {g.gain.mean():+6.2f} ± {se:4.2f}'
                  f'  worse than passing {(g.gain < 0).mean():5.1%}  failed {failed:5.1%}  doubled {doubled:5.1%}')


def holdings(prefixes):
    frames = []
    for prefix in prefixes:
        with open(prefix + '.feat', 'rb') as handle:
            count = int(np.frombuffer(handle.read(4), dtype='<i4')[0])
            f = np.frombuffer(handle.read(), dtype=RECORD, count=count)
        F = f['f']
        frames.append(pd.DataFrame({
            'y': f['y'], 'kind': f['kind'], 'situation': [class_name(c) for c in f['cls']],
            'a': F[:, 1], 'b': F[:, 2], 'c': F[:, 3], 'd': F[:, 4], 'e': F[:, 5], 'p10': F[:, 10], 'p11': F[:, 11],
            'f6': F[:, 6], 'f7': F[:, 7], 'f8': F[:, 8], 'f9': F[:, 9],
        }))
    d = pd.concat(frames, ignore_index=True)

    def show(title, rows, keys):
        print(f'== {title}: {len(rows)} bids, average gain over passing {rows.y.mean():+.2f}')
        table = rows.groupby(keys).y.agg(['count', 'mean', 'sem'])
        print(table[table['count'] >= 100].round(2).to_string())

    suits = d[d.kind == 0].assign(jack=lambda x: x.a, nine=lambda x: x.b, length=lambda x: 2 + x.p10 + x.p11)
    for situation in ['open-none', 'partner-suit', 'opp-suit']:
        show(f'a suit, {situation} (its jack, its nine, length)', suits[suits.situation == situation], ['jack', 'nine', 'length'])
    nt = d[d.kind == 1].assign(aces=lambda x: x.a, ace_tens=lambda x: x.d)
    for situation in ['open-none', 'partner-suit', 'opp-suit']:
        show(f'no trumps, {situation} (aces, aces with their tens)', nt[nt.situation == situation], ['aces', 'ace_tens'])
    at = d[d.kind == 2].assign(jacks=lambda x: x.a, jack_nines=lambda x: x.d)
    for situation in ['open-none', 'partner-suit', 'partner-NT', 'opp-suit', 'opp-NT']:
        show(f'all trumps, {situation} (jacks, jacks with their nines)', at[at.situation == situation], ['jacks', 'jack_nines'])
    doubles = d[d.kind == 3]
    their_suit = doubles[doubles.situation == 'opp-suit'].assign(
        jack=lambda x: x.a, top=lambda x: (x.c + x.d).clip(upper=2), trumps=lambda x: x.e, overcalled=lambda x: x.p11)
    show('double their suit (the trump jack, its ace and ten, trumps, whether they overcalled our bid)', their_suit,
         ['overcalled', 'jack', 'top', 'trumps'])
    show('double their no trumps (aces, aces with their tens)', doubles[doubles.situation == 'opp-NT'].assign(
        aces=lambda x: x.f8, ace_tens=lambda x: x.f9), ['aces', 'ace_tens'])
    show('double their all trumps (jacks, nines)', doubles[doubles.situation == 'opp-AT'].assign(
        jacks=lambda x: x.f6, nines=lambda x: x.f7), ['jacks', 'nines'])


def own(prefixes):
    decisions, feats, rows = load(prefixes)
    chosen = np.array([SLOTS.index(c) for c in decisions.chosen])
    values = decisions[VALUES].to_numpy(dtype=float)
    gain = values[np.arange(len(decisions)), chosen] - decisions.v_pass.to_numpy()
    mine = chosen[rows] == feats['slot']
    d = pd.DataFrame({'kind': [KINDS[k] for k in feats['kind'][mine]], 'situation': [class_name(c) for c in feats['cls'][mine]],
                      'cell': feats['cell'][mine], 'gain': gain[rows[mine]]})
    table = d.groupby(['kind', 'situation', 'cell']).gain.agg(['count', 'mean', 'sem'])
    lost = table[(table['count'] >= 30) & (table['mean'] + 1.5 * table['sem'] < 0)]
    print(f'{len(d)} bids in {len(table)} cells; cells where they lost to passing (mean + 1.5 standard errors below 0): {len(lost)}')
    if len(lost):
        print(lost.round(2).to_string())
    print(table[table['count'] >= 200].round(2).to_string())


if __name__ == '__main__':
    {'made': made, 'holdings': holdings, 'own': own}[sys.argv[1]](sys.argv[2:])
