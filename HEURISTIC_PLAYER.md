# The heuristic player: belot.bg's rules, then exact endgames

October 1, 2026; the bidding learned from how every bid did, October 3.

`ClaudePlayerHeuristic` (`src/AI/Belot.AI.ClaudePlayer`, rules in `Heuristic/`) plays by written
Belot advice: no networks, no Monte Carlo search in the early tricks. Every bid and every card of
the first tricks comes from rules taken from the belot.bg academy and the other sources below,
over what a careful player remembers of the cards. Its second stage keeps the rules for the
first three tricks and plays the last five out exactly over the deals the play allows
(perfect-information minimax per deal with `Neural/EndgameSearch`), the rules choosing among the
cards that do equally well. Since October 3 it bids by a point count fitted to how every bid
did once the deal was over (see Bidding); every bid is still natural.

- `ClaudePlayerProfiles.CreateRulesOnly()`: the rules alone (trainer catalog name `heuristic`),
  5 µs a card.
- `ClaudePlayerProfiles.CreateHeuristic()`: the rules and five-trick exact endgames (catalog name
  `heuristic-endgame`), 0.83 ms a card on average, at most 5.9 ms measured (the limit asked for
  was 10 ms).

Against every other bot (mirrored pairs of whole matches, rules and exact endgames; the full
tables are under Results):

| Opponent | Learned bidding (October 3) | Written bidding (October 1) |
| --- | ---: | ---: |
| SmartPlayer | 89.2% | 87.7% |
| Belot 2.06 (the 2001 program) | 74.0% | 72.3% |
| The app's Expert | 65.5% | 61.9% |
| ISMCTS, 100 ms a card | 53.0% (200 games) | 47.5% (200 games) |
| The app's Master | 34.3% | 30.8% |

## Sources

- The belot.bg academy, all twenty articles. Basics: card strength, scoring, bidding and
  declarations, terms, the most common beginners' mistakes, how not to look like a novice.
  Advanced: how to become a better player, how to follow the cards, what to discard, drawing
  trumps, the first seat ("под ръка"), the finer points of the trump suit, when to double, how
  to make the opponents capot, how the score affects the bidding, the last ten, all trumps, no
  trumps.
- belot.bg blog: how to read the partner's signals; when to pass and when to risk.
- "Тактика при белот" (6ef4et0.blogspot.com, 2009): bidding minimums, competing, long suits
  and entries, stoppers, unblocking, signals, card reading. The richest single source.
- "Ръководство по белот" (honorofwriting.blogspot.com, 2011): signals, not trumping the
  partner's trick, the defence leading trumps only with four.
- advice.bg, "Как се играе Белот - основни правила и трикове".
- French belote and coinche practice (ludicash.com: leads, calls, the belote signal), where it
  agrees with the Bulgarian sources.

## What it remembers (`CardMemory`)

