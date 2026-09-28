# Four-component ownership belief pilot

Date: 2026-09-29. Status: offline experiment complete; no runtime or app integration.

## Motivation and research

The existing ownership model assigns a score to each card/owner pair, then the
sampler conditions the product of those scores on public exclusions and exact
remaining hand sizes. That conditioning already creates dependence between
cards. A mixture can additionally represent several distinct explanations of
the same observed play, such as two different plausible combinations of cards.

The proposal follows the conditional-mixture construction in
[Bishop, *Mixture Density Networks* (1994)](https://www.microsoft.com/en-us/research/publication/mixture-density-networks/).
That paper motivates representing multimodal conditional distributions; it does
not establish that four components help Belot.

Learned card-location estimates improved Skat PIMC in
[Solinas, Rebstock and Buro, *Improving Search with Supervised Learning in Trick-Based Card Games* (2019)](https://arxiv.org/abs/1903.09604).
Their result motivates improving the distribution of sampled worlds as a
separate component of search. Our exact capacity-conditioned likelihood is a
different training objective.

[Rebstock et al., *Policy Based Inference in Trick-Taking Card Games* (2019)](https://arxiv.org/abs/1905.10911)
addresses limitations of independent card-location estimates using player
models and joint inference. It motivates checking correlated holdings, while
also warning that inference depends on how accurately opponents are modeled.

[Hu et al., *Learned Belief Search* (2021)](https://arxiv.org/abs/2106.09086)
uses supervised autoregressive beliefs to support search. Its Hanabi results
support learned joint beliefs as an approach; they do not transfer numerically
to Belot. Its discussion of beliefs trained under a fixed policy is relevant:
belief quality may change when search or different opponents produce the play
history. The present four-component head is a cheaper architecture to test.

An illustrative limitation of a single conditioned product: give two opponents
two cards each from cards 1–4. Let `w12` mean the unnormalized weight when the
first opponent holds cards 1 and 2. A product model satisfies
`w12*w34 = w13*w24`. It therefore cannot simultaneously make worlds 12 and 34
dominate both cross worlds 13 and 24. Two components can express that pattern.
This is an algebraic example, not a measured frequency in Belot data.

## Distribution and loss

Let `C0` contain the public card exclusions, public known cards and remaining
hand sizes reconstructed from the same 600 actor features. The true owner
labels are training targets only. They are checked against `C0` before fitting.

The offline joint NLL conditions on these encoded `C0` masks and capacities.
It excludes extra runtime plain-carre deductions and rejection using full
declaration multiplicities and ranks. That rejection assumes the bots declare
all available combinations; an opponent withholding a declaration remains a
model limitation. Consequently this metric is not the likelihood under the
complete deployed declaration-conditioned distribution.

For component `k`, a feasible complete assignment `w` has score
`s_k(w) = sum_c logits[k,c,owner(w,c)]`. Own and already-played cards are omitted.
The exact dynamic program gives
`Z_k(C0) = sum_(w allowed by C0) exp(s_k(w))`.

Each component is normalized separately:

```text
log p_k(w | x,C0) = s_k(w) - log Z_k(C0)
log pi_k = log_softmax(gate_logits)_k
log p(w | x,C0) = logsumexp_k(log pi_k + log p_k(w | x,C0))
loss = -log p(true_world | x,C0)
```

Training divides the complete-world mixture loss by that state's active-card
count, then averages over states, matching the K1 joint control. Dividing each
component's loss before `logsumexp` would define a different, incorrect mixture.
Evaluation reports whole-state NLL and total NLL divided by total active cards.

The model is `600 -> 128 -> 64 -> 388`, with ReLU hidden layers. Its first 384
outputs are four consecutive 32-card × 3-relative-owner logit tables. The final
four outputs are gate logits. Three independent models cover suit, no-trump and
all-trump contracts. Actor features, policy networks and feature layout1 stay
unchanged.

## Initialization and controlled comparison

Start from the same half-precision CE12 models as the completed K1 joint4
control. Copy the trunk and clone the 96 output rows into all four components.
Gate weights and biases start at zero, giving equal mixture weights.

Add seeded normal noise to the component/card/owner biases, centered along all
three axes and scaled to population RMS **0.05 logits**. Per-card constants and
per-owner offsets cancel under fixed capacities, so the centering removes
perturbations that cannot distinguish component distributions. The component
mean remains the original head. Seeds are **19844, 19845 and 19846**.

The comparison uses identical training/heldout files, row order in every epoch,
batch size 2,048, Adam, gradient clipping at 1, four epochs, and seed 9833.
Learning rates are 1e-4 for epochs 1–2 and 3e-5 for epochs 3–4. The runner checks
all source hashes, per-epoch row-order hashes and learning rates against the K1
control report. All completed epochs are retained. The predefined final result
is epoch4; heldout results do not choose a checkpoint.

Training contains 1,884,525 positions; heldout contains 94,603. These are the
existing frozen neural self-play records. Multiple positions from a deal are
correlated. The experiment reports likelihood differences without treating
positions as independent samples or claiming a game win-rate improvement.

## Reproduction

From the repository root, using the existing CUDA PyTorch environment:

```powershell
& artifacts/neural-20260927/torch-env/Scripts/python.exe tools/NeuralTrainer/Gpu/fit_mixture_ownership.py `
  --input artifacts/fast-bot-20260928/ownership-ce12-seed9821 `
  --data artifacts/neural-20260927/belief-selfplay/data `
  --validation-data artifacts/neural-20260927/belief-validation/data `
  --control-report artifacts/fast-bot-20260928/ownership-joint4-lr1e4-b2048-seed9833/report.json `
  --out artifacts/fast-bot-20260928/ownership-mixture4-joint4-lr1e4-b2048-seed9833 `
  --epochs 4 --batch 2048 --learning-rate 0.0001 --seed 9833 --perturbation 0.05 `
  --cache-device --cache-reserve-mib 1536 --device cuda
```

The runner opts out of Windows power throttling. It uses deterministic PyTorch
operations, two CPU threads, and caches the current contract's tensors on the
GPU when the configured memory reserve is available. It does not overwrite the
warm start or control. `report.json` records arguments, source and export hashes,
row orders, epoch times, and actual-half metrics after reloading every export.

Exports use the checked BNN1 format, version1, layout1, and new tags **14/15/16**.
Each file is exactly **220,844 bytes**, three total **662,532 bytes**, an increase
of 113,880 bytes over the K1 ownership triplet. The existing ownership runtime
rejects these shapes/tags; no candidate can load them accidentally.

CPU checks:

```powershell
& artifacts/neural-20260927/torch-env/Scripts/python.exe -m unittest discover `
  -s tools/NeuralTrainer/Gpu -p test_mixture_ownership.py -v
```

All **10 tests passed** in **1.790 seconds** on 2026-09-29. They cover independent
assignment enumeration, K1 equivalence, cloned-component collapse, component
permutations, card/owner gauge invariance, finite-difference logit/gate gradients,
unique/empty worlds, initialization, strict header/length/finite-parameter
refusal, and a complete tiny matched run with a partial final batch. The latter
checks exported-half metrics and unchanged epoch order against its K1 control.

## Runtime requirements if likelihood supports a pilot

This section is a design constraint, not an implemented runtime path.

Runtime deductions can be stricter than `C0`. Let `C1` be the intersection of
`C0` and runtime masks, including additional known cards. Correct conditioning
updates the gates by the component evidence:

```text
log pi'_k = log pi_k + log Z_k(C1) - log Z_k(C0) - common_normalizer
```

Both partitions must score the same full unseen-card pool, including factors
for newly fixed cards. An implementation using `WeightedWorldSampler` can
represent all known cards as forced-owner masks and pass zero known-card masks
to its raw configure method. This prevents its usual known-card peeling from
dropping evidence factors. Keep gates in log space until the evidence update;
premature float softmax can erase a rare component rescued by new evidence.
Never expand `C0` merely because runtime cleanup relaxed a contradictory mask.

Sampling chooses a component from the conditioned gates, then a constrained
world from that component. Declaration rejection must resample **both** the
component and world on every attempt. Retrying worlds inside one fixed
component would preserve incorrect pre-declaration gate weights. Accepted
sampled worlds receive no second ownership weight.

Exact enumeration evaluates the mixture probability of each valid world using
the separately normalized components. It must include all unseen-card factors
and normalize weighted scores over accepted worlds. Adding raw component
products would give arbitrary component scales unintended influence.

One reusable `C0` scratch DP plus four `C1` samplers needs approximately 100 KB
of mutable sampler storage per player with current arrays. Only the selected
component generates each sampled world, so the number of double-dummy solves
need not increase. The final neural layer grows from 64×96 to 64×388. Desktop
latency and actual allocation cost remain unmeasured.

Before runtime use, add managed/Python half-forward parity and independent
enumeration tests for component probabilities, newly fixed-card evidence,
declaration-conditioned gate changes, sampling frequencies, exact/sampled
agreement, and hidden-hand independence. Promotion still requires game tests
against the current player and independent opponents under the speed limit.

## Offline results

Before training, the runtime follow-up criterion was set to an aggregate
late-phase NLL reduction of approximately **0.03 nats/state** against the fixed
K1 joint4 control, with no large contract regression and noncollapsed
components. This is a resource-priority threshold; playing strength remains the
final test. Smaller likelihood changes alone do not justify the added runtime
machinery.

The fixed epoch4 result improves heldout likelihood in every contract, but its
aggregate late-phase improvement is **0.011373 nats/state (0.204%)**, below the
predefined 0.03 threshold. Retain the experiment and defer runtime integration.
No K4 playing-strength or managed-runtime latency claim is supported by this
offline result.

All values below are joint NLL in nats/state, computed by reloading the actual
half exports. Positive reduction means K4 is better. Aggregate rows weight each
contract by its number of heldout states.

| Contract | All states | K1 all | K4 all | Reduction | Late states | K1 late | K4 late | Reduction |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Suit | 31,782 | 10.457772 | 10.446532 | 0.011240 | 17,986 | 5.542610 | 5.531132 | 0.011477 |
| No trumps | 16,101 | 10.569268 | 10.555547 | 0.013720 | 9,316 | 6.056716 | 6.038768 | 0.017947 |
| All trumps | 46,720 | 9.723165 | 9.713826 | 0.009338 | 27,963 | 5.448583 | 5.439467 | 0.009116 |
| Aggregate | 94,603 | 10.113960 | 10.103237 | 0.010723 | 55,265 | 5.581697 | 5.570324 | 0.011373 |

Late means at least three completed tricks. The aggregate overall reduction is
0.106%. State counts describe this heldout dataset; correlated positions from
the same deal prevent interpreting them as independent samples for a simple
standard error.

Mean effective component counts are `mean(exp(entropy))` over states:

| Contract | Late gate components | Late posterior-responsibility components |
|---|---:|---:|
| Suit | 3.9957 | 3.5656 |
| No trumps | 3.9975 | 3.4324 |
| All trumps | 3.9971 | 3.5349 |
| Aggregate | 3.9967 | 3.5276 |

Gates remained spread across all four components, while posterior
responsibilities separated. This rules out exact cloned-component collapse in
this run. Similar gates alone would not establish diversity: cloned components
would also have four equal gates. The measurable distributional separation did
not produce a large enough likelihood change to prioritize runtime work.

The run finished successfully. The twelve epoch intervals total **257.87
seconds**, including each epoch's export and heldout evaluation. The first to
last training-log write spans **313.51 seconds**, including data loading and
initial/control evaluation between those writes. It used the RTX 2080 SUPER,
PyTorch 2.9.1+cu126, two CPU threads and concurrent fixed-work verification.
These are throughput observations, not an isolated latency benchmark.

Artifacts under
`artifacts/fast-bot-20260928/ownership-mixture4-joint4-lr1e4-b2048-seed9833/`:

- `report.json`: arguments, all input hashes, all epoch orders and actual-half metrics.
- `summary.json`: weighted aggregates and checks of all 12 epoch and three final export hashes, generated by `summarize.py`.
- `training.log`: complete training output.
- `initial/` and `epoch-001/` through `epoch-004/`: preserved checkpoints.

Final SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| `report.json` | `8196e3a83fdc803c399bb5b61eceff9aa60426911a676a64ab091152c80d979d` |
| `trump.bin` | `69f55e4edf025ec068183358a392bc4b109283fa1cbb8a419b16e1d8f0273599` |
| `notrumps.bin` | `a983f002054936f1dc21e082a120bad77456ed853ba553ed10ce2eea94482dc0` |
| `alltrumps.bin` | `a813d1ce34281f25e030fff5aa622e7071388eda150362ddea854c3a873d5f5c` |
