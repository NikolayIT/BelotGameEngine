# PPO with a training-only critic

## Fixed design and first comparison

Starting point: master e89185b, pushed to origin before this experiment. Inference
architecture, 600 policy inputs, bidding weights and the BNN1 format stay unchanged.
Development evaluation uses greedy card choices with both search options
disabled. Final confirmation also checks the app's existing bounded fast profile.

The actor is the existing Q network, parameterizing a masked softmax as
pi(a|o) = softmax(26 * Q(o,a) / temperature). PPO uses the clipped surrogate,
per-contract advantage normalization, a small entropy bonus, and selected-action
Monte Carlo Huber regression to retain point-valued outputs for hints and levels.
This is a Q-parameterized PPO experiment, not an exact PerfectDou reproduction.

The helper is a separate 696-256-128-1 MLP for each contract category: the public
600 inputs plus 96 bits identifying the other three remaining hands, with seats
and suits rotated exactly like the actor. It learns a residual over the public
policy's expected Q. The control has the same architecture with the private bits
zeroed. The helper is never exported or queried by the game player.

Each frozen actor snapshot generates new complete deals. The collector records
exact float32 public inputs, legal actions, action log-probabilities, outcomes,
separate private-card labels, and links to the same seat's next decision. Forced
moves are advanced by the simulator. Terminal rewards are the acting team's deal
points minus its opponents', divided by 26; hanging points are carried between
deals using the existing trainer's match-reset convention. Bidding is frozen.
Gamma=1 and initial GAE lambda=1 make the advantage exactly return minus the
helper's baseline. Baselines are computed before fitting the new batch, so a
sample's action cannot affect its own fitted baseline. Values for another seat
are never substituted for the deciding seat's next-state value.

Training snapshots use a checked BNF1 float32 format to avoid half rounding
between collection and PPO ratio calculation. Final exports remain BNN1 half
weights. Each batch records snapshot hashes and its seed/settings. Before every
update, Python compares the collector's chosen Q and log-probability against the
same actor. Independent math tests cover clipping gradients, terminal rewards,
seat trajectories, sparse records and hidden-input separation.

First fixed comparison: privileged versus public helper, each with 12 warmup
batches and 32 PPO batches of 8192 deals, 12 actor threads, temperature 1,
actor LR 1e-6, critic LR 3e-4, three epochs each, minibatch 2048, clipping .2,
entropy .001, Q regression weight 1, KL stop .01, seed 4401. Checkpoints every
four batches are screened in 4000 games versus the original networks (seed 521).
Finalists get 20000-game independent screening and ISMCTS100 evaluation. Training
loss and helper accuracy alone never authorize promotion. No learning-rate or
input changes are mixed into the first helper comparison.

The smoke test completed four 512-deal batches. Maximum observed C#/CUDA
log-probability difference was 1.39e-5; chosen normalized Q differed by at most
1.08e-6. All exports still total 2,974,830 bytes. This verifies the pipeline,
not strength. The final checks pass 97 C# AI/trainer tests and eight Python PPO
tests (28 Python tests including the earlier GPU tooling).
Resuming the 512-deal smoke run at iteration 2 produces byte-identical final
actor files at iteration 4. Resume restores optimizer/RNG states, honors explicitly
requested learning rates, and refuses mismatched collection settings or progress.

## Initial measured results

Both fixed-budget runs completed 360,448 deals (98,304 critic warmup and 262,144
PPO deals). All matches below use whole-game mirrored pairs; errors are one
empirical standard error across pairs. Each row uses the original frozen networks
as opponent, with both kinds of search disabled.

| Helper / checkpoint | Independent games (seed 523) | Win rate +/- 1 sigma | 95% interval | Points/game |
|---|---|---|---|---|
| Privileged, development-best iteration 28 | 20,000 | 50.130% +/- .189 pp | [49.759%, 50.501%] | +.4 |
| Privileged, fixed endpoint 44 | 20,000 | 50.385% +/- .198 pp | [49.997%, 50.773%] | +.5 |
| Public, fixed endpoint 44 | 20,000 | 50.475% +/- .195 pp | [50.092%, 50.858%] | +.6 |