Every card played and by whom; the suits each player has shown out of and the higher cards a
player has shown not to hold (`RoundKnowledge`: not following, not trumping, not overtrumping,
following below the best card in all trumps or with trumps led); the belote and carre cards
declared; the trumps still out; which cards are masters; the bids; the partner's signals
(cards thrown while the opponents held the trick, cards given to this player's trick, suits
led). The chance that a seat holds an unseen card treats every placement the play allows
alike, except that a seat that bid a suit is taken to hold its jack and nine three times as
often (the academy's "how to follow the cards": what the bids said).

## Bidding: learned from how every bid did (`LearnedBidding`)

October 3, 2026. The first bidding (the written point counts below) took the sources' advice
as it was written. Playing it, people found it too eager: all trumps over the partner's no
trumps with ordinary cards, a suit bid only to get above the opponents. Is that a risk worth
taking or not? The trainer's `bidlab` command measures it instead of arguing about it: it
plays first deals of matches and, at every bid of the player, replays the deal from the start
with **each other natural bid it could have made there**, everybody deciding as usual
afterwards, to the end of the deal. Every bid's value is then known on the same cards: the game
points it brought less what passing would have brought. That is how the bids were judged and
how the new bidding was fitted: 8.1 million decisions with 13.7 million replayed alternatives
with the rules' card play, against the player's own bidding, the October 1 bidding, Belot 2.06,
SmartPlayer and the neural Fast player (which doubles whenever its networks see a profit), and
with Belot 2.06 as the partner (a natural bidder, as a person is), then 0.77 million decisions
with the exact endgames. The scripts are in `tools/NeuralTrainer/Bidding` (README there).

### What the numbers say

Game points a bid brought over passing on the same cards, on average (± one standard error).

**Competing over the opponents' suit pays; it is not too risky.** The October 1 bidding's
competitive suit bids (a higher suit over the opponents' suit contract), rules-only play:

| Opponents | Bids a 100 deals | Gain over passing | Worse than passing | Contract failed | Doubled |
| --- | ---: | ---: | ---: | ---: | ---: |
| Itself | 15.7 | +3.61 ± 0.08 | 37% | 25% | 0.0% |
| Belot 2.06 | 5.3 | +5.66 ± 0.14 | 34% | 24% | 0.4% |
| SmartPlayer | 7.1 | +3.68 ± 0.15 | 37% | 23% | 0.0% |
| Neural Fast | 10.3 | +3.03 ± 0.11 | 38% | 31% | 0.1% |
| Belot 2.06, with a Belot 2.06 partner | 3.0 | +6.62 ± 0.24 | 32% | 24% | 0.3% |

A competitive bid loses to passing about one time in three, but passing costs more: the
opponents then play their contract and usually make it. It held for every natural holding, over
all hands where the bid was possible (the opponents hold a suit):

| Trumps | Two | Three | Four |
| --- | ---: | ---: | ---: |
| The nine without the jack | +0.93 | +3.55 | +5.20 |
| The jack without the nine | +3.16 | +5.73 | +7.22 |
| The jack and the nine | +6.13 | +8.47 | +9.63 |

The risk people fear is the double, and the numbers do not bear it out: doubling a contract
that had taken the bid from the doubler's side cost the doubler 15.9 game points on average. It
paid only with the trumps' jack and two more of them (+3.4), better with the ace or ten beside
the jack (+7.8), or with four trumps and the jack: about one such hand in 400. With the jack of
the opponents' suit the player should not compete at all but pass and defend (below).

**All trumps over the partner's no trumps does not pay without a great hand:**

| Jacks | Without a nine beside them | With a nine beside one | With nines beside two |
| --- | ---: | ---: | ---: |
| One | -14.1 | -9.1 | |
| Two | -6.3 | -0.7 | (+7.5, few hands) |
| Three | +2.1 | +8.6 | |

The other rules a person can read off the tables (October 1 bidding and the first learned
bidding, rules-only play; in brackets the gain over passing):

- **Opening a suit**: the jack and another card (+0.4), the jack and the nine (+1.8), the jack
  and two more (+2.5); not the nine and one small card (-1.6).
- **Opening no trumps**: two aces with the ten of one (+1.6), three aces (+3.9); one ace (-5.4)
  or two bare aces (-0.5) are not enough.
- **Opening all trumps**: two jacks with the nine beside one (+1.4), three jacks (+5.1); two bare
  jacks (-2.4) or one jack, even with its nine (-5.1), are not enough. With the exact endgames all
  trumps is worth one to two points more (two jacks and a nine: +2.3).
- **Over the partner's suit**: another suit only with its jack and length (the jack and two
  more +2.6, the jack and the nine +2.9; a nine and one small card -3.8); all trumps with two
  jacks (+1.8; +8.6 with a nine beside one).
- **Over the opponents' suit**: any natural suit (above); no trumps with two aces and a ten
  (+2.5) or three aces (+3.2); all trumps with two jacks and a nine (+3.3) or three jacks (+3.7).
- **Over the opponents' no trumps** (it scores double): all trumps with two jacks (+2.4; +8.4
  with a nine).
- **Doubles**: the opponents' no trumps only with two aces and a ten (+2.1) or three aces, their
  all trumps practically never (two jacks with their nines still -6).

### The learned bidding

For every natural bid it may make, the player counts its five cards and the auction
(`BidFeatures`): for a suit the trump jack, nine, ace, ten, king, queen and small cards, the
length, the side aces, tens and kings, voids, singletons and declarations; for no trumps the
aces, tens, kings and queens; for all trumps the jacks, the nines beside them, aces, tens,
belotes and declarations; and for every bid who leads, who bid and who passed, and what the
player holds in the suits the partner and the opponents bid. A weight per number and situation
(who holds the contract: nobody, the partner or an opponent, times its kind) turns the count into
the game points the bid should bring over passing (`BidModel`, ridge regression on the replays),
and the best bid that brings more than passing is made. The weights read like advice. Opening a
suit: start from -2.9, the trump jack +1.9 and its nine beside it +1.9 more, the ace +0.9, the ten
+0.8, a side ace +1.3, a void +1.2, a tierce +1.1. Over the opponents' suit, holding the jack of
their suit counts -6.1 for competing with a suit and -8.4 for no trumps: pass and defend. Over
the partner's suit, holding its jack counts -6.6: do not take the partner's suit away.

