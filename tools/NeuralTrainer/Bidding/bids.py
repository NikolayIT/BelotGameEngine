"""Reading the trainer's bid lab files (see README.md).

`NeuralTrainer bidlab` writes <prefix>.bids.csv: one row per bid decision of the subject, with the game
points each natural bid brought on the same cards (v_pass ... v_xx, for the subject's team: its points
from the deal minus the opponents'). `NeuralTrainer bidfeatures` adds <prefix>.feat: for every bid a row
evaluated (pass and redouble apart) the numbers of BidFeatures.cs, its cell and its gain over passing.
"""
import numpy as np
import pandas as pd

KINDS = ['suit', 'nt', 'at', 'double']
SIDES = ['open', 'mine', 'partner', 'opp']
CONTRACTS = ['none', 'suit', 'NT', 'AT']
SLOTS = ['P', 'C', 'D', 'H', 'S', 'N', 'A', 'X', 'R']
VALUES = ['v_pass', 'v_c', 'v_d', 'v_h', 'v_s', 'v_nt', 'v_at', 'v_x', 'v_xx']
FEATURES = 48
RECORD = np.dtype([('row', '<i4'), ('kind', 'u1'), ('cls', 'u1'), ('slot', 'u1'), ('cell', 'u1'),
                   ('y', '<f4'), ('f', '<f4', (FEATURES,))])

# The names of BidFeatures.cs: the cards' block per kind, then the auction's block (from index 24).
HAND_NAMES = [
    ['bias', 'tJ', 't9', 'tJ9', 'tA', 't10', 'tK', 'tQ', 'tKQ', 'tLow', 'tCount3', 'tCount4', 'sideA', 'sideA10',
     'side10', 'sideAK', 'sideK', 'voids', 'combos', 'singletons'],
    ['bias', 'A', 'A2', 'A3', 'A10', 'T', 'KwithA10', 'KwithOne', 'KQ', 'lenA', 'J', 'voids', 'Q', 'N9'],
    ['bias', 'J', 'J2', 'J3', 'J9', 'lone9', 'bare9', 'AwithJ9', 'Alone', 'TwithHonours', 'KQ', 'lenJ', 'combos',
     'A', 'N9'],
    ['bias', 'tJ', 't9', 'tA', 't10', 'tCount', 'J', 'N9', 'A', 'A10', 'sideA', 'overcalled', 'tJ9', 'tK', 'tQ'],
]
CONTEXT_NAMES = ['pos0', 'pos1', 'pos2', 'pos3', 'partnerSuit', 'partnerNT', 'partnerAT', 'partnerPassed',
                 'oppSuits', 'oppNT', 'oppAT', 'oppPassed', 'mine', 'doubled', 'pJ', 'p9', 'pA', 'p10', 'oJ',
                 'o9', 'oA', 'o10', 'laterTurn', 'oppBids']


def class_name(situation):
    """BidFeatures.ClassName: who holds the contract times its kind, e.g. 'opp-suit'."""
    return SIDES[situation >> 2] + '-' + CONTRACTS[situation & 3]


CLASS_INDEX = {class_name(c): c for c in range(16)}


def feature_names(kind):
    hand = HAND_NAMES[kind] + [f'h{i}' for i in range(len(HAND_NAMES[kind]), 24)]
    return hand + CONTEXT_NAMES


def load(prefixes):
    """The decisions (values and chosen bid) and the bids' features of several lab files, rows joined."""
    frames, records, rows = [], [], []
    offset = 0
    for prefix in prefixes:
        frame = pd.read_csv(prefix + '.bids.csv', usecols=['deal', 'leg', 'chosen'] + VALUES, dtype={'chosen': str})
        frame['source'] = prefix.replace('\\', '/').split('/')[-1]
        with open(prefix + '.feat', 'rb') as handle:
            count = int(np.frombuffer(handle.read(4), dtype='<i4')[0])
            feats = np.frombuffer(handle.read(), dtype=RECORD, count=count)
        frames.append(frame)
        records.append(feats)
        rows.append(feats['row'].astype(np.int64) - 1 + offset)  # the lab's rows are 1-based data lines
        offset += len(frame)
    return pd.concat(frames, ignore_index=True), np.concatenate(records), np.concatenate(rows)


def held_out(deals, share=0.2):
    """Whether a deal is held out of fitting (a fixed hash, the same for every model)."""
    return (np.asarray(deals, dtype=np.int64) * 2654435761 % 1000) < share * 1000


def read_model(path_or_text):
    """The trainer's model text: weights, gates and cell values keyed by (kind, situation)."""
    text = path_or_text if '\n' in path_or_text else open(path_or_text).read()
    weights, gates, cells = {}, {}, {}
    for line in text.splitlines():
        parts = line.split()
        if not parts or parts[0].startswith('#'):
            continue
        if parts[0] == 'gate':
            gates[(KINDS.index(parts[1]), CLASS_INDEX[parts[2]])] = int(parts[3], 16)
        elif parts[0] == 'cell':
            cells.setdefault((KINDS.index(parts[1]), CLASS_INDEX[parts[2]]), {})[int(parts[3])] = float(parts[4])
        else:
            weights[(KINDS.index(parts[0]), CLASS_INDEX[parts[1]])] = np.array([float(x) for x in parts[2:]])
    return weights, gates, cells


def predict(model, feats, rows, decisions):
    """Each evaluated bid's expected gain over passing under the model (-inf where it never bids), as LearnedBidding."""
    weights, gates, cells = model
    gain = np.full((decisions, len(SLOTS)), -np.inf)
    for kind in range(len(KINDS)):
        for situation in range(16):
            sel = (feats['kind'] == kind) & (feats['cls'] == situation)
            if not sel.any():
                continue
            key = (kind, situation)
            if key in cells:
                values = cells[key]
                value = np.array([values.get(int(c), -np.inf) for c in feats['cell'][sel]])
            elif key in weights:
                value = feats['f'][sel].astype(np.float64) @ weights[key]
                if key in gates:
                    allowed = np.array([(gates[key] >> int(c)) & 1 for c in feats['cell'][sel]], dtype=bool)
                    value[~allowed] = -np.inf
            elif key in gates:
                value = np.full(sel.sum(), -np.inf)
            else:
                continue
            gain[rows[sel], feats['slot'][sel]] = value
    return gain


def choose(gain, values, chosen, margin=0.0):
    """The bid the model makes at each decision (redoubles left as they were: the written rule makes them)."""
    best = np.where(np.isnan(values), -np.inf, gain)
    best[:, 0] = margin
    best[:, 8] = -np.inf
    choice = np.argmax(best, axis=1)
    choice[chosen == 8] = 8
    return choice
