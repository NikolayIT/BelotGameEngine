# Human-like play: why the Master looked foolish, and the fix

September 30, 2026.

## The complaint

People who played the September 29 Master (neural actor + belief-weighted
five-trick endgames, "belief5-v2-ensemble") found it much weaker than its
ratings. It threw a high card to a trick the opponents were winning when a low
card would do, and then lost a later trick for it. It never used the partner
signals every Belot player uses ("discard the suit you do not want led").

## Measuring it: `audit`

`dotnet NeuralTrainer.dll audit --player master --opponent neural-master --pairs 300`
plays mirrored whole matches through `BelotMatch`, replays every deal from the
record with all four hands, and flags the choices a strong player calls
mistakes. It then checks each one in hindsight: a double-dummy solve (raw card
points) of the rest of the deal after the card played and after the natural
card. It also reports the auction (contracts, how often they fail, doubles) and
the conventions: how often a free discard (the opponents hold the trick, a suit
without honours was available) comes from a suit with no honour in it, and how
often a player leads a non-master card of a suit its partner threw away.
`--deals N` prints N complete deals for reading by hand.

The clearest category is the **same-suit donation**: third or fourth seat, the
opponents hold the trick, and a lower card of the same suit loses it just as
well (the ten under an ace with the seven in hand; the ten of trumps under the
jack with the king in hand).

| Player | Tricks 1-3 | Tricks 4-8 |
|---|---:|---:|
| Master (Sept 29) | 6.5-7.6 per 1,000 decisions, +1.2-1.6 raw points each in hindsight | 7.9-16.7 per 1,000, +3.5-4.0 raw points each |
| Belot 2.06 (2001 heuristics) | 0 | 0 |
| SmartPlayer | 1.0 | 0.1 |
| First human-style Master (no early rollouts) | 1.9 | 1.8 (the remaining ones are deliberate: hindsight -2.1) |
| **Final Master** (September 30, with the dominance rule) | **0** | 1.6 (deliberate: hindsight -5.1) |

## Why it happened

Printing the values behind every flagged card showed two causes.

1. **Exact ties in the endgame, broken by card order.** The endgame solver scores
   the deal in rounded game points. When the contract is already made or already
   lost, or the rounding swallows a few points, several cards have *exactly* the
   same value (`10♦ -18.00, K♦ -18.00`). `NeuralEvaluator.Best` then takes the
   lowest card index, and in deck order the nine comes before the ten and the ten
   before the king and ace: the tie-break systematically threw the valuable card.
   With perfect information in every sampled world the solver also "knows" that
   keeping the ten does not matter, while in the real deal it often did.
2. **Noise in the network's values in the first three tricks.** The donation was
   usually preferred by a few hundredths to a few tenths of a game point
   (`Q♣ -8.41, 7♣ -8.57`), well inside the network's error.

Neither cause costs much against other bots, which is why the ratings did not
show it, but both are exactly what a person sees.

## What changed

* `EndgameSearch.RawTieBreak`: results equal in game points are ordered by the
  raw card points the team takes (tricks, belotes, last ten), 1/1024 of a game
  point each, so the solver keeps its points as a person would.
* `HumanPreference` (`Human/`): a Belot player's judgement of every legal card,
  in card points: what the card gives or wins in this trick, what keeping it is
  worth (masters, guarded cards, trumps, belote pairs), giving points to a
  partner's sure trick, throwing from the suit with nothing in it, leading the
  partner's suit and not the one it threw away, drawing trumps as declarers and
  not leading them as defenders, never leading the nine of trumps into the
  declaring partner's jack.
* `ClaudePlayerNeural.HumanStyle`: among the cards valued within a tolerance of
  the best (0.3 game points for network or rollout values, 0.02 for exact
  endgame values, 0.5 for a discard while the opponents hold the trick) the
  player takes the one `HumanPreference` likes most. A one-point tolerance for
  leads was tried and removed (it cost 13 Elo, see below).
* `HumanPreference.Dominated` (`HumanDominance`, on with the human style): before
  the exact endgames, in a trick surely lost, a card is never played when a lower
  card of the same suit with no more points loses the trick as well.
* `NaturalBidding` and networks fine-tuned for it: see "The networks' private
  bidding convention" below.