Rare situations and doubles are not left to the weights, which would guess there: they bid by
cells, the few holdings a person looks at first (jacks and the nines beside them for all trumps;
for a double the trumps' jack, its ace and ten, the length and whether they overcalled), and
only where the cell's average gain, less 1.5 standard errors, is above zero (a double: above one
game point). That gives all trumps over the partner's no trumps only three jacks or two jacks
each with its nine, and doubles only the clear cases above. Situations no model covers (the
team's own doubled contract, the partner's all trumps) keep the written counts. Every bid is
still natural: a suit with its jack or nine and another card, no trumps with an ace, all trumps
with a jack. A bid takes about 2 µs.

The weights were fitted by policy iteration: on the October 1 bidding's replays (model r1),
then on r1's (r2, r3); a model fitted on its own games alone learned to exploit its own habits
(the second self-play model beat the first by 10 Elo but the October 1 bidding by only 6), so
every round mixes opponents and partners. A non-linear model (boosted trees on the same numbers)
found only 0.016 game points a deal more than the weights: with five cards and the auction, this
is about all there is to find. The weights the player uses were fitted to all of r1's replays (4.1
million decisions, the rules' card play) and r3's with the exact endgames (0.77 million: against
itself, the October 1 bidding, Fast and Belot 2.06, and with Belot 2.06 as the partner). With the
endgames every holding cell of r3's own bids gained over passing (217,000 bids in 94 cells, none
below zero; the final weights change 3% of its decisions): all trumps is worth more with the
endgames, so its bids there look bolder than the rules-only numbers above. With the rules alone two of its opening cells lose a little (two bare
aces for no trumps, -1.5; a nine and one small card, -1.2).

### The written point counts (`HeuristicBidding`)

The bidding until October 3, still used where the learned model has no answer and for comparisons
(`HeuristicSettings.Bids = null`, trainer profile `heuristic+learned=0`).

Every bid is natural, as a person reads it: a suit with its jack or nine and another card of it,
no trumps with an ace, all trumps with a jack.

- **A suit** counts its trumps (the jack 6, the nine 4-5, the ace, ten, king and queen, the
  belote, length) and the side aces and tens: the academy's "jack, nine and ace: bid it" and the
  blog's minimum (the jack and two more with a side ace or ten; four with the nine) clear the
  threshold, a nine with small trumps alone does not.
- **No trumps** counts aces, the tens with them and the kings behind both, with a bonus for the
  first bidder, who leads ("being first is a substantial edge in no trumps").
- **All trumps** counts jacks, the nines with them and the aces behind; two jacks are nearly a
  must. A partner's suit bid shows that suit's jack or nine and helps a lot.
- **Competing**: when the opponents hold the contract a bid needs less, because passing lets
  them play it (the blog: compete "even with relatively weak cards").
- Another suit takes the contract from the partner only when clearly better; an opponent's
  suit without a stopper (its ace, its jack) costs no trumps and all trumps strength.
- **The score**: bolder when the opponents near 151 or lead by 50, more careful 50 ahead (the
  academy: how the score affects the bidding).
- **Doubles** only with a strong defence in the opponents' contract ("say contra only if you
  believe you can stop the opponent"). They rarely arise and made no measurable difference.

## Card play (`HeuristicCardPlay`)

Leading:

- **The partner's call** is answered at once: its jack (all trumps) or ace given to this player's
  trick shows the next card of that suit ("the clearest signal in Belot"); in a suit contract
  the declarers draw trumps first.
- **A suit nobody else holds** is cashed, the most valuable card first; the last trumps stay to
  ruff.
- **Declarers draw trumps** with control (the academy: more trumps than the opponents can
  hold), from the top (the jack before the nine: only lower trumps can fall); always with the
  two top trumps; with long trumps and no master, a small one. The declarer's partner leads a
  trump at the first chance, never the nine while the jack is out (the declarer would have to
  beat it with the jack).
