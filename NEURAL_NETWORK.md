# The Belot Neural Player

`ClaudePlayerNeural` plays Belot with four small neural networks trained by reinforcement
learning in self-play. It values **every action open to it** (each bid, each legal card) in game
points, decides in about **10 microseconds** instead of ClaudePlayerIsmcts's 100 milliseconds,
and can be made weaker on purpose by sometimes taking an action that is nearly as good as the
best.

This document explains what it is, how it is trained, how to reproduce and improve it, and how
to keep it working.

**Results** (September 2026, mirrored pairs of whole games, i7-12700K):

| Player | vs ClaudePlayerIsmcts (100 ms) | vs SmartPlayer | Time per card |
|---|---|---|---|
| Networks alone | **50.2% ± 1.3%** (1,000 games, −2.2 points a game) | 86.2% ± 0.2% (20,000 games, +319 ELO) | **8.6 µs** |
| Networks + 100-deal search (`SearchDeals = 100`) | **55.0% ± 2.2%** (400 games, +5.6 points a game) | | 51 ms |
| ClaudePlayerIsmcts, for scale | | 89.7% ± 1.7% (300 games, +375 ELO) | 100 ms |

The networks alone play as well as ClaudePlayerIsmcts about 10,000 times faster; with a small
search on top (half ISMCTS's time) they beat it. The app's round robin (`elo`, pair ratings
anchored at Dummy = 1200): networks + search 1771, ClaudePlayerIsmcts 1733, the networks played
loose (the app's Expert) 1554, SmartPlayer 1462, DummyPlayer 1200, RandomPlayer 660.

---

## 1. The idea

The strongest player before it, `ClaudePlayerIsmcts`, searches every card: it deals the unseen
cards thousands of times and plays each deal out with a greedy rule that sees all four hands. Its
limit is that rule (more search time does not help), and it costs 100 ms a card on a desktop,
more on a phone.

The neural player learns instead what every action is worth, from millions of deals it plays
against itself:

- **One network per job**: a *bidding* network (5 cards, the auction so far → the value of pass,
  the six contracts, double and redouble) and a *card* network for each kind of contract:
  the suits (with the trump suit always turned to the front, so one network plays all four suit
  contracts), no trumps, and all trumps. Declarations are not a network's job: the player
  declares everything it is offered (as ClaudePlayerIsmcts does), and every belote.
- **Values, not just a choice**: each output is the game points the player's team gets from the
  deal minus the other team's (the hanging points it wins included) if it takes that action and
  everybody plays on as the networks would. The player takes the best; a weaker level takes one
  close to it (see §5). The same values can show a person how good their move was.
- **Pure managed C#**, no libraries, like the Santase engine's neural player: the inference is a
  few hundred lines over `float[]` and `System.Numerics.Vector<float>`, so it runs on Android
  (MAUI) unchanged; the trainer is C# too, CPU only, using every core.

## 2. Where everything lives

| Path | Role |
|---|---|
| `src/AI/Belot.AI.ClaudePlayer/ClaudePlayerNeural.cs` | The player: `GetBid`, `PlayCard`, `EvaluateBids`, `EvaluateCards`, `Temperature`, `MaxRegret`, `MayDouble`. |
| `src/AI/Belot.AI.ClaudePlayer/Neural/NeuralDeal.cs` | A deal as a seat follows it (the auction, declarations, cards played, what the play showed). Built from the engine's contexts, or played whole in self-play. |
| `src/AI/Belot.AI.ClaudePlayer/Neural/FeatureEncoder.cs` | What a seat knows → the networks' sparse inputs. The layout is in its header comment. |
| `src/AI/Belot.AI.ClaudePlayer/Neural/NeuralNetwork.cs` | The multilayer perceptron (inference) and its file format. |
| `src/AI/Belot.AI.ClaudePlayer/Neural/NeuralModels.cs` | The four networks; the trained ones are embedded from `Neural/Weights/*.bin`. |
| `src/AI/Belot.AI.ClaudePlayer/Neural/NeuralEvaluator.cs` | Features → forward pass → a value per legal action. |
| `src/AI/Belot.AI.ClaudePlayer/Search/PlayInference.cs` | What a played card shows about the hand (shared with ClaudePlayerIsmcts). |
| `tools/NeuralTrainer/` | The trainer (not in `src/Belot.sln`; the ClaudePlayer tests reference it, so CI builds it). |
| `src/Tests/Belot.AI.ClaudePlayer.Tests/Neural/` | The tests (§11). |

## 3. The networks

Each is a multilayer perceptron with ReLU hidden layers and a linear output:

| Network | Layers | Parameters |
|---|---|---|
| bid | 97 → 256 → 128 → 9 | 59 k |
| trump (all four suit contracts) | 600 → 512 → 256 → 128 → 32 | 476 k |
| no trumps | 600 → 512 → 256 → 128 → 32 | 476 k |
| all trumps | 600 → 512 → 256 → 128 → 32 | 476 k |

The sizes are data, not constants: the file of a network carries them, and the trainer takes
`--sizes 512,256,128 --bid-sizes 256,128` for new networks. An output is a value in units of 26
game points (`NeuralEvaluator.ValueScale`); the player multiplies it back.

**The file** (`NeuralNetwork.Write`/`Read`): the magic `BNN1`, the version, the network's tag (0
bid, 1 trump, 2 no trumps, 3 all trumps) and feature layout, the layer sizes, then each layer's
weights (input-major: the outputs of one input side by side) and biases as 16-bit floats, so the
four networks take about 3 MB. `NeuralModels` refuses a file whose tag, layout or input/output
size does not match the code: there is no silent random fallback.

## 4. The inputs

Everything a seat may know and nothing else. `FeatureEncoder` writes a sparse list (the index
and value of every non-zero input); most inputs are card planes of 32 (a card's index is
`suit * 8 + type`, types from the seven to the ace). Seats are relative to the one deciding:
itself, the next to play, the partner, the previous.

**Card networks (600 inputs):** planes for the hand, the legal cards, the cards each seat played
in the earlier tricks, the card each other seat played to this trick, the card holding the
trick, the unseen cards each other seat *cannot* hold (the play showed it: `PlayInference`,
the same rules ClaudePlayerIsmcts samples by) and those it *surely* holds (a belote's partner
card, four jacks or nines); then the trick number, the position in it, who holds it, the
declarer, the doubling, each seat's bids and passes, each seat's declared kinds of combination,
the points so far for each team, the points in this trick, the tricks taken, and the hanging
points. In a suit contract the suits are turned so the trump suit comes first (the outputs are
turned back), so a deal in hearts and the same deal in spades look the same.

**Bidding network (97 inputs):** the five cards, the seat's place in the auction, each seat's
bids and passes, the contract so far with its declarer and doubling, the bids open, the hanging
points, and how long the auction has been.

A seat's inputs are the same whether the deal is followed from the engine's context or played in
self-play (`SelfPlayAgreesWithEngineTests` checks it at every decision of 400 random matches);
built from a context, the deal does not even contain the other hands, so nothing hidden can
leak in.

## 5. Choosing, and playing weaker on purpose

`EvaluateCards(context)` and `EvaluateBids(context)` return every legal action with its value,
best first. `PlayCard`/`GetBid` take:

- with `Temperature = 0` (the default): the best;
- with a `Temperature` T > 0 (in game points): an action worth d points less than the best is
  taken e^(−d/T) times as often, and never one worse than the best by more than `MaxRegret`
  points. So the player sometimes takes the second best when it is close, and never blunders
  badly, which is what the app's weaker levels need.

`MayDouble = false` keeps it from doubling and redoubling.

**`SearchDeals`** (0 by default) adds a small search on top of the card networks: every legal card
is played out in N deals of the unseen cards, dealt as ClaudePlayerIsmcts deals them (respecting
what the play has shown), with the networks deciding for every seat from what that seat can see,
and valued by the average result (all cards on the same deals). With 100 deals it beats the
networks alone by +121 ELO and ClaudePlayerIsmcts by 55% at about 51 ms a card.
`SearchTimeLimitMilliseconds` stops starting new deals once a budget is spent (at least eight
deals), so a slow phone plays fewer. `SearchPriorDeals` and `SearchPruneMargin` are the experiments
of the tuning record (§10).

## 6. Speed

A decision is one forward pass over the sparse inputs: the first layer sums only the weight rows
of the non-zero inputs (≈60–120 of 600), the hidden layers skip the units the ReLU zeroed, and
the rows are added a SIMD vector of outputs at a time (Santase's layout: weights transposed,
four vector accumulators). **About 10 µs a decision** on one desktop core (a whole deal of
self-play, 32 cards and the auction, in about 0.3 ms), against 100 ms for ClaudePlayerIsmcts: four
orders of magnitude.

## 7. Training

```
 ClaudePlayerIsmcts games       fit (supervised)          self-play reinforcement learning
 (distill: what its searches ─▶ networks that value  ─▶  (train: every action rolled out in the
  found at every decision)      actions like it          true deal by the current networks)
```

### Stage A: distilling ClaudePlayerIsmcts (the warm start)

`distill` plays whole games of four ClaudePlayerIsmcts (50 ms a card) and records, at every
decision with a choice, the inputs the neural player would see and what the search found: the
average result of each card it tried at the root (`GetRootValues`) and of each bid it weighed
(`GetBidValues`), in game points. `fit` trains new networks on those samples (Huber loss over the
labelled outputs, Adam). An output the samples never labelled (ClaudePlayerIsmcts does not
double) starts at a lost deal, so the warm start never doubles at random.

2,500 games (33 minutes on 18 threads) gave 606 k card and 184 k bid samples; fitting takes two
minutes.

### Stage B: self-play with every action rolled out

`train` improves the networks by Monte Carlo policy iteration. Actor threads play deals on card
masks (`NeuralDeal`, the engine's rules: `SelfPlayAgreesWithEngineTests`). At a labelled decision
**every action open to the seat is tried**: the deal is copied, the action taken, and the rest of
the deal played out by every seat's networks, each deciding from what it can see, **in this very
deal**; the label of the action is the game points its team then gets minus the other team's.
Then the deal goes on with the best action (or, 3% of the time, a random one, to see other
positions).

- The labels are unbiased values of the current play, with no player seeing hidden cards (unlike
  ClaudePlayerIsmcts's greedy rollouts, which see all four hands).
- A decision gives 2–8 labels instead of the single noisy result of plain Deep Monte Carlo
  (DouZero). Only about 1% of a Belot deal's outcome is in the players' hands; the rest is the
  cards, which is why one-label-per-decision learning has failed for Belot elsewhere. Here all
  the actions of a decision share the deal, so the luck of the cards cancels out of their
  differences.
- Bids are labelled the same way: the auction goes on with the networks, the last three cards
  are dealt as the deck says, and the deal is played out.
- A learner thread trains the four networks on the newest samples (each used about twice) and
  hands the actors fresh copies every 10 seconds: better networks make better labels.
- 30% of the deals put one team's seats in the hands of an earlier copy from a pool (a snapshot
  every 20 minutes), so the networks do not only learn to beat themselves.
- Every 30 minutes the networks are saved and measured through the engine in mirrored games
  against SmartPlayer and ClaudePlayerIsmcts (20 ms); the best is kept in `best/`.

Throughput on an i7-12700K (12 actor threads, 8 learner threads): about 440 fully labelled deals
a second.

**The night of the first training** (September 26–27, 2026), against the fixed warm start in
10,000-game mirrored matches (1σ ≈ 3 ELO):

| Stage | Time | vs warm start | vs SmartPlayer | vs ISMCTS 100 ms |
|---|---|---|---|---|
| Warm start (Stage A) | 35 min | — | 76.6% (+206) | 37.0% ± 2.3% (300 games) |
| Self-play, learning rate 1e-4 (run2) | 3 h, 4.6 M deals | +103 | 84% | 36.0% ± 2.8% (200 games) |
| Self-play, rate 5e-5 → 5e-6 (run3) | 6 h, 11.9 M deals | **+150** | 86.2% | **50.2% ± 1.3%** (1,000 games) |

About 16.5 million self-play deals: 270 million labelled card decisions and 30 million bids.
The gains against the warm start (and against SmartPlayer) did not show against ClaudePlayerIsmcts
at the constant learning rate; the falling rate did (+47 ELO against the warm start, 36% → 50%
against ISMCTS). Judge a change against ISMCTS, not only against earlier networks: gains measured
against one's own lineage overstate the real ones.

## 8. Reproduction

From `tools/NeuralTrainer`, in Release:

```powershell
dotnet build -c Release
dotnet bin/Release/net10.0/NeuralTrainer.dll distill --data data/distill --games 2500 --milliseconds 50 --threads 18
dotnet bin/Release/net10.0/NeuralTrainer.dll fit --data data/distill --out checkpoints/distilled --epochs 10 --batch 256 --learners 16
dotnet bin/Release/net10.0/NeuralTrainer.dll train --in checkpoints/distilled --out checkpoints/run2 --hours 3 --bid-label-chance 0.3 --card-label-chance 1 --replay 2
dotnet bin/Release/net10.0/NeuralTrainer.dll train --in checkpoints/run2/0005 --out checkpoints/run3 --hours 6 --learning-rate 5e-5 --final-learning-rate 5e-6 --bid-label-chance 0.3 --card-label-chance 1 --replay 2 --capacity 3000000
dotnet bin/Release/net10.0/NeuralTrainer.dll validate --in checkpoints/run3/final --opponent ismcts:100 --pairs 500 --threads 10
dotnet bin/Release/net10.0/NeuralTrainer.dll validate --in checkpoints/run3/final --opponent ismcts:100 --search-deals 100 --pairs 200 --threads 10
dotnet bin/Release/net10.0/NeuralTrainer.dll bench --in checkpoints/run3/final
dotnet bin/Release/net10.0/NeuralTrainer.dll bench --in checkpoints/run3/final --search-deals 100
```

Every setting of `TrainingSettings` can be given as `--name value`. `validate --opponent` takes
`smart`, `ismcts:<ms>` or a folder of networks; `--smart-bidding true` lets SmartPlayer bid for
the networks (to judge the card play alone); `bench --deals N --threads T` times labelled
self-play.

## 9. Promotion

The trainer never touches the shipped networks. To ship a checkpoint:

1. Compare it with the shipped ones and with ClaudePlayerIsmcts, with the machine otherwise idle:
   `dotnet run -c Release --project src/Tests/Belot.GamesSimulator -- neural-ab 5000 <folder> -`
   and `-- neural 500 100 <folder>` (1σ ≈ 1.6 pp at 1,000 games against ISMCTS).
2. Copy its four `.bin` files over `src/AI/Belot.AI.ClaudePlayer/Neural/Weights/`.
3. Rebuild, run the ClaudePlayer tests, re-run the simulator's `elo` suite and paste the
   ratings into the app's `Game/AiLevels.cs`.

## 10. Maintenance

**Change the rules in the engine** and `SimulatorAgreesWithEngineTests` and
`SelfPlayAgreesWithEngineTests` tell you to change the simulator and `NeuralDeal`; then retrain
(the networks learned the old game).

**Change the inputs** (`FeatureEncoder`): bump `FeatureEncoder.LayoutVersion`, retrain from
Stage A (the old files will be refused), and update §4.

**Troubleshooting:**

| Symptom | Cause / fix |
|---|---|
| `InvalidOperationException: The network … is not embedded` | `Neural/Weights/*.bin` missing from the build: they are `EmbeddedResource`s of Belot.AI.ClaudePlayer. |
| `InvalidDataException` on loading | The file's tag, layout or sizes do not match the code: retrain, or restore the matching files. |
| Training several times slower than `bench` suggests | Windows throttles a background console process (EcoQoS); the trainer opts out at start. |
| The networks double wildly | An output that was never trained (see Stage A); `fit` starts such outputs at a lost deal. |

**Tuning record** (don't re-try the rejects blindly):

- The distilled warm start has no values for double and redouble (ISMCTS does not double): left
  untrained they were chosen at random and the networks lost 34% to SmartPlayer; `fit` now starts
  them at a lost deal (76.6%).
- Labelling every bid (every bid option played out as a whole deal) took 80% of the actors' time:
  bids are labelled at 30% of the decisions, cards at 100%.
- Windows' power throttling of a background console made a run 7× slower (the trainer opts out).
- A constant learning rate of 1e-4 plateaued (+103 against the warm start, no gain against
  ISMCTS); falling linearly from 5e-5 to 5e-6 over six hours gave +47 more and parity with ISMCTS.
- The search: 10 deals is worse than the networks alone (−28 ELO: its averages are noisier than
  their values), 30 deals +49, 100 deals +121. Counting the network's value as 5 deals and
  dropping cards 4 points behind after half the deals: +69 with 40 deals, +96 with 100 (plain
  100 is better).
- Against ISMCTS the gap was in the card play, not the bidding: letting ISMCTS bid for the
  networks changed their points a game by less than one.

**Next steps** that look promising: distil the search (a few dozen deals a decision) into the
networks (expert iteration); wider networks (the loss had flattened); longer runs at a falling
rate.

## 11. Tests

In `src/Tests/Belot.AI.ClaudePlayer.Tests/Neural/`:

- `SelfPlayAgreesWithEngineTests`: 400 random matches (random bids with doubles, declarations,
  cards, belotes sometimes kept back) replayed into self-play deals: the same bids and cards on
  offer at every decision, the same declarations and scores, the same hanging points, and the
  same network inputs as built from the engine's context and from the seat's view.
- `FeatureEncoderTests`: a deal in one suit and the same deal in another look the same; the
  outputs map back to the cards and bids.
- `NeuralNetworkTests`: the SIMD, sparse forward pass equals a plain reference; the 16-bit file
  round-trips; broken or mismatched files are refused.
- `ClaudePlayerNeuralTests`: whole games at every seat without a fallback; every legal action
  valued once, best first, and the best taken; the temperature picks nearly-as-good actions,
  never beyond `MaxRegret`, repeatably with a seed.
- `TrainerTests`: the trainer's gradients equal the loss's numerical derivatives, and it learns
  a simple target.
- `DecideFromViewTests` also checks the neural player decides the same from a seat's view.

## 12. September 27 improvement experiments (in progress)

The fixed reference is the shipped weights at `544708e` (weights introduced in `8a0da0e`),
copied to `artifacts/neural-20260927/baseline/` with SHA-256 hashes before experimentation.
No candidate is promoted merely because training loss or a same-lineage match improved.
Final promotion requires a fresh match of at least 1,000 games against ISMCTS at 100 ms,
with the lower end of a two-sided 95% interval above 50%, plus a significant win against
the frozen reference. Report standard errors over independent mirrored pairs, not over
the individual games. Development and final evaluation use different deal seeds.

### Research and experiment order

The broader primary-source review is in [etc/NeuralResearch.md](etc/NeuralResearch.md).
It covers search distillation, Skat inference, DouZero/PerfectDou/DouZero+, Gongzhu,
Bridge partnership learning, residual networks, ReBeL/Student of Games, and the ICLR
2026 comparison of policy-gradient methods. It distinguishes methods/ablation review
from abstract-only screening; it is not a claim of an exhaustive literature review.

The most relevant architectural lead is to retain **ordered public play** and add
**card-location supervision**. The current encoder loses the order of earlier tricks.
A compact history MLP may capture that evidence within the runtime budget; larger
residual networks and transformers remain candidates to measure. PerfectDou and newer
policy-gradient results also justify considering PPO with a training-only privileged
critic if the current action-value approach saturates. None of those other-game results
proves a gain here. The first substantial test uses our already measured stronger
100-world search as a teacher, then compares fitting error, teacher decision regret,
whole-game strength and inference cost before selecting a replacement architecture.

- [Expert Iteration (Anthony, Tian and Barber, 2017)](https://arxiv.org/abs/1705.08439)
  alternates search and learning from its decisions. The measured strength of our
  100-deal neural search makes it a directly available teacher. Search labels must use
  only the acting seat's information, with identical sampled deals for all legal cards.
- [Dueling networks (Wang et al., 2016)](https://proceedings.mlr.press/v48/wangf16.html)
  separate a state's value from action advantages. Our first experiment applies that
  distinction to the **loss**, leaving the runtime network unchanged. This is a local
  hypothesis inspired by the paper, not an implementation of its architecture.
- [PerfectDou (Yang et al., 2022)](https://arxiv.org/abs/2203.16406) uses privileged
  information in a training critic while its deployed policy sees only its information
  set. Our all-action true-deal rollouts already supply training-only outcomes; a new
  privileged critic is a larger change to consider if simpler variance reduction fails.
- [DouZero (Zha et al., 2021)](https://proceedings.mlr.press/v139/zha21a.html) demonstrates
  deep Monte Carlo self-play at scale. Its success does not establish that our current
  absolute-value loss is suitable for Belot's much smaller differences between moves.
- [Weight averaging (Izmailov et al., 2018)](https://arxiv.org/abs/1803.05407) motivated a
  cheap check of averaging the last three old checkpoints' card weights. Our declining-rate
  checkpoints are not the paper's SWA training schedule; any benefit needs measurement.

For one sample, let `e[a] = Q[a] - target[a]`, and `m` be its mean over **labelled legal
actions only**. The experimental loss is
`sum Huber(e[a] - m) + actionCount * valueWeight * Huber(m)`.
The shared part of deal luck is removed before the action losses are clipped. The common
value term retains absolute point calibration. Its exact gradient includes subtracting
the mean of the centred Huber derivatives; treating `m` as a constant is incorrect.
`--card-value-weight -1` retains the original independent Huber loss; `0.05` is the first
candidate. Bidding remains on the original loss. No feature layout or weight format changes.

First compare equal-duration continuations from the frozen weights, with bidding frozen,
using the original and centred losses. Evaluate both against the reference and ISMCTS.
Then use the evidence to choose longer self-play or distillation of the search teacher.
Architectural and training-stack changes remain open. Use teacher-fitting diagnostics
and the history/inference evidence above to choose the next controlled experiment.

### Measurement corrections

The old `bench` denominator included warmup decisions while its timer excluded warmup,
understating the mean by about 9%. It also timed bids and self-play bookkeeping together
with cards. The corrected command excludes warmup and separately reports actual card
callbacks through the engine on one thread. The historical 8.6 us number above remains
the historical measurement, not an engine-callback timing. The untouched baseline binary
reported 9.1 us by that old method on this run; corrected timings will be used for candidates.

Training now saves the snapshot it actually evaluated as `best/`. Previously the learner
could publish a newer snapshot during evaluation, and that unmeasured snapshot was saved.

Experiment artifacts and full command logs live in `artifacts/neural-20260927/` (ignored).
The final results and reproduction commands will be recorded here after the runs finish.

### Reproduced baseline

| Measurement | Frozen shipped networks |
|---|---|
| ISMCTS, 100 ms, 400 games (seed 27) | 49.5% +/- 2.2 percentage points (1 sigma), -3.4 points/game, -3 ELO |
| SmartPlayer, 20,000 games (seed 28) | 86.8% +/- 0.2 percentage points (1 sigma), +57.3 points/game, +326 ELO |
| Corrected self-play benchmark | 11.3 us/decision (518,891 measured decisions, excludes warmup) |
| Engine card callbacks on one thread | 18.5 us/card (21,336 decisions across 100 games, excludes 20 warmup games) |

Initial candidates (all uncertainty is one standard error over mirrored pairs):

| Candidate | vs frozen baseline, 20,000 games, seed 29 | Other evidence |
|---|---|---|
| Old loss, 5-minute continuation | 49.6% +/- 0.2 pp, -0.5 points/game, -3 ELO | No improvement |
| Old loss, 15-minute continuation | 49.6% +/- 0.2 pp, -0.3 points/game, -2 ELO | 86.6% +/- 0.2 pp vs Smart, 20,000 games; 48.0% +/- 2.2 pp vs ISMCTS 100 ms, 400 games, seed 30, -3.7 points/game, -14 ELO |
| Centred loss, 15-minute continuation | 50.9% +/- 0.2 pp, +1.1 points/game, +6 ELO | 86.8% +/- 0.2 pp vs Smart, 20,000 games, +57.9 points/game, +327 ELO; 48.0% +/- 2.2 pp vs ISMCTS 100 ms, 400 games, seed 30, -2.8 points/game, -14 ELO |
| Mean card weights of run3/0010, run3/0011 and final; final bidding kept | 50.1% +/- 0.2 pp, +0.1 points/game, +1 ELO | Tie; fails the first promotion gate |

The 15-minute control processed 498,266 deals and 16,478 optimiser batches. Its settings
were `--learning-rate 1e-5 --final-learning-rate 2e-6 --actors 12 --learners 8 --batch 1024
--replay 2 --capacity 1000000 --bid-capacity 1000 --warmup 20000 --card-label-chance 1
--bid-label-chance 0 --bid-exploration 0 --card-value-weight -1 --evaluate-minutes 5
--smart-pairs 0 --ismcts-pairs 0 --pool-minutes 5 --seed 371 --hours 0.25`.
The centred-loss pilot changes only `--card-value-weight 0.05` and the output folder.

The baseline ISMCTS match used the untouched binary copy. The corrected benchmark is a
different measurement from the historical 8.6 us/decision; compare new candidates against
the corrected baseline, not against the historical number.

### Search distillation tooling

`distill --teacher neural --in <teacher-folder> --search-deals 100 --card-label-chance 1`
records plain fixed-deal neural search. Every labelled decision plays the best teacher
card, with the same tie-break as the public player. At unlabelled decisions it plays the
fast teacher networks. The teacher receives only the engine's seat context. Its values
are recorded for every legal action with the ordinary layout-1 encoder and sample format.
Bidding stays fixed; `fit --in <warm-start>` preserves any network with no samples, and
refuses to leave a random network in its place when no warm start was supplied.
Fitting includes the final partial batch, and retains warm-start values of unlabelled
actions. New random networks still initialise unlabelled actions pessimistically.

Tests cover centred-loss gradients through every network layer, output gradients on both
sides of Huber clipping, cancellation of common deal offsets, absolute-value calibration,
empty and single-action masks, and exact search-label/feature parity through a whole game.

The centred pilot also scored 51.0% +/- 0.2 pp against the equal-duration control over
20,000 games (+1.5 points/game, +7 ELO). Its engine timing was 16.3 us/card over 21,516
card decisions in 100 games after warmup. The small lineage gain is not a promotion;
independent ISMCTS evaluation is required. Timing differences between these identical
architectures reflect run variability and scheduling, not an inference optimisation.

### Capacity and fitting diagnostics

Both 15-minute continuations finished at 48.0% +/- 2.2 pp against ISMCTS over 400 games
(seed 30, 100 ms, 10 threads, with no concurrent training). The centred loss has not
shown an independent strength gain, despite its small advantage over the frozen networks.
Neither candidate is promoted. These short checks do not rule out smaller improvements.

`expand --in <folder> --out <wider-folder> --sizes 640,384,192 --seed 371` widens the card
MLPs while preserving their initial function. The old units and weights are copied;
new units have random incoming weights and zero connections into old units. Those zero
connections can learn immediately. Bidding remains byte-for-byte unchanged. This lets
an equal-data experiment test capacity without discarding the warm start. The candidate
shape is 600-640-384-192-32; inference timing still needs an idle benchmark.

`fit --fit-checkpoints true` retains each epoch for later matches. Optional
`--validation-data <independent-prefix>` uses separately generated games for validation;
otherwise the original random 5% sample split is retained, which can contain decisions
from games also represented in training. The logs report absolute and centred RMSE in
points, teacher regret (best teacher value minus the teacher value of the student's
choice), and the fraction choosing an equally best labelled action. `diagnose --in
<folder> --data <prefix>` reports these measures without training. These are descriptive
fit diagnostics, not independent match-strength estimates.

Tests verify legal-action masking and point units in diagnostics, use of independent
validation samples, complete per-epoch checkpoints, expansion parity before and after
16-bit export, refusal of shrinking/mismatched layer counts, and learning through the
new units. The full ClaudePlayer suite has 69 passing tests and builds without warnings.

### Optional CUDA fitter and first search-teacher fits

`tools/NeuralTrainer/Gpu/fit.py` is a training-only PyTorch implementation of the same
masked ordinary/centred Huber objectives. It reads the existing sparse samples and
writes checked-header BNN1 files for the unchanged managed runtime. See its
[README](tools/NeuralTrainer/Gpu/README.md) for the isolated Python 3.12/CUDA setup,
reproduction commands, limits, and tests. The global CPU-only Python installation
is unchanged. Seven Python tests cover formats, gradients, masks, diagnostics, and
partial batches/empty-contract preservation. C# successfully loaded an exported
model; diagnostics on 307 real decisions matched Python to 0.001 point.

A synthetic 47,500-sample fitting epoch took 0.66-0.76 seconds on the RTX 2080 SUPER,
versus approximately 2 seconds in the eight-learner C# fitter. Both measurements ran
during teacher collection; they establish a useful training option, not a controlled
hardware benchmark. GPU fitting is faster enough to compare more settings cheaply.

The first periodic search-100 dataset contains roughly 100,000 labelled card decisions.
These pilots use the same data, seed 401, batch 1024, 12 epochs, learning rate 5e-5
(1.5e-5 for the last four epochs), and frozen bidding. The wider warm start uses
640/384/192 units; others use the shipped 512/256/128 shape.

| Search-teacher student | vs frozen baseline, seed 29 | Fit evidence |
|---|---|---|
| Ordinary Huber, epoch 12 | 47.7% +/- 0.2 pp, 20,000 games, -3.5 points/game, -16 ELO | All-trumps validation RMSE fell from 8.329 to 4.275 points, but teacher regret only from 0.666 to 0.650 |
| Centred Huber, value weight 0.05, epoch 12 | 48.7% +/- 0.5 pp, 4,000 games, -2.3 points/game, -9 ELO | All-trumps teacher regret 0.604 points |
| Wider, centred Huber, epoch 12 | 48.0% +/- 0.5 pp, 4,000 games, -3.1 points/game, -14 ELO | No capacity gain in this pilot |
| Pure action differences, value weight 0, epoch 12 | 48.2% +/- 0.5 pp, 4,000 games, -2.7 points/game, -12 ELO | Removing mean-value learning does not rescue the pilot |
| Policy KL, temperature 2 points, mean-value weight 0.01, epoch 12 | 48.1% +/- 0.5 pp, 4,000 games, -3.3 points/game, -13 ELO | All-trumps teacher regret 0.642 points; no strength gain |

All uncertainties are one standard error across mirrored pairs. These students fail
the first promotion gate. Lower fitting error does not establish stronger decisions.
The default random sample validation split shares games with training; separately
seeded search-teacher validation games and the full 2,000-game training dataset are
being collected. The GPU fitter has eight passing tests, including finite-difference
policy-KL gradients, illegal-action masking, and offset invariance. No weights or app
levels have changed.

The 30-minute collection checkpoint contains about 181,000 decisions. Continuing
centred fitting for 24 epochs on it scored 47.6% +/- 0.6 pp over 4,000 games
(-3.4 points/game, -17 ELO). An additive 128/64/64 branch with the original model
frozen, zero initial output, and learning rate 0.001 scored 48.0% +/- 0.5 pp at
epoch 4, 41.1% +/- 0.6 pp at epoch 12, and 39.0% +/- 0.6 pp at epoch 24 (4,000
games each). It overfit: on separately seeded 100-game teacher validation data,
all-trumps teacher regret rose from 0.62 to 0.73 points. It is rejected. Its
branches export as one ordinary MLP; nine Python tests include frozen-parameter
and merged/exported prediction checks. More parameters alone have not helped.

At this stage the Engine (741), UI (71), and prior ClaudePlayer (69) tests passed.
The Windows and Android app builds both completed with zero warnings and errors.

`distill --teacher neural --teacher-play-chance 0` records the same public search
values but lets the fast student choose the played card. The default remains 1
(play the teacher at labelled decisions). This separates label generation from
the policy generating positions. The next experiment uses 10 sampled worlds per
position on student trajectories: noisier targets permit more distinct positions
at the same collection cost. It does not deploy the already-known weak search-10
action selector. This is an experiment, not evidence of a gain. The expanded
73-test ClaudePlayer suite verifies both trajectory modes against public search
values and seat features, including refusal of invalid play probabilities.

The direct teacher recheck scored **62.0% +/- 2.8 pp against the frozen fast
player over 200 games** (seed 48173, +19.0 points/game, +85 ELO). Fixed 100-world
search ran without a time cap; the independent opponent was the fast network.
The unsuccessful students therefore do not invalidate the stronger teacher.

The first student-trajectory/search-10 fit used 212,814 labelled decisions from
1,000 games, with the independent 100-game search-100 dataset for validation.
At learning rate 5e-5, centred loss weight 0.05, it scored 48.7% +/- 0.5 pp at
epoch 4, 48.4% +/- 0.5 pp at epoch 12, and 47.6% +/- 0.5 pp at epoch 24 against
the frozen player (4,000 games each). It is not promoted. More diverse states
alone did not rescue this small-data experiment.

Search collection now has an optional training-only GPU backend: `--teacher
neural-gpu`, with `Gpu/serve.py` serving the same frozen networks on loopback.
It advances independent rollouts together, preserving the C# simulator and
seat encoder. Managed batch values match the original search in the whole-game
tests; CUDA matched 96,585 rollout choices and all 416 records from two paired
collection runs. One-actor collection took 55 seconds in C#, 27 on eager CUDA,
and 17 with CUDA graphs; a four-actor pilot collected 4,183 positions in 76 seconds.
The machine was also collecting the larger CPU dataset, so these are throughput
pilots. See the [GPU README](tools/NeuralTrainer/Gpu/README.md) for commands,
protocol checks and numerical limits. The app has no GPU or Python dependency.

Suit augmentation is restricted to positions without bids in any permuted suit
(and fixes the trump slot). It maps every card plane, output and legal mask
together, preserving the observed auction. On the 30-minute dataset, with the
independent validation prefix and 24 epochs, it scored 48.0% +/- 0.5 pp against
the frozen player over 4,000 games (-2.6 points/game, -14 ELO). It is not a gain.
This fit used all training records; the earlier default holdout used 95%, so
their difference is not an isolated estimate of the augmentation effect.

The next target check anchors each position's mean Q target to the warm start
while retaining the teacher's action differences. It tests preservation of the
useful value representation when the teacher samples hidden hands differently
from the true-deal training distribution. Fourteen Python tests pass, including
augmentation/auction consistency and exact preservation of teacher action gaps
under mean anchoring. On the 30-minute dataset the anchored fit scored
48.2% +/- 0.5 pp over 4,000 games at epoch 24 (-2.6 points/game, -13 ELO).

The completed 2,000-game teacher dataset contains 431,684 positions: 149,554 suit,
75,404 no-trumps and 206,726 all-trumps. Collection took 1:17:40. The separately
seeded 100-game validation dataset contains 22,213 positions and is excluded from
training. Two matched 24-epoch fits use every training record, learning rate
5e-5 (1.5e-5 in the last eight epochs), centred loss weight 0.05, batch 1024 and
seed 401. Bidding remains frozen.

| Full-data fit | Epoch 4 | Epoch 12 | Epoch 24 |
|---|---|---|---|
| Centred targets | 48.9% +/- 0.2 pp | 48.5% +/- 0.2 pp | 48.4% +/- 0.2 pp |
| Mean-anchored targets | 49.2% +/- 0.2 pp | 49.1% +/- 0.2 pp | 49.1% +/- 0.2 pp |

Each cell is 20,000 games against the frozen network, seed 29, one standard
error across mirrored pairs. Neither passes the first gate. More teacher data
reduced the regression, but these fits have not recovered the teacher's gain.
The next collection holds search at 100 worlds and uses fast-network trajectories
(`--teacher-play-chance 0`, 1,000 games, seed 8371) to separate target noise from
the trajectory change in the earlier search-10 experiment.

### Shared card head and auxiliary card-location supervision

A shared correction head was tested on branch `neural-shared-card-head`, commit
`9983543`. It freezes the existing MLP and shares a 32-unit correction function
across cards, reading pretrained final features, a card's 16 existing input
planes, rank and suit embeddings. It adds 5,057 parameters per card network.
The branch includes checked version-2 weight files and managed inference, but
is not merged into the production runtime.

On the full search100 dataset, mean-anchored centred targets, learning rate .001
(then .0003), 36 epochs, batch 1024 and seed 401, it scored 48.1% +/- .5 pp at
epoch 4, 47.9% +/- .5 at epoch 12, 47.3% +/- .5 at epoch 24 and 47.1% +/- .5 at
epoch 36 against the frozen player. Each is 4,000 mirrored games, seed 29. The
final difference was -3.6 points/game, -20 Elo. It fails the first promotion
gate. The branch passed 76 C# and 16 Python tests, including independent scalar
inference, finite-difference gradients, frozen base and file corruption checks.
Exported C#/Python diagnostics on all 22,213 independent validation positions
agreed to 0.001 point. No inference-speed claim or ISMCTS gain is made for it.

The next experiment tested true-card locations as an auxiliary training objective,
inspired by the Skat and DouZero+ evidence in the research review. It differs from
DouZero+: predictions do not become explicit policy inputs. A training-only head
shares the final hidden representation with Q-values, learns the relative owner
of unseen cards, and is discarded when exporting the original MLP. A matched
zero-weight control uses identical data, warmup and Q training. Thus inference
architecture, inputs, point units and feature layout stay unchanged.

`record-selfplay` records frozen-baseline Monte Carlo Q targets and a separate
checked ownership sidecar. The comparison used 100,000 training deals
(seed 3931) and 5,000 independent validation deals (seed 2931), 3% card exploration,
frozen bidding, then four epochs at learning rate 1e-5. See the GPU README for
reproduction. Collection produced 1,884,525 training positions in 4:22 and 94,603
validation positions in 0:13. Both runs used the same samples, seed 1401,
batch 1024, centred Q-loss weight .05 and four epochs; the auxiliary weight was
.01. Against the frozen player, each 20,000 mirrored games, seed 29:

| Fit | Epoch 1 | Epoch 2 | Epoch 4 |
|---|---|---|---|
| Q-only control | 49.9% +/- .2 pp | 49.7% +/- .2 pp | 50.0% +/- .2 pp |
| Q plus card-location loss | 49.9% +/- .2 pp | 49.7% +/- .2 pp | 49.9% +/- .2 pp |

The auxiliary objective did not improve playing strength in this pilot. All-trumps
held-out ownership accuracy rose from the control's 41.6% to 42.1%, which is a
prediction diagnostic rather than evidence of better card choices. A stronger
auxiliary weight (.1) scored 49.6% +/- .2 pp at epoch 1, 49.2% +/- .2 at epoch 2,
and 49.2% +/- .2 at epoch 4 (20,000 games each, seed 29). Its final difference
was -1.1 points/game, -6 Elo. The 75-test C# suite
and 17 Python tests pass; they include a direct check that removing all hidden
hands changes ownership labels without changing any policy input.

A further search-target pilot used a two-point minimum teacher regret: keep
the teacher labels only where its best card exceeds the warm start's choice by
at least two points, and preserve the warm start's Q-values elsewhere. This tests
whether changing small, noisy preferences causes the earlier regression. It is
not a confidence interval on the search estimates. The fitter's tests cover the
threshold, legal-action masking and preservation of the original validation data.
It retained teacher labels at 33,982 of 431,684 positions, with warm-start targets
elsewhere, then used mean anchoring and the matched 24-epoch fit settings above.
Epochs 4/12/24 scored 50.2% +/- .2 pp, 50.4% +/- .2 pp and 50.3% +/- .2 pp against
the frozen player over 20,000 games each (seed 29). Epoch 24 was +.6 points/game,
+2 Elo. This removes most of the regression but is not a demonstrated ISMCTS gain.

The public-history prototype is isolated on branch `neural-public-history`.
It appends 64 scalar inputs specifying the trick number and position within it
for each public card, bumps feature layout to 2, and explicitly extends the old
weights with zero input rows before retraining. This initially preserves the
playing function. Its tests pass: 78 C# and 20 Python, including engine/view parity,
history reconstruction, struct-copy independence, widened sample counts, layout
refusal and export checks. Across the 94,603 validation records, all original
features, Q targets and ownership labels exactly match the layout-1 collection;
the history fields are additional public information. The production runtime stays
on layout 1.

The public-history branch (`1d06f40`) collected the same 100,000 training deals,
yielding 1,884,525 positions. Matched four-epoch fits used learning rate 1e-5,
batch 1024, seed 1401 and centred Q-loss weight .05. Every cell below is 20,000
games against the function-equivalent layout-2 baseline, seed 29, one standard
error across mirrored pairs:

| Public-history fit | Epoch 1 | Epoch 2 | Epoch 4 |
|---|---|---|---|
| Q-only | 50.2% +/- .2 pp | 50.0% +/- .2 pp | 50.0% +/- .2 pp |
| Q plus card-location loss, weight .1 | 49.7% +/- .2 pp | 49.1% +/- .2 pp | 49.2% +/- .2 pp |

These short, low-learning-rate fits provide no evidence of stronger play. They
do not establish that public history is useless: the 64 added input rows start
at zero, and longer online training may be needed to learn from them. No trained
history model is promoted, and no idle speed claim is made for this branch.

### Search labels on student trajectories, completed

The 1,000-game search-100 collection on fast-student trajectories finished in
50:16, with 213,144 positions (72,504 suit, 38,936 no-trumps, 101,704 all-trumps).
The teacher, bidding, independent validation data and 24-epoch fitting schedule
match the earlier full-data tests. Every cell is 20,000 games against the frozen
network, seed 29, with one standard error across mirrored pairs:

| Fit | Epoch 4 | Epoch 12 | Epoch 24 |
|---|---|---|---|
| Mean-anchored teacher targets | 49.2% +/- .2 pp | 48.4% +/- .2 pp | 48.4% +/- .2 pp |
| Mean anchoring plus two-point teacher-regret filter | 50.4% +/- .2 pp | 50.1% +/- .2 pp | 50.0% +/- .2 pp |

Neither recovers the teacher's gain. Search quality, trajectory distribution,
loss choice and additional capacity have all been tested; the present evidence
does not justify replacing the deployed neural weights with these students.
Reproduce with the GPU README's collection/fitting commands, adding
`--teacher-play-chance 0 --games 1000 --seed 8371` for collection and
`--anchor-mean` (optionally `--minimum-teacher-regret 2`) for fitting.

### Bounded endgame search candidate

Branch `neural-bounded-endgame`, commit `0417b5c`, retains the frozen networks and
adds optional managed C# endgame search. It enumerates hands consistent with
public observations, solves each by partnership alpha-beta, and averages every
legal action's game-point outcome. Future choices within each world have perfect
information: this is a PIMC approximation, not an exact information-set value.

The default optional horizon is two tricks, at most 90 worlds. A three-trick
setting solves positions with at most eight consistent worlds and otherwise
falls back. An additional declaration model reconstructs hypothetical original
hands from remaining and publicly played cards. It conditions on the bots'
policy of declaring all available combinations; humans who withhold declarations
can violate that assumption. Actual secret hands never enter evaluation.

Development matches against the frozen network, 20,000 games each, seed 29:

| Variant | Win rate, one standard error | Points/game | Elo |
|---|---|---|---|
| Two tricks, known declaration ranks only | 51.0% +/- .1 pp | +1.7 | +7 |
| Two tricks, declaration model | 52.3% +/- .1 pp | +3.6 | +16 |
| Three tricks, eight-world cap and declaration model | 53.0% +/- .1 pp | +4.5 | +21 |

Independent baseline checks of the fixed three-trick candidate scored
53.0% +/- .1 pp over 20,000 games at seed 97 (+4.6 points/game, +21 Elo), and
53.2% +/- .1 pp over 20,000 games at seed 1000000007 (+4.4 points/game, +22 Elo).
`validate` multiplies the supplied seed by 100,000 before adding each pair index;
these ranges are distinct. The latter seed wraps to starting RNG seed 277147232
under the current unchecked C# arithmetic. The seed-97 ISMCTS run was interrupted
before any score was reported; its replacement uses 500 pairs at seed 1000000007.
The completed independent match scored **52.4% +/- 1.4 pp against ISMCTS at
100 ms over 1,000 games**, +2.7 points/game, +17 Elo (20:52, 10 threads, no
concurrent training or builds). The approximate 95% interval is 49.7-55.1%, so
this candidate **does not pass the promotion gate**. It remains experimental.

Idle engine benchmarks after 20 warmup games, then 100 measured games:

| Variant | Mean us/card | Card decisions |
|---|---|---|
| Frozen network | 19.2 | 21,336 |
| Two tricks plus declaration model | 22.4 | 21,286 |
| Three tricks plus declaration model | 25.0 | 21,391 |

The last configuration meets the latency budget, with 3,612 endgame evaluations.
It is a stronger fast candidate against its lineage; its neural weights have
not improved. Eighty C# tests pass on the branch, including alpha-beta versus
exhaustive play, independent world enumeration, hidden-hand independence,
three-trick overflow fallback, scoring variants and engine/view parity.
See `ENDGAME_EXPERIMENT.md` on that branch for implementation and commands.
Next tests will vary the bounded three-trick world count and fit endgame targets
while retaining the original network's targets elsewhere. No app level changes
are justified by the current evidence.

The next sweep added a configurable three-trick world limit (branch `ede8ba8`).
Every two-trick world is still included. Development results against the frozen
network, seed 29, 20,000 mirrored games per row:

| Three-trick world limit | Win rate, one standard error | Points/game | Elo |
|---|---|---|---|
| 8 | 53.0% +/- .1 pp | +4.5 | +21 |
| 32 | 53.8% +/- .2 pp | +5.6 | +27 |
| 64 | 54.5% +/- .2 pp | +6.5 | +31 |
| 90 | 54.7% +/- .2 pp | +6.9 | +33 |

The 90-world version scored **89.7% +/- .2 pp against SmartPlayer over 20,000
games** (seed 197, +63.5 points/game, +377 Elo). It has not yet passed an
independent ISMCTS gate. The earlier 52.4% ISMCTS result is for eight worlds.

Timing varied substantially between the first sweep and three subsequent
interleaved runs, all with training/builds stopped. Keep both measurements;
the cause of the difference is not established. Each entry uses 100 games after
20 warmup games; these are mean times per non-forced engine card callback:

| Configuration | First sweep, us/card | Three repeated runs, us/card | Decisions per run |
|---|---|---|---|
| Frozen network | 28.3 | 14.9, 14.0, 13.6 | 21,336 |
| 32 worlds | 31.0 | 21.6, 18.2, 18.8 | 21,377 |
| 64 worlds | Not measured | 28.0, 29.0, 28.0 | 21,317 |
| 90 worlds | 54.9 | 26.0, 25.7, 26.1 | 21,380 |

The 90-world teacher collected 2,132,215 card positions from 10,000 whole games
in 24 seconds (seed 120001), retaining original network Q targets wherever
the endgame does not apply. It plays the fast student's chosen cards. A separate
250-game collection (seed 220001) supplies 53,821 validation positions. Training
is now inexpensive enough to test a larger dataset; the CUDA fitter's optional
`--stream-data` keeps it in CPU memory and transfers batches to the GPU.
Eighteen Python tests pass, including identical exported weights between resident
and streamed data on CPU and CUDA. The endgame branch passes 84 C# tests.

```powershell
dotnet artifacts/neural-20260927/endgame-tune-bin/NeuralTrainer.dll distill --teacher endgame --in artifacts/neural-20260927/baseline --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --card-label-chance 1 --teacher-play-chance 0 --games 10000 --threads 12 --seed 120001 --data artifacts/neural-20260927/endgame-teacher90/data
dotnet artifacts/neural-20260927/endgame-tune-bin/NeuralTrainer.dll distill --teacher endgame --in artifacts/neural-20260927/baseline --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --card-label-chance 1 --teacher-play-chance 0 --games 250 --threads 12 --seed 220001 --data artifacts/neural-20260927/endgame-teacher90-validation/data
artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit.py --in artifacts/neural-20260927/baseline --data artifacts/neural-20260927/endgame-teacher90/data --validation-data artifacts/neural-20260927/endgame-teacher90-validation/data --out artifacts/neural-20260927/endgame-teacher90/student-1e-5 --epochs 12 --batch 1024 --learning-rate 1e-5 --card-value-weight .05 --anchor-mean --stream-data --seed 2401
```

The matched second fit uses learning rate `5e-5`. Epochs 4 and 12 will be
screened against the frozen player before any independent ISMCTS test.

The `1e-5` student scored 50.0% +/- .2 pp at epoch 4 and 50.3% +/- .2 pp at
epoch 12 against the frozen player (20,000 games each, seed 29). Epoch 12 was
+0.5 points/game, +2 Elo. This does not recover the endgame teacher's gain.

A matched depth candidate inserts two identity-initialised 128-unit ReLU layers
before each card output: 600-512-256-128-128-128-32, total files 3,172,998 bytes
versus the original 2,974,830. Bidding, features, format and managed runtime stay
the same. `Gpu/deepen.py` and its tests implement the transformation described in
the research review. Before fitting, C# diagnostics on all 53,821 validation
positions match the original at printed precision, and 20,000 mirrored games
against the original give exactly 50.0% +/- 0.0 pp and zero point difference.
Twenty Python tests pass, including export equivalence and learning gradients
in the inserted layers. The deeper `5e-5` fit uses the same data, epochs and seed
as the ordinary student; strength and idle timing results are pending.