* Partner signals (`PlaySignals`, `EndgameSearch.PartnerSignalWeight` 0.3): each
  suit the partner threw away while the opponents held the trick makes worlds in
  which it holds that suit's honours (an ace or ten; in all trumps or trumps a
  jack or nine) three times less likely.
* `DoubleMargin` 2: double or redouble only when the network values it two game
  points above every other bid (no measurable cost; it removes doubles people
  find strange).
* **Early-trick search** (the strength): in tricks 1-3 the network's three best
  cards within three game points of its best are played out in up to 32 worlds
  weighted by the learned ownership model (`NeuralSearch.Ownership`); the
  networks play every seat to the last five tricks, which are then solved
  exactly (`SearchDoubleDummyTricks` 5 with a transposition-table leaf solver).
  Later tricks keep the belief-weighted five-trick endgame, with a larger budget
  (256 worlds, 1.5 million nodes, 24 ms).
* The Expert is the fast profile choosing the most natural card within five game
  points of the best: it is weaker than the Master by design, but never by an
  absurd card, and no longer by random choices. Hints use the fast profile with
  the human style.

## Development results (mirrored whole matches, one standard error)

| Change | Opponent | Games | Result |
|---|---|---:|---|
| Human style alone | Sept 29 Master | 6,000 | 49.8% +/- 0.4 pp: no cost, no gain |
| Wider network tolerance (0.8) | human style (0.3) | 4,000 | 48.65% +/- 0.5 pp |
| Pure heuristic card play (network bids) | SmartPlayer | 3,000 | 49.0% +/- 0.8 pp |
| Reading partner signals (0.3) | human style | 4,000 | 50.3% +/- 0.4 pp |
| ... with convention-first discards (0.5) | human style | 4,000 | 50.0% +/- 0.4 pp |
| Doubling margin 2 | human style | 4,000 | 49.9% +/- 0.5 pp |
| No doubling | Sept 29 Master | 4,000 | 50.3% +/- 0.5 pp |
| SmartPlayer's / ISMCTS's bids with human-style play | human style | 2,000 / 800 | 39.4% / 43.75%: the bid network is strong |
| Endgame budget 40 ms, 256 worlds, 1.5M nodes | Sept 29 Master | 4,000 | 51.1% +/- 0.4 pp |
| Six-trick endgames (40 ms) | five-trick (40 ms) | 2,000 | 49.25% +/- 0.7 pp |
| Tricks 1-3 rollouts, networks to the end, uniform worlds | human style | 800 | 43.5% +/- 1.2 pp |
| ... ownership-weighted worlds | human style | 800 | 48.75% +/- 1.3 pp |
| ... networks to the last five tricks, then exact | human style | 3,800 | 51.2% (52.5 +/- 1.2 on 800, 50.9 +/- 0.65 on 3,000) |
| ... only the network's best three within 3 points | human style | 3,000 | **54.1% +/- 0.64 pp** |
| Match equity at the leaves | the same | 3,000 | 50.5% +/- 0.6 pp (-4.2 points a game): not adopted |
| The same network filter in the endgames | the same | 2,400 | 47.8% +/- 0.7 pp: the exact endgame knows better |
| Rollouts through trick 4, endgames from trick 5 | the same | 2,000 | 51.05% +/- 0.8 pp: not adopted |

The pure heuristic player is only SmartPlayer's equal even with the network's
bids: thousands of lines of hand-written strategy would not have beaten the
neural player. What works is search plus a human's judgement where the search
cannot tell the cards apart.

## Conventions, measured

Share of free discards from a suit with no honour when such a suit was
available, and share of leads of a non-master card of a suit the partner threw
away when another suit could be led (400 games against the Sept 29 Master):

| Player | Conventional discards | Leads into the partner's thrown suit |
|---|---:|---:|
| Sept 29 Master | 76.4% | 13.1% |
| Human style | 81.8% | 11.0% |
| Human style + signal tolerances | 83.3% | 9.9% |

## The Expert

The September 29 Expert weakened the fast profile with randomness: temperature
1.5 and maximum regret 4 picked any card up to four game points below the best.
That is exactly how a player looks foolish: it made **56 same-suit donations per
1,000 decisions in the first three tricks**. The new Expert is the fast profile
with the human style and a five-point tolerance: among the cards valued within
five points of the best it plays the most natural one, and it bids the network's
best bid (doubling with the two-point margin). 600 games against the old Expert:

