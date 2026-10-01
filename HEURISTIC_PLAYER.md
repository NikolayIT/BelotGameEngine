# The heuristic player: belot.bg's rules, then exact endgames

October 1, 2026.

`ClaudePlayerHeuristic` (`src/AI/Belot.AI.ClaudePlayer`, rules in `Heuristic/`) plays by written
Belot advice: no networks, no Monte Carlo search in the early tricks. Every bid and every card of
the first tricks comes from rules taken from the belot.bg academy and the other sources below,
over what a careful player remembers of the cards. Its second stage keeps the rules for the
first three tricks and plays the last five out exactly over the deals the play allows
(perfect-information minimax per deal with `Neural/EndgameSearch`), the rules choosing among the
cards that do equally well.

- `ClaudePlayerProfiles.CreateRulesOnly()`: the rules alone (trainer catalog name `heuristic`),
  5 µs a card.
- `ClaudePlayerProfiles.CreateHeuristic()`: the rules and five-trick exact endgames (catalog name
  `heuristic-endgame`), 0.83 ms a card on average, at most 5.9 ms measured (the limit asked for
  was 10 ms).

Against every other bot (mirrored pairs of whole matches; the full tables are under Results):

| Opponent | Rules only | Rules + exact endgames |
| --- | ---: | ---: |
| SmartPlayer | 77.2% | 87.7% |
| Belot 2.06 (the 2001 program) | 59.9% | 72.3% |
| The app's Expert | 45.8% | 61.9% |
| ISMCTS, 100 ms a card | 32.0% | 45.3% |
| The app's Master | 21.2% | 30.8% |

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

## Bidding (`HeuristicBidding`)

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
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests/Belot.AI.ClaudePlayer.Tests.csproj --filter "FullyQualifiedName~ClaudePlayerHeuristicTests"
```
