# The Belot Neural Player

`ClaudePlayerNeural` values every legal bid and card in game points. Its default
constructor uses the four small actor networks; `Temperature` and `MaxRegret`
allow weaker play. The strongest validated profile combines that policy with
separate ownership predictions and bounded endgame search.

**Current Master, September 29, 2026: belief5-v2-ensemble.** On the i7-12700K, all of the
following were independent mirrored whole-game comparisons with other jobs
stopped. The errors are one empirical standard error over mirrored pairs:

| Opponent | Win rate +/- one SE | Games |
|---|---:|---:|
| ClaudePlayerIsmcts, 100 ms/card | **62.000% +/- 1.247 pp** | 1,000 |
| Previous Master, 100 neural rollouts | **59.100% +/- 1.152 pp** | 1,000 |
| Previous fast profile, three-trick endings | **60.120% +/- .343 pp** | 10,000 |
| Frozen pure network | **63.730% +/- .349 pp** | 10,000 |
| SmartPlayer | **92.520% +/- .253 pp** | 10,000 |
| Adapted SharpBelot | **87.080% +/- .324 pp** | 10,000 |
| Adapted Belot 2.06 (2001) | **77.150% +/- .395 pp** | 10,000 |
| belief5-v1, without suit averaging | **50.930% +/- .217 pp** | 20,000 |

The idle card benchmark averages **1.449 ms**, with **6.083-ms p99**, an observed
maximum of **8.006 ms**, and **0/21,246 decisions above 10 ms**. The profile has
an 8-ms search cap; scheduling and garbage collection prevent a universal hard
real-time guarantee. All seven embedded files total **3,523,482 bytes** and
inference is pure managed C#. Enable the exact app profile with:

```csharp
ClaudePlayerProfiles.CreateMaster();
```

The original actor files are unchanged. The default pure network previously
measured **50.000% +/- .978 pp against ISMCTS100 over 2,000 games**; its fresh idle
benchmark is **13.6 us per card**. Its three-trick/90-world fast profile previously
passed at 54.100% +/- 1.386 pp over 1,000 games and now measures 27.4 us/card. Hints retain that
cheap profile; Expert adds temperature 1.5 and MaxRegret 4. The previous Master
remains available as `ClaudePlayerProfiles.CreateRolloutMaster()`.