| | New Expert | Old Expert |
|---|---:|---:|
| Same-suit donations, tricks 1-3 | 1.4 per 1,000 | 56.2 per 1,000 |
| Conventional discards | 83.5% | 67.2% |
| Leads into the partner's thrown suit | 7.6% | 12.8% |

Tolerance calibration against the old Expert (6,000 games at 5 points, 3,000
otherwise): 4 points +53 Elo, **5 points 53.1% +/- 0.55 pp (+22 Elo)**, 6 points
+3, 10/8 points -44. The five-point Expert scores 75.0% +/- 0.5 pp against
SmartPlayer (the old one 74.1%): a small step up, far below the Master.

## The networks' private bidding convention

The auction statistics of `audit` showed the largest difference from human play.
Of the Sept 29 Master's suit bids, **30% held neither the suit's jack nor its
nine** in the five cards it bid on (Belot 2.06: 1.2%; SmartPlayer: 0.4%;
SharpBelot: 8%); 9% of its no-trumps bids held no ace and 5% of its all-trumps
bids no jack (Belot 2.06: 1.8% and 0.5%). The examples explain it: in self-play
the networks invented a relay. Clubs, the cheapest bid, says "I have a strong
hand" (`Clubs on Q♣ 10♦ A♦ 10♥ A♠`, `Clubs on J♦ J♥ A♥ 8♠ 9♠`), and the partner,
the same network, answers with no trumps or all trumps. Between two copies of
the network this is worth something; with a person as partner it is a disaster,
because the person reads clubs as clubs. It is probably the main reason the bot
felt far weaker to play with than its ratings: every rating was measured with a
partner that shared the convention.

`NaturalBidding` (`Human/NaturalBidding.cs`) allows only bids a person can read:
a suit with its jack or nine and another card of it, no trumps with an ace, all
trumps with a jack. With the networks as trained, the filter costs **27 Elo**
between bots (46.1% +/- 0.56 pp over 4,000 games against the human style
without it): the value of the private convention, which the partner's answers
also relied on. The networks are therefore fine-tuned in self-play in which every
seat bids naturally (`train --natural-bidding true`), so that both the bids and
the answers to a partner's bids learn their natural meaning.

### What the relay costs a human partner

A bot's rating against other bots cannot show this: its partner in those games
shares the convention. So each network is also measured with a partner that bids
and reads bids naturally: Belot 2.06 or SmartPlayer, standing in for a person.
Both teams have the same stand-in partner; only the network differs (the bare
networks, no search; 10,000 games each; `arena --player "A&partner"`):

| Team with a natural partner | vs the relay networks with the same partner |
|---|---:|
| Natural fine-tune (60 min) + Belot 2.06 | **62.0% +/- 0.4 pp (+85 Elo)** |
| Natural fine-tune (60 min) + SmartPlayer | **63.9% +/- 0.4 pp (+99 Elo)** |
| Original networks with the natural filter only + Belot 2.06 | 53.9% +/- 0.3 pp (+27 Elo) |

Between bots with shared conventions the relay is worth something (the bare
fine-tuned networks score 48.4% +/- 0.4 pp against the relay networks after 60
minutes, 47.7% after 30, and the filter alone 45.9%). As the partner of someone
who bids naturally it is worth about -85 to -99 Elo. The fine-tune matters: the
filter alone recovers only a third of that, because the networks must also read
their partner's natural bids naturally.

### The natural-bidding fine-tune

`train --in <Sept 29 networks> --natural-bidding true --hours 2.5 --actors 15
--learners 4 --learning-rate 3e-5 --final-learning-rate 5e-6 --bid-label-chance 1
--card-label-chance 0.25 --pool-chance 0 --seed 1301` (all four networks, every
seat restricted to natural bids, in the deals and in the labels' rollouts; 9.3
million deals at 1,040 a second). The bare networks against the Sept 29 networks
with their relay, 10,000 games each (seed 10001):

| Fine-tune | Score | Elo |
|---|---:|---:|
| none (natural filter only) | 45.9% +/- 0.36 pp | -29 |
| 30 minutes | 47.7% +/- 0.40 pp | -16 |
| 60 minutes | 48.4% +/- 0.40 pp | -11 |
| 90 minutes | 48.7% +/- 0.40 pp | -9 |
| 150 minutes (final) | **49.1% +/- 0.41 pp** | **-6** |

