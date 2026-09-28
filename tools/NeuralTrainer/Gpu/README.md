# CUDA fitting (training only)

This optional Python tool fits the C# trainer's search-teacher `.samples` files.
The app still uses the existing managed C# forward pass and embedded BNN1 weights.
It adds no Python, CUDA, or native dependency to the player.

## Environment

Tested on Windows, Python 3.12.3, an RTX 2080 SUPER (8 GB), and driver 591.86.
Use a separate environment; the commands do not replace system Python packages:

```powershell
python -m venv artifacts/torch-env
artifacts/torch-env/Scripts/python.exe -m pip install -r tools/NeuralTrainer/Gpu/requirements.txt
artifacts/torch-env/Scripts/python.exe -m unittest discover -s tools/NeuralTrainer/Gpu -v
```

The pinned CUDA 12.6 wheel is from the [official PyTorch index](https://pytorch.org/get-started/previous-versions/).
The script refuses a requested CUDA run if CUDA is unavailable. `--device cpu` is
available for testing. It opts its own Windows process out of power throttling.

## Fit and evaluate

First collect data using a copied C# trainer build, so long jobs do not lock the
test project's build output. The four sample files must finish writing before
fitting starts. Copy a periodic dataset to a separate folder before using it.

```powershell
dotnet artifacts/trainer/NeuralTrainer.dll distill --teacher neural --in artifacts/baseline --search-deals 100 --card-label-chance 1 --games 2000 --threads 12 --seed 1731 --data artifacts/teacher/data
artifacts/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit.py --in artifacts/baseline --data artifacts/teacher/data --out artifacts/student --epochs 12 --batch 1024 --learning-rate 5e-5 --card-value-weight 0.05 --seed 401
dotnet artifacts/trainer/NeuralTrainer.dll diagnose --in artifacts/student --data artifacts/teacher/data
dotnet artifacts/trainer/NeuralTrainer.dll validate --in artifacts/student --opponent artifacts/baseline --pairs 10000 --threads 16 --seed 29
```

Use `--card-value-weight -1` for ordinary masked Huber loss. A nonnegative value
selects the same centred action-value objective as `ActionValueLoss.cs`.
The learning rate drops to 30% for the final 30% of epochs. Adam and norm clipping
are PyTorch implementations; their floating-point updates are not expected to
match the C# optimiser bit for bit.

Experimental `--policy-temperature 2 --card-value-weight 0.01` replaces the
advantage regression term with KL divergence between teacher and student
softmaxes over legal actions. Temperature is in game points. Both softmaxes
use that temperature, and the loss is scaled by its square in network units;
this preserves point-scaled outputs. The weighted Huber error of the mean Q
value remains. This is a variant of policy distillation, not an exact
reproduction of Rusu et al.'s teacher-only temperature. The default is zero
(disabled); first pilot results did not improve playing strength.

`--residual-sizes 128,64,64` freezes the loaded card network and trains a small
additive branch with zero initial output. It has one width per original hidden
layer. Export merges the branches into a single block-diagonal MLP, so the C#
loader and inference code remain unchanged. Tests check initial equivalence,
that the original parameters stay fixed, and equivalence after merging/export.
This increases file size and inference work; benchmark before promotion. The
first limited-data pilot overfit and became substantially weaker.

Two data options are experimental:

- `--suit-augmentation` permutes all 16 card planes and their labels/masks together.
  It fixes trumps in suit contracts and only changes positions whose auction has
  no bid in an affected suit. Scalar auction features remain unchanged. This
  avoids inventing illegal auction histories; the learned teacher itself need
  not be perfectly symmetric, so strength still requires measurement.
- `--anchor-mean` keeps each teacher action difference but shifts the training
  targets to the warm-start network's mean value over legal actions. This tests
  whether learning a new common value under sampled worlds disrupts the useful
  pretrained representation. Validation still uses the original teacher labels.

`--minimum-teacher-regret 2` is a separate experimental filter: use search targets
only where the teacher values its best action at least two game points above the
warm start's chosen action. Elsewhere, use the warm start's own Q-values as
preservation targets. It is applied before optional mean anchoring. This threshold
is a hypothesis about suppressing noisy small action differences, not a statistical
confidence bound. Held-out validation labels remain untouched.

`--validation-data <prefix>` uses a separately collected dataset. Without it,
5% of samples are held out at random. That split can share games with training
and is only a fitting diagnostic. Promotion requires independent whole-game
matches, including ISMCTS at 100 ms with the machine otherwise idle.

Each `epoch-NNN/` and the output root contain four checked-header, 16-bit BNN1
files. Wait for fitting to finish before using an epoch folder: contracts train
one at a time. An empty contract dataset copies its warm-start file unchanged,
including bidding when collecting only card decisions. `sources.json` records
SHA-256 hashes of source weights and datasets; stdout records configuration,
library version, hardware, timings, and diagnostics. Save stdout with the run.

The reader checks tags, feature layout, dimensions, finite parameters, exact
length, sparse feature indices and action masks. Export rejects non-finite or
overflowed parameters. Only layout 1 is supported; an encoder change requires
updating this tool and its checks alongside the C# loader.

## Verification and measured cost

Unit tests cover input-major weight layout, exact half-weight round trips,
malformed files, sparse samples, finite-difference loss gradients, common-offset
cancellation, policy-distillation gradients, partial batches and empty-contract
preservation, and legal-action diagnostics in game points.

On September 27, 2026, exported weights loaded in the C# trainer. On 307 real
labelled decisions, C# and Python agreed to the C# log's precision (0.001 point)
on absolute/centred RMSE and teacher regret, and selected the same fraction of
optimal actions. A 47,500-sample synthetic fitting epoch took 0.66-0.76 seconds
on CUDA versus approximately 2 seconds in C# with eight learners. Both ran
during teacher collection; this is a throughput pilot, not a player-strength
result or an isolated hardware benchmark.

## Batched search-label collection

The optional server batches inference for search rollouts. C# still samples
worlds, encodes each deciding seat's public information, applies legal moves,
and scores deals. The server receives layout-1 features and returns independent
card choices. It binds only to loopback and checks SHA-256 hashes of all four
weight files when a worker connects.

```powershell
artifacts/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/serve.py --in artifacts/baseline --port 18731 --graphs
```

Run the copied trainer while that server is running:

```powershell
dotnet artifacts/trainer/NeuralTrainer.dll distill --teacher neural-gpu --in artifacts/baseline --gpu-port 18731 --search-deals 100 --teacher-play-chance 0 --card-label-chance 1 --games 2000 --threads 4 --seed 1732 --data artifacts/gpu-teacher/data
```

`--teacher neural-batch` uses managed inference with the same batched simulation
algorithm. `--gpu-verify true` additionally checks every GPU choice against C#
and reports mismatches and their point-value gaps; use it for small verification
runs. It refuses a disagreement above 0.01 game points. Normal collection skips
that extra CPU work. The original `--teacher neural` path remains available.
GPU requests are limited to 4,096 rollout positions; 100 worlds and eight root
cards require at most 800 positions.

`--graphs` captures static batches rounded up to powers of two, reducing kernel
launch overhead. Input/output buffers stay alive and a lock prevents concurrent
capture or buffer reuse. Unused padded rows do not affect real rows. This follows
the [PyTorch 2.9 CUDA graph constraints](https://docs.pytorch.org/docs/2.9/notes/cuda.html#cuda-graphs).
Without `--graphs`, the server uses ordinary eager execution. Twelve Python
tests cover the fitter, binary transport, hash rejection, legality, physical
tie-breaking after suit rotation, and graph/eager agreement across changing
batch sizes (the CUDA test is skipped if no GPU is available).

Managed batched search matched the original search's action values exactly in
the whole-game C# tests. A GPU check matched all 96,585 managed rollout choices.
Across two identical games, all 416 stored feature/label records matched C#
exactly, with both eager CUDA and CUDA graphs. Collection took 55 seconds with
managed inference, 27 with eager CUDA, and 17 with graphs on one actor. A separate
four-actor run collected 4,183 positions in 76 seconds across 20 games. These
are training-throughput pilots during other collection work, not app latency
measurements or proof of universal floating-point equivalence.

For collections larger than available GPU memory, `fit.py --stream-data` keeps
samples in CPU memory and transfers one training batch at a time. The default
keeps one contract's dataset on the GPU. GPU features retain their source file's
float16 representation and each batch converts exactly back to float32 for all
network arithmetic; this reduces GPU storage without further rounding the inputs.
Both modes use the same sample order, loss and export format; tests compare exported weights, including CUDA when
available. Host-memory streaming trades transfer time for a bounded GPU footprint.

## Function-preserving depth

`deepen.py --in <folder> --out <deeper-folder> --layers 2` inserts two identity
ReLU layers before each card network's output. The original hidden activations
are nonnegative, so the initial function is preserved. Bidding is copied unchanged.
The experimental shape becomes 600-512-256-128-128-128-32, adding 198,168 bytes across
the three exported files. The existing managed loader and BNN1 format handle it.

This applies [Net2DeeperNet](https://arxiv.org/html/1511.05641), not a residual
architecture. It tests depth without discarding the warm start. Tests check exact
prediction/export parity, gradients through new layers and invalid depth. Match
strength and idle latency must be measured after fitting; deeper is not assumed better.

## Distil the suit-ensemble teacher

`distill_suit_ensemble.py` fits the frozen baseline's inference-time suit average
into the original `600 -> 512 -> 256 -> 128 -> 32` card networks. It uses public
layout-1 states from existing `.samples` files, discards their old Monte Carlo
targets, and checks each action mask against the public legal-card plane. It
reads no ownership sidecars. Bidding is copied byte for byte to every checkpoint.

The teacher averages every permitted suit permutation, fixing trumps and every
suit bid by any seat. All 16 card planes move together; scalar features stay
fixed and outputs map back before averaging. For each legal action, its target is
`ensembleQ - legalMean(ensembleQ) + legalMean(baselineQ)`. Targets remain float32
in normalized Q units (one unit = 26 game points). The centered Huber objective
uses mean-value weight 1. A separate `control/` branch fits baseline Q targets
with the same half-weight warm start, fresh Adam, row order and learning rates.

Run from the repository root after preserving the frozen source weights:

```powershell
& artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/distill_suit_ensemble.py --input artifacts/fast-bot-20260928/baseline --data artifacts/neural-20260927/belief-selfplay/data --validation-data artifacts/neural-20260927/belief-validation/data --out artifacts/fast-bot-20260928/ensemble-distill4-seed9851 --epochs 4 --batch 2048 --teacher-batch 4096 --learning-rate 0.00001 --seed 9851 --device cuda
```

The fixed pilot uses learning rate 1e-5 for epochs 1–2 and 3e-6 for epochs 3–4,
gradient clipping at 1, two CPU threads and Windows power-throttling opt-out.
`ensemble/` and `control/` each contain the four ordinary BNN1 files and all
`epoch-NNN/` checkpoints. Wait until fitting finishes: contracts train in turn.
`report.json` records source hashes, matched row-order hashes and diagnostics
after reloading each actual half export. It checks that bidding remains unchanged.
The predefined evaluated checkpoint was epoch 4, without heldout checkpoint selection.

Seven focused tests cover complete suit groups, all fixed auction masks, an
independent dense reference, legal-mean anchoring, old-target independence,
units, matched orders and exported files. Actual managed/Python parity passed
on 240 inputs and 7,680 outputs for the teacher and both final students, covering
1/2/6/24-view groups; maximum error was below 7.2e-7 normalized Q. Top1 diagnostics
use canonical card-index tie breaking, because the corpus omits physical trump
rotation. `ensemble_optimal_choice_rate` accepts every zero-regret action;
`ensemble_regret_game_points` is the principal heldout action metric.

The September 29 pilot finished in 121.95 seconds including teacher preparation
and both fits. Aggregate heldout teacher regret improved from .045554 to .038956
game points, but the fixed epoch-4 student scored **50.035% +/- .175 percentage
points against the frozen baseline over 20,000 mirrored whole games** (seed 853;
95% interval [49.691%, 50.379%]). Against its matched self-distillation control it
scored 50.505% +/- .177 pp over another 20,000 games (seed 857). Uncertainty is one
empirical standard error across mirrored pairs. The control's strength against
the baseline was not measured directly. With no demonstrated baseline gain,
the pilot stopped without promoting weights or changing runtime defaults.

The run folder contains `plan.json`, `run-screens.ps1`, exact screen arguments,
arena reports, parity reports, training logs and `summary.json`. See
[FAST_BOT_EXPERIMENT.md](../../../FAST_BOT_EXPERIMENT.md) for the complete results,
including the stronger inference-time ensemble's separate opponent comparisons.

## Auxiliary card-location experiment

`record-selfplay` freezes the supplied networks and records the existing true-deal
Monte Carlo action targets, plus a separate `.samples.owners` training-label file.
The two-bit owner for each rotated card is relative to the deciding seat; zero
marks own/played cards, excluded from the auxiliary loss. The sidecar has a BPO1
magic, feature layout, output count, sample count and one uint64 per sample. It
uses exactly the same buffer slots as the ordinary samples. Overflow is refused.

```powershell
dotnet artifacts/trainer/NeuralTrainer.dll record-selfplay --in artifacts/baseline --deals 100000 --threads 10 --capacity 1500000 --card-label-chance 1 --bid-label-chance 0 --bid-exploration 0 --card-exploration .03 --seed 3931 --data artifacts/belief-train/data
dotnet artifacts/trainer/NeuralTrainer.dll record-selfplay --in artifacts/baseline --deals 5000 --threads 10 --capacity 100000 --card-label-chance 1 --bid-label-chance 0 --bid-exploration 0 --card-exploration .03 --seed 2931 --data artifacts/belief-validation/data
artifacts/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit_belief.py --in artifacts/baseline --data artifacts/belief-train/data --validation-data artifacts/belief-validation/data --out artifacts/belief-student --belief-weight .01 --epochs 4 --learning-rate 1e-5
```

Run the matched control with `--belief-weight 0`. Both runs warm up a random
128-to-96 ownership head for one pass while keeping the playing network frozen,
then jointly fit action values and per-card three-seat cross-entropy. Data stays
in CPU memory and batches move to CUDA, avoiding whole-dataset GPU allocation.
The auxiliary head is discarded on export. Policy inputs, inference cost, file
layout and point-valued outputs remain unchanged; ownership labels never enter
the policy's input tensor. This tests shared representation learning, unlike
DouZero+'s explicit predicted-hand inputs, and requires measured playing gains.

Tests cover ownership rotation through complete deals, removal of hidden hands
without changing any policy input, sidecar alignment after ring wraparound,
malformed sidecars, finite-difference loss gradients, masked own/played cards,
frozen warmup parameters and policy-only export. No feature-layout bump is needed
because the input encoding is unchanged.

## PPO with a helper critic

`ppo.py` alternates complete on-policy deals in the C# simulator with CUDA PPO
updates. The existing card Q networks parameterize a legal-action softmax;
selected-action return regression retains their point scale. Bidding stays frozen.
A separate critic sees public inputs and the three other remaining hands only
during training. `--critic public` zeros the private inputs for a matched control.
Neither helper is exported to the app. See [PPO_EXPERIMENT.md](../../../PPO_EXPERIMENT.md)
for the fixed comparison, measured results and research sources.

```powershell
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/ppo-bin
artifacts/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in artifacts/baseline --out artifacts/ppo-private --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 32 --deals 8192 --threads 12 --seed 4401
```

`--updates` counts actor updates after the `--warmup` critic-only batches.
Defaults use temperature 1 game point, GAE lambda 1, actor LR 1e-6 and critic LR
3e-4. `--critic-sizes 512,256,128` changes only the training helper's hidden
layers; the default is `256,128`. Resume requires the same helper architecture.
Each checkpoint contains the ordinary four BNN1 actor files plus a
training-only `training.pt` containing models, optimizers and random states.
Training uses checked BNF1 float32 snapshots so the PPO probability ratio does
not include half-weight rounding. The BPP1 records keep private cards separate
from policy inputs. The collector and Python compare snapshot hashes, chosen
Q-values and action log-probabilities before every update.

Resume with `--resume <checkpoint>/training.pt` and the same input path, critic
mode, seed, temperature, lambda, warmup, deals and thread count. `--updates` is
the desired total number of actor updates, not an extra count. Requested learning
rates override the saved optimizer rates. Use a fresh output directory to fork
an earlier checkpoint; appending requires its iteration to match the last progress
record. Keep source weights unchanged. CPU/GPU random states are restored, and
a resumed smoke run produced byte-identical final actor files.
New checkpoints also verify the source-weight hashes when resuming. Each
`run-NNNN.json` records the settings, software/device versions, initial weights,
trainer DLL and Python source hashes. Earlier pilot checkpoints without hashes
remain readable and require the caller to preserve their source weights.

Automatic checkpoint matches use greedy, search-free actors against the supplied
input folder. They are development checks. Promotion also requires independent
mirrored whole games against ISMCTS at 100 ms, with training stopped, and an idle
managed inference benchmark. Helper accuracy and training loss do not establish
playing strength.

### External opponent teams

`--opponents neural,smart,sharpbelot,belot206 --opponent-chance 1` chooses one
opposing team uniformly from that pool for each deal. Its two seats bid and play
through public engine contexts. The other team uses the current stochastic actor;
only its actions become PPO samples. A chance below one mixes these deals with
ordinary four-seat self-play. The default is zero and preserves the old collector's
output byte for byte. `neural` here means the frozen networks embedded in the copied
trainer, so preserve that build throughout the run.

`--resume` requires the same opponent list, order and chance. New checkpoints also
reject changed opponent assemblies, including their embedded frozen neural weights.
Older checkpoints remain readable. Run manifests record the assembly hashes;
progress records count deals assigned to each pool
member. The legacy adapters are training/evaluation dependencies only. They add
nothing to the app. See [OPPONENTS_EXPERIMENT.md](../../../OPPONENTS_EXPERIMENT.md)
for source fidelity, the matched control, results and complete reproduction commands.

For evaluation, `NeuralTrainer arena --player neural --opponent sharpbelot` uses
fresh seeded players for every mirrored leg and writes pair outcomes and adapter
diagnostics to `<data>.arena.json`. Names also include `belot206`, `smart`, `random`,
`dummy`, `fast`, `expert`, `master`, `rollout-master` and `ismcts:100`. The `master`
profile uses the promoted embedded ownership model and bounded five-trick
endgames; `rollout-master` preserves the former 100-rollout/400-ms profile.
`--in` chooses neural actor weights;
`--opponent-in` chooses separate opposing neural weights. `validate` retains its
individual search/temperature settings and now uses the same seeded match runner.
Time-limited search still depends on machine load: run those comparisons idle.

`bench --player master|rollout-master|fast|expert --bench-games 100` measures the
same shared profile factories used by the app and arena. It reports effective
settings, warmed engine-card latency percentiles, maximum time and counts above
10 ms. Default `neural` and explicit `candidate` retain configured flag behavior.
`--card-suit-ensemble true` averages the permitted suit permutations in ordinary
card-network fallback only; successful searches, bids and rollout policies are
unchanged. The option remains off unless explicitly enabled.

### Separate ownership models and late corrections

`fit_ownership.py` trains separate 600 -> 128 -> 64 -> 96 card-location networks
from public actor features and ownership sidecars. Layout 2 accepts 664 inputs,
adding public play chronology; it requires matching layout-2 data and sidecars.
Ownership exports use checked BNN1 headers with tags 11/12/13. These models do
not replace the actor or change its feature layout. `--history-features zero`
provides the matched 664-input control.

`finetune_ownership.py` can compare ordinary cross-entropy with exact conditional
likelihood of the complete hidden assignment. Its dynamic program enforces
public masks and remaining hand sizes. `--objective both` starts both branches
from the same half-precision files, optimizer settings and data order; it retains
every epoch. `--normalization card` averages per-state/card losses. This differs
from weighting a state in proportion to its unseen-card count. Optional GPU
caching checks a memory reserve before moving a contract's data.

```powershell
$python = 'artifacts/neural-20260927/torch-env/Scripts/python.exe'
& $python tools/NeuralTrainer/Gpu/finetune_ownership.py --input artifacts/fast-bot-20260928/ownership-ce12-seed9821 --data artifacts/neural-20260927/belief-selfplay/data --validation-data artifacts/neural-20260927/belief-validation/data --out artifacts/fast-bot-20260928/ownership-joint4-lr1e4-b2048-seed9833 --objective both --normalization card --epochs 4 --batch 2048 --learning-rate .0001 --seed 9833 --device cuda --cache-device
```

The conditional objective matches untempered product weights before extra
declaration filtering. Test runtime power 1 and uniform mix 0 when comparing
that objective directly. More accurate labels or lower validation loss do not
establish stronger play. The managed sampler only receives predicted weights,
public hand sizes, visible cards and legal-play deductions.

`ownership_rollout.read_ownership` reads existing `.ppo` files without action
targets or additional playouts. It returns the same 600 public features and
separate ownership labels, checked against public constraints. Source tags are
actor tags 1/2/3. Deal/seat identifiers are metadata for grouped validation, not
inputs. It does not create synthetic Q-value training files.

`fit_mixture_ownership.py` is an offline four-component ownership experiment.
It shares a `600 -> 128 -> 64` trunk, then emits four 96-logit ownership heads
and four gate logits. Each head is normalized separately under public masks and
remaining hand sizes before the gated mixture likelihood is computed. The
runner checks source hashes, per-epoch row orders and learning rates against
the existing K1 joint-training control. New checked export tags 14/15/16 and
the 388-output shape are deliberately incompatible with the current ownership
runtime; no runtime mixture sampler was added.

```powershell
& artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit_mixture_ownership.py --input artifacts/fast-bot-20260928/ownership-ce12-seed9821 --data artifacts/neural-20260927/belief-selfplay/data --validation-data artifacts/neural-20260927/belief-validation/data --control-report artifacts/fast-bot-20260928/ownership-joint4-lr1e4-b2048-seed9833/report.json --out artifacts/fast-bot-20260928/ownership-mixture4-joint4-lr1e4-b2048-seed9833 --epochs 4 --batch 2048 --learning-rate 0.0001 --seed 9833 --perturbation 0.05 --cache-device --cache-reserve-mib 1536 --device cuda
```

Fixed epoch 4 reduced aggregate late-phase joint NLL from 5.581697 to 5.570324
nats/state over 55,265 heldout positions with at least three completed tricks.
The .011373 reduction (0.204%) was below the predefined .03-nat criterion for
prioritizing runtime integration. Every contract improved, and components did
not collapse, but no K4 playing-strength or runtime-latency claim was made.
The experiment was retained without integration. See
[MIXTURE_BELIEF_NOTE.md](../../../MIXTURE_BELIEF_NOTE.md) for the mathematics,
conditioning requirements, full tables and hashes.

`fit_late_correction.py` freezes all base actor parameters and learns a separate
600 -> 64 -> 32 correction for each contract, from endgame-teacher samples.
Only nonforced choices after four completed tricks enter fitting. Corrections
are centered across legal actions, remain in the actor's point/26 units, and are
added only after ordinary network evaluation. Earlier decisions, illegal output
slots and successful search values are unchanged. Checked export tags are
21/22/23; each file is 81,120 bytes. Validation selects each contract's actual
half-precision export by held-out teacher regret, followed by independent games.

```powershell
& $python tools/NeuralTrainer/Gpu/fit_late_correction.py --in artifacts/fast-bot-20260928/baseline --data artifacts/fast-bot-20260928/correction-train/data --validation-data artifacts/fast-bot-20260928/correction-validation/data --out artifacts/fast-bot-20260928/correction-fit --epochs 12 --batch 1024 --learning-rate .001 --seed 11921 --device cuda
```

`arena --player candidate --opponent-config opponent.json` compares two
independently configured neural players. The JSON contains `TrainingSettings`
properties such as `In`, `EndgameTricks`, `EndgameOwnership` and `CardCorrection`.
Its `In` overrides outer `--opponent-in`, then falls back to candidate weights.
Unknown/duplicate properties and nested opponent configurations are refused.
The resolved opposing settings are printed and stored in the arena report.
Use no wall-clock cap for development comparisons under shared machine load;
measure the final time-capped configuration and timing with other work stopped.

See [FAST_BOT_EXPERIMENT.md](../../../FAST_BOT_EXPERIMENT.md) for the frozen
baseline, exact collection settings, independent seed ranges, results and
failed configurations. Only the selected ownership-weighted endgame configuration
is an app default; rejected fitting and rollout variants remain opt-in tooling.
