# Legacy opponents and PPO diversity (September 28, 2026)

Starting actor weights: branch `master` at `8bf1eeb`, including the promoted all-trump PPO file.
The user's own bot is `SmartPlayer`. External adapters are development tooling in
`tools/LegacyOpponents`; the app and inference library acquire no new dependency.

## Fair comparisons

- All bots play the current `Belot.Engine` rules, in mirrored pairs of whole games.
  Each pair uses the same deal seed twice with teams swapped. A new seeded bot is
  created for each leg, so legacy random choices do not depend on worker scheduling.
  Player seeds are mixed separately from the deal seed; the shuffler and player
  must not restart the same pseudorandom sequence. Both teams share the player
  seeds for common random numbers. Preliminary results that reused a deal seed
  directly for a player were superseded and archived under `superseded-arena-seeds/`.
  The tables here use the corrected runner. PPO collection already used a separate
  opponent RNG and is unaffected by this evaluation correction.
- `sharpbelot` runs Konstantin Ivanov's existing AI source. Its card ordering and
  auction snapshot are preserved. The adaptation injects a seeded random generator
  and supplies the engine's legal-card set. Differences from SharpBelot's own move
  predicate are counted, as are rejected bids. Only the deciding hand and public
  history are supplied; all other hands in its legacy manager remain empty.
- `belot206` runs the existing C# transcription of Valentin Tsekov's 2001 AI.
  Bidding and play share its Delphi RNG. It uses the existing public-history adapter;
  forbidden bids become passes and are counted. Card-selection fallbacks are counted.
  This is a port, not live execution of the Windows program. The older live check
  documented 99.5% decision agreement and 0.8% unanswered emulator positions. The
  saved golden vectors pass again: 17,499 bids and 5,160 card decisions, plus the
  existing card-table, announcement, move-legality and trick-winner checks, and
  all 2,500 scoring vectors.
- New tests cover exact self-match symmetry, reproducibility across thread counts,
  legal decisions and context/view parity under all contracts, doubles and redoubles.
  Training contexts are replayed against engine histories and encoded features.

`NeuralTrainer arena` reports win rate, the empirical standard error across paired
outcomes, a paired normal 95% interval, points/game and diagnostic counts. It also
writes the individual pair results to `<data>.arena.json`. All numbers below are
whole games; `+/-` is one standard error in percentage points.

## Historical player comparisons (September 28, 2026)

20,000 games per fast matchup, seed 611, 10 threads, Release. The historical weights are
frozen in `artifacts/opponents-20260928/baseline`. These deterministic comparisons
may overlap development work; their elapsed times are not inference benchmarks.
The timed former Master and ISMCTS matchups each use 1,000 games, seed 619 and 10 workers,
with training and build jobs stopped. The entire suite runs sequentially.
Here, **former Master** means the 100-rollout, 400-ms profile now named
`rollout-master`. These historical results do not describe the current `master`
profile; its September 29 results appear separately below.

| Our player | vs SharpBelot | vs Belot 2.06 C# port | Games per opponent |
|---|---|---|---|
| Random | 0.550% +/- .053 | 0.170% +/- .029 | 20,000 |
| Dummy / Beginner | 28.730% +/- .307 | 8.730% +/- .195 | 20,000 |
| Smart / Skilled | 58.820% +/- .310 | 31.925% +/- .293 | 20,000 |
| Neural, no search | 79.545% +/- .273 | 67.665% +/- .307 | 20,000 |
| Neural, bounded endgames / hints | 82.990% +/- .255 | 72.075% +/- .298 | 20,000 |
| Expert, T=1.5, MaxRegret=4, bounded endgames | 62.630% +/- .318 | 48.830% +/- .325 | 20,000 |
| Former Master / `rollout-master`, search 100, 400-ms cap | 84.100% +/- 1.108 | 70.800% +/- 1.311 | 1,000 |
| ISMCTS, 100 ms/card | 89.400% +/- .947 | 75.400% +/- 1.214 | 1,000 |

