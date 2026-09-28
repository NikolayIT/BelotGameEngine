# Faster, stronger player: September 28 research run

## Objective and budget

The target is a substantial gain over both the current fast profile and the
strongest existing opponents, including Master and ISMCTS100, within 10 ms per
card. A small same-lineage improvement is insufficient to call this objective
complete. Prefer the existing microsecond neural inference wherever it suffices.
The user authorized eight hours of machine experiments. This run started at
23:12 Europe/Sofia on September 28, 2026. Reserve time for independent matches,
integration tests and both app builds. Do not promote solely from a development
match, loss curve or a lower latency mean.

Reference: commit `cba8cb3`, with unchanged embedded weights copied to
`artifacts/fast-bot-20260928/baseline`. The corrected seeded arena from the
previous experiment provides the reference executable. The initial setup check
is 200 mirrored pairs against ISMCTS100, seed 701, ten workers, otherwise idle.

## Initial hypotheses

1. **Extend double-dummy endings.** The successful existing profile only solves
   three-trick positions with at most 90 publicly consistent hands. Up to 1,680
   allocations are possible even with no exclusions. Try full three-trick
   enumeration, then sampled four- and five-trick endings with explicit node and
   time budgets. Every legal root action must share the same complete worlds.
   A partially solved world must never bias only the later root actions.
2. **Sample constrained hands uniformly.** The old sampler ensures legal hands
   but its room-weighted constrained assignment is not a uniform draw over
   complete valid assignments. Dynamic programming can count completions and
   sample proportionally, using only observed cards, voids and known ownership.
   Rejection against public declaration types conditions on the same declare-all
   assumption used by the existing endgame profile. Human withheld declarations
   remain a model limitation.
3. **Reduce neural-rollout sampling variance.** Estimate each action with many
   cheap heuristic continuations plus the average neural-minus-heuristic outcome
   on a smaller shared subset. This control variate differs from the previously
   rejected blend of network values with sampled outcomes. It is useful only if
   paired action-difference residuals have substantially lower variance and
   whole-game strength improves at the measured cost.

Existing failed experiments are recorded in `NEURAL_NETWORK.md`: ordinary and
centred continuation, multiple search-distillation losses and architectures,
public-history and ownership pilots, privileged-critic PPO, and uniform weak
opponent pools. They justify prioritizing different mechanisms in this run;
they do not establish that every neural approach is exhausted.

### Research basis