Against SmartPlayer the final networks score 88.0% (600 games), as the Sept 29
networks do (88.5%). The ownership networks were then refitted on 100,000 deals
of natural-bidding self-play by the fine-tuned networks (the same recipe as the
CE12 models: 12 epochs, `fit_ownership.py`); their held-out quality matches the
originals (all trumps: masked NLL .922, 51.6% owner accuracy).

### The full Master on the natural networks

With the new networks and ownership, the full human-style Master (early-trick
rollouts, larger endgame, conventions) against the Sept 29 Master rebuilt from the
frozen files (`neural-master+natural=0+own=<sept29-ownership> --opponent-in
<sept29-weights>`), and diagnostics, 2,000-4,000 games each:

| Match | Score |
|---|---:|
| New Master vs Sept 29 Master | 50.55% +/- 0.91 pp |
| ... with the Sept 29 ownership model on the new side | 51.05% +/- 0.91 pp |
| Human style without rollouts, new vs old networks and ownership | 49.4% +/- 0.6 pp |
| New Master vs the same networks without rollouts | 51.75% +/- 0.80 pp |
| **New Master + Belot 2.06 vs Sept 29 Master + Belot 2.06** | **64.55% +/- 0.90 pp (+104 Elo)** |

The ownership model was not the cause of the small head-to-head margin. The
ablations found it: the one-point lead tolerance (the preference choosing leads
within a point of the best, even in the endgames) cost 13 Elo.

| Ablation of the candidate Master (2,000 games each) | Score |
|---|---:|
| No lead tolerance | 51.85% +/- 0.77 pp: removed from the profile |
| No lead or discard tolerance and no signal reading | 51.35% +/- 0.72 pp |
| No time cap on the rollouts | 49.60% +/- 0.76 pp |
| Candidates chosen by the suit ensemble | 50.70% +/- 0.74 pp: not adopted |

## Final validation (September 30, fresh seeds, otherwise idle machine)

The Master as shipped (`ClaudePlayerProfiles.CreateMaster()`: embedded natural-bidding
networks and refitted ownership, early-trick candidate rollouts, 256-world endgames,
human style without the lead tolerance) against the September 29 Master rebuilt
from the frozen files, the other bots, and the Expert (`validate.sh` in the session
notes; mirrored whole matches, one standard error across pairs):

| Match | Games | Score | Points a game | Elo |
|---|---:|---:|---:|---:|
| Master vs Sept 29 Master | 10,000 | **51.71% +/- 0.41 pp** (95% [50.91, 52.51]) | +3.3 | +12 |
| Master + Belot 2.06 vs Sept 29 Master + Belot 2.06 | 4,000 | **64.78% +/- 0.64 pp** | +23.0 | +106 |
| Master vs ISMCTS 100 ms | 1,000 | 63.60% +/- 1.30 pp (Sept 29 Master: 62.0%) | +19.2 | +97 |
| Master vs Belot 2.06 | 2,000 | 79.50% +/- 0.83 pp | +64.9 | +235 |
| Master vs SharpBelot | 2,000 | 90.40% +/- 0.63 pp | +69.8 | +390 |
| Master vs SmartPlayer | 2,000 | 93.65% +/- 0.53 pp | +75.1 | +467 |
| Expert vs Sept 29 Expert | 6,000 | 52.27% +/- 0.57 pp | +4.1 | +16 |
| Expert vs SmartPlayer | 6,000 | 74.32% +/- 0.52 pp (Sept 29 Expert: 74.1%) | +34.4 | +185 |

Audit of 600 games against the Sept 29 Master:

| | Master | Sept 29 Master |
|---|---:|---:|
| Suit bids without the suit's jack or nine | 0 of 2,972 | 1,336 of 4,644 (29%) |
| No trumps without an ace / all trumps without a jack | 0 / 0 | 119 / 70 |
| Doubles (won) | 53 (79%) | 695 (72%) |
| Conventional free discards | 81.0% | 76.7% |
| Leads into the partner's thrown suit | 9.7% | 11.3% |
| Same-suit donations, tricks 4-8 | 1.4 per 1,000 (hindsight -5.8 raw points: deliberate) | 15.7 per 1,000 (+4.9) |
| Same-suit donations, tricks 1-3 | 19.7 per 1,000 (+0.5) | 7.4 per 1,000 (+2.4) |