The private helper improves prediction accuracy but has no demonstrated playing
advantage over the public helper. Before fitting the twelfth identical warmup
batch, explained variance was .713/.518/.660 for the privileged helper versus
.588/.421/.567 for the public helper (suit/no-trump/all-trump). Warmup leaves all
four exported networks byte-identical to the original; an identity match gave
50.000% +/- .000 pp in 4,000 games. Its zero observed error is expected from
identical deterministic players, not a strength estimate against another player.

Directly comparing the two fixed endpoints on seed 541 gave the private helper
**49.795% +/- .182 pp in 20,000 games**, 95% interval [49.437%, 50.153%],
+.1 point/game. This also fails to establish a playing benefit from hidden hands.

The public endpoint had a small positive result against the original, but neither
initial run passed the required ISMCTS gate. Neither initial export was promoted.
Training took 307.9 seconds for the private run and 721.3 seconds for the public
run, excluding Python startup and the final evaluation. The latter overlapped
other tests/evaluations, so these times are not a controlled throughput comparison.

## Follow-up experiments

The initial actor LR produces KL changes around .00003-.0002 per update, well
below the .01 stop, and changes roughly 1% of greedy decisions on each new batch.
The next fixed comparison raised only actor LR to 1e-5, resuming the privileged
warmup checkpoint (iteration 12), then ran the same 32 PPO updates/seeds.
The clipping and KL stop remained enabled. A separate temperature-.25 run tested
less exploration while keeping the original 1e-6 actor LR. Its critic warmed
up again because the sampled policy changed.

The LR-1e-5 endpoint scored **50.090% +/- .230 pp in 20,000 games**, 95% interval
[49.640%, 50.540%], +.1 point/game (seed 523). Larger changes did not improve
strength in this budget. A third follow-up kept temperature 1 and LR 1e-6, changed
only GAE lambda to .5, and ran 12 warmup plus 64 PPO batches. It evaluated after
32 updates for comparison, then after 64. This tested whether bootstrapping from
the helper reduced terminal-return noise enough to outweigh value-estimation bias.

The temperature-.25 endpoint scored **50.265% +/- .171 pp in 20,000 games**,
95% interval [49.929%, 50.601%], +.7 point/game (seed 523). It does not establish
an improvement. These seed-523 comparisons now also inform experiment selection;
a final candidate must receive a new seed for confirmation.

The lambda-.5 checkpoint after 32 updates scored **50.465% +/- .208 pp in
20,000 games**, 95% interval [50.057%, 50.873%], +.4 point/game (seed 523).
After 64 updates it scored **50.450% +/- .218 pp in 20,000 games**, 95% interval
[50.023%, 50.877%], +.6 point/game (seed 523). This small gain against the original
does not establish a gain against ISMCTS.

A separate capacity test used a 696-512-256-128-1 privileged helper, while
retaining the original actor, temperature 1, LR 1e-6 and lambda 1. It trained
12 warmup plus 64 PPO batches, with an evaluation at 32 updates for the original
budget comparison. This tested the helper's capacity without increasing deployed
weights or inference work. `--critic-sizes` configures only this discarded helper;
resume refuses an architecture mismatch.

The capacity test completed: iteration 44 scored **50.090% +/- .200 pp in 20,000
games**, 95% [49.699%, 50.481%], +.1 point/game. Iteration 76 scored **50.250% +/-
.211 pp in 20,000 games**, 95% [49.837%, 50.663%], +.4 point/game. Both use seed
523. Contract-only checks of iteration 76 (20,000 games each, same seed) gave
all trumps 50.485% +/- .172 pp, no trumps 50.165% +/- .116 pp, and suits 49.640%
+/- .135 pp. Larger capacity did not improve the whole actor in this pilot.

Contract checks from the GAE-64 endpoint replace only one card file at a time,
retaining the other original networks. On 20,000 games each, seed 523: all trumps
50.610% +/- .179 pp; no trumps 49.945% +/- .129 pp; suits 49.515% +/- .136 pp.
The all-trump variant's fresh seed-547 check gave 50.370% +/- .187 pp in 20,000
games, 95% [50.003%, 50.737%]. This small positive signal motivated a fixed
continuation of GAE from 64 to 128 updates and another all-trump-only export.
That continuation used seed 523 for comparison, then a new confirmation seed
after candidate selection. It does not authorize promotion without ISMCTS.

## Final candidate and confirmation

