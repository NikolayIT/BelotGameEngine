# PPO with a training-only critic

## Fixed design and first comparison

Starting point: master e89185b, pushed to origin before this experiment. Runtime
networks, 600 policy inputs, bidding weights and BNN1 exports are unchanged.
All evaluation uses greedy card choices with both search options disabled.

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
not strength. Initial tests: 97 C# tests and seven new Python PPO tests pass.
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

The public endpoint has a small positive result against the original, but neither
run has passed the required ISMCTS gate. No weights or app levels are promoted.
Training took 307.9 seconds for the private run and 721.3 seconds for the public
run, excluding Python startup and the final evaluation. The latter overlapped
other tests/evaluations, so these times are not a controlled throughput comparison.

## Follow-up experiments

The initial actor LR produces KL changes around .00003-.0002 per update, well
below the .01 stop, and changes roughly 1% of greedy decisions on each new batch.
The next fixed comparison raises only actor LR to 1e-5, resuming the privileged
warmup checkpoint (iteration 12), then runs the same 32 PPO updates/seeds.
The clipping and KL stop remain enabled. A separate temperature-.25 run will test
less exploration while keeping the original 1e-6 actor LR. Its critic must warm
up again because the sampled policy changes. These are hypotheses, not promotions.

The LR-1e-5 endpoint scored **50.090% +/- .230 pp in 20,000 games**, 95% interval
[49.640%, 50.540%], +.1 point/game (seed 523). Larger changes did not improve
strength in this budget. A third follow-up keeps temperature 1 and LR 1e-6, changes
only GAE lambda to .5, and runs 12 warmup plus 64 PPO batches. It evaluates after
32 updates for comparison, then after 64. This tests whether bootstrapping from
the helper reduces terminal-return noise enough to outweigh value-estimation bias.

The temperature-.25 endpoint scored **50.265% +/- .171 pp in 20,000 games**,
95% interval [49.929%, 50.601%], +.7 point/game (seed 523). It does not establish
an improvement. These seed-523 comparisons now also inform experiment selection;
a final candidate must receive a new seed for confirmation.

The lambda-.5 checkpoint after 32 updates scored **50.465% +/- .208 pp in
20,000 games**, 95% interval [50.057%, 50.873%], +.4 point/game (seed 523).
After 64 updates it scored **50.450% +/- .218 pp in 20,000 games**, 95% interval
[50.023%, 50.877%], +.6 point/game (seed 523). This small gain against the original
does not establish a gain against ISMCTS.

A separate capacity test will use a 696-512-256-128-1 privileged helper, while
retaining the original actor, temperature 1, LR 1e-6 and lambda 1. It will train
12 warmup plus 64 PPO batches, with an evaluation at 32 updates for the original
budget comparison. This tests the helper's capacity without increasing deployed
weights or inference work. `--critic-sizes` configures only this discarded helper;
resume refuses an architecture mismatch.

Contract checks from the GAE-64 endpoint replace only one card file at a time,
retaining the other original networks. On 20,000 games each, seed 523: all trumps
50.610% +/- .179 pp; no trumps 49.945% +/- .129 pp; suits 49.515% +/- .136 pp.
The all-trump variant's fresh seed-547 check gave 50.370% +/- .187 pp in 20,000
games, 95% [50.003%, 50.737%]. This small positive signal motivates a fixed
continuation of GAE from 64 to 128 updates and another all-trump-only export.
That continuation will first use seed 523 for comparison, then a new confirmation
seed after candidate selection. It does not authorize promotion without ISMCTS.

## Sources

- [PPO](https://arxiv.org/pdf/1707.06347), equations 7, 9-12 and algorithm 1: clipped
  probability ratios, entropy and advantage estimation.
- [GAE](https://arxiv.org/html/1506.02438v6): bias/variance tradeoff; lambda 1 here
  retains the full sampled terminal return rather than bootstrapping early.
- [PerfectDou](https://arxiv.org/html/2203.16406): privileged training critic
  alongside a policy restricted to its own information.

## Reproduction

Use the CUDA environment in tools/NeuralTrainer/Gpu/README.md. Build to a copied
output directory so long collection jobs do not lock the trainer's normal bin:

```powershell
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/ppo-bin
artifacts/torch-env/Scripts/python.exe -X utf8 tools/NeuralTrainer/Gpu/ppo.py --in src/AI/Belot.AI.ClaudePlayer/Neural/Weights --out artifacts/ppo-private --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 32 --deals 8192 --threads 12 --seed 4401
# Repeat with --critic public and a separate --out directory.
```

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
