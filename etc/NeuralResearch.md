# Neural Belot research review, September 27, 2026

## Scope and decision

This is a task-focused review, not a claim to have read every important paper. It covers
learning from search, hidden-card inference, partnership learning, privileged training,
network design, and alternatives to Monte Carlo value regression. For the core papers
below, the methods and relevant experiments/ablations were inspected. The background
and discovery-only sources are identified separately. Results in other games are leads
for experiments; they do not establish strength in Bulgarian Belot.

The first substantial experiment was **distillation of our measured stronger 100-world
search**, with bidding frozen. It tested whether better targets alone improve the fast
player, comparing ordinary and centred action-value losses on the same data.
Measure decision regret under the teacher as well as actual mirrored matches. Low
training error with poor generalisation calls for more varied data or better inputs;
high training and validation error may justify more capacity. Neither diagnostic
replaces the ISMCTS promotion gate.

The strongest architectural leads are **ordered public history and an auxiliary hidden-card
prediction objective**. The current encoder retains which cards each seat played, but
discards their order after the trick. That can erase evidence about leads, discards and
partner signals. A compact history encoder can be an MLP: Belot has only 32 plays, so a
transformer is not required to preserve this information. True hidden cards may be
training labels or critic inputs, never inputs to the deployed seat policy.

## Core evidence

### Learning from search and reducing target variance