The 128-update GAE endpoint (iteration 140) supplied only `alltrumps.bin`;
`bid.bin`, `trump.bin` and `notrumps.bin` remain the original files. Including
warmup, its lineage used 1,146,880 deals. All four exported files still total
2,974,830 bytes, with identical inference shapes and no helper at runtime.

Its development result was 50.650% +/- .189 pp in 20,000 games (seed 523).
After selection, seed 557 confirmed **50.628% +/- .135 pp in 40,000 games**
against the original pure networks, 95% [50.364%, 50.891%], +.9 point/game.
With bounded endgames enabled for both sides, it scored **50.620% +/- .187 pp
in 20,000 games**, 95% [50.253%, 50.987%], +.8 point/game. Both are about +4 Elo.
The CLI uses `seed * 100000 + pairIndex`, so confirmation deals are disjoint
from development. Mirrored pairs are the independent units for the reported error.

Against SmartPlayer, the pure candidate scored **86.980% +/- .226 pp in 20,000
games**, compared with the original's **86.495% +/- .230 pp in 20,000 games**,
both on seed 557. These are separate match estimates, not a significance test
of their difference. See `NEURAL_NETWORK.md` section 14 for intervals and the
full ablation tables.

The final ISMCTS100 comparisons were fixed before their results: pure networks,
2,000 games, seed 563; bounded fast profile, 1,000 games, seed 569. The proposed
app profile retains three-trick endgames, declaration constraints and the 90-world
limit. Its gate requires a 95% interval above 50% and less than 50 us/card, plus
the already confirmed head-to-head gain. The pure check finished at **50.000% +/-
.978 pp in 2,000 games**, 95% [48.084%, 51.916%], +.1 point/game. There is no
demonstrated search-free win against ISMCTS. Idle engine timing measured 15.2
us/card for the selected pure policy (21,187 choices) and 27.6 us for its bounded
fast profile (21,225 choices), each over 100 games after warmup. The corresponding
original profiles measured 16.5 and 27.9 us. Architecture is unchanged and timings
vary between passes, so this is not evidence of an architectural speed gain.
The bounded profile scored **54.100% +/- 1.386 pp in 1,000 games** against
ISMCTS100, 95% [51.384%, 56.816%], +5.7 points/game. It passes the fast-player
gate. Against SmartPlayer it scored **89.835% +/- .205 pp in 20,000 games**,
95% [89.433%, 90.237%]. These results belong to PPO plus the existing bounded
endgames; they do not establish a pure-network win against ISMCTS.

Master with the selected weights measured 56.20 ms/card (669 choices in four
games). Two predeclared 1,000-game comparisons checked it against the new fast
profile (seed 577) and original Master (seed 587), both at 100 search worlds and
a 400-ms cap. The first finished at **51.500% +/- 1.099 pp in 1,000 games**,
95% [49.346%, 53.654%], +4.9 points/game. Its higher point estimate does not
establish a search advantage at 95% confidence. Against the original Master,
the candidate scored **50.000% +/- 1.184 pp in 1,000 games**, 95%
[47.679%, 52.321%], +.3 point/game. No significant change was found; this does
not prove equivalence. Master retains its existing 100-world/400-ms settings.

The selected `alltrumps.bin` is now embedded after passing the fast-profile gate.
The rebuilt assembly and selected folder produced identical outcomes over 2,000
mirrored games, including equal point totals. The other three files are byte
identical to the frozen original. App recalibration (`elo 20000 60`, 241,080
games, 20:55) gives Random 656 +/- 3.1, Beginner 1200 (fixed), Skilled 1466 +/-
1.7, Expert 1600 +/- 2.2, Master 1760 +/- 23.1, and ISMCTS 1762 +/- 20.9.
The first four played 120,240 games each; Master and ISMCTS played 600 each.
Errors are one shared-seed bootstrap standard deviation. Ratings are copied to
`AiLevels.cs`; Expert keeps temperature 1.5 and MaxRegret 4 because it remains
between Skilled and Master. Post-promotion checks pass: 741 engine tests, 97 AI
tests and 72 UI tests; both Windows and Android app builds finish with zero
warnings or errors. The PPO tooling's final source revision passes 28 Python
tests.

The evidence supports a small PPO gain in the all-trump policy. It does not show
that larger helpers help, nor that private inputs beat a matched public helper.
The longer GAE schedule was only run with the private helper, so that attribution
remains untested.

## Sources