The early-trick rollouts reintroduced donations in the first three tricks: their
noise can favour the higher of two losing cards. They cost little in hindsight but
look exactly like the original complaint, so `HumanPreference.Dominated` now keeps
them out before the exact endgames: in a trick surely lost, a card of the suit
that loses as well as a lower one with no more points is never played. With it
the audit finds **no** same-suit donations in tricks 1-3 (1.6 per 1,000 in tricks
4-8, all deliberate: hindsight -5.1 raw points). It costs nothing: the Master with
the rule scores 49.80% +/- 0.60 pp against the Master without it over 3,000 games
(-1 +/- 4 Elo), so it stays on (`HumanDominance`).

Idle desktop timing of the Master (100 games, warmup excluded): 13.1 ms a card on
average, p95 30.8 ms, p99 40.1 ms, maximum 44.5 ms (the September 29 Master:
1.4 ms mean, 8 ms maximum). Both searches have wall-clock caps (40 ms for the
rollouts, at least twelve deals; 24 ms for the endgame), so a slower phone plays
fewer deals and worlds rather than taking longer; the app's thinking pauses
(200-900 ms) hide the time.

## Confirmation with the shipped binary

The Master exactly as shipped (with the dominance rule), new seeds, otherwise idle
machine (`final2.sh` in the session notes):

| Match | Games | Score | Points a game | Elo |
|---|---:|---:|---:|---:|
| Master vs Sept 29 Master | 5,000 | **52.14% +/- 0.57 pp** (95% [51.03, 53.25]) | +3.0 | +15 +/- 4 |
| Master vs the former 100-rollout Master (`CreateRolloutMaster`, Sept 29 networks) | 600 | **60.83% +/- 1.74 pp** (95% [57.42, 64.25]) | +13.3 | +76 +/- 13 |
| Master + Belot 2.06 vs Sept 29 Master + Belot 2.06 | 2,000 | **64.65% +/- 0.89 pp** (95% [62.91, 66.39]) | +23.0 | +105 +/- 7 |

## The app's ratings

`elo 20000 60 3000` (September 30, 1:26:04; pair against pair, mirrored, Bradley-Terry
anchored at Dummy = 1200; one standard deviation from 1,000 shared-seed bootstrap
samples). The fast levels played 20,000 pairs a matchup, the Master 3,000 and ISMCTS 60:

| Level | Rating | Sept 29 | Games |
|---|---:|---:|---:|
| Master (human style) | **1886 +/- 4.6** | 1838 | 24,120 |
| ClaudePlayerIsmcts (reference) | 1786 +/- 17.6 | 1758 | 600 |
| Expert (human style) | **1647 +/- 2.5** | 1611 | 126,120 |
| Skilled (SmartPlayer) | 1490 +/- 1.9 | 1462 | 126,120 |
| Beginner (DummyPlayer) | 1200 (anchor) | 1200 | 126,120 |
| Random | 646 +/- 3.2 | 669 | 126,120 |

Matchups: the Master beats SmartPlayer 93.97% +/- 0.30 pp, the Expert 77.38% +/- 0.46 pp
and ISMCTS 66.7% +/- 3.5 pp (120 games); the Expert beats SmartPlayer 74.47% +/- 0.20 pp.
Ratings shift with the whole field (SmartPlayer moved from 1462 to 1490 against
stronger opponents); the gaps are what matter: Master - Expert 239 (Sept 29: 227),
Master - SmartPlayer 396 (376).

## Deterministic hosts

A host that needs the same decision from the same view and random state regardless of
scheduling (the ednaigra.com level 6 runs `CreateMaster()` with
`EndgameTimeLimitMilliseconds = 0`) must now also set `SearchTimeLimitMilliseconds = 0`:
the early-trick rollouts have their own wall-clock cap. With both off the Master does
fixed work (32 rollout deals, 256 endgame worlds, 1.5 million nodes);
`HumanMasterTests` checks that it then decides the same from the context and from a
view. Idle desktop timing of that fixed-work Master (`bench --player
"master+ms=0+sms=0" --bench-games 50`, 10,657 cards): **14.2 ms a card on average**,
p50 13.1 ms, p95 32.8 ms, p99 40.8 ms, maximum 65.2 ms. The September 29 Master took
about 1.5 ms, so a host with a tight per-move budget should measure first.