SharpBelot needs the host legal set in 0.06-0.15% of card decisions across these
matchups. No SharpBelot bids were rejected. The 2.06 port has zero card fallbacks;
0.15-1.14% of its bids become passes under the host's auction rules. Exact counts
and denominators are in each arena JSON, so these adaptations remain visible.

With training stopped, warmed engine callbacks average 14.7 us/card for the
pure NN (21,187 decisions, 100 games), 30.5 us for bounded endgames (21,225,
100 games), and 58.37 ms for former Master / `rollout-master` (669, 4 games). These are mean latencies,
not game-strength measurements; no latency uncertainty was collected.

The different matchups motivate diversity: Smart beats SharpBelot, yet the neural
policy wins fewer games against SharpBelot than against Smart. One scalar rating
does not describe every pairing.
ISMCTS also performs better than the former neural Master against both legacy opponents in
these checks. Its paired differences are +5.300 +/- 1.348 pp against SharpBelot
and +4.600 +/- 1.722 pp against 2.06 (1,000 games per profile and opponent).
The existing fast-versus-ISMCTS head-to-head gate does not
establish that the neural policy is stronger against every opponent.
Neither historical legacy comparison establishes an advantage for the former
Master over the much cheaper bounded-endgame profile; the smaller timed samples
have wider uncertainty.

In their direct match, the 2.06 port beats SharpBelot **69.440% +/- .294 pp** over
20,000 games, seed 611. That run also has zero card fallbacks; 3,228 of 858,939
2.06 bids become passes and SharpBelot's move predicate differs at 1,912 of
2,222,500 card decisions.

## Current Master update (September 29, 2026)

The current `master` profile is **belief5-v2-ensemble**: the frozen actor plus
the CE12 ownership model, five-trick endgame search with transpositions and an
8-ms budget, and suit averaging on ordinary card-network fallback. It uses no
full-deal rollout search. The former Master remains available as `rollout-master`.
The profiles' results are kept separate throughout this note.

These independent whole-game gates use ten workers with competing training and
build jobs stopped. Each uncertainty is one empirical standard error across
mirrored pairs, expressed in percentage points. Time-capped search depends on
machine load, so the seed alone does not guarantee identical results.

| Opponent | Current Master win rate +/- SE | Games | Seed |
|---|---:|---:|---:|
| SmartPlayer | 92.520% +/- .253 pp | 10,000 | 879 |
| SharpBelot | 87.080% +/- .324 pp | 10,000 | 881 |
| Belot 2.06 C# port | 77.150% +/- .395 pp | 10,000 | 883 |
| ISMCTS, 100 ms/card | 62.000% +/- 1.247 pp | 1,000 | 867 |
| Former Master / `rollout-master` | 59.100% +/- 1.152 pp | 1,000 | 871 |

The SmartPlayer, ISMCTS and former Master reports record zero rejected bids,
card fallbacks and legal-set differences for both teams. SharpBelot records
zero rejected bids in 363,797 bid decisions, zero card fallbacks in 1,008,667
card decisions, and 510 differences from its own legal-set predicate. The
adapter uses the host legal set, as in the historical comparison.
The Belot206 port records 3,887 rejected bids in 388,333 bid decisions, zero
card fallbacks in 1,024,658 card decisions, and zero legal-set differences.
Current Master's diagnostic counters are zero in both legacy matchups.
Exact settings, individual pair outcomes and diagnostics are in
`artifacts/fast-bot-20260928/belief5-v2-{smart,sharpbelot,belot206,ismcts,rollout-master}-gate.arena.json`.
The frozen candidate settings include `EndgameTricks=5`, `EndgameWorlds=1680`,
`EndgameSampledWorlds=128`, `EndgameNodes=250000`, `EndgameMilliseconds=8`,
`EndgameDeclarations=true`, `EndgamePruning=false`, `EndgameTranspositions=true`,
ownership power 1, ownership uniform mix .1,
`CardSuitEnsemble=true`, and `SearchDeals=0`.

