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
