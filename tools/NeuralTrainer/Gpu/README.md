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
cancellation, and legal-action diagnostics in game points.

On September 27, 2026, exported weights loaded in the C# trainer. On 307 real
labelled decisions, C# and Python agreed to the C# log's precision (0.001 point)
on absolute/centred RMSE and teacher regret, and selected the same fraction of
optimal actions. A 47,500-sample synthetic fitting epoch took 0.66-0.76 seconds
on CUDA versus approximately 2 seconds in C# with eight learners. Both ran
during teacher collection; this is a throughput pilot, not a player-strength
result or an isolated hardware benchmark.