- **Defenders** cash their aces before the declarers draw trumps and ruff them ("when in doubt,
  play the aces"), lead trumps only with four, and lead a singleton to ruff its next round.
- **All trumps**: masters are cashed, but a lone jack waits for three tricks (the blog: avoid
  premature cashing; it is the stopper and the way back in) unless the nine is behind it or it is
  the partner's suit; long suits first; never a bare nine into the jack.
- **No trumps**: the aces wait for the last tricks (the academy) unless the ten is behind the
  ace; the top of two touching honours drives the master out.
- Otherwise: the partner's bid or led suit, never the suit it threw away; not a suit the
  opponents may ruff or bid; the cheapest card of the suit that costs least to give up.

Following:

- **Every card is valued by its expected gain**: the chance the team keeps the trick with it
  (against the opponents still to play: what they may hold, what the play showed) times the
  points at stake, minus what keeping the card is worth (a master's points and the cards it
  will draw, a trump's control, a guarded second card, half a belote). This one rule gives the
  sources' advice its measure: give points to a trick the partner surely takes, the strongest
  card of a suit this player does not need (the academy: "discard the strong cards when the
  partner takes"); never a trump or half a belote; do not trump the partner's trick unless the
  next player is likely to take it; take the opponents' trick with the cheapest card that holds;
  in no trumps keep the aces and tens off early tricks with few points; otherwise lose the
  cheapest card.
- **Discards** come from the suit with nothing in it, which tells the partner not to lead it
  ("what you discard, you don't have"), never from the partner's suits, and a singleton in a
  suit contract makes a void to ruff.
- In no trumps the partner's low lead is taken with the ace from ace-small (the blog:
  unblocking).

## The exact endgame (the second stage)

From the fourth trick on (the last five tricks), `EndgameSearch` deals the unseen cards
consistently with everything above (the voids, what each player has shown not to hold, the
belote and carre cards, every bot's habit of declaring everything) and solves each deal by
partnership minimax with a transposition table; the card with the best average game points is
played, and among the cards within 0.02 game points the rules choose. Three tricks enumerate
every deal (at most 1,680), four and five sample 48. Following the cards: a deal is a third as
likely when a player who bid a suit holds neither its jack nor its nine, and half as likely for
each honour it gives the partner in a suit the partner threw away. The work is bounded by
150,000 solver nodes and 8 ms.

## Results

Mirrored pairs of whole matches to 151 (the same deals twice, the teams swapped), fresh seeded
players per game, the arena of the trainer (`arena`, seeds 8101-8110); the score is the share
of matches won, ± one standard error, and the Elo follows from the score. The final version (v7)
against every other bot, beside the first measured rules (v1):

| Opponent | Games | Rules + endgames (v7) | Elo | Rules only (v7) | Elo | v1 rules |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Random | 4,000 | 99.98% ± 0.03 | +1441 | 99.80% ± 0.07 | +1079 | 99.70% |
| Beginner (DummyPlayer) | 4,000 | 98.10% ± 0.21 | +685 | 93.68% ± 0.38 | +468 | 86.58% |
| Skilled (SmartPlayer) | 10,000 | 87.70% ± 0.31 | +341 | 77.19% ± 0.39 | +212 | 60.91% |
| SharpBelot (open source) | 10,000 | 84.18% ± 0.34 | +290 | 74.76% ± 0.40 | +189 | 62.98% |
| Belot 2.06 (2001, C# transcription) | 10,000 | 72.27% ± 0.40 | +166 | 59.90% ± 0.44 | +70 | 45.14% |
| Expert (neural, the app's level 4) | 4,000 | 61.90% ± 0.68 | +84 | 45.83% ± 0.68 | -29 | 31.40% |
| ISMCTS, 100 ms a card | 1,000 (rules: 200) | 45.30% ± 1.26 | -33 | 32.00% ± 2.80 | -131 | 18.50% |
| Fast (neural, the app's hints) | 4,000 | 42.10% ± 0.66 | -55 | 29.18% ± 0.64 | -154 | 16.75% |
| September 29 Master (neural) | 1,000 | 36.40% ± 1.31 | -97 | 24.50% ± 1.24 | -196 | 13.70% |
| Master (the app's level 5) | 600 | 30.83% ± 1.63 | -140 | 21.17% ± 1.54 | -228 | 13.50% |

Without search the rules beat every other rule-based program clearly, Belot 2.06 by 70 Elo, and
nearly tie the neural Expert. The exact endgames add 100-130 Elo against every opponent: the
player then beats the Expert, comes within 33 Elo of ISMCTS (which searches 100 ms on every
card; the endgame player takes under 1 ms; 47.5% ± 3.1 in the panel's 200 games, 45.3% ± 1.3,
95% [42.8%, 47.8%], in 1,000 more with seed 8120) and stays 55-140 Elo below the neural players
with search.

Timing, one thread on the idle i7-12700K, 200 self-play games (50,000 card callbacks, warmup
excluded; `dotnet NeuralTrainer.dll timing --player heuristic-endgame --bench-games 200`):

| Profile | Mean | p95 | p99 | Maximum | Over 10 ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Rules only | 0.005 ms | 0.008 ms | 0.011 ms | 0.5 ms | 0 |
| Rules + five-trick endgames | 0.833 ms | 3.4 ms | 3.7 ms | 5.9 ms | 0 |

### October 3: the learned bidding

The same panel, the same seeds and so the same deals, the card play unchanged, the bidding
learned (`heuristic-endgame` now) against the written bidding of October 1 (v7):

| Opponent | Games | Learned bidding | Elo | Written bidding (v7) | Elo | Change |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Random | 4,000 | 99.95% ± 0.04 | +1320 | 99.98% ± 0.03 | +1441 | |
| Beginner (DummyPlayer) | 4,000 | 97.55% ± 0.24 | +640 | 98.10% ± 0.21 | +685 | -0.55 pp |
| Skilled (SmartPlayer) | 10,000 | 89.18% ± 0.30 | +366 | 87.70% ± 0.31 | +341 | +25 Elo |
| SharpBelot | 10,000 | 85.24% ± 0.33 | +305 | 84.18% ± 0.34 | +290 | +15 Elo |
| Belot 2.06 | 10,000 | 73.95% ± 0.40 | +181 | 72.27% ± 0.40 | +166 | +15 Elo |
| Expert (neural) | 4,000 | 65.48% ± 0.64 | +111 | 61.90% ± 0.68 | +84 | +27 Elo |
| Fast (neural) | 4,000 | 45.63% ± 0.66 | -30 | 42.10% ± 0.66 | -55 | +25 Elo |
| September 29 Master | 1,000 | 38.40% ± 1.35 | -82 | 36.40% ± 1.31 | -97 | +15 Elo |
| Master | 600 | 34.33% ± 1.66 | -113 | 30.83% ± 1.63 | -140 | +27 Elo |
| ISMCTS, 100 ms a card | 200 | 53.00% ± 2.92 | +21 | 47.50% ± 2.96 | -17 | (too few games) |

Every serious opponent loses 15 to 27 Elo more to it. The deals are the same in both columns,
so a change is more certain than the two errors suggest, but 200 games against ISMCTS say only
"about even". The beginner's slightly better result against the learned bidding is within two
standard errors.

Head to head, the card play the same on both sides (seeds 10301-10305):

| The learned bidding against the written one | Games | Score | Points a game | Elo |
| --- | ---: | ---: | ---: | ---: |
| Rules and exact endgames (`heuristic-endgame`) | 20,000 | 54.00% ± 0.27 | +5.9 | +28 ± 2 |
| The same, Belot 2.06 as the partner on both sides | 20,000 | 51.98% ± 0.25 | +3.1 | +14 ± 2 |
| Rules only (`heuristic`) | 40,000 | 53.59% ± 0.19 | +5.3 | +25 ± 1 |
| The same, Belot 2.06 as the partner on both sides | 40,000 | 51.84% ± 0.17 | +2.7 | +13 ± 1 |
| Both against the Master, the same 2,000 games | 2,000 | 37.35% against 34.00% | +5.3 | +25 |

With a partner who bids its own way (Belot 2.06 standing in for a person) half the gain remains:
one of the two seats bids, and the partner's bids mean what that partner means by them. A bid
costs 1.9 µs on average (4 µs at the 99th percentile; the written counts 1.5 µs).

Against the neural networks' bidding (`fast|heuristic`: the Fast player's bids, the rules' card
play), the learned bidding scores 50.79% ± 0.19 (+5 Elo, 40,000 games), the written one 47.06%
(-20 Elo): the point count bids at least as well as the networks trained in self-play.

### How it got there

Each change was measured in mirrored pairs (20,000 games against the version before it unless
noted; at this sample a 1 Elo standard error) and kept only when it won. The trainer's `regret`
command, written for this, lets the neural fast player grade every card and bid of the rules'
games and sums the loss per situation and rule: it showed where to look (it only grades; the
player uses no network).

| Step | Change | Result |
| --- | --- | --- |
| v0 | First rules, bids at the sources' minimums | 48.9% against SmartPlayer (its bids 42.7%, its play 47.2%) |
| | A master's keep value includes its own points; the belote kept or declared, never lost | 51.7% against SmartPlayer |
| | Side aces cashed even when a ruff is possible ("when in doubt, play the aces") | +24, then +14 Elo |
| | Taking a trick with a card that is only likely to hold | +18 Elo |
| | No covering of the partner's card; the call signal off | +6, +3 Elo |
| | The declarer's partner leads trumps | +12 Elo |
| | Bid thresholds: suit 11.5 to 9.5, all trumps 19.5 to 16.5, no trumps 19.5 to 14.5 | +53 Elo |
| v1 | (panel below) | 60.9% against SmartPlayer, 45.1% against Belot 2.06 |
| v2 | Exhausted suits cashed (+5), patience with jacks in all trumps (+8), drawing by control (+5) | 47.9% against Belot 2.06 |
| v3 | The last trumps kept for ruffing; competing (+27); partner's suit for all trumps (+3) | 55.2% against Belot 2.06 |
| v4 | Forced plays by expected gain (+9), no trumps aces wait to trick 4 (+10), patience for lone jacks only (+13) | 57.0% against Belot 2.06 |
| | Endgames of 2, 3, 4 tricks | +26, +59, +90 Elo over the rules; 5 tricks another +17 to +23 |
| v5 | Five-trick endgames, 48 worlds, 150,000 nodes | 86.3% against SmartPlayer, 71.9% against Belot 2.06, 47.5% against ISMCTS (200 games) |
| v6 | Every follow by expected gain (+14), bidders' honours (+2), no unblocking (+3) | +21 Elo; 59.4% against Belot 2.06 (rules only) |
| v7 | Endgame worlds weighted by the bids and the partner's discards | +4 and +8 Elo (two seeds) |

Rejected, each measured: covering the partner's card at a 30% chance of it being beaten (-4 to
-6 Elo); giving the jack or ace to call a suit (-2 to -3); unblocking the nine or ten under the
partner's jack or ace (the blog, -3); the blog's forcing lead in all trumps, the king from
ace-ten-king (-16, -8 for the declarers only); leading the lowest of several sure cards (-8);
patience for one team only (-2 declarers, -20 defenders); drawing with a lone master trump from
trick 3 (-2); keeping cards second to one unseen card (-2 to -6); an ace worth more in no trumps
(-7); doubling and redoubling thresholds, discards weighted in the card chances, scaled contract
margins (no difference); bid thresholds re-tuned with the endgames (within ±3 Elo). Six-trick
endgames tied five (+3 ± 2) and cost more time.

## Commands

From the repository root, with the trainer built to a separate folder:

```bash
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/heuristic/bin
# Against any bot of the catalog (heuristic, heuristic-endgame, smart, belot206, fast, master, ismcts:100, ...)
dotnet artifacts/heuristic/bin/NeuralTrainer.dll arena --player heuristic-endgame --opponent belot206 --pairs 5000 --threads 18 --seed 8105
# A rule switched off or retuned, against the defaults ("heuristic+name=value+...", see HeuristicSettings.Set)
dotnet artifacts/heuristic/bin/NeuralTrainer.dll arena --player "heuristic+followvalue=0" --opponent heuristic --pairs 10000 --threads 18 --seed 7080
# Where the rules lose, graded by the fast neural player
dotnet artifacts/heuristic/bin/NeuralTrainer.dll regret --player heuristic-endgame --opponent belot206 --pairs 300 --threads 18 --seed 7090
# Card latency
dotnet artifacts/heuristic/bin/NeuralTrainer.dll timing --player heuristic-endgame --bench-games 200
# The bids: replays of every bid with each natural alternative, the numbers the learned bidding counts,
# then the fit, the tables and the embedding (tools/NeuralTrainer/Bidding, Python; see its README)
dotnet artifacts/heuristic/bin/NeuralTrainer.dll bidlab --player heuristic-endgame --opponent fast --pairs 30000 --threads 18 --seed 10103 --data artifacts/bidding/r3end-fast
dotnet artifacts/heuristic/bin/NeuralTrainer.dll bidfeatures --data artifacts/bidding/r3end-fast
# The learned bidding against the written one ("heuristic+learned=0"), and a model file ("heuristic@m.txt")
dotnet artifacts/heuristic/bin/NeuralTrainer.dll arena --player heuristic-endgame --opponent "heuristic-endgame+learned=0" --pairs 10000 --threads 18 --seed 10301
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests/Belot.AI.ClaudePlayer.Tests.csproj --filter "FullyQualifiedName~Heuristic"
python -m unittest discover -s tools/NeuralTrainer/Bidding -v
```