- [Veness, Lanctot and Bowling (2011)](https://proceedings.neurips.cc/paper/2011/file/d736bb10d83a904aefc1d6ce93dc54b8-Paper.pdf)
  motivates control variates and common random numbers in Monte Carlo tree
  search. Its stochastic single-agent experiments do not demonstrate Belot
  gains. Our local hypothesis uses correlated cheap and neural continuations
  on identical hypothetical deals, with fixed coefficient one.
- [Khairy and Balaprakash (2022)](https://arxiv.org/abs/2206.05165) studies
  multifidelity reinforcement learning with correlated returns. This review
  screened the abstract; the implemented estimator follows the standard
  control-variate identity, not a reproduction of that training algorithm.
- [Arjonilla, Saffidine and Cazenave (2024)](https://arxiv.org/html/2408.02380v1)
  postpones perfect-information evaluation and solves an imperfect-information
  subgame first. The method and experiment sections motivate a later attempt
  if ordinary PIMC saturates. Its results do not remove partnership information
  constraints: giving a simulated partner the real deciding hand as knowledge
  would be invalid. No EPIMC implementation or Belot gain is claimed here.

The new hand sampler has an exact counting justification. A counterexample for
the old method has capacities `(2,1,1)` and allowed-seat masks `(3,5,6,7)` for
four unseen cards. There are four valid complete assignments. Summing over all
24 random card orders and the room-weighted feasible choices gives probabilities
`25/108,25/108,29/108,29/108`, instead of four quarters. The new sampler selects a
uniform integer over the exact completion count and maps each integer to one
unique valid assignment. This improves the specified sampling distribution;
actual playing strength still needs measurement, and uniform legal worlds are
not a full posterior conditioned on the opponents' policies.

## Measurement plan

- Development comparisons: fixed seed 709, initially 2,000 whole games per
  variant against the unchanged fast profile; extend promising variants to
  20,000 games and compare legacy opponents. Retain every attempted result.
- Independent selection checks: separate seed ranges (starting at 751), at
  least 1,000 whole games against ISMCTS100 and Master for a fixed candidate.
  Use idle timed opponents and empirical standard errors over mirrored pairs.
- Report mean, p50, p95, p99, maximum observed callback time and the number above
  10 ms. A mean alone cannot establish the requested move budget. Scheduling and
  garbage collection can still interrupt a managed process.
- Preserve legal-action values, context/view parity, all public neural options,
  embedded-weight limits and the pure managed runtime. No engine rule/API change.
- Promotion still requires a significant win over the frozen current player and
  a 95% interval above 50% against ISMCTS100 over at least 1,000 games. A promoted
  small gain does not by itself complete the stronger overall objective.

Results and reproduction commands will be appended as measurements complete.

## Reproduced reference

The unchanged fast profile scores **58.000% +/- 2.198 percentage points** against
ISMCTS100 over **400 games**, seed 701, 95% interval **[53.693%,62.307%]**,
+10.2 points/game, +56 +/-16 Elo. The otherwise idle match took 7:56.755.
This is a setup check with substantial sampling error, not a new improvement.
The unchanged copied runner's warm engine benchmark reports **27.1 us/card**
over 21,225 decisions in 100 games, excluding warmup; 4,919 used endgames.

```powershell
dotnet artifacts/opponents-20260928/eval-bin/NeuralTrainer.dll arena --in artifacts/fast-bot-20260928/baseline --player fast --opponent ismcts:100 --pairs 200 --threads 10 --seed 701 --data artifacts/fast-bot-20260928/baseline-ismcts
dotnet artifacts/opponents-20260928/eval-bin/NeuralTrainer.dll bench --in artifacts/fast-bot-20260928/baseline --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90
```

## Initial development screen

All rows use 2,000 whole games against the frozen fast profile, seed 709,
one empirical standard error across mirrored pairs. These are development
comparisons selected among multiple variants, not promotion tests. The four
and five-trick variants use a 250,000-node per-decision cap, no time cap, exact
three-trick limit 1,680, and respectively 32/128/32 sampled worlds. The exact
three-trick variant has no node cap. Bidding and weights are unchanged.

| Variant | Win rate vs current fast | Points/game |
|---|---|---|
| exact3 | 51.550% +/- 0.413 pp | +1.96 |
| sample4-32 | 53.150% +/- 0.647 pp | +5.16 |
| sample4-128 | 53.600% +/- 0.618 pp | +5.78 |
| sample5-32 | 52.500% +/- 0.731 pp | +4.08 |

Idle latency screen (20 whole games, warmup excluded):

| Variant | Decisions | Mean us | p99 us | Maximum us | Above 10ms |
|---|---:|---:|---:|---:|---:|
| exact3 | 4,249 | 197.5 | 4,622.6 | 23,288.5 | 20 |
| sample4-32 | 4,237 | 366.1 | 4,236.7 | 4,945.1 | 0 |
| sample4-128 | 4,189 | 642.1 | 4,700.2 | 6,920.9 | 0 |
| sample5-32 | 4,390 | 941.2 | 4,784.0 | 7,673.4 | 0 |

The exact-only variant needs a work/time bound before it can meet the requested
latency. More horizon did not automatically improve this screen: sampled five
tricks trails sampled four, although these small samples do not prove their
ordering. Budget truncation can also bias the completed-world sample toward
cheaper worlds. An incomplete world is always discarded for every root action.

The control-variate pilot (8 neural worlds, 128 heuristic worlds, old 3/90
endgames) used four whole games and 695 measured decisions. Mean latency was
4,208.1 us, p99 16,204.1 us, maximum 18,099.5 us, with 88 decisions above 10 ms.
Across root action differences, summed estimated residual variance was
101,357.117 versus 75,820.034 for ordinary neural returns: ratio 1.337. The
coefficient-one hypothesis increases noise at these settings and fails the
latency screen; no playing-strength claim or promotion match is made for it.

All 143 AI/trainer tests pass after these changes; the copied Release trainer
build reports zero warnings and errors. Other integration checks remain pending.

The selected four-trick/128-world variant's extended development comparison
finished at **53.970% +/- .192 pp**, 20,000 games, 95% interval
**[53.594%,54.346%]**, +5.9 points/game, +28 +/-1 Elo. It uses the same
seed 709 range as the pilot, so the initial 2,000 games are included, not an
additional independent sample. This confirms a lineage gain; ISMCTS/Master
confirmation remains pending. The exact uniform sampler and its six tests are
committed separately as `d5a4459`.

The next search pilot uses neural rollouts for one/two completed tricks, then
bootstraps with the maximum legal neural Q. By default it continues up to three
extra cards until the original deciding seat next acts: a different seat's Q
would discard the root's known private hand. An any-seat leaf with team sign
conversion is retained as an ablation. Terminal states still score exactly.
The 33 new cases verify independent reference values, continuation length,
public-view parity and hidden-hand independence. All 176 AI/trainer tests pass;
the copied rollout build has zero warnings/errors. No actor weights change.

### Truncated-leaf defect found before selection

The first implementation evaluated Q at forced single-card leaves. Both the
original Monte Carlo collector and PPO skip training these states; their Q
values are therefore outside the training distribution. The audit found no
unit/sign or struct-copy error, but this leaf eligibility was wrong.
Root16/horizon1 scored 36.550% +/- .876 pp and root32/horizon1 scored 39.300%
+/- .852 pp against current fast, each 2,000 games, seed 709. These are results of
the defective implementation, not evidence against corrected truncated search.
The horizon 2 match was interrupted before completion; it has no reported result.
Its benchmark completed separately. No incomplete count is added to other runs.

The repair continues through forced actions until a trained kind of decision
(at least two legal cards) or terminal scoring. When waiting for root, this can
require more than three extra cards. The independent reference and tests must
cover that case. Even with this repair, inaccurate state-value calibration
may make bootstrapping weaker; only new whole-game matches can settle that.
The defective copied binary/results remain under rollout-bin and pilot-root*;
corrected experiments will use separate names.

### Corrected short rollouts and equivalent-card pruning

The forced-leaf repair still loses decisively. Root16/horizon1 scores
39.350% +/- .860 pp and root16/horizon2 scores 39.600% +/- .873 pp against
current fast, each 2,000 games (seed 709). Their mean latencies are 2,527.5
and 3,995.2 us; p99 8,260.6 and 11,643.4 us; observed maxima 9,929.0 and
21,771.7 us, over 778 and 715 decisions respectively. The second exceeds
10 ms on 29 decisions. Reject both. Cross-state Q calibration and bootstrap
bias remain plausible explanations; the measurements do not isolate them.

Exact pruning of equivalent zero-point cards is covered by independent solver
comparisons. With 128 worlds / 250,000 nodes, four tricks scores 53.400% +/-
.620 pp and five tricks 53.100% +/- .701 pp, each 2,000 games against fast.
These pilots do not establish a gain over the unpruned four-trick variant.
Four-trick pruning latency: mean 650.2 us, p99 4,800.2, maximum 6,357.9, zero
above 10 ms / 4,230 decisions. Five-trick: mean 1,233.0 us, p99 4,897.7,
maximum 6,678.6, zero above 10 ms / 4,202 decisions.

### Explicit inference about unseen hands

Two complementary hypotheses are now under test:

- Reconstruct previous public decisions in each hypothetical world, then weight
  it by softened neural probabilities of the observed leads/discards. The
  historical mover sees only their hypothetical hand and the public prefix;
  later declarations are hidden. Forced moves have probability one. A bounded
  per-position cache avoids repeating identical historical network inputs.
- Train a separate 600 -> 128 -> 64 -> 96 ownership model for each contract,
  using existing self-play data and ownership labels. The existing actor is
  unchanged. Each card has three probabilities for the other relative seats.
  A dynamic-programming sampler draws from their product subject to public
  exclusions and exact hand sizes. Enumerated endings use the same product as
  weights; sampled endings must not multiply that prior a second time.

The ownership model is an explicit input to search, unlike the earlier discarded
auxiliary ownership head. Its three half-precision files total 548,652 bytes.
Raw/masked held-out prediction metrics are diagnostic only; whole-game strength
and latency determine whether it is useful. Policy and ownership weights can
repeat evidence, so combining them is an ablation rather than an exact Bayesian
posterior claim.

#### Ownership model: fixed 12-epoch run

Implementation: `b1d15f7`; training-only CUDA PyTorch 2.9.1+cu126 in the existing
isolated environment. No actor weights changed. Each separate model has
600 -> 128 -> 64 -> 96 units, ReLU hidden layers and 32 groups of three owner
logits. BNN1 tags 11/12/13 identify suit/no-trumps/all-trumps; feature layout is 1.
The files are 182,884 bytes each (548,652 bytes together).

The reused collection has 1,884,525 training positions: 653,908 suit, 332,699
no-trumps and 897,918 all-trumps. Independent validation has 94,603 positions:
31,782 / 16,101 / 46,720 respectively. Collection was previously documented as
100,000 training deals at seed 3931 and 5,000 validation deals at seed 2931,
frozen neural bidding and 3% card exploration. See the ownership pilot in
`NEURAL_NETWORK.md` for the original collection. This run fits ownership only;
it ignores the stored Q targets.

```powershell
artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit_ownership.py --data artifacts/neural-20260927/belief-selfplay/data --validation-data artifacts/neural-20260927/belief-validation/data --out artifacts/fast-bot-20260928/ownership-ce12-seed9821 --epochs 12 --batch 1024 --learning-rate .001 --seed 9821 --device cuda
```

All models use raw owner cross-entropy, Adam, gradient norm cap 1, and learning
rate .001 for epochs 1-8 then .0003 for epochs 9-12. Seed 9821 and twelve epochs
were fixed before fitting. The final epoch is used for every contract; there
was no checkpoint selection. Windows power-throttling opt-out remains enabled.
Reported epoch work totals 180.377 seconds: 67.352 suit, 35.317 no-trumps and
77.708 all-trumps. These totals include export and validation. The log spans
232.90 seconds, excluding Python startup. Another fixed-node arena ran during
fitting, so these are observed run times rather than an isolated throughput test.

The following metrics use the actual half-precision exports. NLL is mean
negative log-likelihood in natural-log units; lower is better. Masked metrics
remove owners ruled out by public exclusions or known ownership. The baseline
is uniform among those locally allowed owners, with expected accuracy `1/k`.
It does **not** condition on joint remaining hand capacities and is therefore
weaker than the exact uniform-world sampler used in play. Cards from the same
deal are correlated; these counts are not independent observations for a
confidence interval.

| Contract | Hidden-card labels | Raw NLL | Masked NLL | Uniform NLL | Masked accuracy | Uniform expected accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Suit | 436,548 | .952498 | .951417 | 1.024381 | 48.855% | 36.915% |
| No trumps | 218,312 | .976304 | .975131 | 1.058164 | 48.806% | 35.227% |
| All trumps | 623,603 | .923327 | .921990 | 1.020540 | 51.918% | 37.301% |

Because the per-card loss gives early positions more labels, a separate phase
audit checks the part used by endgame search. These are subsets of the same
validation set, with the fixed final models; they were not used for fitting or
checkpoint selection.

| Phase / contract | Positions | Hidden-card labels | Masked NLL | Uniform NLL | Masked accuracy | Uniform expected accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Last four tricks / suit | 13,566 | 104,000 | .798880 | .913556 | 59.084% | 42.479% |
| Last four tricks / no trumps | 6,992 | 53,626 | .889168 | 1.016604 | 55.639% | 37.237% |
| Last four tricks / all trumps | 20,998 | 160,857 | .801198 | .954968 | 60.279% | 40.660% |
| Last three tricks / suit | 9,069 | 55,915 | .743374 | .872388 | 62.432% | 44.636% |
| Last three tricks / no trumps | 4,633 | 28,537 | .858272 | 1.002120 | 57.781% | 37.943% |
| Last three tricks / all trumps | 13,994 | 86,189 | .758366 | .933256 | 63.081% | 41.759% |

Evidence lives under `artifacts/fast-bot-20260928/ownership-ce12-seed9821/`:

- `report.json`: arguments, PyTorch version, sample/sidecar SHA-256 hashes and
  every epoch's training and validation measurements.
- `training.log`: complete console output; `phase-metrics.json`: the additional
  phase audit, including raw metrics and the first four tricks.
- `managed-parity.json`: actual trained-file C# versus Python forward check.
  Sixty-four evenly spaced held-out positions per contract produce 18,432 logits.
  Maximum absolute difference is 2.861023e-6 and RMS difference is 3.611773e-7
  (asserted tolerance 3e-5). Both sides load the same exported half weights;
  Python uses dense float32 and C# the existing managed sparse SIMD kernel.
  This isolates serialization/inference parity from owner-mapping tests.
- `parity_inputs.py`, `check-managed-parity.ps1` and `phase_metrics.py` reproduce
  those additional checks. The parity report records the checked assembly hash.

Final weight SHA-256 hashes:

```text
trump.bin     20D7BEC4D4BF20F8FEEBDA74D3AD40A422CC50C8342FE3131E352A8B678D6239
notrumps.bin  1CA6B99A48A163F3B36BBE2F89EF0B8EC54B5AD787D30BFAA4074B00666463CC
alltrumps.bin 57F12BCA5A310EC1985880F196BF6B7F3D8722789ED1C1BFE2124550FF4D8F1C
```

The mapping audit found no mismatch: sidecars pack the owner of each rotated
card as relative seat 1/2/3; the Python reader converts these to classes 0/1/2,
and ignores own/played cards. The runtime unrotates cards while preserving those
relative owner classes. Existing tests check sidecar/sample slot alignment,
true-hand labels, and unchanged public inputs when hidden hands are removed.
New tests check every suit rotation/seat, exact format refusal, and endgame
weighting, including avoiding a second ownership factor after weighted sampling.

Limits remain. Training/validation use an older frozen neural policy and
declare-all behavior; performance against ISMCTS, legacy bots or human signaling
is not established by these prediction metrics. The 600 features preserve
per-seat played-card sets but omit the full order of earlier plays. Forced-card
positions were not training examples; current use is at genuine player choices.
Marginal owner probabilities multiplied across cards are not a learned joint
posterior. Exact hand-size conditioning can duplicate information the predictor
already learned, and adding policy likelihood may reuse the same evidence.
Power/mixture settings and whole-game comparisons must test these approximations.

### Public-play and ownership development comparisons

The historical-policy screen compares against `sampled4`, the unchanged
four-trick/128-world/250,000-node candidate above. Each row is 2,000 whole games,
seed 709, with one empirical standard error over mirrored pairs. Temperature is
2 points and uniform action mixing .1. These are selection data.

| Recent actions | Likelihood power | Win rate vs sampled4 | Points/game |
|---:|---:|---:|---:|
| 1 | .5 | 50.700% +/- .316 pp | +.5 |
| 2 | .5 | 51.300% +/- .337 pp | +1.3 |
| 4 | .25 | 51.200% +/- .298 pp | +1.1 |
| 4 | .5 | 51.250% +/- .382 pp | +1.5 |

The separate 600-input ownership model, fixed epoch 12, is then tested with
historical-policy weighting disabled. Uniform ownership mixing is .1. Same
2,000-game development seed and same opponent:

| Ownership power | Win rate vs sampled4 | Points/game |
|---:|---:|---:|
| .25 | 51.250% +/- .481 pp | +2.0 |
| .5 | 52.150% +/- .498 pp | +2.5 |
| 1 | 53.250% +/- .545 pp | +3.7 |
| 2 | 52.100% +/- .571 pp | +2.7 |

This is evidence for explicit belief-guided search, not yet an independent
ISMCTS/Master result. Ownership power 1 is selected for a larger development
comparison. Combining it with historical-action weights could count the same
evidence twice, so any combined variant needs its own ablation.

```powershell
dotnet artifacts/fast-bot-20260928/belief-bin/NeuralTrainer.dll arena --in artifacts/fast-bot-20260928/baseline --player candidate --opponent sampled4 --endgame true --endgame-declarations true --endgame-tricks 4 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-policy-actions 2 --pairs 1000 --threads 10 --seed 709 --data artifacts/fast-bot-20260928/pilot-policy2-p05-sampled4
dotnet artifacts/fast-bot-20260928/belief-bin/NeuralTrainer.dll arena --in artifacts/fast-bot-20260928/baseline --player candidate --opponent sampled4 --endgame true --endgame-declarations true --endgame-tricks 4 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-ownership artifacts/fast-bot-20260928/ownership-ce12-seed9821 --endgame-ownership-power 1 --pairs 1000 --threads 8 --seed 709 --data artifacts/fast-bot-20260928/pilot-ownership-p1-sampled4
```

The policy timing screen's final two rows overlapped a test build and are not
valid idle measurements. All policy variants will be re-timed after the current
fixed-node experiments finish. The overlap does not affect the arena outcomes:
these development candidates use fixed work limits and no wall-clock cutoff.

### Matched public-history ownership experiment

To test whether the order of earlier public plays helps card-location inference,
train two separate 664 -> 128 -> 64 -> 96 ownership models. The extra 64 features
record, for each played card, its trick number divided by 8 and its position in
the trick divided by 4. Both use the history dataset and its matching ownership
sidecars; those rows have a different collection order from the older 600-input
files. The actor keeps its original 600 inputs and layout 1.

The full-history run and zero-history control use identical data hashes, row
order, seed 9821, optimizer, learning-rate schedule and fixed epoch 12. Both
initialize the extra 64 input weights to zero. The control additionally zeros
those input columns during fitting; its exported extra input weights remain
exactly zero, so providing real history at runtime does not change its output.
All three contracts have exactly identical initial validation metrics between
runs. This controls for initialization and collection order when measuring the
extra features; comparing only against the earlier 600 model would not.

```powershell
$python = 'artifacts/neural-20260927/torch-env/Scripts/python.exe'
foreach ($mode in @('full', 'zero')) {
    & $python tools/NeuralTrainer/Gpu/fit_ownership.py --data artifacts/neural-20260927/history-selfplay/data --validation-data artifacts/neural-20260927/history-validation/data --out "artifacts/fast-bot-20260928/ownership-history-$($mode)12-seed9821" --layout 2 --history-features $mode --epochs 12 --batch 1024 --learning-rate .001 --seed 9821 --device cuda
}
```

The data counts match the earlier ownership experiment: 1,884,525 training
positions and 94,603 held-out positions. Each model has a checked BNN1 header,
layout 2 and tag 11/12/13, and is 199,268 bytes; all three total 597,804 bytes.
The following measurements load the actual half-precision exports. These are
prediction metrics, not whole-game win rates; no game-strength selection uses
this held-out table.

| Contract | Hidden-card labels | Full raw NLL | Zero raw NLL | Full masked NLL | Zero masked NLL | Full masked accuracy | Zero masked accuracy |
|---|---:|---:|---:|---:|---:|---:|---:|
| Suit | 436,548 | .953230 | .953799 | .952120 | .952708 | 48.748% | 48.715% |
| No trumps | 218,312 | .974719 | .976021 | .973578 | .974887 | 48.867% | 48.797% |
| All trumps | 623,603 | .922561 | .924504 | .921260 | .923217 | 51.950% | 51.850% |

The effect is small overall. In the last four tricks, where the current endgame
search uses ownership predictions:

| Contract | Positions | Full masked NLL | Zero masked NLL | Full masked accuracy | Zero masked accuracy |
|---|---:|---:|---:|---:|---:|
| Suit | 13,566 | .801176 | .801411 | 58.807% | 58.580% |
| No trumps | 6,992 | .886284 | .888314 | 55.686% | 55.755% |
| All trumps | 20,998 | .798923 | .802628 | 60.349% | 60.127% |

Full-history epoch work totals 236.77 seconds and its log spans 275.86 seconds;
the zero-history control totals 239.17 seconds and its log spans 289.20 seconds.
Both ran alongside fixed-node development games, so these are observed run
durations rather than isolated throughput benchmarks. The log spans omit Python
startup. Training uses the same 2.9.1+cu126 environment and learning-rate decay
as the 600-input run.

Each output folder contains `report.json` with source SHA-256 hashes, arguments
and every epoch; `training.log`; `phase-metrics.json`; `parity-inputs.json`; and
`managed-parity.json`. The actual exported weights pass the existing managed
sparse SIMD kernel versus Python dense float32 check: 192 positions and 18,432
logits per variant, maximum absolute error 2.861023e-6 for each (tolerance 3e-5).
RMS error is 3.526316e-7 for full history and 3.461989e-7 for the control.
`artifacts/fast-bot-20260928/check-history-ownership.py` and
`check-history-managed-parity.ps1` reproduce the phase and inference checks;
`run-history-ownership.ps1` is the exact training wrapper used.

Final SHA-256 hashes, in suit / no-trumps / all-trumps order:

```text
full  EBEA6317106720038ABE13EE0B9F5EBD2D110D81D8F5FE36DEFD8A98B5E04956
full  6BFB0B77B43F58CD0E072E7AE11269EE1B46402DED1F0144A1EC11D79CAB9E41
full  879CD0B5D189FDD0559A12460C11C525DFDD2ACB7A75AA512C877287F8F92DBC
zero  EF2B5A17A053E97B29282644318A2FED3119EDB69D749B440B93FB67AC356B91
zero  2BCC3DE4BD25E5A3E8AB9F40FF7CED0D5D56110AED0A33428E59D6AD95423919
zero  703C0F3037EFE54C0E1AA3AC5C724DBC8FCE2A53DF763F424E58E64C7EBCD67F
```

The local-mask baseline and older-policy distribution limits above still apply.
History may also carry evidence already represented by exclusions and known
cards. Lower held-out NLL alone does not establish stronger play after joint
hand-size conditioning, declarations and bounded search. Paired strength and
idle latency measurements remain required before selecting this variant.

### Transposition tables and the five-trick candidate

A bounded table caches minimax results at completed-trick boundaries. Entries
include all four remaining hands, turn, trick count, both raw scores, trick counts
and announcement totals. The table generation fixes the contract, declaring team,
hanging points and evaluating team for one decision. Full keys are compared after
hashing; interrupted searches do not cache their unfinished parent. The table is
optional and has 8,192 entries, approximately 512 KB per player.

Development games use seed 709, 1,000 mirrored pairs (2,000 games), 128 sampled
worlds and a 250,000-node limit, without a wall-clock cutoff. All uncertainties
below are one empirical standard error across mirrored pairs. The opponent is
the fixed `sampled4` control. Ownership uses the 600-input CE12 model, power 1,
uniform mix .1. Historical policy weighting is otherwise disabled.

| Candidate | Win rate vs sampled4 | Points/game |
|---|---:|---:|
| Four tricks, table | 50.250% +/- .391 pp | +0.0 |
| Five tricks, table | 52.300% +/- .656 pp | +3.2 |
| Four tricks, ownership and table | 52.850% +/- .538 pp | +3.7 |
| Five tricks, ownership and table | 56.700% +/- .679 pp | +8.8 |
| Four tricks, ownership, table and two policy actions | 53.200% +/- .524 pp | +4.4 |

The separate four-trick ownership candidate without the table was extended
against the frozen current `fast` player: **56.770% +/- .203 pp over 20,000 games**,
95% interval [56.372%, 57.168%], +9.8 points/game and +47 +/- 1 Elo. This is
development seed 709 and overlaps its smaller pilot; those are not independent
replications. It is also a different configuration from the five-trick candidate.

Neural rollouts ending in perfect-information minimax remained weak with the
original rollout declaration sampling. Against `fast`, each on 2,000 development
games: 8 worlds/two-trick leaf 34.700% +/- .847 pp; 8/three-trick leaf
36.800% +/- .841 pp; 16/two-trick leaf 41.950% +/- .859 pp; and 16/three-trick
leaf 43.400% +/- .847 pp. A separate optional sampler now requires declarations
to match the sampled hands; its strength still needs measurement. These results
do not test that repaired sampler.

All timing profiles were repeated with training, tests and other matches stopped.
The five-trick ownership/table profile averaged 1,449.0 microseconds across 4,281
decisions, with p99 6,067.5 and maximum 9,923.3 microseconds. No decision exceeded
10 ms. This selected the configuration for a larger screen with an explicit
8 ms search budget, called **belief5-v1**. No actor weights changed.

The exact time-limited candidate then completed an idle 100-game benchmark,
excluding 20 warm-up games: **21,672 card decisions**, mean **1,426.9 us**, median
31.3 us, p95 5,540.3 us, p99 6,088.7 us, maximum 8,006.1 us, and **0/21,672 above
10 ms**. It completed 12,612 endgame decisions; the table had 111,534,681 probes,
45,976,179 hits and 44,504,935 cutoffs. There were 4,088 incomplete sampled worlds;
their root-action values are discarded together. A time budget can still favor
worlds that finish quickly, so fixed-node development results do not establish
the strength of the time-limited configuration.

With at most three tricks remaining, the solver first enumerates every
declaration-consistent world up to the configured count limit. Overflow discards
that enumerated prefix and switches to sampling. Interruption during enumeration
or during any exact-world solve causes a complete network fallback. Only sampled
mode can retain earlier completed worlds after a later cutoff; if none completed,
it also falls back. Within a sampled world, every legal root action must finish
before any of its values enter the average.

The candidate, weight hashes, binary hashes and validation settings are frozen in
`artifacts/fast-bot-20260928/belief5-v1-manifest.json`. Its independent validation
uses seed 751, 500 mirrored pairs and ISMCTS at 100 ms/card, with ten workers and
no competing jobs. The measured timing is a desktop observation, not a hard
real-time guarantee on every device. App defaults and embedded weights remain
unchanged pending strength validation.

The independent ISMCTS100 match completed in 19:45.970: **64.000% +/- 1.301 pp
over 1,000 games**, 95% interval **[61.451%, 66.549%]**, **+19.3 points/game** and
**+100 +/- 10 Elo**. This is seed 751, fixed in the manifest before observing
the result, with all training and other game experiments stopped. It establishes
a clear gain against the search benchmark. The exact time-limited candidate's
independent match against frozen current fast then scored **59.370% +/- .323 pp
over 10,000 games**, seed 757, 95% [58.737%, 60.003%], **+13.7 points/game** and
**+66 +/- 2 Elo**, in 3:34.340. This ran with all fitting and other game jobs
stopped. Against the previous Master (100 neural rollouts, 400-ms cap), the same
candidate scores **56.900% +/- 1.103 pp over 1,000 games** (seed 761, 95% interval
**[54.738%, 59.062%]**, +11.2 points/game, +48 +/- 8 Elo). This otherwise-idle
match took 13:28.692. It passes the independent strength requirements against
both existing strong opponents and the frozen fast profile. Earlier four-trick
results are not substituted for these comparisons.

The same frozen candidate then completed all three independent comparisons
against the user's SmartPlayer and the adapted external opponents. Each used
5,000 mirrored pairs / 10,000 whole games, ten workers and an otherwise idle
machine. All uncertainties below are one empirical standard error over pairs:

| Opponent | Seed | Win rate +/- pair SE | 95% interval | Points/game |
|---|---:|---:|---:|---:|
| SmartPlayer | 769 | 92.530% +/- .253 pp | [92.034%, 93.026%] | +71.3 |
| SharpBelot adapter | 773 | 87.080% +/- .325 pp | [86.442%, 87.718%] | +63.2 |
| Belot 2.06 (2001) adapter | 787 | 76.420% +/- .404 pp | [75.629%, 77.211%] | +61.3 |

There were no card fallbacks. SharpBelot reported 530 legal-set differences
over 1,015,508 card decisions and no rejected bids. The 2.06 adapter rejected
3,911 proposals over 391,049 bid decisions through its documented legality
filter; it reported no card legal-set differences. These are measurements of
the repository adapters under the engine's rules, with the compatibility
limits described in `OPPONENTS_EXPERIMENT.md`.

The follow-up arguments and results are retained under `belief5-v1-*-gate.*`.
The initial ISMCTS result is `belief5-v1-ismcts.arena.json`, with its frozen
settings in `belief5-v1-manifest.json`. The common plan and wrapper are
`belief5-v1-independent-plan.json` and `run-belief5-v1-independent.ps1`.
A compact checked-in summary lives beside the promoted
ownership weights as `Neural/Weights/Ownership/validation.json`; it records all
six independent opponents and the idle benchmark. App integration and final
level calibration are in progress.

```powershell
dotnet artifacts/fast-bot-20260928/search-bin/NeuralTrainer.dll bench --in artifacts/fast-bot-20260928/baseline --endgame true --endgame-declarations true --endgame-tricks 5 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-milliseconds 8 --endgame-transpositions true --endgame-ownership artifacts/fast-bot-20260928/ownership-ce12-seed9821 --endgame-ownership-power 1 --bench-games 100
dotnet artifacts/fast-bot-20260928/search-bin/NeuralTrainer.dll arena --in artifacts/fast-bot-20260928/baseline --player candidate --opponent ismcts:100 --endgame true --endgame-declarations true --endgame-tricks 5 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-milliseconds 8 --endgame-transpositions true --endgame-ownership artifacts/fast-bot-20260928/ownership-ce12-seed9821 --endgame-ownership-power 1 --pairs 500 --threads 10 --seed 751 --data artifacts/fast-bot-20260928/belief5-v1-ismcts
```

### Direct ownership-model comparisons

The next screen compares independently configured five-trick players directly,
using `arena --opponent-config`. Both sides use the same frozen actor, 128 worlds,
250,000 nodes, transpositions and declarations, with no wall-clock cutoff. Each
row is 1,000 mirrored pairs / 2,000 games, development seed 709. Other fixed-work
experiments and fitting ran concurrently; these are strength comparisons, not
idle timing measurements. The exact argument arrays and opposing JSON settings
are retained as `paired-*.arguments.json` and `paired-*.opponent.json`.

| Candidate / opponent | Win rate +/- one pair SE | Points/game |
|---|---:|---:|
| 664 history / matched 664 zero-history | 50.100% +/- .592 pp | -.5 |
| 664 history / original 600 CE12 | 49.800% +/- .609 pp | -.8 |
| CE12, uniform mix 0 / CE12, mix .1 | 49.950% +/- .564 pp | +.0 |
| Joint4 / continued CE4, both mix 0 | 50.400% +/- .592 pp | +.8 |
| Joint4 / original CE12, both mix 0 | 51.000% +/- .542 pp | +1.2 |
| Joint4 / original CE12, both mix .1 | 49.450% +/- .545 pp | -.3 |
| CE12 plus two policy actions / CE12 alone, both mix .1 | 50.050% +/- .450 pp | +.3 |

The history extension and extra policy likelihood do not establish an advantage
in these comparisons. Joint4's initial mix-0 point estimate is favorable, but
its 95% interval against CE12 is [49.937%, 52.063%], so that setting remains
inconclusive. The subsequent mix-.1 comparison tests the ownership smoothing
used by the intended belief5 profile and does not show a gain. CE12 stays selected.

The mix-.1 confirmation uses immutable `ensemble-bin`, the frozen baseline actor
on both teams, ownership power 1, five tricks, 128 sampled worlds, a 250,000-node
limit, exact three-trick limit 1,680, declarations and transpositions. Full-deal
rollouts, suit ensembling and policy likelihood are disabled. The wall-clock cap
is zero on both sides, so concurrent fixed-work matches and builds do not alter
the requested search work. The result is **49.450% +/- .545 pp over 2,000 games**
(1,000 mirrored pairs, seed 709), 95% interval **[48.381%, 50.519%]**,
**-.3 points/game** and **-4 +/- 4 Elo**. No latency claim comes from this run.

Before observing the result, the rule was to extend to 10,000 mirrored pairs at
independent seed 809 only if the pilot point estimate exceeded 50%. It did not,
so the extension was not started. The predeclared rule is saved in
`artifacts/fast-bot-20260928/joint-mix01-plan.json`; `run-joint-mix01.ps1`
reproduces the conditional run. Exact arguments, opponent configuration, log and
completed result use the prefix `paired-joint-ce12-mix01-709` in that folder.

Joint4 and its continued-CE control start from the same CE12 half exports,
use the same 1,884,525 training positions and 94,603 held-out positions, four
epochs, batch 2,048, seed 9833, Adam at .0001 with .3 decay after 70%, and
per-state/card loss normalization. Initial metrics and shuffle hashes match
exactly. Final held-out joint negative log likelihood per state, CE / Joint:
suit 10.46639 / 10.45777; no trumps 10.57599 / 10.56927; all trumps
9.73632 / 9.72316. After at least three completed tricks, the corresponding
numbers are 5.54921 / 5.54261 (17,986 states), 6.06088 / 6.05672 (9,316),
and 5.45973 / 5.44858 (27,963). These small prediction gains are not a substitute
for whole-game results.

Artifacts are under `ownership-joint4-lr1e4-b2048-seed9833/{ce,joint}`. The report
preserves every epoch and checked half export, source hashes and phase metrics.
Verification checks all 24 checkpoint hashes, final-file equality to epoch 4,
matching initial metrics/order, and unchanged CE12 source weights. The fitting
implementation includes finite-difference and brute-force partition tests.

### Late four-trick residual with an ownership teacher: rejected

This bounded pilot tested whether a better teacher and an exact phase gate could
avoid the earlier residual failures. Three separate **600 -> 64 -> 32** correction
networks leave the current actor frozen. They start with zero output weights and
apply only after four completed tricks, with at least two legal cards. At runtime
the legal-action mean is subtracted and the correction is added in game points.
Earlier decisions, bidding, forced moves and illegal output slots remain unchanged;
successful search/endgame results bypass the correction. The files use checked
BNN1 v1/layout1 headers, tags 21–23, and total **243,360 bytes**. No actor feature
layout or embedded weights changed.

The teacher used four-trick double-dummy continuations, 512 sampled worlds,
the 600-input CE12 ownership model at power 1/uniform mix .1, transpositions,
public declarations, a three-trick exact-world limit of 1,680, no node/time limit,
and no historical-policy weighting. Student trajectories were retained with
`--teacher-play-chance 0`. Unlike the actor, the teacher can use public declaration
multiplicity in rare cases; its values are therefore only approximately representable
by the unchanged 600 inputs.

The 100-game cost pilot took **18.69 seconds** with six threads and was excluded
from fitting. The final collection used 1,000 training games in 160.84 seconds
(211,745 total rows; **92,240 late nonforced rows**) and 100 validation games in
18.55 seconds (21,394 total rows; **9,336 retained**). Game seed ranges were
810000–810099 for the pilot, 811000–811999 for training and 831000–831099 for
validation. They are disjoint: this collector uses `seed + gameIndex` directly,
so literal starting seeds 811 and 831 would have overlapped. Parallel teacher
streams depend on scheduling; corpus SHA-256 hashes are retained in each manifest.

Fitting used centered teacher-minus-base action advantages, Huber loss, Adam,
learning rate .001, batch 1,024, seed 11921 and 12 CUDA epochs with two CPU
threads. It took 16.98 seconds. Each contract selected its epoch by the
predeclared heldout teacher-regret metric using the exported half-precision
weights. Later epochs overfit.

| Contract | Training / validation rows | Selected epoch | Base teacher regret | Selected teacher regret |
|---|---:|---:|---:|---:|
| Suit | 31,127 / 2,978 | 2 | .338061 | .299427 |
| No trumps | 15,906 / 1,578 | 1 | .462605 | .402007 |
| All trumps | 45,207 / 4,780 | 1 | .529795 | .516524 |

Regret is in game points. Despite these reductions, the selected correction
scored **49.000% +/- .494 pp over 2,000 mirrored games** against the frozen
pure neural player (seed 823), with 95% interval **[48.031%, 49.969%]**,
**-0.8 points/game** and **-7 +/- 3 Elo**. Uncertainty is one empirical standard
error across mirrored pairs. Both players had search/endgames disabled. This
fails the first promotion requirement, so the pilot is rejected at these settings;
there is no further correction fitting or collection in this experiment.

The full Release AI suite passed **311/311**, with zero build warnings. Seven
Python tests cover finite-difference gradients, frozen-base fitting, phase and
legal masks, sample filtering, checked export refusal, and the complete fitter
CLI. Actual half-export C# checked-loader/forward parity passed **192 heldout
states / 6,144 outputs**, maximum absolute error **2.98e-8** normalized units
and RMS **2.98e-9**. Correction latency has not yet been measured in an idle phase.

The copied runner is `artifacts/fast-bot-20260928/correction-bin`. Evidence is in
`correction-cost/plan.json`, `correction-train/manifest.json`,
`correction-validation/manifest.json`, and `correction-fit/` (all under the same
artifact root); the latter contains every checkpoint, report, log, parity inputs,
the parity runner, and a full result note. The recorder now obtains its teacher
from the central configured factory, so ownership, sampled worlds, transpositions
and policy options reach the labels. Its batch shortcut is restricted to the plain
full-rollout mode that it implements.

```powershell
dotnet artifacts/fast-bot-20260928/correction-bin/NeuralTrainer.dll distill --teacher endgame --in artifacts/fast-bot-20260928/baseline --search-deals 0 --endgame-declarations true --endgame-tricks 4 --endgame-worlds 1680 --endgame-sampled-worlds 512 --endgame-nodes 0 --endgame-milliseconds 0 --endgame-pruning false --endgame-transpositions true --endgame-policy-actions 0 --endgame-ownership artifacts/fast-bot-20260928/ownership-ce12-seed9821 --endgame-ownership-power 1 --endgame-ownership-uniform-mix .1 --card-label-chance 1 --teacher-play-chance 0 --games 1000 --threads 6 --seed 811000 --data artifacts/fast-bot-20260928/correction-train/data
# Repeat with --games 100 --seed 831000 --data artifacts/fast-bot-20260928/correction-validation/data.
artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit_late_correction.py --in artifacts/fast-bot-20260928/baseline --data artifacts/fast-bot-20260928/correction-train/data --validation-data artifacts/fast-bot-20260928/correction-validation/data --out artifacts/fast-bot-20260928/correction-fit --epochs 12 --batch 1024 --learning-rate .001 --seed 11921 --threads 2 --device cuda
dotnet artifacts/fast-bot-20260928/correction-bin/NeuralTrainer.dll validate --in artifacts/fast-bot-20260928/baseline --opponent artifacts/fast-bot-20260928/baseline --card-correction artifacts/fast-bot-20260928/correction-fit --search-deals 0 --endgame false --opponent-search-deals 0 --opponent-endgame false --pairs 1000 --threads 6 --seed 823
```

### Ownership fitting from mixed opponents: no demonstrated strength gain

This experiment isolates the value of fresh opponent diversity for the auxiliary
ownership model. The actor and search implementation stay frozen. One collection
uses neural opponents only; the other chooses the opposite team uniformly from
`neural,smart,sharpbelot,belot206` on every deal. SmartPlayer is the user's own bot;
SharpBelot and the adapted 2.06 (2001) bot provide different play patterns. These
are ownership labels from actual trajectories, so collecting them needs no
per-action search or Q-value targets.

Both collections use the frozen `baseline` networks, card temperature .05,
opponent chance 1, six workers, 100,000 training deals at seed 98391 and 5,000
held-out deals at seed 97421. The mixed training pool contains 25,075 neural,
24,965 SmartPlayer, 25,027 SharpBelot and 24,933 Belot206 deals; validation contains
1,270 / 1,204 / 1,208 / 1,318 respectively. Each collection retains the exact
arguments, actor hashes and opponent counts in `data.arguments.json` and
`data.ppo.json`.

The deal budgets match, but different play yields different numbers of recorded
nonforced neural decisions. This is an equal-deal comparison, not an equal-row
or equal-optimizer-step comparison:

| Collection | Suit positions | No-trumps positions | All-trumps positions | Total positions |
|---|---:|---:|---:|---:|
| Neural training | 323,072 | 168,428 | 451,775 | 943,275 |
| Mixed training | 333,538 | 174,398 | 369,532 | 877,468 |
| Neural validation | 16,679 | 8,660 | 22,262 | 47,601 |
| Mixed validation | 16,506 | 8,950 | 18,687 | 44,143 |

The direct PPO reader and `finetune_ownership.py --data-format ppo` use the stored
600 public actor features and real owner labels. They validate ownership against
public unseen cards, hand counts and known/excluded owners. Actor targets,
advantages and privileged critic inputs are not training inputs. The existing
`samples` format remains the default; no dummy Q targets or sidecars are created.
The reader path and provenance tests pass; the source integration is `13e89e2`.

Both fits start from the same `ownership-ce12-seed9821` half exports and train
only the three 600 -> 128 -> 64 -> 96 ownership networks. Settings are four fixed
CE epochs, Adam, batch 2,048, seed 9837, learning rate .0001 for epochs 1-2 and
.00003 for epochs 3-4. Each state's CE is divided by its active-card count before
averaging states, matching the earlier continued-CE control. GPU dataset caching
changes transfer cost only. Epoch 4 was specified in advance; all twelve exports
per fit are preserved, their hashes checked, and each final file equals epoch 4.

Cross-validation loads the actual half exports and evaluates both distributions.
Joint NLL is the mean negative log probability of the true ownership assignment
conditioned on encoded known/excluded owners and public remaining hand sizes;
lower is better. Extra runtime carre deductions and full declaration rejection
remain outside this metric. The phase with at least three completed tricks is
the part available to five-trick endgame search. Values below combine contracts
weighted by position counts:

| Validation / phase | Positions | CE12 joint NLL | Neural CE4 | Mixed CE4 |
|---|---:|---:|---:|---:|
| Neural / all | 47,601 | 10.14915 | 10.10972 | 10.17743 |
| Mixed / all | 44,143 | 10.39780 | 10.36534 | 10.22602 |
| Neural / at least three completed tricks | 27,697 | 5.61483 | 5.56173 | 5.63303 |
| Mixed / at least three completed tricks | 25,724 | 5.81225 | 5.77283 | 5.62578 |

Late-phase masked per-card accuracy is 56.706% / 57.186% / 56.490% on neural
validation and 55.136% / 55.508% / 56.795% on mixed validation, in CE12 / neural /
mixed order. Mixed fitting improves mixed-pool joint NLL in all three contracts,
but worsens neural-pool suit and no-trumps inference. Its all-trumps inference
improves against CE12 on both pools, while the neural control remains better on
neural-only all-trumps play. This is evidence of specialization. Correlated
positions from a deal are not independent samples for a confidence interval.

Direct playing-strength comparisons use five tricks, 128 sampled worlds,
250,000 nodes, exact three-trick limit 1,680, declarations and transpositions,
ownership power 1/uniform mix .1, and no wall-clock cutoff on both sides. Each
row uses development seed 709, ten workers and 1,000 mirrored pairs / 2,000 whole
games. Uncertainty is one empirical standard error across mirrored pairs:

| Candidate / opponent | Win rate +/- one pair SE | Games |
|---|---:|---:|
| Mixed CE4 / neural CE4 | 50.000% +/- .548 pp | 2,000 |
| Neural CE4 / original CE12 | 50.150% +/- .594 pp | 2,000 |
| Mixed CE4 / original CE12 | 49.650% +/- .602 pp | 2,000 |

None demonstrates a strength gain. The improved mixed-pool prediction metrics
therefore do not justify promotion, and the frozen belief5-v1 CE12 model remains
the candidate for independent opponent validation. These three matches compare
ownership variants against one another; they do not measure the variants'
individual win rates against SmartPlayer, SharpBelot or Belot206.

Reproduction (from the repository root, using a fresh output folder on reruns):

```powershell
$python = 'artifacts/neural-20260927/torch-env/Scripts/python.exe'
foreach ($pool in @('neural', 'diverse')) {
    & $python tools/NeuralTrainer/Gpu/finetune_ownership.py --input artifacts/fast-bot-20260928/ownership-ce12-seed9821 --data "artifacts/fast-bot-20260928/ownership-$pool-train/data" --validation-data "artifacts/fast-bot-20260928/ownership-$pool-validation/data" --data-format ppo --out "artifacts/fast-bot-20260928/ownership-$pool-ce4-lr1e4-b2048-seed9837" --objective ce --epochs 4 --batch 2048 --cache-device --learning-rate .0001 --seed 9837 --device cuda
}
& $python artifacts/fast-bot-20260928/ownership_diversity_cross_metrics.py
```

Artifact prefixes are `ownership-{neural,diverse}-{train,validation}/data` and
`ownership-{neural,diverse}-ce4-lr1e4-b2048-seed9837/`, all under
`artifacts/fast-bot-20260928/`. Each fit's `report.json` records actual PPO and
warm-start hashes, collection manifests, all epoch metrics and shuffle/export
hashes. Final model folders end in `/ce`. Cross-distribution details, including
all contract rows, are in `ownership-diversity-cross-metrics.json`; the paired
match reports are `paired-diverse-neural.arena.json`,
`paired-neural-ce12.arena.json` and `paired-diverse-ce12.arena.json`.

### Inference-time suit averaging: positive pure-network screen

This experiment keeps every actor weight fixed and averages ordinary card-network
values over the full allowed suit-permutation group. The trump suit and every
suit bid by any seat are fixed; the remaining suits produce 1, 2, 6 or 24 views.
All 16 card planes move consistently, each output is mapped back to its original
physical card, and normalized values are averaged before the game-point scale is
applied. Scalar bid features remain fixed. A single identity view calls the
original evaluator directly, preserving its bits; the disabled option preserves
the original path and does not allocate an ensemble helper.

These maps preserve the legal observed auction. They need not preserve the
posterior induced by a learned bidding policy, which can prefer particular suits
even when those suits were not bid. Therefore this is an empirical value-averaging
experiment, without a claim of exact Bayesian invariance. Unlike the earlier
unsuccessful suit augmentation fits, it changes inference only.

Only ordinary card-network fallback uses the ensemble. Bidding, completed
endgame searches, full-deal search results and all rollout policies remain
unchanged. No new weights, features, runtime dependencies or random draws are
introduced. `CardSuitEnsemble` / `--card-suit-ensemble true` defaults to false;
the independently validated belief5-v1 promotion settings remain unchanged.

Both pure-network teams use the frozen baseline actor, with search and endgames
disabled. Eight workers run mirrored whole games; uncertainty is one empirical
standard error across pairs. The larger seed was chosen after the initial
positive screen, and its games are independent of that screen.

| Evaluation | Seed | Games | Win rate +/- one pair SE | 95% interval | Points/game | Elo |
|---|---:|---:|---:|---:|---:|---:|
| Initial screen | 797 | 2,000 | 51.550% +/- .675 pp | [50.227%, 52.873%] | +2.8 | +11 +/- 5 |
| Independent confirmation | 798 | 20,000 | 52.030% +/- .220 pp | [51.599%, 52.461%] | +2.8 | +14 +/- 2 |

The runs took 3.39 and 31.64 seconds respectively; those wall times are throughput
observations under concurrent work, not idle per-card latency measurements.
This is a modest gain against the network's own lineage, so it does not replace
the strong-opponent promotion gate. Hybrid and latency checks are separate.

All 23 new ensemble tests pass: complete groups and fixed bid/trump suits, an
independent dense reference across all 16 planes and all six contracts,
engine/view parity, hidden-hand independence, unchanged bidding and RNG,
identity-only bit preservation, and successful search/endgame bypass. Seven
additional benchmark tests cover named profiles versus configured flags. The
complete AI suite passes 361/361 with no warnings, and the copied Release trainer
build reports zero warnings and errors. `bench --player master` now measures the
actual shared embedded profile and reports its effective settings; named neural
profiles skip the unrelated self-play microbenchmark.

Reproduction using the copied runner (baseline weights are unchanged):

```powershell
dotnet artifacts/fast-bot-20260928/ensemble-bin/NeuralTrainer.dll arena --player candidate --in artifacts/fast-bot-20260928/baseline --opponent neural --opponent-in artifacts/fast-bot-20260928/baseline --card-suit-ensemble true --search-deals 0 --endgame false --pairs 10000 --threads 8 --seed 798 --data artifacts/fast-bot-20260928/ensemble-confirm798/data
```

Exact settings, pair scores and results are saved in
`ensemble-screen797/data.arena.json` and `ensemble-confirm798/data.arena.json`,
under `artifacts/fast-bot-20260928/`. Build/test logs are `ensemble-build.log` and
`ensemble-ai-tests-final.log` in that folder.

The follow-up adds the ensemble only to the neural fallback of the five-trick
ownership profile. Both sides use the frozen actor and CE12 ownership model,
128 worlds, 250,000 nodes, exact three-trick limit 1,680, declarations,
transpositions, ownership power 1 / uniform mix .1, and no historical-play
likelihood. Both wall-clock caps are disabled, so concurrent training/builds
cannot change the amount of search completed. Only `CardSuitEnsemble` differs.

| Hybrid evaluation | Seed | Games | Win rate +/- one pair SE | 95% interval | Points/game | Elo |
|---|---:|---:|---:|---:|---:|---:|
| Development screen | 709 | 2,000 | 50.750% +/- .691 pp | [49.396%, 52.104%] | +1.1 | +5 +/- 5 |
| Independent confirmation | 799 | 20,000 | 50.895% +/- .219 pp | [50.465%, 51.325%] | +1.2 | +6 +/- 2 |

Before observing the development result, the continuation rule was a positive
point estimate followed by 10,000 independent pairs at seed 799. The development
interval includes 50%; the larger confirmation establishes a modest gain. It
took 18:05.8 with eight workers. This fixed-work result does not replace the
8-ms candidate's independent strong-opponent checks; the promoted Master and
the ensemble's default-off setting remain unchanged pending further decisions.
Idle latency is measured separately.

The independent opponent settings are stored in `ensemble-hybrid-opponent.json`;
they match the candidate flags below, with `CardSuitEnsemble=false` and the same
baseline actor path. Reproduction:

```powershell
dotnet artifacts/fast-bot-20260928/ensemble-bin/NeuralTrainer.dll arena --player candidate --in artifacts/fast-bot-20260928/baseline --opponent configured --opponent-config artifacts/fast-bot-20260928/ensemble-hybrid-opponent.json --card-suit-ensemble true --search-deals 0 --endgame true --endgame-declarations true --endgame-tricks 5 --endgame-worlds 1680 --endgame-sampled-worlds 128 --endgame-nodes 250000 --endgame-milliseconds 0 --endgame-transpositions true --endgame-policy-actions 0 --endgame-ownership artifacts/fast-bot-20260928/ownership-ce12-seed9821 --endgame-ownership-power 1 --endgame-ownership-uniform-mix .1 --pairs 10000 --threads 8 --seed 799 --data artifacts/fast-bot-20260928/ensemble-hybrid799/data
```

Full settings, pair scores and outcomes are in
`ensemble-hybrid709/data.arena.json` and `ensemble-hybrid799/data.arena.json`
under `artifacts/fast-bot-20260928/`.

### Four-component ownership mixture: small offline gain, runtime deferred

The fixed four-epoch K4 pilot clones the CE12 ownership trunk into four
separately normalized, capacity-conditioned ownership heads plus learned gates
(`600 -> 128 -> 64 -> 388`). It matches the existing K1 joint4 control's datasets,
batch size, seed 9833, per-epoch row orders, optimizer and learning rates. Only
training tooling changed; actor networks and runtime search stayed unchanged.
See [MIXTURE_BELIEF_NOTE.md](MIXTURE_BELIEF_NOTE.md) for the research, exact mixture
likelihood, initialization, strict export format and reproduction command.

Before results, the runtime follow-up criterion was approximately **0.03
nats/state** lower aggregate late-phase joint NLL, no large contract regression,
and noncollapsed components. Late means at least three completed tricks. The
predefined final checkpoint is epoch4, evaluated after half export and reload.

| Contract | Heldout states, all / late | All NLL reduction, K1 minus K4 | Late NLL reduction, K1 minus K4 |
|---|---:|---:|---:|
| Suit | 31,782 / 17,986 | 0.011240 | 0.011477 |
| No trumps | 16,101 / 9,316 | 0.013720 | 0.017947 |
| All trumps | 46,720 / 27,963 | 0.009338 | 0.009116 |
| State-weighted aggregate | 94,603 / 55,265 | 0.010723 | 0.011373 |

Aggregate late NLL changes from **5.581697 to 5.570324**, a **0.204%** reduction;
overall it changes from 10.113960 to 10.103237 (0.106%). Late effective component
counts average 3.9967 for gates and 3.5276 for posterior responsibilities, so the
heads separated without gate collapse. The improvement is below the predefined
threshold: retain the result and defer runtime integration. No game win rate or
runtime latency was measured for K4, and correlated positions are not treated
as independent samples for an uncertainty estimate.

All 10 new mathematical/export/runner tests pass. Source hashes, every epoch's
row order and learning rate match the K1 control. After training, all **12 epoch
checkpoint hashes and three final export hashes** were rechecked against the
recorded report. Training/export/evaluation epoch intervals total 257.87 seconds;
the first-to-last log span is 313.51 seconds. The three final files total 662,532
bytes, with new tags 14–16 that the current ownership runtime refuses.

Evidence is in
`artifacts/fast-bot-20260928/ownership-mixture4-joint4-lr1e4-b2048-seed9833/`:
`report.json`, `summary.json`, `summarize.py`, `training.log`, all preserved
epochs, and final exports. The note records final SHA-256 values and all
conditioning requirements for any future runtime implementation.

### Pure-network suit ensemble transfers to three independent opponents

The six cases were declared before their outcomes: baseline and ensemble versus
SmartPlayer at seed 821, SharpBelot at 827, and Belot206 at 829. Each uses 5,000
mirrored pairs (10,000 whole games), four workers, the frozen baseline actor,
zero full-deal search, and no endgame search. The only candidate-setting change
is `CardSuitEnsemble`; the two variants use identical mirrored seeds for each
opponent. All six cases run sequentially from the same copied runner.

For each pair `i`, the paired change is the ensemble score minus the baseline
score. Its uncertainty is the sample standard deviation of these 5,000
differences divided by `sqrt(5000)`. Absolute rates also use empirical pair
standard errors. Every uncertainty below is one standard error; changes and
their intervals are in percentage points.

| Opponent | Seed | Games per variant | Baseline win rate +/- SE | Ensemble win rate +/- SE | Paired change +/- SE | Paired 95% interval |
|---|---:|---:|---:|---:|---:|---:|
| SmartPlayer | 821 | 10,000 | 86.780% +/- .321 pp | 88.190% +/- .307 pp | +1.410 +/- .290 pp | [+0.841, +1.979] pp |
| SharpBelot | 827 | 10,000 | 78.860% +/- .386 pp | 80.740% +/- .374 pp | +1.880 +/- .326 pp | [+1.240, +2.520] pp |
| Belot206 | 829 | 10,000 | 67.180% +/- .439 pp | 69.530% +/- .431 pp | +2.350 +/- .416 pp | [+1.534, +3.166] pp |

This supports transfer beyond the network's own lineage. It does not establish
single-forward speed or replace the ISMCTS promotion gate. These runs overlap
other fixed-work experiments and make no latency claim. No app default changed.
The comparisons use the repository's adapted SharpBelot and Belot206 players;
their complete compatibility diagnostics are retained in the arena reports.

Reproduction and evidence are under
`artifacts/fast-bot-20260928/ensemble-transfer/`: the predeclared `plan.json`,
`run.ps1`, six exact argument files, six logs and `.arena.json` reports,
`summarize.py`, and `summary.json`. Each opponent's `*-paired.json` preserves all
5,000 per-pair differences. The summary checks every non-experiment setting for
equality, independently recomputes absolute rates/SE, and records source hashes.

### Distilling the suit ensemble: better proxy metrics, no baseline strength gain

The follow-up tries to retain the ensemble's benefit at the original
single-forward cost. It changes only training tooling and weights in an
experiment folder. Each card MLP keeps layout1 and shape
`600 -> 512 -> 256 -> 128 -> 32`; every bidding file remains byte-identical to
the frozen baseline. No candidate weights were promoted.

The public-state corpus is the existing `belief-selfplay/data` and separate
`belief-validation/data`: 1,884,525 training and 94,603 heldout positions. The
loader discards the old Monte Carlo targets and verifies each action mask
against the public legal-card plane. It reads no ownership labels or hidden
inputs. The frozen baseline generates deterministic ensemble Q targets using
the complete group fixing the trump and every suit bid by any seat. All 16
card planes move; scalars stay fixed; outputs map back before averaging.

For legal action `a`, the target is
`ensembleQ(a) - legalMean(ensembleQ) + legalMean(baselineQ)`. Training retains
normalized Q units internally (one unit = 26 game points), float32 teacher
targets, the existing centered Huber loss with mean-value weight 1, fresh Adam,
gradient clipping at 1, and a baseline half-weight warm start. The matched
self-distillation control uses `baselineQ` targets with identical initialization,
optimizer, row order and learning rates. The fixed schedule is four epochs,
batch 2,048, seed 9851, learning rate 1e-5 for epochs 1–2 and 3e-6 for epochs 3–4.
Teacher batching is 4,096. Epoch 4 was selected before outcomes; all epochs remain
available as evidence, without retrospective checkpoint selection.

The teacher passed actual managed/CUDA parity on 240 states and 7,680 outputs:
192 actual heldout states plus 48 synthetic auction-mask cases used only for
parity. These cover all admissible fixed-suit masks and 1/2/6/24 views. Maximum
error was 7.153e-7 normalized Q, or 1.86e-5 game points. The final half exports also
pass managed/Python single-forward parity on the same 240 inputs: maximum error
7.153e-7 for the distilled candidate and 5.961e-7 for the control. The reports
include all fixture, assembly and weight hashes.

All heldout errors below are in **game points** and come from actual half
exports. Centered RMSE removes each state's mean error before averaging over
legal actions. Regret is the ensemble teacher's best legal value minus its
value for the student's choice, averaged over states.

| Contract | Heldout states | Baseline / control / distilled teacher regret | Baseline / distilled centered RMSE | Distilled mean-value drift RMSE |
|---|---:|---:|---:|---:|
| Suit | 31,782 | .015671 / .015666 / .013311 | .151090 / .135822 | .029464 |
| No trumps | 16,101 | .064495 / .064549 / .057648 | .515814 / .477432 | .073617 |
| All trumps | 46,720 | .059354 / .059252 / .049959 | .660701 / .595310 | .112152 |
| Aggregate | 94,603 | .045554 / .045511 / .038956 | .527692 / .477615 | .086173 |

Any-optimal-action agreement with the teacher increases from 86.021% to 86.710%.
The report also contains canonical-index top1 agreement; it must not be read as
exact runtime physical-card agreement on tied suit-contract values, because the
corpus omits the original trump rotation. Teacher regret avoids that ambiguity.
These proxy improvements require a game test.

The two game screens and seeds were also declared before outcomes. Both use
only single-forward networks, no endgame/full-deal search or inference ensemble,
eight workers, and 10,000 mirrored pairs each. Uncertainty is one empirical pair
standard error.

| Opponent | Seed | Games | Distilled win rate +/- SE | 95% interval | Points/game |
|---|---:|---:|---:|---:|---:|
| Frozen baseline | 853 | 20,000 | 50.035% +/- .175 pp | [49.691%, 50.379%] | +.216 |
| Matched self-distillation control | 857 | 20,000 | 50.505% +/- .177 pp | [50.157%, 50.853%] | +.446 |

The candidate demonstrates no gain over the frozen baseline, so this bounded
pilot stops without promotion or further opponent screens. Its result against
the matched control is recorded separately; the control's absolute strength
against the baseline was not measured. Better heldout ensemble imitation alone
did not establish a stronger player.

The complete teacher preparation and both matched fits took 121.95 seconds.
Seven new focused tests pass, and the full Python discovery passes 78/78. Tests
cover independent group enumeration, all card planes and fixed auction masks,
legal-mean anchoring, independence from old Monte Carlo targets, units, matched
orders, half exports and unchanged bidding. All 30 card checkpoint/final hashes
and 10 bidding copies were rechecked after training.

Reproduce fitting from the repository root:

```powershell
& artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/distill_suit_ensemble.py --input artifacts/fast-bot-20260928/baseline --data artifacts/neural-20260927/belief-selfplay/data --validation-data artifacts/neural-20260927/belief-validation/data --out artifacts/fast-bot-20260928/ensemble-distill4-seed9851 --epochs 4 --batch 2048 --teacher-batch 4096 --learning-rate 0.00001 --seed 9851 --device cuda
& artifacts/fast-bot-20260928/ensemble-distill4-seed9851/run-screens.ps1
```

The output folder contains `plan.json`, `training.log`, `report.json`,
`summary.json`, teacher parity fixtures/reports, both objectives' checkpoints,
final-export parity reports, and both screens' exact arguments/logs/arena JSON.
The fitter opts out of Windows power throttling and uses two CPU threads.
Its report SHA-256 is
`2c16f2f7cb813f031f609f24e4494c0ca91e3deb50882b4f014edd02fce74b54`.

### Final idle timing of the frozen profiles

The September 29 timing pass used one callback thread, 20 warmup games excluded
from statistics, then 100 engine games per profile. All other training, tests,
builds and matches were stopped. The six profiles were run sequentially with the
same copied trainer. These are measured warmed desktop callbacks; scheduling and
managed execution do not provide a hard real-time guarantee on every device.

| Profile | Card decisions | Mean us | Median us | p95 us | p99 us | Maximum us | Above 10 ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| Frozen pure network | 21,187 | 13.6 | 13.0 | 17.2 | 25.6 | 221.1 | 0 |
| Pure network with suit ensemble | 21,295 | 68.9 | 60.9 | 260.0 | 288.9 | 615.6 | 0 |
| Embedded belief5-v1 Master | 21,650 | 1,442.9 | 31.4 | 5,646.8 | 6,151.0 | 8,006.5 | 0 |
| belief5-v2, adding suit ensemble | 21,246 | 1,449.1 | 70.8 | 5,571.9 | 6,082.7 | 8,006.0 | 0 |
| Previous fast, three tricks / 90 worlds | 21,225 | 27.4 | 13.2 | 87.3 | 321.6 | 1,104.1 | 0 |
| Expert, temperature 1.5 / maximum regret 4 | 20,821 | 27.0 | 13.2 | 83.6 | 304.5 | 1,263.3 | 0 |

The pure ensemble exceeds the original 50-us target for the fast network. It is
an opt-in comparison, while the constructor and fast profile retain their current
single-forward behavior. Its extra cost is small beside the five-trick search.
No actor or ownership weight file differs between v1 and v2.

Exact arguments and logs are `final-bench-*.arguments.json` and
`final-bench-*.log` under `artifacts/fast-bot-20260928/`; the aggregate is
`final-timing-summary.json`. `bench --player master` uses the same shared factory
as the app and Elo suite, ignoring conflicting candidate flags. At this timing
checkpoint that factory was v1; v2 was measured through `--player candidate`
with the complete settings in its saved arguments.

### Independent check of the eight-millisecond ensemble candidate

Before these matches, `belief5-v2-manifest.json` froze the copied executable,
the four actor files and all three CE12 ownership files by SHA-256. The full
settings equal v1 with `CardSuitEnsemble=true`. All builds, fitting, collection,
tests and other matches were stopped for the timed comparisons.

The first independent gate, seed 863, used the actual eight-millisecond cap on
both sides. Against v1 it scored **50.930% +/- .217 pp over 20,000 games**,
95% interval **[50.505%, 51.355%]**, +1.346 points/game and +6.46 +/- 1.51 Elo.
It took 14:11.884 with ten workers. This passes the predeclared requirement that
the lower 95% endpoint exceed 50%, and triggers the external-opponent checks.
The result agrees with the earlier fixed-work comparison but is reported
separately because its stopping rule is different.

The predeclared remaining gates require lower 95% endpoints above 50% against
ISMCTS100 and the previous 100-rollout Master, each over 1,000 games. Five
additional 10,000-game comparisons cover the previous fast profile, pure NN,
SmartPlayer and both adapted legacy bots. A separate 1,000-game pure-ensemble
check against ISMCTS100 follows regardless of the first gate. Exact commands,
seeds and ordering are in `belief5-v2-independent-plan.json` and
`run-belief5-v2-independent.ps1`; each completed match retains its arguments,
log, pair scores and full arena JSON. External results are pending at this
checkpoint; the app factory still selects v1.