## Training design and fixed pilot

PPO accepts `--opponents` and `--opponent-chance`. A selected external team plays
both opposing seats for the entire deal, including bidding. Only the current
stochastic policy's actions enter PPO: external actions have no PPO probability
ratio and never become actor samples. The current policy still bids greedily with
the frozen bidding network. Combinations use the established declare-all policy.
Public contexts retain the ordered auction and cards, game score and incoming
hanging points. The training-only critic keeps its separate private ownership
inputs. The inference architecture and feature layout stay unchanged.
Training retains the existing collector's deal-point reward and score-counter
reset when either team reaches 151. The arena uses the engine's complete
whole-game ending rules. The supported catalog players decide from their current
contexts; the collector does not deliver end-of-trick/round/game observer callbacks.

The pool is chosen independently of the deal and actor RNG. Empty/zero-chance
settings preserve the previous collector byte for byte. Resume refuses changes
to the pool or chance; new checkpoints also reject changed opponent assemblies.
Older checkpoints remain readable. Manifests include assembly hashes and per-kind
deal counts. The named `neural` training opponent uses the weights embedded in the
copied collector, frozen throughout the run.

Before examining training results, fix two runs with 12 critic-only warmup batches
and 64 PPO updates, 8,192 deals each (622,592 deals per run), seed 9901, 12 workers,
GAE lambda .5, actor LR 1e-6, helper LR 3e-4, temperature 1, existing losses and
256/128 helper widths:

1. Control: a frozen neural opposing team on every deal.
2. Diverse: a uniform choice of frozen neural, SmartPlayer, SharpBelot and 2.06 on
   every deal.

Both record only the two learner seats; equal deal budgets do not imply identical
numbers of non-forced decisions. Evaluate the fixed final checkpoints, both all
three updated card networks and an all-trump-only combination, on seed 631 against
the frozen current networks, Smart and both legacy bots (20,000 games each).
Any selected candidate needs fresh confirmation and the original ISMCTS promotion
gate before replacing embedded weights.

### Pilot results

All numeric entries below are 20,000 mirrored whole games, seed 631, pure greedy neural
inference. AT means only the all-trump file is updated; the other three files come
from the frozen baseline. Each uncertainty is one paired standard error in pp.

| Candidate | vs current NN | vs Smart | vs SharpBelot | vs 2.06 port |
|---|---|---|---|---|
| Current NN | - | 86.735% +/- .226 | 79.785% +/- .271 | 66.755% +/- .308 |
| Frozen-NN control, all cards | 49.970% +/- .188 | 86.785% +/- .226 | 79.350% +/- .273 | 66.910% +/- .309 |
| Frozen-NN control, AT only | 50.005% +/- .127 | 86.790% +/- .226 | 79.770% +/- .272 | 66.830% +/- .306 |
| Diverse pool, all cards | 49.250% +/- .205 | 87.110% +/- .224 | 79.910% +/- .270 | 66.970% +/- .308 |
| Diverse pool, AT only | 49.885% +/- .158 | 86.980% +/- .225 | 79.640% +/- .272 | 66.760% +/- .307 |

The diverse all-card policy also scores **49.365% +/- .203 pp** against the matched
control (20,000 games); the AT-only comparison is **49.960% +/- .153 pp** (20,000).
The all-card mix regresses against the current NN: 95% [48.847%, 49.653%]. Its small
Smart gain is +.375 +/- .185 pp when pairing the two policies' results on the same
seeds. Its legacy differences are inconclusive: +.125 +/- .203 pp against
SharpBelot and +.215 +/- .280 pp against 2.06, paired by seed. These are development tests
from one training seed; errors describe game sampling, not variation across
training runs, and no multiple-comparison correction is applied.