- [PPO](https://arxiv.org/pdf/1707.06347), equations 7, 9-12 and algorithm 1: clipped
  probability ratios, entropy and advantage estimation.
- [GAE](https://arxiv.org/html/1506.02438v6): bias/variance tradeoff; lambda 1 here
  retains the full sampled terminal return rather than bootstrapping early.
- [PerfectDou](https://arxiv.org/html/2203.16406): privileged training critic
  alongside a policy restricted to its own information.

## Reproduction

Use the CUDA environment in tools/NeuralTrainer/Gpu/README.md (the experiment used
Python 3.12.3, PyTorch 2.9.1+cu126, NumPy 2.2.6 and an RTX 2080 SUPER). Recover
the fixed source weights from the pre-PPO commit rather than starting a reproduction
from whichever weights are currently embedded. Build to a copied output directory
so long collection jobs do not lock the trainer's normal bin:

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null
git archive --format=zip --output=artifacts/ppo-original.zip e89185b src/AI/Belot.AI.ClaudePlayer/Neural/Weights
Expand-Archive -LiteralPath artifacts/ppo-original.zip -DestinationPath artifacts/ppo-original
$original = 'artifacts/ppo-original/src/AI/Belot.AI.ClaudePlayer/Neural/Weights'
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/ppo-bin
artifacts/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in $original --out artifacts/ppo-private --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 32 --deals 8192 --threads 12 --seed 4401
# Repeat with --critic public and a separate --out directory.
```

The selected run first trained 64 updates with GAE lambda .5, then resumed for
a total of 128 updates. Only its all-trump actor is kept:

```powershell
artifacts/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in $original --out artifacts/ppo-gae64 --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 64 --deals 8192 --threads 12 --seed 4401 --gae-lambda .5
artifacts/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in $original --out artifacts/ppo-gae128 --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 128 --deals 8192 --threads 12 --seed 4401 --gae-lambda .5 --save-every 16 --resume artifacts/ppo-gae64/iteration-0076/training.pt
New-Item -ItemType Directory artifacts/ppo-selected | Out-Null
Copy-Item -Path "$original/*.bin" -Destination artifacts/ppo-selected
Copy-Item -LiteralPath artifacts/ppo-gae128/iteration-0140/alltrumps.bin -Destination artifacts/ppo-selected/alltrumps.bin
dotnet artifacts/ppo-bin/NeuralTrainer.dll validate --in artifacts/ppo-selected --opponent $original --pairs 20000 --threads 8 --seed 557
dotnet artifacts/ppo-bin/NeuralTrainer.dll validate --in artifacts/ppo-selected --opponent smart --pairs 10000 --threads 8 --seed 557
dotnet artifacts/ppo-bin/NeuralTrainer.dll validate --in artifacts/ppo-selected --opponent ismcts:100 --pairs 1000 --threads 10 --seed 563
dotnet artifacts/ppo-bin/NeuralTrainer.dll validate --in artifacts/ppo-selected --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --opponent $original --opponent-endgame true --pairs 10000 --threads 12 --seed 557
dotnet artifacts/ppo-bin/NeuralTrainer.dll validate --in artifacts/ppo-selected --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90 --opponent ismcts:100 --pairs 500 --threads 10 --seed 569
dotnet artifacts/ppo-bin/NeuralTrainer.dll bench --in artifacts/ppo-selected
dotnet artifacts/ppo-bin/NeuralTrainer.dll bench --in artifacts/ppo-selected --endgame true --endgame-declarations true --endgame-tricks 3 --endgame-worlds 90
```

Run timed opponents and benchmarks while training/build jobs are stopped. Games
against timed search are not bitwise reproducible: scheduling affects how many
iterations each decision completes.

Progress is append-only JSONL. Each saved checkpoint contains managed actor
weights and a separate training.pt with actors, critics, optimizers and RNG
states. Resume with --resume path/to/training.pt and matching collection settings.
Batch files are reused; saved snapshot states and seeds reproduce them.
New run manifests (`run-NNNN.json`, where NNNN is the resumed iteration) record
Python/PyTorch/CUDA versions, device, source-weight hashes, trainer DLL hash and
Python source hashes. New checkpoints refuse source weights changed in place.
Earlier pilot checkpoints predate this hash check and require unchanged source
files when resumed. Their frozen baseline hashes are recorded in the experiment
artifacts and the preceding research results.
