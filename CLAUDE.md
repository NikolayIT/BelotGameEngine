# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A C# engine for **Belot** (Bridge-Belote), a 4-player (2v2) 32-card trick-taking game. The
core engine ships as the `BelotGameEngine` NuGet package. The repository's real purpose is to
**evolve card-playing AIs and measure each change in ELO**. There are three: the hand-written
`SmartPlayer`, measured against its previously committed version (see "The ELO benchmark
workflow" below, the single most important thing to understand about it), the much stronger
search player `ClaudePlayerIsmcts` (see "ClaudePlayerIsmcts design"), and the neural player
`ClaudePlayerNeural`, trained by reinforcement learning in self-play (see "ClaudePlayerNeural
design" and `NEURAL_NETWORK.md`), measured in mirrored matches. People play them in the MAUI app
for Android and Windows (see "The MAUI app"). The full rules are in `etc/Rules.md`.

## Commands

The solution lives in `src/`. Library projects target `netstandard2.0` (except
`Belot.AI.ClaudePlayer`, which needs `net10.0` for `BitOperations` and `[InlineArray]`);
runnable/test projects target `net10.0`.

```bash
# Run the unit tests (xUnit)
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj

# Run a single test or class (xUnit filter on fully-qualified name)
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj --filter "FullyQualifiedName~ScoreManagerTests"

# Run the ELO benchmark / simulator — ALWAYS in Release, and it needs internet (see below)
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj

# The search player's tests (simulator vs engine, inference soundness, the bidding model)
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests/Belot.AI.ClaudePlayer.Tests.csproj

# ClaudePlayerIsmcts vs SmartPlayer, and two configurations of it against each other, in mirrored
# pairs of games: claude [pairs] [ms per card]; claude-ab [pairs] [ms] [candidate] [baseline]
# (options are listed in Program.cs, e.g. "c=0.3,margin=1"; "-" = the defaults). No internet needed.
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- claude 100 100
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- claude-ab 200 30 c=0.3 -

# ClaudePlayerNeural (the embedded networks, or a folder of them) against SmartPlayer and
# ClaudePlayerIsmcts: neural [pairs] [ms] [folder]; two sets of networks: neural-ab [pairs] [a] [b]
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- neural 500 100
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- neural-ab 5000 <folder> -

# The app's levels (and ISMCTS for reference) in a pair-vs-pair round robin, printing the ratings
# for Game/AiLevels.cs: elo [fast pairs] [pairs with the slow levels] (defaults 20000 and 150).
# The simulator writes UTF-16 to the console: pipe it through iconv -f UTF-16LE to grep it.
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- elo

# Train the neural player's networks (tools/NeuralTrainer, not in the sln; see NEURAL_NETWORK.md):
# distill | fit | train | validate | arena | bench, every setting as --name value
dotnet run -c Release --project tools/NeuralTrainer/NeuralTrainer.csproj -- validate --in <folder> --opponent ismcts:100
dotnet run -c Release --project tools/NeuralTrainer/NeuralTrainer.csproj -- arena --player fast --opponent belot206 --pairs 10000 --seed 611 --data artifacts/fast-206

# The MAUI app (needs the MAUI workloads): run it on Windows, or build it for Android
dotnet build src/UI/Belot.UI/Belot.UI.csproj -f net10.0-windows10.0.19041.0 -t:Run
dotnet build src/UI/Belot.UI/Belot.UI.csproj -f net10.0-android

# The app's game layer, playing whole games (no MAUI needed)
dotnet test src/Tests/Belot.UI.Tests/Belot.UI.Tests.csproj
```

Build projects individually with `dotnet build`, as CI (`.github/workflows/build.yml`) does:
building the whole `src/Belot.sln` includes the MAUI app, which needs the MAUI workloads (CI
builds and runs `Belot.UI.Tests`, not the app).

## The ELO benchmark workflow (read this before touching the AI)

The git history is unusual: **most commit messages are the simulator's output**, because every
meaningful change is judged by ELO delta, not by hand. `Belot.GamesSimulator` plays 200,000 games
for each of several matchups (SmartPlayer vs previous version / Dummy / Random) and prints ELO.

The headline matchup is `TwoSmartVsTwoPreviousVersionGames`: the current `SmartPlayer` plays
against `SmartPlayerPreviousVersion`, which **downloads the committed `master` version of
`SmartPlayer.cs` and its strategies from raw GitHub URLs and compiles them at runtime with Roslyn**
(`Microsoft.CodeAnalysis.CSharp`). Consequences:

- The "previous version" is whatever is on GitHub `master`, **not** your local working tree. The
  simulator therefore **requires network access**; offline runs fail to build that opponent.
- The loop for improving the AI is: edit `SmartPlayer` / its strategies → run the simulator in
  Release → confirm `TwoSmartVsTwoPreviousVersion` ELO is meaningfully positive (≈0 means no
  change) and the other matchups don't regress → commit, conventionally pasting the output as the
  message.
- The simulator sets `ProcessPriorityClass.RealTime`, warms up before timing, and runs at
  `Environment.ProcessorCount / 2` parallelism, so numbers are comparable run-to-run. Only Release
  numbers are meaningful.

## Engine architecture

A game can be driven two ways, both over the same rules:

- **Pull, with `IPlayer`s** (`Belot.Engine/Players/IPlayer.cs`): `BelotGame.PlayGame(firstToPlay)`
  asks the players through six callbacks (`GetBid`, `GetAnnounces`, `PlayCard` decide;
  `EndOfTrick`, `EndOfRound`, `EndOfGame` inform) until a team wins. This is what the simulator,
  and the bots' tests use. **To add an AI, implement `IPlayer`.**
- **Push, with `GameMechanics/BelotMatch`** (modelled on the Santase engine's `SantaseMatch`):
  `Start()`, then read `ToMove` and `Decision` (`Bid`, `Announce` or `PlayCard`), give that seat
  `CreateBidContext()` / `CreateAnnouncesContext()` / `CreatePlayCardContext()` (copies) and pass
  its answer to `Act(seat, BelotAction.Bid(...) / Declare(...) / PlayCard(...))`. The match plays
  forward (passes for a seat that can only pass, plays a forced card, finishes the trick, scores the
  deal, deals the next) up to the next decision and never waits for anybody, so a UI or a server
  holds no thread while a person thinks. `Act` returns `BelotActResult` (`Ok`, `InvalidAction`,
  `NotYourTurn`, `MatchFinished`) and changes nothing unless `Ok`; `Validate` answers the same
  without acting; `Stop()` ends a match early (no winner). Optional per-seat `IPlayer` observers get
  the informing callbacks in the usual order. **UIs should be built on this, not on `BelotGame`
  with blocking players.**
- **Views and records** (plain models, public setters, no engine internals): `GetView(seat)`
  returns a `BelotSeatView` with what that seat may see (its hand, its options, card counts, the
  auction, the tricks, the last trick, earlier deals' results); another seat's declared combination
  shows its type but not its cards until the deal is over (belotes show: their card was played in
  the open). `view.CreateBidContext()` and the others rebuild the exact contexts, so
  `player.Decide(view)` (`PlayerViewExtensions`) asks any bot that decides from its context (all of
  them here) with the view alone. `GetRecord()` / `GetFinalView()` (after the match or a stop)
  give every deal in full: the deck order, the auction, the declarations with their cards and
  whether they scored, the tricks and the results. They need `BelotMatchOptions.RecordHistory` (on
  by default; `BelotGame` turns it off).

Inside, one deal is a step-by-step state machine, `GameMechanics/Round` (internal): deal 5 each,
`Auction` (the bidding, until three passes after a bid or four without), deal 3 more,
`TrickPlay` (the 8 tricks: the combinations asked for in trick 1 and resolved before trick 2 by
`ValidAnnouncesService.UpdateActiveAnnounces`, belotes, trick winners, the cards each team wins)
and `ScoreManager.GetScore` (no-trumps doubling, last 10, capot +90, double/redouble, hanging
points, the **rounding** rules). `BelotMatch` strings deals together and applies the end-of-game
rule. `BelotGame`, `RoundManager.PlayRound`, `ContractManager.GetContract` and
`TricksManager.PlayTricks` are thin loops over these machines (`PlayerDriver` asks the `IPlayer`
and applies its answer; an illegal card or bid from a bot throws `BelotGameException`). On that
path the players get the live context objects (tests rely on it, and the simulator avoids the
copies); `BelotMatch` hands out copies. The rewrite onto the state machines was checked with a
harness that played 31,000 seeded games (SmartPlayers, ClaudePlayerIsmcts and a random bot that
bids, doubles, declares bogus combinations and claims belotes at random) before and after and
hashed every callback with its arguments: identical. Do the same for any change to the flow.

`new BelotGame(..., Random)` (and `BelotMatchOptions.Random`) seeds the deals: every deal shuffles
the whole deck once, so the n-th deal depends only on the seed and n, and the same seed with the
teams swapped replays the same deals (a mirror match).

Rule logic lives in stateless services under `GameMechanics/`: `ValidCardsService` (follow-suit /
must-trump / must-overtrump → returns the legal `CardCollection`; if only one card is legal the
engine plays it itself, without a belote), `TrickWinnerService`, `ValidAnnouncesService`.

Player callbacks receive context objects that all extend `BasePlayerContext`
(`PlayerGetBidContext`, `PlayerGetAnnouncesContext`, `PlayerPlayCardContext`) carrying the player's
hand, the bids, the current contract, the game score and the hanging points, and the trick/round
history.

## Performance is the primary design constraint

The simulator plays millions of rounds, so the hot path is allocation-averse and bit-twiddly.
Match this style when extending the engine:

- **`Card` is a flyweight.** There are exactly 32 immutable `Card` singletons in `Card.AllCards`,
  indexed by `hashCode = (int)suit * 8 + (int)type`. **Never `new Card`** — use
  `Card.GetCard(suit, type)`. Equality and `GetHashCode` are that index.
- **`CardCollection` is a 32-bit bitmask** (`uint cards`), one bit per card — this is the most
  important data structure in the codebase. It implements `ICollection<Card>` but adds
  allocation-free helpers used everywhere: `Where`, `Any`, `Highest`/`Lowest` (by a key selector),
  `GetCount`, and `HasAnyOfSuit` (precomputed per-suit masks). Prefer these over LINQ on the hot
  path.
- **Card strength is precomputed**, not derived from enum order: `Card.TrumpOrder` and
  `Card.NoTrumpOrder`. Comparisons (trick winner, valid cards) use these ints. `CardType`/`CardSuit`
  are `byte` enums in deck order (Seven=0 … Ace=7), which is *not* strength order.
- **Enums are `[Flags] byte`.** `BidType` (Clubs…AllTrumps, plus Double/ReDouble bits — check with
  `HasFlag`). `PlayerPosition` flags encode teams: `SouthNorthTeam = South | North`. Position math
  is in `PlayerPositionExtensions`: `.Next()`, `.Index()` (0–3 into the player/card arrays),
  `.IsInSameTeamWith()`, `.GetTeammate()`.
- Hot methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.

## SmartPlayer design

- **Bidding** (`SmartPlayer.GetBid`): heuristic point-counting per candidate contract
  (`CalculateTrumpBidPoints` / `CalculateAllTrumpsBidPoints` / `CalculateNoTrumpsBidPoints`); bids
  the highest-scoring option that clears a threshold of 100, otherwise `Pass`.
- **Card play** (`SmartPlayer.PlayCard`): delegates to one of **six `IPlayStrategy` objects**
  selected by *(contract category × ours/theirs)* — AllTrumps/NoTrumps/Trump × Ours/Theirs (e.g.
  `TrumpOursContractStrategy`). Each strategy implements `PlayFirst/Second/Third/Fourth` for the
  player's seat within the trick. Shared logic (e.g. "a card that surely wins this trick" by
  tracking already-played cards) lives in `Strategies/CardHelpers.cs`.
- `playedCards` is reconstructed on each `PlayCard` from `context.RoundActions` (cards from earlier
  tricks). When you add a new heuristic, this is the state you reason over.

Baseline opponents for the benchmark live in `AI/Belot.AI.DummyPlayer`: `DummyPlayer` (simple
rules) and `RandomPlayer`.

## ClaudePlayerIsmcts design (the search benchmark)

`AI/Belot.AI.ClaudePlayer`, a port of the design of the strongest player of the sister project
Santase (github.com/NikolayIT/SantaseGameEngine, `ClaudePlayerIsmcts`). Measured in mirrored pairs
at its 100 ms default (September 2026): two of it beat two SmartPlayers in 89.7% ± 1.7% of 300
games (+375 ELO, +64 points a game), and one of it with a SmartPlayer partner wins 76.0% ± 2.2%
(+200 ELO). For scale, the 2001 `belot.exe` AI beats SmartPlayer by ~+123 ELO.

- **Card play: single-observer ISMCTS.** One tree keyed by the public play; every iteration deals
  the unseen cards anew, walks the tree choosing among the cards legal in that deal (UCB with
  availability counts, `ExplorationConstant` 0.7), adds one node, and plays the deal out with a
  greedy perfect-information rollout (`BelotSimulator.ChooseRolloutMove`, a random card 10% of
  the time). The reward is the team's game points minus the other team's for the deal (hanging
  points carried in included), mapped to [0, 1]; the partner's nodes maximise it, the opponents'
  minimise it. The most visited card is played.
- **It keeps no state between decisions**: `Search/RoundKnowledge` rebuilds everything from the
  context (the hanging points included), so a player can be created for any position and decides
  the same from a `BelotSeatView` (`DecideFromViewTests`).
- **What the play shows** (`RoundKnowledge`): not following shows a void; in all trumps, or with
  trumps led, following below the best card shows nothing higher; in a suit contract not trumping
  while an opponent holds the trick shows no trumps, and not overtrumping shows no higher trump;
  a belote shows the other card of the pair, a trump king or queen without one shows there is
  none; four jacks/nines (and a carre whose rank only one rank fits) show the cards.
  `Search/WorldSampler` deals within these constraints (Hall's condition keeps it from getting
  stuck). `RoundKnowledgeTests` checks at every decision of thousands of deals that the real hands
  always fit.
- **The simulator** (`Search/BelotSimulator`, card masks only) mirrors `ValidCardsService`,
  `TrickWinnerService`, `IsBeloteAllowed` and `ScoreManager`; `SimulatorAgreesWithEngineTests`
  replays thousands of random deals in every contract through both. **Change a rule in the engine
  and that test tells you to change the simulator.** Combinations: `Search/AnnounceScorer`
  (declare-everything, which keeps the carre; which ones score, hidden ranks drawn at random).
- **Bidding: Monte Carlo** (`Search/BidEvaluator`): the contracts the player may bid and the one
  that stands if it passes are played out with the rollout policy over the same 300 random deals,
  and the best is bid if it is worth at least `BidMargin` (0) game points more than passing. The
  others' first five cards are dealt to fit their bids so far under `Search/BidModel`, which is
  **SmartPlayer's bidding on card masks** (every bot bids like it). Without that conditioning the
  opponents' contracts look hopeless and the bidding loses badly. `SmartBidIsSmartPlayersBid`
  compares the two on 50,000 hands: **change SmartPlayer's bidding and update `BidModel`.**
- **Tuning record** (`claude-ab`, 400 mirrored games at 30 ms, 1σ ≈ 2 pp; don't re-try the
  rejects blindly). Kept: Monte Carlo bidding with the auction-fitting deals (58% against
  SmartPlayer's bidding), margin 0 (54% against 1), `ExplorationConstant` 0.2 → 0.4 (52.5%) →
  0.7 (51.7%), 10% random rollout cards (52%, twice). Rejected: Monte Carlo bidding with uniform
  deals (34%); margins 2.5 (43% against 1) and -1 (47% against 0); doubling and redoubling (47% at
  margin 0, 52% at margin 1: noise, so off, `MayDouble`); making the card-play deals also explain
  the auction (`UseBidInference`, 50.2%); rollouts leading into the partner's winning suit
  (48.5%); `ExplorationConstant` 0.1 (45% against 0.2) and 1.2 (51%, fewer points, against 0.7);
  1,000 bidding deals instead of 300 (50.5%). **More search time does not help** (100 ms against
  30 ms: 49%, and 84% vs 86.5% against SmartPlayer): the greedy rollout's judgement, not the
  number of deals searched, is the limit, so that is where the next gains are.

## ClaudePlayerNeural design (the app's Master)

`AI/Belot.AI.ClaudePlayer/ClaudePlayerNeural.cs` and `Neural/`; the full story (inputs, training,
results, reproduction, promotion) is `NEURAL_NETWORK.md`. The PPO-updated all-trump
network, with the original other three networks, still ties ClaudePlayerIsmcts
(100 ms): 50.000% +/- .978 pp in 2,000 games. The validated fast configuration adds bounded endgames:
`UseEndgameSearch = true`, `EndgameUseDeclarations = true`, `EndgameTricks = 3`,
`EndgameThreeTrickWorldLimit = 90`. It scores **54.100% +/- 1.386 pp over 1,000 games**
against ISMCTS, 95% interval [51.384%, 56.816%], and **50.620% +/- .187 pp over
20,000 games** against the previous fast profile (about +4 Elo). The latest idle
means (September 28) are **30.5 us/card** for this profile and **14.7 us** for networks alone.
Only `alltrumps.bin` changed; total weights remain 2,974,830 bytes. The default
constructor still uses networks alone. Master retains 100-deal search with a
400-ms budget, measured at 58.37 ms/card on September 28. It scores 51.500% +/- 1.099 pp against
the new fast profile and 50.000% +/- 1.184 pp against the original Master, each
over 1,000 games. The former is the higher point estimate but its 95% interval
includes 50%; the latter finds no significant change. Earlier, adding endgames
to Master scored 52.8% +/- 1.667 pp over 500 games, also inconclusive, so that
combination remains unpromoted.

- **Four multilayer perceptrons value every action in game points** (the team's points from the
  deal minus the other team's, hanging points included): a bidding network (97 inputs → pass, the
  six contracts, double, redouble) and a card network per kind of contract (600 inputs → 32
  cards): the suits (turned so the trump suit comes first, so one network plays all four), no
  trumps, all trumps. It takes the best; `Temperature`/`MaxRegret` make it take a nearly-as-good
  action (the app's Expert), `EvaluateCards`/`EvaluateBids` return every value. Declarations: all
  offered, like ISMCTS. The networks are embedded from `Neural/Weights/*.bin` (16-bit floats, a
  header with the tag, feature layout and sizes, refused on a mismatch; `.gitattributes` marks
  them binary).
- **`Neural/NeuralDeal`** follows a deal as a seat sees it: built from the engine's contexts (so a
  `BelotSeatView` works too) or played whole in self-play on card masks (the auction, the
  declarations, the tricks and the score, on `BelotSimulator`). **`SelfPlayAgreesWithEngineTests`
  replays 400 random matches both ways: change a rule and it fails** (with
  `SimulatorAgreesWithEngineTests`). The inputs come only from what the seat may know
  (`PlayInference`, shared with ISMCTS's `RoundKnowledge`, is what the play showed).
- **Original training** (`tools/NeuralTrainer`, C#, CPU): distil ClaudePlayerIsmcts's searches into a
  warm start, then self-play reinforcement learning where every action of a labelled decision is
  played out in the true deal by all four seats' networks (Monte Carlo policy iteration, the
  actions sharing the deal's luck). Run the trainer from a copy of its binaries (`-o`) when you
  want to build meanwhile: a running trainer locks its `bin`, which the ClaudePlayer tests build.
  Experimental training options: `--card-value-weight 0.05` separates action errors from a
  common deal-value error (default `-1` keeps the original Huber objective);
  `distill --teacher neural --in <folder> --search-deals 100 --card-label-chance 1` records
  the stronger sampled-world teacher for `fit --in <warm-start>`. Empty data preserves the
  corresponding warm-start model. These options have not yet produced promoted weights.
  `--teacher-play-chance 0` labels positions with search while the fast student plays
  them; the default 1 plays the teacher at labelled decisions.
  Optional `tools/NeuralTrainer/Gpu/fit.py` fits the same sample format with CUDA PyTorch
  and exports the existing BNN1 format. It is training-only; see its README for the
  isolated environment, reproducible commands, checks and limitations.
  Its optional `Gpu/serve.py` server accelerates batched search-label collection
  (`--teacher neural-gpu`); simulation and seat features remain in C#, and worker
  connections verify the weight hashes. This is also training-only.
  `record-selfplay` can record true-deal action targets plus separate ownership
  labels. `Gpu/fit_belief.py` tests an auxiliary card-location objective against
  a matched zero-weight control; the auxiliary head is discarded on export.
  Hidden cards are labels only, never policy inputs. The .01-weight pilot tied
  the baseline; .1 weight regressed. An isolated layout-2 public-history prototype
  also tied with Q-only fitting and regressed with .1-weight ownership loss.
  `record-ppo` and `Gpu/ppo.py` now support on-policy PPO with a separate helper
  critic. The critic sees the other hands during training; the original 600-input
  actor does not. A public-only helper is the control. Actor outputs retain Q
  units through return regression, and exports use the existing BNN1 format.
  Float32 BNF1 snapshots preserve collection/update probability parity; BPP1
  records keep public inputs, private labels and same-seat trajectories separate.
  PPO's longer GAE-.5 run improves only the all-trump file: 50.628% +/- .135 pp
  against the original pure networks in 40,000 held-out games (about +4 Elo),
  and 50.620% +/- .187 pp with bounded endgames on both sides in 20,000 games.
  Hidden hands improve critic prediction but did not beat the matched public
  control in the initial experiment. The pure policy still ties ISMCTS100:
  50.000% +/- .978 pp in 2,000 games. September 27 idle timing was 15.2 us/card,
  or 27.6 us with bounded endgames. That fast profile passes the ISMCTS gate at
  54.100% +/- 1.386 pp in 1,000 games (95% [51.384%, 56.816%]). Its all-trump
  weights are embedded; the other three files remain original. App ratings were
  recalibrated with these weights. Post-promotion checks pass: 741 engine,
  97 AI and 72 UI tests; Windows and Android builds have zero warnings or errors.
  The PPO tooling also passes all 28 Python tests.
  See `PPO_EXPERIMENT.md` and `NEURAL_NETWORK.md` section 14 for results and commands.
  `tools/LegacyOpponents` adapts SharpBelot and the C# transcription of Belot 2.06
  for comparison and PPO training, without adding app dependencies. `arena`
  supports named profiles (including smart, sharpbelot, belot206, neural, fast,
  expert, master and ismcts:100), fresh seeded players per mirrored leg, pair
  JSON and adapter diagnostics. `--opponent-in` loads separate opposing neural
  weights. `validate` also uses the seeded runner and accepts both legacy names.
  PPO's `--opponents` / `--opponent-chance` mix in external opposing teams;
  only learner actions become PPO samples. Named neural opponents use the copied
  collector's frozen embedded weights. Resume checks pool settings and, in new
  checkpoints, opponent assembly hashes. The initial uniform four-bot pool on
  every deal regressed: 49.250% +/- .205 pp against the current NN over 20,000
  games. No new weights or app ratings were promoted. All 110 AI tests, 741 engine
  tests, 72 UI tests and 28 Python tests pass; both app targets build cleanly.
  See `OPPONENTS_EXPERIMENT.md` and `NEURAL_NETWORK.md` section 15.
  Search-teacher students have not earned promotion. A separate bounded-endgame
  implementation (`Neural/EndgameSearch`) was first tested with the original networks and averages
  perfect-information endings over publicly consistent hands. Its eight-world
  three-trick variant scored 52.4% +/- 1.4 pp against ISMCTS over 1,000 games;
  the 95% interval includes 50%, so it was not promoted. The 90-world variant
  scores 54.65% +/- .168 pp against the frozen network over 20,000 games at
  31.1 us/card in the latest idle benchmark. Its 2,000-game ISMCTS confirmation
  passed at 53.45% +/- .942 pp. Fitting 2.13 million endgame-teacher positions, including a
  function-preserving deeper MLP, has not produced stronger neural weights.
  See `etc/NeuralResearch.md` for the broader research review and experiment rationale.
- **Timing**: the September 27 trainer fixes warmup counting in `bench` and separately times
  warmed card callbacks through the engine. The frozen networks measured 18.5 us/card in
  that check; the historical 8.6 us figure in `NEURAL_NETWORK.md` used a different, slightly biased measure.
  Use the corrected benchmark for new comparisons.
- **`SearchDeals`** plays each legal card out in N deals of the unseen cards (dealt like ISMCTS's,
  `WorldSampler`) with the networks for every seat and averages: +121 ELO over the networks alone
  at 100 deals; 10 deals is worse than none. `SearchTimeLimitMilliseconds` limits sampled
  deals on slow phones, with its budget checked between complete deals after at least eight.
- **Changing the inputs** (`FeatureEncoder`): bump `LayoutVersion` and retrain. **Lesson**: judge
  a change against ClaudePlayerIsmcts, not only against earlier networks, whose head-to-head gains
  overstated the real ones several times over.

## The MAUI app (`src/UI/Belot.UI`)

Android and Windows (`net10.0-android`; `net10.0-windows10.0.19041.0` only when building on
Windows), modelled file for file on the Santase engine's `Santase.UI`. The person plays South and
picks the level of each other seat separately: the partner (North) and the rivals West (on the
left) and East (on the right), from `Game/AiLevels.cs`: Random (`RandomPlayer`), Beginner
(`DummyPlayer`), Skilled (`SmartPlayer`), Expert (`ClaudePlayerNeural` played loose,
`Temperature` 1.5, `MaxRegret` 4, plus the validated bounded endgames) and Master
(`ClaudePlayerNeural` with `SearchDeals` 100 and a 400-ms search budget). Hints use
`AiLevels.CreateFastPlayer()`, the unrestricted neural/endgame profile. The Master keeps the id
`claude` it had as ClaudePlayerIsmcts, so people's history and records carry over. The app
references the three AI projects, so an `IPlayer` break in any of them breaks the app build too.

- **The game is one async flow on the UI thread, never a thread of its own.** `Game/GameSession.cs`
  drives a `BelotMatch`: the person's decision is an awaited `TaskCompletionSource`, registered
  before `TurnStarted` is raised (`TryBid`/`TryDeclare`/`TryPlay`, checked with
  `BelotMatch.Validate`; the person's belote is always claimed); a computer seat decides with
  `Task.Run(() => player.Decide(view))` during the think pause (the only work off the UI thread;
  each seat has its own player, built per game by `Lineup.CreatePlayer`); a finished trick stays
  for the settle pause, the passes and cards the rules make wait for the auto-move pause, and a
  finished deal waits for `Continue`. Once stopped or restarted, a run raises nothing more: it
  checks for the stop after every await and every raised event. **Don't reintroduce a game
  thread, blocking waits or `Thread.Sleep` pacing.**
- **One `Act` is many things at the table**: after a bid the seats that can only pass pass; a card
  is followed by every forced card, across tricks and to the end of the deal, which is then scored
  and the next one dealt. `Game/ActReplay.cs` (pure) turns an act into the ordered events by
  comparing South's view before and after it; a finished deal's auction and tricks come from
  `PreviousRounds[^1]` (`BelotRoundSummary.Bids`/`Tricks`). **`GameViewModel` renders from the
  events only** (the live view is already past them while they are replayed) and re-syncs from
  `GetView(South)` at each of the person's turns.
- **The game layer is MAUI-free**: all of `Game/` except `PreferencesSettingsStore.cs`, plus
  `Localization/AppStrings.cs` (English and Bulgarian, in code; `{loc:Tr Key}` in XAML) and
  `LocalizationManager.cs`. MAUI sits behind two seams: `ISettingsStore` (`SettingsStore.Current`,
  MAUI `Preferences`, set first thing in `MauiProgram`) and `IGameTableHost` (UI timers, vibration,
  leaving the page; `GamePage` implements it). The value converters live in `Converters/`.
- **Sizes follow the window**: every size in the pages (fonts, cards, gaps, buttons, paddings)
  is a design size for a small phone written `{ui:Size n}` (the scrolling pages) or
  `{ui:TableSize n}` (the game table), a binding to `Game/UiScale.cs` that the pages update from
  their size: the table is designed for 360 x 640 and fits whichever side is short, the scrolling
  pages grow with the width (views built in code use `Scaling/Ui.Size`). So a phone, a tablet, an
  emulator at any density and a desktop window show the same picture, never small text in a big
  empty screen. **No fixed sizes and no `OnIdiom` in the XAML** (`UiScaleTests` checks); the
  table is capped at 600 design units wide so a landscape window keeps the seats together.
- **Rating**: an on-device ELO (`PlayerRatingStore`, start 1000, K = 32) by the team formula:
  expected = 1 / (1 + 10^((rivals − (person + partner) / 2) / 400)), the rivals rated as the
  average of their two levels (`Lineup.RivalsElo`). The levels' ratings in `AiLevels` are pair
  ratings from the simulator's `elo` suite (`EloTournament`: two of a level against two of
  another in mirrored pairs, a Bradley-Terry fit anchored at Dummy = 1200; ClaudePlayerIsmcts
  plays too, for reference). Latest run (September 27, 2026, PPO weights, `elo 20000 60`,
  20:55, 241,080 games): Random 656 +/- 3.1, Beginner 1200 (fixed anchor),
  Skilled 1466 +/- 1.7, Expert 1600 +/- 2.2, Master 1760 +/- 23.1, and
  ClaudePlayerIsmcts 1762 +/- 20.9. Errors are one standard
  deviation from 1,000 shared-seed mirrored-pair bootstrap samples. Fast levels each played
  120,240 games; Master and ISMCTS each played 600 (120 per matchup). Re-run the suite
  and re-paste ratings if the players change. Expert retains temperature 1.5 /
  MaxRegret 4 after this check: its measured rating lies between Skilled and Master.
- **`src/Tests/Belot.UI.Tests`** compiles those files and plays whole games on a UI-like
  single-threaded `SynchronizationContext`: `ActReplayTests` (every act of 300 engine matches
  replays into exactly `GetRecord()`), `GameSessionTests` (every level, the table's event order,
  illegal decisions refused, stop and restart from any handler, hints, pacing, a player per seat),
  `GameTableTests` through `TableTester` (it plays through the view model's commands like a person
  and checks the screen against the person's view at every decision, then every deal's result
  screen, the game over, the rating, history and records), plus the strings (both languages, and
  every key the app uses), the hand order and the history. Tests that touch the static app state
  run in the non-parallel `AppState` collection on a fresh in-memory store. Keep the linked files
  MAUI-free.

## Conventions

- **StyleCop.Analyzers** (`stylecop.json` + `Rules.ruleset`) is enforced on every project. The
  load-bearing rules: `using` directives go **inside** the `namespace`, `System.*` first, with a
  blank line between using groups; files end with a newline. Namespaces are block-bodied (not
  file-scoped). The `companyName` in `stylecop.json` ("PressCenters") is a copied-config artifact;
  XML docs are not broadly required (`documentInternalElements`/`documentInterfaces` are off).
- Tests are **xUnit** (`[Fact]`/`[Theory]`) with **Moq** for `IPlayer` fakes (plus a hand-written
  `FakeObjects/FakePlayer`). `Belot.Engine` exposes internals to its test assembly via
  `InternalsVisibleTo`, so `internal` members are directly testable.
- The simulator's `GlobalCounters` (a static `long[]`) is a profiling scratchpad: increment a
  counter from anywhere in the AI and the simulator prints the totals per matchup — handy for
  measuring how often a heuristic branch fires.