**That PPO pilot promoted no new weights.** None of the four candidates establishes a win
against the current network, so none advances to the expensive ISMCTS promotion
test. At that point, app levels and ratings retained the previously validated weights and settings.
The adapters and configurable pool are retained for future training and comparison.
This experiment rejects replacing the opposing team with this uniform pool on
every deal at this training budget. It does not establish that all diversity is
unhelpful. A smaller pool fraction or a pool containing stronger past policies
would require its own matched experiment.

The control recorded 2,015,382 suit, 1,021,957 no-trump and 2,789,006 all-trump
learner decisions. The diverse run recorded 2,058,172 / 1,070,517 / 2,296,523.
Its 622,592 deals split into 156,491 frozen-NN, 154,495 Smart, 155,499 SharpBelot
and 156,107 2.06 deals. Loop times were 265 and 294 seconds, respectively; some
validation/build work overlapped, so these are not isolated throughput benchmarks.

This is a controlled test of opponent diversity, not an assumption that weaker
bots always improve training. It follows the motivation for opponent populations
in [PSRO](https://arxiv.org/abs/1711.00832) and the PPO/opponent-pool recipe in
[Kita et al.'s bridge bidding work](https://arxiv.org/html/2406.10306v1). Neither
paper establishes a gain for Belot card play; that is what the comparisons test.

## Reproducing the September 28 experiment

```powershell
New-Item -ItemType Directory -Path artifacts/baseline -Force
Copy-Item artifacts/opponents-20260928/baseline/*.bin artifacts/baseline
# Preserve the archived actor weights from branch master at 8bf1eeb for historical reproduction.
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/opponents-bin
dotnet artifacts/opponents-bin/NeuralTrainer.dll arena --in artifacts/baseline --player neural --opponent sharpbelot --pairs 10000 --threads 10 --seed 611 --data artifacts/neural-sharp
dotnet artifacts/opponents-bin/NeuralTrainer.dll arena --in artifacts/baseline --player fast --opponent belot206 --pairs 10000 --threads 10 --seed 611 --data artifacts/fast-206
# Also: random, dummy, smart, expert, rollout-master, ismcts:100. Preserve each historical profile's recorded settings.
# Current master is belief5-v2-ensemble and does not reproduce the former Master row.
# validate also accepts --opponent sharpbelot and --opponent belot206.
dotnet artifacts/opponents-bin/NeuralTrainer.dll arena --in artifacts/baseline --player rollout-master --opponent sharpbelot --pairs 500 --threads 10 --seed 619 --data artifacts/rollout-master-sharp
dotnet artifacts/opponents-bin/NeuralTrainer.dll arena --in artifacts/baseline --player ismcts:100 --opponent sharpbelot --pairs 500 --threads 10 --seed 619 --data artifacts/ismcts-sharp
# Repeat these two timed commands with --opponent belot206 and distinct --data prefixes.
# For distinct neural folders in arena:
dotnet artifacts/opponents-bin/NeuralTrainer.dll arena --player neural --opponent neural --in artifacts/candidate --opponent-in artifacts/baseline --pairs 10000 --threads 10 --seed 631 --data artifacts/candidate-baseline

artifacts/neural-20260927/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in artifacts/baseline --out artifacts/pool-diverse --trainer artifacts/opponents-bin/NeuralTrainer.dll --opponents neural,smart,sharpbelot,belot206 --opponent-chance 1 --warmup 12 --updates 64 --deals 8192 --threads 12 --seed 9901 --gae-lambda .5 --save-every 64 --eval-pairs 0
# Use --opponents neural and a separate output for the control.
```

Timed-search matches must run without competing training or build jobs. Training
uses the existing isolated CUDA environment documented in the GPU README.

## Verification of the September 28 experiment

- 741 engine, 110 AI and 72 UI tests pass. This adds seven adapter/runner cases and six
  training-context/learner-trajectory cases. Windows and Android builds have zero
  warnings and errors. No engine API or runtime neural dependency changes.
- All 28 Python tests pass, including refusal of changed opponent settings and
  changed embedded-opponent assemblies on resume.
- An 80-deal self-play batch is byte-identical to the preceding collector for all
  three contract files when opponent chance is zero.
- Resuming a mixed-opponent CUDA smoke run from its critic warmup produces exactly
  the same four exported BNN1 files as the uninterrupted run.
- Logs, individual pair outcomes, frozen baseline, checkpoints and run manifests
  are under `artifacts/opponents-20260928/`. `pilot-*` is the fixed development
  comparison; `baseline-*` is the current-player comparison.
- The corrected suite contains 38 matchups and 684,000 whole games. Recomputing
  every win rate, paired standard error and points/game from the saved pair
  outcomes agrees with the reports. All runs have zero card-selection fallbacks.
  Embedded weight hashes still match the frozen baseline exactly.

## Website Master integration (September 29)

ednaigra.com level 6 uses `ClaudePlayerProfiles.CreateMaster()` with
`EndgameTimeLimitMilliseconds = 0`. Its bot interface requires the same view and
random state to produce the same action regardless of thread scheduling. The
128-world / 250,000-node bounds, ownership models, declarations, transpositions
and suit ensemble are retained. Levels 1-5 keep their existing fast profiles.

An independent engine-arena check of this exact player configuration against
`belot206`, seed **919**, eight workers, gives **76.720% +/- .398 percentage
points** over **10,000 whole games / 5,000 mirrored pairs**. The 95% interval is
**75.941-77.499%**, points per game **+62.5466**, and Elo difference
**+207.17 +/- 3.87**. Errors are one empirical standard error over paired games.
All 5,000 pair outcomes and seven weight hashes are published in
[`etc/Benchmarks/website-master-20260929.json`](etc/Benchmarks/website-master-20260929.json).
The run used source commit `5e29d53942689eb1d9a604ec9ec9df264d20f53f` and took
262.6 seconds. These independent games do not estimate the effect of disabling
the deadline by subtracting the earlier 77.150% result, which used another seed.

The original program is **Bridge Belot Multiplayer 2.06**, shown in its executable
as **Bridge Belot v2.06 MP**, by **Valentin Tsekov / Валентин Цеков**. Its
[original homepage](https://belot.hit.bg/) names the version and copyright 2001;
the local executable and help file agree. The comparison runs the C# reconstruction
under this engine's rules. It does not execute the original Windows program.
The adapter rejected 3,954 of 390,260 bid proposals (converted to passes), with
zero card fallbacks or legal-card-set differences across 1,031,071 card decisions.

Reproduce from that engine revision, using a copied Release runner:

```powershell
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/website-master-20260929/bin
dotnet artifacts/website-master-20260929/bin/NeuralTrainer.dll arena --player candidate --opponent belot206 --in src/AI/Belot.AI.ClaudePlayer/Neural/Weights --endgame true --endgame-declarations true --endgame-tricks 5 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-milliseconds 0 --endgame-transpositions true --endgame-ownership src/AI/Belot.AI.ClaudePlayer/Neural/Weights/Ownership --endgame-ownership-power 1 --endgame-ownership-uniform-mix 0.1 --card-suit-ensemble true --search-deals 0 --pairs 5000 --threads 8 --seed 919 --data artifacts/website-master-20260929/website-master-206
```

The website's serialized-view adapter is checked separately for equality with
engine decisions, legal whole games and seeded sequential/parallel determinism.
Its new level 6 beats the recreated previous 3-trick/90-world profile **59.600%
+/- 1.067 pp in 1,000 mirrored games** (seed 2,992,026). Single-thread timing
through view creation, JSON, decision and action serialization averages **1.501 ms**
per card; p99 **6.505 ms**, maximum **10.846 ms**, with **1 of 22,127** callbacks
above 10 ms across 100 measured games after 20 warmup games. This fixed-work
website variant has no hard wall-clock limit. The engine's 8-ms Master remains
unchanged. Website methods and results are recorded in its `docs/belot-bots.md`.