| Source | Evidence inspected | Consequence for this engine |
|---|---|---|
| [Expert Iteration, Anthony et al., 2017](https://arxiv.org/abs/1705.08439) | Search produces improved decisions; a network generalises them and supports subsequent search. | A direct fit to our already stronger search teacher is the first experiment. The original result does not supply imperfect-information or partnership guarantees. |
| [DouZero, Zha et al., 2021](https://proceedings.mlr.press/v139/zha21a.html) | Deep Monte Carlo learning, action representations and large-scale self-play achieve strong DouDizhu performance. Its history LSTM feeds a six-layer, 512-wide MLP. | Supports Monte Carlo training as a credible baseline. Belot has at most eight legal cards, so evaluating a separate action-conditioned network for each card would waste our latency budget. Preserve one forward pass for all 32 outputs. |
| [PerfectDou, Yang et al., 2022; revised 2024](https://arxiv.org/html/2203.16406) | Sections 3-4 use PPO/GAE with a privileged value model and an information-limited policy. The system also changes action encoding and adds an oracle-derived dense reward. It beats the public DouZero model at 54.3% on 10,000 decks in its tournament. | A credible alternative is an asymmetric actor-critic. Do not attribute its entire gain to a critic or copy its shedding-game reward into Belot. Our true-deal all-action rollouts already use training-only outcomes, but are not the same algorithm. |
| [ScrofaZero, Shi et al., 2021](https://arxiv.org/html/2102.07495) | Gongzhu training uses perfect information, then sampled hidden hands at play time. It studies stratified sampling, policy-based inference and search budgets. Appendix 8.7 reports that more search during training can degrade performance. | Supports testing sample quality and variance, not simply increasing search count. A perfect-information network with guessed hands is a search agent, not a directly deployable information-set policy. Distillation would still be needed for our fast player. |
| [Dueling networks, Wang et al., 2016](https://proceedings.mlr.press/v48/wangf16.html) | Separates state value and action advantages within the network. | Motivates our centred **loss** experiment, which is not a dueling architecture. Removing a common error before Huber clipping may expose small action differences; it must earn its place in matches. |
| [Policy Distillation, Rusu et al., 2016](https://arxiv.org/html/1511.06295v2) | Sections 3.2 and 4.2 compare value regression, best-action classification and temperature-based KL. Regression error improves while playing strength can fall; KL performs best in their four-game comparison. | Directly relevant to our first distillation failures. Test a masked action-preference objective, preserving a separate mean-value term for point-valued hints. Our variant applies the same temperature to both networks to preserve output scale; their Atari result is not a Belot guarantee. |

### History, beliefs, and cooperation

| Source | Evidence inspected | Consequence for this engine |
|---|---|---|
| [Skat inference, Solinas et al., 2019](https://arxiv.org/html/1903.09604) | Sections 3-5 predict each card's location from bidding and play. A separate MLP compresses the ordered 24-play history to 32 features. Likelihood-weighted sampling improves suit and null play, but not grand, in 2,500 mirrored matches per matchup. | Strong direct support for retaining order and learning card locations. Independent card probabilities are only an approximation to a joint legal deal distribution; preserve hand-size and rule constraints. The reported inference-enhanced search is slower, so an auxiliary head discarded on export is attractive. |
| [DouZero+, Zhao et al., 2022](https://arxiv.org/html/2204.02558) | An LSTM/MLP predicts the next player's cards, supervised by the true hand; predictions feed the decision model. A separate coach selects more balanced starting deals. Ablations report larger benefits for the collaborating peasants. | Test card-location supervision and opponent diversity. Balanced-deal filtering changes the training distribution: Belot also rewards limiting losses in bad deals, so do not discard those deals wholesale. |
| [Learned Belief Search, Hu et al., 2021](https://arxiv.org/html/2106.09086) | Autoregressive beliefs replace expensive exact inference. A public-history RNN can be reused across sampled private hands; truncated rollouts bootstrap a value. Section 4 warns that beliefs trained for a fixed blueprint can become inaccurate when multiple players independently search. | Useful for a later search Master or teacher. Train beliefs against the policies actually encountered and evaluate under opponent shifts. A cached recurrent state must still be reconstructible from `BelotSeatView`. Its Hanabi results do not guarantee improvement in adversarial Belot. |
| [Joint Policy Search, Tian et al., 2020](https://arxiv.org/html/2008.06495) | Jointly changes a player and partner's decisions; Bridge experiments use search on a small fraction of trajectories and keep ordinary self-play data. JPS improves an A2C baseline; its explicit-belief ablation has little effect on Bridge bidding. | Single-action improvement with a fixed rollout partner can plateau at poor conventions. Partner-aware training and mixed trajectories are worthwhile alternatives if simple distillation saturates. Bidding results are not card-play results, and the tabular guarantee is not a Belot guarantee. |
| [A Simple, Solid, and Reproducible Baseline for Bridge Bidding AI, Kita et al., 2024](https://arxiv.org/html/2406.10306v1) | Supervised pretraining plus PPO and a pool of old opponents; four 1024-wide MLP layers. Reports +1.24 +/- 0.19 IMPs/board against WBridge5 on 1,000 games. Removing pretraining is substantially worse; a pool alone does not rescue training from scratch. | Supports keeping our useful warm start and using varied opponents. Does not justify replacing our bidder: the measured weakness here is card play. Its double-dummy bidding reward also abstracts away actual card play. |

### Architecture and alternative training algorithms

| Source | Evidence inspected | Consequence for this engine |
|---|---|---|
| [DouRN, Chen et al., 2024](https://arxiv.org/html/2403.14102) | Added residual depth improves results against DouZero, reaching roughly 57% in the deepest experiment after 462 hours. Simply replacing layers with residual blocks is near a tie. | Depth is an experiment, not an automatic upgrade. Compare under equal data and time budgets, and measure managed C# latency before a long run. |
| [Net2Net, Chen et al., ICLR 2016](https://arxiv.org/html/1511.05641) | Sections 2.4 and 3.3 insert identity ReLU layers, preserving the initial function and accelerating deeper-model training on ImageNet. | Tests extra depth while retaining the warm start; our late-game teacher experiment adds two 128-unit layers. Initial equivalence is verified. After fitting, it scores only 50.4% +/- .2 pp against the original over 20,000 games, which is not a useful improvement. |
| [ScrofaZero architecture ablation](https://arxiv.org/html/2102.07495) | Its 24-layer fully connected network outperforms the 16-layer variant, while ResNet-18 is weaker. These models have roughly 9-11 million parameters. | Evidence is mixed on architecture family and outside our few-MB budget. Compact MLP/history designs deserve a fair test before a large residual or attention network. |
| [Reevaluating Policy Gradient Methods, Rudolph et al., ICLR 2026](https://www.mit.edu/~gfarina/2026/iclr26_reevaluating/iclr26_reevaluating.pdf) | Compares seven algorithms on five games, with more than 7,000 runs and exact exploitability. Tuned PPO/PPG/MMD compete strongly with specialised methods. Entropy choices matter greatly; the authors explicitly limit their conclusions to their benchmarks and tuning conditions. | PPO with a training-only privileged critic is a serious replacement option. Do not dismiss it based on older weak PPO baselines, or import its entropy coefficient without accounting for reward scale. |
| [ReBeL, Brown et al., 2020](https://arxiv.org/html/2007.13544) | Learns values over public beliefs and solves subgames with CFR or fictitious play. Section 4 explains why imperfect-information leaf values depend on the strategy/beliefs. Its theoretical and empirical results are for two-player zero-sum games. | A single ordinary state value plugged into PUCT is not a principled substitute. Belot's two partners have separate private information; treating the team as one player with both hands would cheat. A full ReBeL conversion is a large research project, not the next latency-conscious experiment. |
| [Student of Games, Schmid et al., 2023](https://arxiv.org/html/2112.03178) | Growing-tree CFR, safe re-solving, and counterfactual value/policy learning. Search queries as well as whole trajectories generate training targets. The paper focuses on two-player zero-sum games. | Reinforces the importance of consistent information-set search. It is not evidence that adding AlphaZero-style PUCT to the current neural Q-values will solve the partnership problem. |

## Background and implementation leads

- [Information Set MCTS, Cowling et al., 2012](https://eprints.whiterose.ac.uk/id/eprint/75048/1/CowlingPowleyWhitehouse2012.pdf):
  reviewed the determinization discussion and multiple-observer algorithm. Separate
  trees share statistics over observations; this remains a meaningful independent opponent.
- [Understanding PIMC, Long et al., 2010](https://www.cs.du.edu/~sturtevant/papers/pimc.pdf):
  inspected strategy fusion, non-locality and the game-tree properties that make sampling
  effective. Double-dummy labels can be useful but need not be correct imperfect-information
  action values. Our neural rollouts use each seat's own features rather than an oracle.
- [Deep CFR, Brown et al., 2019](https://proceedings.mlr.press/v97/brown19b/brown19b.pdf):
  inspected the external-sampling and regret-network formulation. [NFSP, Heinrich and
  Silver, 2016](https://arxiv.org/abs/1603.01121) was screened at abstract/background level.
  Neither is implemented here. Equilibrium learning remains relevant to robustness, but
  those poker results do not settle the best method for decentralised partnership play.
- [SWA, Izmailov et al., 2018](https://arxiv.org/abs/1803.05407): screened as a cheap
  checkpoint-averaging lead. Our three-checkpoint average was a measured tie; it was not
  a reproduction of the SWA schedule.
- [Colver's public implementation](https://github.com/Avo-k/colver): a related French
  Belote Contree project, not a peer-reviewed Bulgarian Belot result. Its README describes
  suit canonicalisation, a residual dueling model, and learned world generation. Its
  reported sub-millisecond player and multi-million-parameter world model do not meet
  our 50-microsecond constraint. The code is a design lead, not benchmark evidence here.
- [DTCard thesis, 2026](https://open.metu.edu.tr/handle/11511/119482): discovered a
  transformer study of Hearts, Whist and Spades. Only its repository abstract was
  accessible in this review; it is **not** evidence for choosing a transformer here.

## Experiments implied by the review

1. Freeze the reference and verify whole-game evaluation and warmed timing. Done; see
   [the results record](../NEURAL_NETWORK.md#12-september-27-improvement-experiments).
2. Compare ordinary versus centred loss at equal continuation time. A small lineage gain
   is only a screening result; check ISMCTS before committing a long run.
3. Collect all legal card values from 100-world search. Fit the same data with both losses,
   measure teacher regret and independent whole-game strength, and repeat only if it helps.
4. Test suit permutation augmentation, preserving the trump slot and mapping every affected
   feature and output. This is a local data-efficiency hypothesis, not a claim from a paper.
   Auction suit order is not a symmetry: do not manufacture illegal permuted bid histories.
5. If teacher fitting shows a representation bottleneck, retain ordered public history and
   test auxiliary card-location supervision. Bump the feature version and verify encoder
   parity before retraining. Compare a compact history MLP with additional residual capacity
   at the same runtime budget. A transformer remains an option if those fail to fit history.
6. If stronger labels and representations plateau, test PPO with a privileged training critic,
   mixed opponents and partner-aware improvement. Retain a calibrated Q head for app hints
   and difficulty settings; policy logits alone cannot replace point-valued `EvaluateCards`.

Search teacher strength, distilled student strength, and actual ISMCTS results must remain
separate measurements. There is no theoretical guarantee that our approximate search,
teacher distribution, or finite-capacity student improves at every iteration.

## Measured conclusions

The [results record](../NEURAL_NETWORK.md#12-september-27-improvement-experiments)
contains the counts, uncertainty and commands. Search-100 remains a stronger teacher,
but its tested students regress or tie: ordinary and centred Q losses, policy KL,
mean anchoring, suit augmentation, wider networks, frozen residual corrections,
shared card heads and student trajectories have not produced a promotable network.
Filtering small teacher advantages mostly prevents regression without establishing
an independent ISMCTS gain. Lower prediction error has repeatedly failed to imply
better play.

Ownership supervision at weights .01 and .1 and public-order inputs were implemented
and tested. The four-epoch Q-only history fit ties; the stronger auxiliary objective
regresses. These are short pilot results, not evidence against history or learned
beliefs in general. A longer online run or explicit belief-conditioned policy would
test different hypotheses. At this checkpoint, PPO with a privileged critic had
not yet been implemented; the subsequent experiment is in
[PPO_EXPERIMENT.md](../PPO_EXPERIMENT.md).

A compact alternative from the PIMC literature has completed its first independent evaluation:
solve the final two tricks, and three-trick positions with at most eight publicly
consistent worlds. It gains about three percentage points against the frozen network
at 25 us/card, retaining the original weights. Against ISMCTS it scored 52.4% +/-
1.4 pp over 1,000 games, so the 95% interval includes 50% and promotion is refused.
Its declaration model assumes the bots'
policy of declaring every combination. Its future actions know each sampled world,
so this is approximate imperfect-information search.

Increasing the three-trick cap to 90 worlds produces a stronger fast candidate:
54.65% +/- .168 pp against the frozen network and 89.74% +/- .206 pp against SmartPlayer,
each over 20,000 games. Earlier idle timing varied between runs, including
one 54.9-us result. Its independent 2,000-game ISMCTS match
scored 53.45% +/- .942 pp, with a 95% interval of [51.605%, 55.295%], passing the
predeclared promotion gate. The integrated build measured 31.1 us/card.

The inexpensive endgame teacher supplied 2.13 million positions on student
trajectories, with independent validation games. Twelve-epoch fits at two learning
rates and with extra identity-initialised depth scored only 50.3-50.4% against the
frozen network over 20,000 games per student (standard errors .2 pp). These networks
remain unpromoted. The measured improvement is in the bounded search configuration;
we have not established an improvement in the neural weights.

One remaining input question is declaration detail. `FeatureEncoder` records flags
for declared types; `NeuralDeal.DeclarationBit` also groups sequences of five or
more cards. The endgame teacher uses exact sequence lengths and multiplicities.
Some publicly different states therefore share the neural encoding. Preserving
those details is a concrete future feature experiment, requiring a new layout
version and parity checks. This observation does not establish why the tested
students failed or predict the size of a playing gain.

## September 28-29 follow-up

The later [bounded-search experiment](../FAST_BOT_EXPERIMENT.md) tests the belief
models directly in play: separate compact ownership networks weight complete
legal hands, and an exact constrained sampler feeds bounded five-trick endings.
That combination produces a substantial independently measured gain while
retaining the actor weights. Longer public-history features, joint ownership
loss, mixed-opponent ownership data and a four-component mixture did not justify
replacing the selected model in their controlled pilots. This distinguishes the
successful use of predicted ownership in search from the earlier unsuccessful
auxiliary loss on the actor.

Auction-preserving suit averaging also improves the frozen policy in whole-game
comparisons. A controlled attempt to compress that teacher into one ordinary
network improved imitation metrics but tied the frozen baseline. The current
configuration, independent opponent results, timing, promotion decisions and app
calibration are maintained in [NEURAL_NETWORK.md](../NEURAL_NETWORK.md#16-faster-master-with-learned-ownership-september-28-29-2026).