The final app calibration is complete: **400,600 games** in **1:03:02**,
with Master rated **1838 +/- 2.5**, Expert **1611 +/- 2.2** and Skilled
**1462 +/- 1.7** (160,120 games involving each). Expert retains its settings
because it remains between Skilled and Master. The complete ratings, bootstrap
uncertainty and reproduction command are in [the final calibration](#final-v2-app-calibration).

Section 16 records the new promotion, controls and follow-ups. See
[FAST_BOT_EXPERIMENT.md](FAST_BOT_EXPERIMENT.md) for complete results, failed
experiments and reproduction commands; section 14 preserves the earlier PPO
promotion and its small all-trump gain. This document also explains the original
architecture, training and measurement conventions below.

**Earlier measurements** (September 2026, mirrored pairs, i7-12700K):

| Player | vs ClaudePlayerIsmcts (100 ms) | vs SmartPlayer | Time per card |
|---|---|---|---|
| Networks alone | **50.2% ± 1.3%** (1,000 games, −2.2 points a game) | 86.2% ± 0.2% (20,000 games, +319 ELO) | **8.6 µs** |
| Networks + 100-deal search (`SearchDeals = 100`) | **55.0% ± 2.2%** (400 games, +5.6 points a game) | | 51 ms |
| ClaudePlayerIsmcts, for scale | | 89.7% ± 1.7% (300 games, +375 ELO) | 100 ms |

The historical 8.6-us figure uses the older mixed decision benchmark. Corrected
engine-card timing is reported in section 12; these timings are not interchangeable.
The preceding app round robin (`elo`, pair ratings
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
  (MAUI) unchanged. Training can use the C# CPU trainer or the optional CUDA
  PyTorch tooling; no Python or native training dependency ships in the player.

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

Network evaluation uses one forward pass over the sparse inputs: the first layer sums only the weight rows
of the non-zero inputs (≈60–120 of 600), the hidden layers skip the units the ReLU zeroed, and
the rows are added a SIMD vector of outputs at a time (Santase's layout: weights transposed,
four vector accumulators). The final September 29 warmed engine-card benchmarks measured
**13.6 us/card** for networks alone, **27.4 us/card** for the three-trick hint profile,
and **1.449 ms/card** for the selected Master, each on one desktop core over 100 games.
The search-free suit ensemble measures **68.9 us/card**. ISMCTS uses 100 ms/card.
Older self-play timings include bidding
and omit some engine-context work; use the corrected engine-card benchmark for comparisons.

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

## 12. September 27 improvement experiments

### Short research note

The [primary-paper review](etc/NeuralResearch.md) covers search distillation,
hidden-card inference, partnership learning, privileged critics and compact network
designs. The experiments below were tested against the frozen shipped player;
independent ISMCTS matches remain the promotion gate. Privileged-critic PPO
remains a future experiment.

- **Better targets:** centred Monte Carlo loss, search-100 labels, policy KL,
  mean anchoring, suit augmentation and student trajectories produced ties or
  regressions. More accurate value prediction did not reliably improve decisions.
- **Architecture and information:** wider and residual models, a shared card head,
  ordered public history and auxiliary card-location supervision did not produce
  a promotable student in these pilots. Two added identity-initialised layers
  preserved the warm start, but their trained student scored only 50.4% +/- .2 pp
  against it over 20,000 games. These pilots do not rule out longer training or PPO.
- **Fast endgames:** public-hand enumeration plus bounded double-dummy continuations
  is the promoted fast configuration. With up to 90 worlds in the final three tricks,
  it scored 54.65% +/- .168 pp against the frozen player and 89.74% +/- .206 pp against
  SmartPlayer, each over 20,000 games, at 31.1 us/card in the integrated idle benchmark.
  Its separate 2,000-game ISMCTS confirmation scored 53.45% +/- .942 pp,
  95% interval [51.605%, 55.295%], passing promotion. It uses the original neural
  weights. No tested replacement network earned promotion.

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

The matched second fit uses learning rate `5e-5`. Epochs 4 and 12 were
screened against the frozen player before considering an independent ISMCTS test.

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
as the ordinary student; the completed measurements follow.

The completed ordinary `5e-5` fit scored 49.9% +/- .2 pp at epoch 4 and
50.3% +/- .2 pp at epoch 12; the deeper fit scored 50.0% +/- .2 pp and
50.4% +/- .2 pp respectively. Each is 20,000 games against the frozen player,
seed 29. Final point differences were +.4 and +.5 points/game (+2 and +3 Elo).
Neither recovers enough of the stronger teacher to justify neural-only promotion.

With all training and other matches stopped, the final idle benchmark measured
20.0 us/card for the frozen network (21,336 decisions), 20.5 for the deeper
epoch-12 student (21,262), and 32.3 for the frozen network plus 90-world endgame
(21,380). Each uses 100 measured games after 20 warmup games. The depth change
fits the latency/storage constraints but has not established useful playing gains.

The fixed 90-world configuration's fresh baseline comparison at seed 293 scored
54.65% +/- .168 pp over 20,000 games, 95% interval [54.320%, 54.980%], +6.7
points/game, +32 +/- 1 Elo. A predeclared **2,000-game** ISMCTS100 confirmation
uses that same separate seed range and 10 threads, with other heavy work stopped.
It completed at **53.45% +/- .942 pp**, 95% interval **[51.605%, 55.295%]**,
+5.3 points/game, **+24 +/- 7 Elo**, in 39:08. This passes the predeclared gate.
Trainer match summaries now expose the precise normal interval
and the delta-method Elo standard error, with four new tests (88 C# tests total
on branch `53fa698`). Promotion uses the interval rather than a rounded sigma.

The existing 100-deal search player scored 54.5% +/- 2.5 pp against the fast
90-world endgame configuration over 200 games (seed 297, +3.1 points/game,
+31 Elo). This fixed-sample comparison used no time cap and ran alongside
training. It suggests a remaining search gain, but its 95% interval includes
50%. The larger idle comparison below establishes the advantage under the app budget.

### Integration and app calibration

The optional endgame implementation is integrated into `master`. It adds no native
runtime dependency or weight bytes. A review found a hint-specific edge case:
`RoundKnowledge` reconstructs the deciding seat's melds under the bot's declare-all
policy. The endgame evaluator now falls back when that conflicts with the seat's
actual declarations. This preserves hints for a human who withheld a meld.

The integrated build passes **94 AI/trainer tests**, including 90-world
engine/view parity, withheld-meld fallback, and five Elo-statistics tests. The
Release trainer and simulator builds have zero warnings/errors. Re-recording the
250-game validation corpus produces identical features and half-precision labels
for all **53,821 positions**. Repeating the baseline and Smart comparisons on the
same seeds gives the same outcomes; these repetitions are not added to game counts.
The precise Smart result is **89.74% +/- .206 pp**, 95% interval
[89.337%, 90.143%], **+377 +/- 4 Elo**, over 20,000 games.

Idle engine benchmarks after integration:

| Profile | Mean us/card | Decisions | Measured games |
|---|---|---|---|
| Original network | 16.2 | 21,336 | 100 |
| Fast, 90-world endgames | 31.1 | 21,380 | 100 |
| Search 100, 400-ms cap | 62,081.0 | 672 | 4 |
| Search 100 plus endgames, 400-ms cap | 60,869.0 | 707 | 4 |

Warmups are excluded (20 games for fast profiles, one for search). Endgames handled
4,996 fast decisions and 175 combined-search decisions. The small four-game search
timing difference alone does not establish an advantage for the combined profile.

Expert calibration against SmartPlayer, 20,000 games per row, seed 311, MaxRegret 4:

| Expert profile | Temperature | Win rate | Elo difference |
|---|---|---|---|
| Previous networks-only Expert | 1.25 | 69.845% +/- .298 pp | +146 +/- 2 |
| New fast profile | 1.0 | 78.425% +/- .269 pp | +224 +/- 3 |
| New fast profile | 1.25 | 74.750% +/- .283 pp | +189 +/- 3 |
| New fast profile | **1.5** | **70.920% +/- .294 pp** | **+155 +/- 2** |

Choose **1.5**: it keeps Expert close to its previous difficulty while remaining
above Skilled. Against the unrestricted fast profile it scores **26.465% +/-
.276 pp**, -178 +/- 2 Elo, over 20,000 independent games (seed 313). Hints use the
unrestricted fast profile. The final app round robin is reported below.

The predeclared Master selection compared the existing search against fast endgames
over 1,000 games (seed 397), then the combined search/endgame profile against the
existing search over 500 games (seed 401). Both used 100 sampled deals, the app's
400-ms cap, 10 threads, and no other heavy work. The selection rule kept the
existing Master unless the combined profile's independent 95% interval lay above 50%.

The first completed match supports retaining search for Master: **53.1% +/-
1.255 pp over 1,000 games** against the fast endgame profile, 95% interval
**[50.641%, 55.559%]**, +4.6 points/game, **+22 +/- 9 Elo**, in 19:25.
This used the actual 400-ms cap.

The combined search/endgame profile scored **52.8% +/- 1.667 pp over 500 games**
against existing Master, 95% interval **[49.532%, 56.068%]**, +2.0 points/game,
**+19 +/- 12 Elo**, in 19:36. Its interval crosses 50%, so it remains unpromoted.
**Master retains SearchDeals 100 and a 400-ms budget, with endgames disabled.**
Hints and Expert use the validated fast endgame profile; Expert uses temperature
1.5 and MaxRegret 4. The search budget is checked between whole sampled deals,
with at least eight played, so a decision may exceed that budget.

```powershell
dotnet artifacts/neural-20260927/promoted-bin/NeuralTrainer.dll validate --in artifacts/neural-20260927/baseline --opponent ismcts:100 --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --pairs 1000 --threads 10 --seed 293
dotnet artifacts/neural-20260927/promoted-bin/NeuralTrainer.dll validate --in artifacts/neural-20260927/baseline --opponent smart --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --pairs 10000 --threads 16 --seed 197
dotnet artifacts/neural-20260927/promoted-bin/NeuralTrainer.dll bench --in artifacts/neural-20260927/baseline --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90
```

The final ISMCTS match used the pre-integration `endgame-final-bin` build. The
integrated command above is its reproduction; the added own-meld fallback does
not affect declare-all bot games, as the replay and repeated matches verify.


## 13. Final app calibration and verification

The pre-PPO `elo 20000 60` run completed in **21:36** on an otherwise idle i7-12700K:
**241,080 whole games**, 20,000 mirrored pairs per fast matchup and 60 per matchup
involving Master or ISMCTS. Expert uses the fast endgame profile at temperature 1.5,
MaxRegret 4. Master uses SearchDeals 100, a 400-ms budget, and no endgames.

| App level / reference | Fitted Elo +/- 1 sigma | Games involving this level |
|---|---|---|
| Master | **1799 +/- 26.4** | 600 |
| ClaudePlayerIsmcts, 100 ms | 1770 +/- 26.7 | 600 |
| Expert | **1575 +/- 2.1** | 120,240 |
| Skilled / SmartPlayer | **1464 +/- 1.7** | 120,240 |
| Beginner / DummyPlayer | 1200 (fixed anchor) | 120,240 |
| RandomPlayer | **660 +/- 3.1** | 120,240 |

These were the pre-PPO ratings in `AiLevels.cs`; section 16 records the current
calibration. They use the existing Bradley-Terry fit
and 1% fifty-fifty prior. The new uncertainty calculation uses 1,000 bootstrap
replicates of complete mirrored pairs, sharing sampled seed indexes across matchups
and preserving the common 60-pair and additional fast-pair strata. Counts in the
rating table overlap because each game involves two levels. The anchor has no
sampling error by definition. Expert remains between Skilled and Master.

Every matchup is recorded below; uncertainties are empirical standard errors across
mirrored pairs. A zero observed error in a small blowout sample does not establish
a zero underlying chance of a different outcome.

| First player | Second player | First player's win rate +/- 1 sigma | Games |
|---|---|---|---|
| Beginner | Random | 97.335% +/- .080 pp | 40,000 |
| Beginner | Skilled | 11.748% +/- .156 pp | 40,000 |
| Beginner | Expert | 14.650% +/- .171 pp | 40,000 |
| Beginner | Master | .833% +/- .833 pp | 120 |
| Beginner | ISMCTS | .000% +/- .000 pp | 120 |
| Random | Skilled | .663% +/- .040 pp | 40,000 |
| Random | Expert | .973% +/- .049 pp | 40,000 |
| Random | Master | .000% +/- .000 pp | 120 |
| Random | ISMCTS | .833% +/- .833 pp | 120 |
| Skilled | Expert | 28.723% +/- .208 pp | 40,000 |
| Skilled | Master | 10.833% +/- 2.682 pp | 120 |
| Skilled | ISMCTS | 14.167% +/- 3.165 pp | 120 |
| Expert | Master | 25.000% +/- 3.465 pp | 120 |
| Expert | ISMCTS | 25.833% +/- 3.661 pp | 120 |
| Master | ISMCTS | 55.000% +/- 4.065 pp | 120 |

The 120-game Master/ISMCTS result is too small to establish their ordering by
itself. The independent fast-player promotion remains the 2,000-game ISMCTS match
in section 12; this rating run does not replace or enlarge that sample.

Build a separate simulator copy to keep its output unlocked during long runs:

```powershell
dotnet build src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -c Release -o artifacts/neural-20260927/elo-final-bin
cmd.exe /d /c 'dotnet artifacts/neural-20260927/elo-final-bin/Belot.GamesSimulator.dll elo 20000 60 2> artifacts/neural-20260927/final-elo.err | "C:\Program Files\Git\usr\bin\iconv.exe" -f UTF-16LE -t UTF-8 > artifacts/neural-20260927/final-elo.log'
```

The equivalent direct command is `dotnet run -c Release --project
src/Tests/Belot.GamesSimulator -- elo 20000 60`, with the same UTF-16LE conversion.
The simulator now opts out of Windows background power throttling, matching the
trainer. Original binary weights remain unchanged: the promoted configuration
uses them as its normal policy and fallback.


Final verification after integration and rating changes:

| Check | Result |
|---|---|
| Engine tests | 741 passed |
| ClaudePlayer / trainer tests | 94 passed |
| UI tests with final level settings | 72 passed |
| GPU tooling Python tests | 20 passed |
| Release trainer and simulator builds | Zero warnings/errors |
| Windows app build | Zero warnings/errors |
| Android app build | Zero warnings/errors |

The C# test builds also report no analyzer warnings. Existing engine-test warnings
were corrected in test assertions, names and file encoding; production engine
sources and the NuGet engine API are unchanged. All changed C# files use UTF-8 BOM
and CRLF. No app was installed or deployed.

```powershell
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests/Belot.AI.ClaudePlayer.Tests.csproj
dotnet test src/Tests/Belot.UI.Tests/Belot.UI.Tests.csproj
artifacts/neural-20260927/torch-env/Scripts/python.exe -m unittest discover -s tools/NeuralTrainer/Gpu -p "test_*.py"
dotnet build src/UI/Belot.UI/Belot.UI.csproj -f net10.0-windows10.0.19041.0
dotnet build src/UI/Belot.UI/Belot.UI.csproj -f net10.0-android
```

Local logs and test reports are under `artifacts/neural-20260927/`: the final
`final-engine-clean-tests`, `final-ai-tests`, `final-rated-ui-tests`,
`final-python-tests`, `final-windows-build`, `final-android-build`, and `final-elo`
logs. Training data, failed candidates and isolated experimental branches are
retained for reproduction; none replaces the embedded weights.

## 14. PPO with a training-only helper (September 27)

After pushing the preceding work at `e89185b`, the next experiment implemented
on-policy PPO with a privileged critic, inspired by
[PerfectDou](https://arxiv.org/html/2203.16406). The policy still sees only the
original 600 public inputs. Three separate 696-256-128-1 helper networks also see
the other three remaining hands, represented by 96 bits. They are discarded for
deployment. The existing four actor weight files still total 2,974,830 bytes;
bidding weights, inference architecture and feature layout remain unchanged.

This implementation uses the Q outputs as legal-action softmax logits (temperature
in game points), [PPO's clipped objective](https://arxiv.org/pdf/1707.06347), and
selected-action return regression to preserve point units. The helper learns a
residual over the public policy's expected Q. Its predictions are taken before
fitting each new batch to avoid fitting a sample's baseline to its own action.
The initial gamma/lambda are both 1, so the advantage is exactly terminal deal
return minus that baseline. Other lambda settings use links to the same seat's
next decision. This is an adaptation, not an exact PerfectDou reproduction.

`record-ppo` collects complete deals using frozen policies and exact float32
snapshots (training-only BNF1). Checked BPP1 records separate public features from
ownership labels and retain legal masks, old probabilities, outcomes and trajectory
links. The Python trainer verifies weight hashes and C#/CUDA prediction parity
before every update. Deployment exports stay in the existing half-weight BNN1
format. Checkpoints also save critic/optimizer/RNG states; a resumed smoke run
reproduces uninterrupted final actor files byte for byte. Both training processes
opt out of Windows background power throttling.

### Matched helper comparison

The private-helper run and public-helper control each used 12 critic-only warmup
batches and 32 PPO batches of 8,192 deals: **360,448 deals** each. Settings:
temperature 1, actor LR 1e-6, critic LR 3e-4, three epochs each, minibatch 2,048,
clip .2, entropy .001, Q-regression weight 1, KL stop .01, training seed 4401.
Public control keeps the identical helper architecture but zeros its private
inputs. Warmup does not change any actor byte.

All following results are greedy, search-free whole games in mirrored pairs
against the original networks. Error is one empirical standard error across pairs.
Checkpoint development used 4,000 games, seed 521; the table uses independent
20,000-game checks, seed 523. The CLI maps a seed to `seed * 100000 + pairIndex`,
so these runs and the later seed-557 confirmation use disjoint deals. Seed 523
became development data once its results were used to choose follow-ups.

| Helper / checkpoint | Win rate +/- 1 sigma | Games | 95% interval | Points/game |
|---|---|---|---|---|
| Private, development-best iteration 28 | 50.130% +/- .189 pp | 20,000 | [49.759%, 50.501%] | +.4 |
| Private, fixed endpoint 44 | 50.385% +/- .198 pp | 20,000 | [49.997%, 50.773%] | +.5 |
| Public, fixed endpoint 44 | 50.475% +/- .195 pp | 20,000 | [50.092%, 50.858%] | +.6 |

The privileged helper improved fresh-batch return prediction: before fitting
warmup batch 12, explained variance was .713/.518/.660 versus .588/.421/.567
for the public control (suit/no-trump/all-trump). This did **not** establish a
playing advantage from privileged information. Neither initial model has passed
the ISMCTS promotion gate. Small gains against the original alone are insufficient.
The direct private-versus-public endpoint match also tied: **49.795% +/- .182 pp
for the private helper in 20,000 games**, 95% [49.437%, 50.153%], seed 541.

Follow-ups used the private helper and 32 actor updates, changing one setting
from the first run. These are
20,000-game checks on seed 523, again against the original, without search:

| Change | Win rate +/- 1 sigma | Games | 95% interval | Points/game |
|---|---|---|---|---|
| Actor LR 1e-5 | 50.090% +/- .230 pp | 20,000 | [49.640%, 50.540%] | +.1 |
| Temperature .25 | 50.265% +/- .171 pp | 20,000 | [49.929%, 50.601%] | +.7 |
| GAE lambda .5, 32 updates | 50.465% +/- .208 pp | 20,000 | [50.057%, 50.873%] | +.4 |
| GAE lambda .5, 64 updates | 50.450% +/- .218 pp | 20,000 | [50.023%, 50.877%] | +.6 |
| Larger helper, 32 updates | 50.090% +/- .200 pp | 20,000 | [49.699%, 50.481%] | +.1 |
| Larger helper, 64 updates | 50.250% +/- .211 pp | 20,000 | [49.837%, 50.663%] | +.4 |

The higher LR changed roughly 4-10% of greedy choices per early update versus
about 1% at LR 1e-6, without a corresponding strength gain. GAE lambda .5 uses
intermediate helper predictions. Doubling its update count did not show a further
whole-model gain. The seed-523 results inform experiment selection; a final
candidate needs a fresh confirmation seed.

Replacing just one contract network with its GAE-64 version exposed opposing
effects. Other card networks and bidding stayed at the original weights:

| Changed network | Win rate +/- 1 sigma vs original | Games | Seed | Points/game |
|---|---|---|---|---|
| All trumps | 50.610% +/- .179 pp | 20,000 | 523 | +.9 |
| No trumps | 49.945% +/- .129 pp | 20,000 | 523 | +.1 |
| Suit contracts | 49.515% +/- .136 pp | 20,000 | 523 | -.6 |
| All trumps, fresh confirmation | 50.370% +/- .187 pp | 20,000 | 547 | +.6 |

The all-trump-only confirmation has a 95% interval of [50.003%, 50.737%]: a
small gain, close to the significance threshold. These are separate whole-game
matches; contract effects do not add linearly. No ISMCTS result is implied.

The larger 696-512-256-128-1 helper also failed to improve the whole actor. Its
corresponding contract-only checks (20,000 games each, seed 523) gave all trumps
50.485% +/- .172 pp, no trumps 50.165% +/- .116 pp, and suits 49.640% +/- .135 pp.
At identical warmup batch 12 its fresh-batch explained variance was
.705/.512/.641 versus .713/.518/.660 for the smaller private helper. Larger
capacity did not solve either the prediction or playing-strength problem in this
budget. These are single-training-seed pilots, not an architecture ranking.

### Selected all-trump network

The smaller privileged helper with GAE lambda .5 continued from 64 to 128 actor
updates. Including its 12 critic warmup batches, this lineage saw **1,146,880
deals**. Checkpoint selection kept the original bidding, suit and no-trump files
and replaced only `alltrumps.bin`. The following whole-game development checks
all use 20,000 games against the original pure networks, seed 523:

| Saved iteration | Actor updates | Win rate +/- 1 sigma | 95% interval | Points/game |
|---|---|---|---|---|
| 96 | 84 | 50.300% +/- .184 pp | [49.939%, 50.661%] | +.8 |
| 112 | 100 | 50.415% +/- .189 pp | [50.045%, 50.785%] | +.8 |
| 128 | 116 | 50.555% +/- .187 pp | [50.189%, 50.921%] | +1.0 |
| 140 | 128 | 50.650% +/- .189 pp | [50.280%, 51.020%] | +1.0 |

Iteration 140 was fixed before the following seed-557 confirmation. The candidate
is `artifacts/neural-20260927/ppo-gae-long-0140-alltrumps`; its changed file has
SHA-256 `2C8C7E15682B764A231DFF74B461F6B70DF1099C1974C0D30D20FC2675B1E5CB`.
All errors below are one empirical standard error across mirrored pairs.

| Candidate / opponent | Games | Win rate +/- 1 sigma | 95% interval | Points/game |
|---|---|---|---|---|
| Pure network / original pure network | 40,000 | 50.628% +/- .135 pp | [50.364%, 50.891%] | +.9 |
| Pure network / SmartPlayer | 20,000 | 86.980% +/- .226 pp | [86.538%, 87.422%] | +57.9 |
| Original pure network / SmartPlayer (control) | 20,000 | 86.495% +/- .230 pp | [86.044%, 86.946%] | +57.1 |
| Bounded fast profile / original bounded fast profile | 20,000 | 50.620% +/- .187 pp | [50.253%, 50.987%] | +.8 |
| Pure network / ISMCTS100 (seed 563) | 2,000 | 50.000% +/- .978 pp | [48.084%, 51.916%] | +.1 |
| Bounded fast profile / SmartPlayer | 20,000 | 89.835% +/- .205 pp | [89.433%, 90.237%] | +63.9 |
| Bounded fast profile / ISMCTS100 (seed 569) | 1,000 | 54.100% +/- 1.386 pp | [51.384%, 56.816%] | +5.7 |

The confirmed gain is about **+4 Elo**, including with the existing bounded
endgames. It is small. The first matched helper comparison does not establish
that privileged inputs caused it; no matched public-helper run used this entire
longer GAE schedule. Neither helper accuracy nor the lineage comparison alone
establishes a win over ISMCTS.

The pure-network ISMCTS check completed in 40:49 with an exact 50% result.
**PPO has not established a search-free improvement against ISMCTS.** The small
gain against its own lineage did not transfer into a demonstrated benchmark win.

Idle engine-callback timing uses 100 whole games after warmup:

| Profile | Card choices | Mean us/card |
|---|---|---|
| Original pure network | 21,336 | 16.5 |
| Selected pure network | 21,187 | 15.2 |
| Original bounded fast profile | 21,380 | 27.9 |
| Selected bounded fast profile | 21,225 | 27.6 |

Earlier same-day pure-network passes measured 26.4 us for the original and
18.4 us for the candidate. Architecture and parameter counts are identical;
these variable timings do not establish an architectural speed gain. Both
profiles remain below the 50-us limit in the idle confirmation.

The candidate with Master search (100 worlds, 400-ms cap) measured **56.20
ms/card**, 669 choices in four whole games after warmup. Against the new bounded
fast profile it scored **51.500% +/- 1.099 pp in 1,000 games**, seed 577,
95% [49.346%, 53.654%], +4.9 points/game, +10 +/- 8 Elo. This is the higher
point estimate, but does not establish a search advantage at 95% confidence.
Against the original Master with the same 100-world/400-ms settings, it scored
**50.000% +/- 1.184 pp in 1,000 games**, seed 587, 95% [47.679%, 52.321%],
+.3 point/game, +0 +/- 8 Elo. This finds no significant change; it is not an
equivalence proof. Master keeps its existing settings, which have the higher
current point estimate against the fast profile and previously beat the original
fast profile at 53.1% +/- 1.255 pp in 1,000 games. The new comparison alone does
not establish that Master needs search.

Final ISMCTS100 checks were fixed before their results: 2,000 games without
search (seed 563), plus 1,000 games for the existing bounded fast profile
(seed 569). The latter is the proposed app configuration: three tricks, public
declaration constraints, and at most 90 worlds for a three-trick decision.
The bounded fast profile **passes the promotion gate**: its 95% interval is above
50%, its gain over the previous fast profile is confirmed, and its idle card
benchmark is below 50 us. This is a result for PPO plus bounded endgames; the
pure network still ties ISMCTS. The selected all-trump file is now embedded.
A rebuilt-assembly check against the selected folder gave identical play over
2,000 mirrored games (50% with zero observed pair error and equal total points);
this checks export/embedding parity, not playing strength.

### App calibration after PPO promotion

`elo 20000 60` ran with the newly embedded weights in **20:55**, over **241,080
whole games**. The simulator was built separately in Release, then its UTF-16LE
output was piped through `iconv -f UTF-16LE -t UTF-8`. Pair-rating errors below
are one standard deviation from 1,000 shared-seed mirrored-pair bootstrap samples;
Dummy is the fixed anchor.

| Player / app level | Pair Elo +/- 1 sigma | Games |
|---|---|---|
| ClaudePlayerIsmcts, reference | 1762 +/- 20.9 | 600 |
| Master, search 100 / 400-ms cap | 1760 +/- 23.1 | 600 |
| Expert, bounded endgames, T=1.5 / MaxRegret=4 | 1600 +/- 2.2 | 120,240 |
| Skilled / SmartPlayer | 1466 +/- 1.7 | 120,240 |
| Beginner / DummyPlayer | 1200 (fixed) | 120,240 |
| RandomPlayer | 656 +/- 3.1 | 120,240 |

These PPO-era ratings were copied to `AiLevels.cs`; section 16 records the current
calibration. **Expert keeps temperature 1.5 and
MaxRegret 4**: its measured strength remains between Skilled and Master. Relevant
direct matchups from the same tournament are:

| First player / opponent | First player's win rate +/- 1 sigma | Games |
|---|---|---|
| Skilled / Expert | 25.843% +/- .202 pp | 40,000 |
| Expert / Master | 35.000% +/- 4.168 pp | 120 |
| Expert / ISMCTS100 | 32.500% +/- 3.910 pp | 120 |
| Master / ISMCTS100 | 50.000% +/- 3.565 pp | 120 |

Master's fitted rating has a wide error bar and Expert is a changed opponent.
The dedicated 1,000-game old-versus-new Master match above is the direct test of
that weight change; the change in fitted rating alone does not show a regression.
The bounded fast profile's 1,000-game ISMCTS result remains the promotion test.
Post-promotion verification passes: **741 engine tests, 97 AI tests and 72 UI
tests**, with no failures or skips. Both requested Windows and Android app
builds complete with **zero warnings and zero errors**. The PPO tooling's final
source revision also passes **28 Python tests**, including gradients, data
validation, actor export parity and reproducible resume.

Full experiment settings, follow-ups and reproduction commands are in
[PPO_EXPERIMENT.md](PPO_EXPERIMENT.md) and
[Gpu/README.md](tools/NeuralTrainer/Gpu/README.md#ppo-with-a-helper-critic).

## 15. Legacy opponents and mixed-opponent PPO (September 28)

SharpBelot and the reconstructed 2001 Belot 2.06 AI now work as seeded `IPlayer`
adapters in `tools/LegacyOpponents`. `SmartPlayer` is the user's own bot. The
trainer's `arena` command compares any named profile against any other, using
fresh players for each mirrored leg. It saves individual pair outcomes and
counts rule adaptations. `validate` now uses that seeded runner too. Timed search
remains dependent on machine load and is measured with training stopped.

Against SharpBelot and the 2.06 port, respectively, the current pure NN wins
**79.545% +/- .273 pp** and **67.665% +/- .307 pp**; bounded endgames score
**82.990% +/- .255 pp** and **72.075% +/- .298 pp**. Each entry is 20,000 whole
games, seed 611, with one empirical standard error across mirrored pairs. The
2.06 adapter uses the C# transcription, not the original executable, and all
players follow the current engine's rules. Full comparisons, including app
levels, Master and ISMCTS, and adapter fidelity limits are in
[OPPONENTS_EXPERIMENT.md](OPPONENTS_EXPERIMENT.md).

The search profiles played 1,000 games per legacy opponent, seed 619. ISMCTS100
scores **89.400% +/- .947 pp** against SharpBelot and **75.400% +/- 1.214 pp**
against 2.06; Master scores **84.100% +/- 1.108 pp** and **70.800% +/- 1.311 pp**.
These opponents expose a broader weakness that the fast profile's favorable
head-to-head result against ISMCTS alone does not capture. Neither comparison
establishes a Master advantage over the cheaper bounded-endgame profile.

**Research test:** opponent populations can expose self-play blind spots, as
motivated by [PSRO](https://arxiv.org/abs/1711.00832) and
[Kita et al.'s bridge PPO work](https://arxiv.org/html/2406.10306v1). Test this
directly with equal-budget PPO runs: a frozen-NN opposing team versus a uniform
pool of frozen NN, SmartPlayer, SharpBelot and 2.06. Each run uses 622,592 deals,
12 warmup batches plus 64 updates, seed 9901, GAE lambda .5, and the existing
training-only helper critic. External decisions never become PPO actor samples.
Only the two current-policy seats are trained; public actor inputs and BNN1
inference remain unchanged.

**Result:** the diverse all-card update regresses, scoring **49.250% +/- .205 pp**
against the current NN and **49.365% +/- .203 pp** against the matched control,
20,000 games each. Updating only all trumps scores **49.885% +/- .158 pp** against
the current NN, also 20,000 games. The control itself ties at **49.970% +/- .188 pp**
for all cards and **50.005% +/- .127 pp** for all trumps, 20,000 games each. The
diverse policy's small improvement against Smart is accompanied by inconclusive
legacy differences and a regression against the current neural player. These are
development results from one training seed, not evidence against every possible
opponent mix.

No candidate passes the first promotion requirement, so none advances to the
ISMCTS gate. Embedded weights, app levels and Elo ratings remain unchanged.
Keep the adapters and configurable pool; do not adopt this 100%-external uniform
mix as a training default. Smaller fractions and stronger past-policy pools
remain untested. The full table, sample counts, commands and failure record are
in the experiment note. New options are:

```powershell
dotnet run -c Release --project tools/NeuralTrainer -- arena --player neural --opponent sharpbelot --pairs 10000 --threads 10 --seed 611 --data artifacts/neural-sharp
dotnet run -c Release --project tools/NeuralTrainer -- arena --player fast --opponent belot206 --pairs 10000 --threads 10 --seed 611 --data artifacts/fast-206
# In Gpu/ppo.py, add: --opponents neural,smart,sharpbelot,belot206 --opponent-chance 1
# Use --opponents neural with the same budget for the frozen-opponent control.
```

Idle engine-card means with the unchanged baseline are **14.7 us** for pure NN
(21,187 decisions / 100 games), **30.5 us** for bounded endgames (21,225 / 100),
and **58.37 ms** for Master (669 / 4); warmup is excluded. The latest checks pass
741 engine, 110 AI and 72 UI tests, 28 Python tests, and Windows/Android builds
with zero warnings and errors. Golden 2.06 checks pass again, including all
17,499 bid and 5,160 card vectors. Zero-chance collection and mixed-pool resume
also preserve the expected binary output exactly.


## 16. Faster Master with learned ownership (September 28-29, 2026)

The user expanded the move budget to 10 ms and authorized eight hours of machine
experiments. [FAST_BOT_EXPERIMENT.md](FAST_BOT_EXPERIMENT.md) records the research
basis, controls, every attempted variant, artifacts and reproduction commands.
The frozen reference is commit `cba8cb3`; its unchanged actor files were copied
to `artifacts/fast-bot-20260928/baseline`. A fresh 400-game setup check reproduced
current fast versus ISMCTS100 at 58.000% +/- 2.198 pp. This small control sample
was not treated as an improvement.

### First promoted profile: belief5-v1

The main gain comes from estimating the unseen hands and solving longer endings.
Three separate 600 -> 128 -> 64 -> 96 networks predict each unseen card's owner.
A dynamic program samples hands with the public exclusions and hand sizes,
then filters public declarations. Five-trick perfect-information minimax uses
128 sampled worlds, 250,000 nodes, a transposition table and an 8-ms search cap;
three-trick positions enumerate at most 1,680 worlds. Ownership power is 1,
with uniform mix .1. Every legal action shares the same completed worlds.
Interrupted sampled worlds contribute no values; interrupted exact enumeration
falls back entirely. Earlier play uses the original actor.

The actor weights and layout remain unchanged. The CE12 ownership exports add
548,652 bytes, bringing all seven embedded files to **3,523,482 bytes**. Inputs
remain public and inference remains managed C#. `ClaudePlayerProfiles.CreateMaster`
first used this configuration and now adds the validated suit ensemble below;
`CreateRolloutMaster` preserves the previous 100-rollout,
400-ms profile. Expert and hints retain their cheaper three-trick/90-world search.

All independent comparisons below used the same frozen candidate, ten workers,
mirrored whole games and no competing training, builds or game jobs. Uncertainty
is one empirical standard error over mirrored pairs, in percentage points:

| Opponent | Games | Seed | Win rate +/- one SE | 95% interval |
|---|---:|---:|---:|---:|
| ISMCTS, 100 ms/card | 1,000 | 751 | **64.000% +/- 1.301 pp** | [61.451%, 66.549%] |
| Previous Master, 100 neural rollouts | 1,000 | 761 | **56.900% +/- 1.103 pp** | [54.738%, 59.062%] |
| Previous fast, three-trick endings | 10,000 | 757 | **59.370% +/- .323 pp** | [58.737%, 60.003%] |
| SmartPlayer | 10,000 | 769 | **92.530% +/- .253 pp** | [92.034%, 93.026%] |
| Adapted SharpBelot | 10,000 | 773 | **87.080% +/- .325 pp** | [86.442%, 87.718%] |
| Adapted Belot 2.06 (2001) | 10,000 | 787 | **76.420% +/- .404 pp** | [75.629%, 77.211%] |

The gains against ISMCTS, old Master and previous fast are respectively
+100 +/- 10, +48 +/- 8 and +66 +/- 2 Elo; point differences are +19.3, +11.2
and +13.7 per game. These independent results pass the strength gates. External
rates apply to the checked repository adapters and their documented compatibility
filters; there were no card fallbacks. Full diagnostics are in the research note.

An otherwise-idle 100-game benchmark, excluding 20 warmup games, measured
**21,672 card decisions**: mean **1.4269 ms**, median .0313 ms, p95 5.5403 ms,
p99 **6.0887 ms**, maximum **8.0061 ms**, and **0 decisions above 10 ms**.
These are observed desktop callback times, not a hard real-time bound on every
device. This is a neural policy with learned beliefs and bounded search; the
unchanged pure actor alone is not claimed to have these win rates.

Provenance and compact validation reports are checked in beside the ownership
weights. To reproduce using the current shared profile (use the frozen artifact
runner and full flags in the research note for an exact historical configuration):

```powershell
dotnet run -c Release --project tools/NeuralTrainer -- arena --player master --opponent ismcts:100 --pairs 500 --threads 10 --seed 867 --data artifacts/master-ismcts
dotnet run -c Release --project tools/NeuralTrainer -- arena --player master --opponent rollout-master --pairs 500 --threads 10 --seed 871 --data artifacts/master-previous
dotnet run -c Release --project tools/NeuralTrainer -- bench --player master --bench-games 100
```

### What did not justify adoption

- Control-variate rollouts increased action-difference variance by about 34%
  and exceeded 10 ms on 88/695 measured decisions. Truncated neural rollouts
  scored only about 39-40% against previous fast (2,000 games per variant).
- A 664-input ownership model with public chronology, diverse-opponent ownership
  data and historical-play likelihoods did not establish an additional gain over
  the selected ownership model. Direct comparisons and matched controls are in
  the research note.
- Exact joint ownership loss reduced heldout assignment NLL, but with the
  intended uniform mix .1 it scored 49.450% +/- .545 pp against CE12 over 2,000
  games. The predeclared trigger for a larger match failed.
- A four-component normalized ownership mixture improved late-phase NLL by
  only .01137 nats/state (0.204%), below the predefined .03 follow-up threshold.
  No runtime mixture was added and no playing-strength gain is claimed;
  [MIXTURE_BELIEF_NOTE.md](MIXTURE_BELIEF_NOTE.md) has the mathematical details.
- A search-free late correction trained on 92,240 positions lowered heldout
  teacher regret but scored **49.000% +/- .494 pp against the frozen pure NN
  over 2,000 games** (seed 823, 95% [48.031%, 49.969%]). It is rejected.
  Checked managed/Python half-forward parity passed 192 states / 6,144 outputs.

At the integration checkpoint, 741 engine, 361 AI, 74 UI and 78 Python tests
passed, and Windows and Android builds had zero warnings and errors. The
follow-ups below and final level calibration are now complete; final post-rating
verification is recorded at the end of this section.

### Suit-ensemble follow-up

An optional inference-time suit ensemble improves the frozen pure network without
retraining: **52.030% +/- .220 pp over 20,000 games** against the original pure
network (independent seed 798, 95% [51.599%, 52.461%], +2.8 points/game,
+14 +/- 2 Elo). The initial 2,000-game screen scored 51.550% +/- .675 pp
(seed 797). It averages 1/2/6/24 suit permutations that fix trump and every suit
mentioned in the auction, mapping all 16 card planes and outputs together.
The observed auction stays legal, but a learned bidding policy can still treat
unbid suits differently; exact Bayesian invariance is not claimed. This tests
inference averaging separately from the earlier failed augmentation/retraining
experiments. `--card-suit-ensemble true` affects ordinary card-network fallback
only; bidding, successful searches and rollout policies stay unchanged. The
option remains off in the constructor and fast/Expert profiles. Master enables
it after the independent checks below. Its 23 tests and seven named-benchmark tests pass
within the complete 361-test AI suite, with a zero-warning copied trainer build.
Adding the ensemble only to neural fallback in the ownership/TT five-trick
profile scores **50.895% +/- .219 pp over 20,000 games** against the identical
profile without it (independent seed 799, 95% [50.465%, 51.325%], +1.2 points/game,
+6 +/- 2 Elo). Both sides disable the wall-clock cap for this comparison while
retaining 128 worlds and 250,000 nodes. The initial 2,000-game hybrid screen was
50.750% +/- .691 pp (seed 709). This fixed-work result triggered the separate
idle timing and actual eight-millisecond validation below.

The pure ensemble also transfers to independent opponents. Paired comparisons
against Smart, SharpBelot and Belot 2.06 improve by respectively **1.410 +/- .290,
1.880 +/- .326 and 2.350 +/- .416 percentage points**, with 10,000 games per
variant per opponent. A matched four-epoch attempt to distil the exact ensemble
into one ordinary network improved teacher regret but scored only **50.035% +/-
.175 pp against the frozen baseline over 20,000 games**. It was not promoted.
The control, target construction, parity checks and commands are in the full note.

### Selected profile: belief5-v2-ensemble

The final profile adds suit averaging only to ordinary neural fallback in v1.
It changes no weights, search parameters, bidding or rollout policies. On a fresh
20,000-game match with the actual eight-millisecond cap on both sides, it scores
**50.930% +/- .217 pp against v1**, 95% [50.505%, 51.355%], seed 863. The external
checks in the opening table then pass the ISMCTS, previous Master and frozen-fast
gates. ISMCTS and previous Master have 95% intervals [59.556%, 64.444%] and
[56.842%, 61.358%]. Different seed sets mean the v1/v2 scores against a third bot
are not a paired estimate of the ensemble effect.

The independently frozen v2 executable and settings, all eight matches, full
uncertainty, adapter diagnostics and the idle benchmark are preserved in
`Neural/Weights/Ownership/validation-v2.json`. The earlier `validation.json`
remains v1 evidence. All results use mirrored whole games; the timed matches ran
sequentially on an otherwise idle machine. The fresh 100-game v2 benchmark,
excluding 20 warmup games, covers 21,246 card callbacks: mean 1.4491 ms, median
.0708 ms, p95 5.5719 ms, p99 6.0827 ms and maximum 8.0060 ms, with none above
10 ms. The pure suit ensemble costs 68.9 us/card, so it exceeds the original
50-us pure-network target. The shared Master adopts it within the expanded
10-ms budget; the default constructor, hints and Expert keep their existing
profiles. The final round robin retains Expert's temperature 1.5 and maximum
regret 4 because its fitted rating remains between Skilled and Master.

The separate pure-ensemble ISMCTS100 check scores **51.100% +/- 1.418 pp over
1,000 games**, seed 887, 95% [48.320%, 53.880%], +.24 points/game. It does not
pass the independent ISMCTS gate. Its gains against the frozen NN and three
external heuristic bots remain valid comparisons, but the substantial Master
gain requires the learned-ownership/endgame combination measured above.

### Final v2 app calibration

The otherwise-idle `elo 20000 60` round robin completed on September 29 in
**1:03:02**, covering **400,600 whole games**. Master now uses the fast matchup
count: every pair of non-ISMCTS levels plays 20,000 mirrored pairs / 40,000 games.
Only matchups involving ISMCTS use 60 pairs / 120 games. All five fast levels
therefore appear in 160,120 games each; ISMCTS appears in 600. Per-level counts
overlap because each game involves two levels.

| App level / reference | Pair Elo +/- 1 sigma | Games involving this level |
|---|---:|---:|
| Master / belief5-v2-ensemble | **1838 +/- 2.5** | 160,120 |
| ClaudePlayerIsmcts, 100 ms | 1758 +/- 17.7 | 600 |
| Expert, T=1.5 / MaxRegret=4 | **1611 +/- 2.2** | 160,120 |
| Skilled / SmartPlayer | **1462 +/- 1.7** | 160,120 |
| Beginner / DummyPlayer | 1200 (fixed anchor) | 160,120 |
| RandomPlayer | **669 +/- 3.0** | 160,120 |

The Bradley-Terry fit retains the 1% fifty-fifty prior and Dummy's fixed 1200
anchor. Rating errors are one standard deviation from 1,000 shared-seed
mirrored-pair bootstrap samples. The ratings are copied into `AiLevels.cs`.
Expert retains temperature 1.5 and maximum regret 4: it beats Skilled at
**74.125% +/- .202 pp** and scores **23.143% +/- .189 pp** against Master,
with **40,000 games per comparison** and one empirical pair standard error.
The small rating-suite Master/ISMCTS matchup is **58.333% +/- 3.789 pp over
120 games**; the separate **62.000% +/- 1.247 pp over 1,000 games** remains
the independent promotion test. These samples are not pooled.

The recorded run used the frozen `f5f950a` simulator copy. Its executable and
dependency hashes, arguments and idle-run conditions are in
`artifacts/fast-bot-20260928/final-elo-manifest.json`; all 15 matchup results and
the fitted ratings are in `final-elo.log` in that folder. The copied Release
build log is `final-elo-build-v2.log`, and `run-final-elo.ps1` records the original
invocation. Reproduce from a fresh output folder, preserving the original logs:

```powershell
dotnet build src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -c Release -o artifacts/master-elo-recheck
dotnet artifacts/master-elo-recheck/Belot.GamesSimulator.dll elo 20000 60 | & 'C:/Program Files/Git/usr/bin/iconv.exe' -f UTF-16LE -t UTF-8 > artifacts/master-elo-recheck/final-elo.log
```

The equivalent direct invocation is `dotnet run -c Release --project
src/Tests/Belot.GamesSimulator -- elo 20000 60`, with the same `iconv` conversion.
Stop other training, tests, builds and matches for either run because Master and
ISMCTS have wall-clock search budgets.

Final verification is complete:

| Check | Result | Local evidence |
|---|---|---|
| Engine tests | 741 passed | `final-engine-tests.log` |
| AI / trainer tests for v2 | 361 passed | `final-ai-v2-tests.log` |
| UI tests after final rating changes | 74 passed | `final-ui-ratings-tests.log` |
| GPU training-tool Python tests | 78 passed | `final-python-tests.log` |
| Windows app build | Zero warnings/errors; 18.31 s | `final-windows-v2-build.log` |
| Android app build | Zero warnings/errors; 18.58 s | `final-android-v2-build.log` |

All evidence files are under `artifacts/fast-bot-20260928/`. The actor files and
engine/Python source are unchanged by final rating calibration. All 46 changed
C# files since the frozen baseline were checked for UTF-8 BOM and CRLF, with no
violations. Engine production source and the NuGet engine API are unchanged.
Earlier tables in this document retain their original profiles and game counts.
