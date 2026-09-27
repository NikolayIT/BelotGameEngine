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
entropy .001, Q regression weight 1, KL stop .01, seed4401. Checkpoints every
four batches are screened in 4000 games versus the original networks (seed521).
Finalists get 20000-game independent screening and ISMCTS100 evaluation. Training
loss and helper accuracy alone never authorize promotion. No learning-rate or
input changes are mixed into the first helper comparison.

The smoke test completed four 512-deal batches. Maximum observed C#/CUDA
log-probability difference was 1.39e-5; chosen normalized Q differed by at most
1.08e-6. All exports still total 2,974,830 bytes. This verifies the pipeline,
not strength. Initial tests: 97 C# tests and six new Python PPO tests pass.

## Sources

- [PPO](https://arxiv.org/pdf/1707.06347), equations7,9-12 and algorithm1: clipped
  probability ratios, entropy and advantage estimation.
- [GAE](https://arxiv.org/html/1506.02438v6): bias/variance tradeoff; lambda1 here
  retains the full sampled terminal return rather than bootstrapping early.
- [PerfectDou](https://arxiv.org/html/2203.16406): privileged training critic
  alongside a policy restricted to its own information.

## Reproduction

Use the CUDA environment in tools/NeuralTrainer/Gpu/README.md. Build to a copied
output directory so long collection jobs do not lock the trainer's normal bin:

```powershell
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/ppo-bin
python tools/NeuralTrainer/Gpu/ppo.py --in src/AI/Belot.AI.ClaudePlayer/Neural/Weights --out artifacts/ppo-private --trainer artifacts/ppo-bin/NeuralTrainer.dll --critic privileged --warmup 12 --updates 32 --deals 8192 --threads 12 --seed 4401
# Repeat with --critic public and a separate --out directory.
```

Progress is append-only JSONL. Each saved checkpoint contains managed actor
weights and a separate training.pt with actors, critics, optimizers and RNG
states. Resume with --resume path/to/training.pt and matching collection settings.
Batch files are reused; saved snapshot states and seeds reproduce them.
