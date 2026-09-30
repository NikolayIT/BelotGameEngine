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
design" and `NEURAL_NETWORK.md`), measured in mirrored matches. The app's Master is that neural
player with search in every trick and a strong human player's technique and conventions (see
"The human-style Master" and `HUMAN_PLAY.md`). People play them in the MAUI app for Android and
Windows (see "The MAUI app"). The full rules are in `etc/Rules.md`.

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

# The app's levels (and ISMCTS for reference) in a pair-vs-pair round robin,
# printing the ratings for Game/AiLevels.cs: elo [fast pairs] [ISMCTS pairs] [Master pairs]
# (defaults 20000, 150 and 3000: the Master searches in every trick, ISMCTS on every card).
# The simulator writes UTF-16 to the console: pipe it through iconv -f UTF-16LE to grep it.
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj -- elo

# Train the neural player's networks (tools/NeuralTrainer, not in the sln; see NEURAL_NETWORK.md):
# distill | fit | train | validate | arena | bench | audit | outcomes | equity, every setting as --name value
dotnet run -c Release --project tools/NeuralTrainer/NeuralTrainer.csproj -- validate --in <folder> --opponent ismcts:100
dotnet run -c Release --project tools/NeuralTrainer/NeuralTrainer.csproj -- arena --player fast --opponent belot206 --pairs 10000 --seed 611 --data artifacts/fast-206
# Player names in arena/audit/bench: master, neural-master (Sept 29), fast, expert, smart, belot206,
# ismcts:100, ...; "bidder|player" mixes one's bids with another's play; "profile+option=value+..."
# changes settings (see OpponentCatalog.Modify), e.g. master+ms=40+worlds=256.
# The audit flags human-obvious mistakes (with hindsight costs), the auction and the conventions;
# --deals N prints N whole deals for reading.
dotnet run -c Release --project tools/NeuralTrainer/NeuralTrainer.csproj -- audit --player master --opponent neural-master --pairs 300 --deals 5 --data artifacts/audit

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

## The human-style Master (the app's Master since September 30)

`ClaudePlayerProfiles.CreateMaster()`; `HUMAN_PLAY.md` has the full account. People found the
September 29 Master (now `CreateNeuralMaster`, below) foolish despite its ratings: it gave a ten
to a trick the opponents were winning when a seven would do, and never signalled to its partner.
The `audit` trainer command measured it (6.5-16.7 such "same-suit donations" per 1,000 decisions
against 0 for Belot 2.06) and showed why: **exact ties** in the endgame solver (rounded game
points; a decided contract makes every card equal) broken by the lowest card index (the nine
and ten come before the king and ace), and **network noise** of a few tenths of a point in the
first three tricks. What changed:

- **`HumanStyle`**: among the cards valued within a tolerance of the best (0.3 game points for
  network and rollout values, 0.02 for exact endgame values, 0.5 for a discard while the
  opponents hold the trick) the player takes the one `Human/HumanPreference` prefers: points
  given or won, what keeping a card is worth (masters, guards, trumps, belote pairs), smears
  onto a partner's sure trick, discards from the suit with nothing in it (the signal), leads of
  the partner's suit and never the one it threw away, trumps drawn by the declarers and not led
  by the defenders, a belote declared. The preference alone plays only at SmartPlayer's level:
  it must never overrule a real value difference (a wider 0.8 tolerance loses 9 Elo, a one-point
  tolerance for leads 13 Elo). **`HumanDominance`** (on): before the exact endgames, in a trick
  surely lost, never a card when a lower one of the same suit with no more points loses as well
  (`HumanPreference.Dominated`); it removed the rollouts' noisy donations in tricks 1-3.
- **`EndgameRawTieBreak`**: results equal in game points are ordered by raw card points
  (1/1024 of a point each). **`EndgameSignalWeight`** 0.3: worlds where the partner holds an
  honour of a suit it threw away count 0.3 times as much. **`DoubleMargin`** 2 (no cost).
- **Strength: early-trick search.** In tricks 1-3 the network's best three cards within three
  points (`SearchCandidateCards`/`Margin`) are played out in up to 32 ownership-weighted worlds
  (`SearchOwnershipModel`), all seats by the network to the last five tricks, then solved exactly
  (`SearchDoubleDummyTricks` 5, a transposition-table `EndgameSearch` leaf solver); 40 ms, at
  least 12 deals. The candidate filter is essential: unfiltered it is 50.9%, uniform worlds 43.5%,
  rollouts to the end ~ the network itself (they estimate what it already predicts). Later tricks
  keep the belief-weighted five-trick endgame with 256 worlds, 1.5M nodes, 24 ms.
- **Natural bidding** (`NaturalBidding`, `Human/NaturalBidding.cs`): the audit found the networks
  had invented a private relay in self-play (30% of their suit bids held neither the suit's jack
  nor its nine: clubs meant "strong hand", answered with no trumps or all trumps), useless or
  harmful with a human partner. Only bids a person can read are allowed: a suit with its jack or
  nine and another card, no trumps with an ace, all trumps with a jack. The embedded networks are
  fine-tuned for it (`train --natural-bidding true`, 2.5 hours, every seat natural; ownership
  refitted on the new self-play). All neural profiles that use them must set `NaturalBidding`:
  the outputs of unnatural bids are no longer trained. With a natural partner (Belot 2.06
  standing in for a person) the new networks' team beats the relay networks' team 62-64%.
- Rejected (see `HUMAN_PLAY.md`): six-trick endgames, a network filter in the endgames (-15 Elo),
  match-equity leaves (`PlayForMatch`, +4 +/- 4 Elo, not adopted), rollouts through trick 4.
- **Validation** (fresh seeds, idle machine, mirrored whole matches): 51.71% +/- 0.41 pp against
  the September 29 Master over 10,000 games (+12 Elo, although between bots the old networks'
  relay still helps them); **64.8% +/- 0.6 pp (+106 Elo) when both teams have Belot 2.06 as the
  partner**, the case of a person's partner; 63.6% against ISMCTS100 (the Sept 29 Master 62.0%),
  79.5% against Belot 2.06, 90.4% SharpBelot, 93.7% SmartPlayer. The dominance rule costs
  nothing (49.8% +/- 0.6 pp against the Master without it). The audit finds no unnatural bids
  and no same-suit donations in tricks 1-3. Idle desktop timing: 13.1 ms a card on average,
  p99 40 ms, maximum 44.5 ms; the wall-clock caps make slower phones search less, not longer.
  Against the former 100-rollout Master: 60.8% +/- 1.7 pp (600 games).
- **Deterministic hosts** (ednaigra.com level 6 runs `CreateMaster()` with
  `EndgameTimeLimitMilliseconds = 0`) must now also set `SearchTimeLimitMilliseconds = 0`:
  fixed work then decides the same from a context and a view (`HumanMasterTests`) and takes
  14.2 ms a card on average, p99 41 ms, maximum 65 ms (idle desktop; Sept 29 Master ~1.5 ms).
- **Checks** (September 30): 391 AI, 746 engine and 287 UI tests pass; Windows and Android
  Release builds have zero warnings and errors; the 32 changed C# files have UTF-8 BOM and
  CRLF. The engine's source and NuGet API are unchanged.
- The Expert (`CreateExpert`) is the fast profile with the human style and a 5-point tolerance:
  weaker, never absurd, no random choices. Hints use the fast profile with the human style.

## ClaudePlayerNeural design (the September 29 Master, now `CreateNeuralMaster`)

`AI/Belot.AI.ClaudePlayer/ClaudePlayerNeural.cs` and `Neural/`; see
`NEURAL_NETWORK.md` for training and promotion history, and `FAST_BOT_EXPERIMENT.md`
for the September 28-29 experiments. Shared `ClaudePlayerProfiles` factories keep
app, trainer and Elo configurations identical. Master now uses the frozen actor
plus three compact ownership networks and bounded five-trick endgames: 128 sampled
worlds, 1,680-world exact three-trick limit, 250,000 nodes, declarations,
transpositions and an 8-ms cap. Ownership power is 1 with uniform mix .1.
Ordinary neural fallback averages auction-preserving suit permutations.
This **belief5-v2-ensemble** profile scores **62.000% +/- 1.247 pp against ISMCTS100** and
**59.100% +/- 1.152 pp against the former 100-rollout Master**, each over 1,000
independent mirrored games. It also scores **60.120% +/- .343 pp against the
previous fast profile over 10,000 games**. Its idle 100-game benchmark has a
1.449-ms card mean, 6.083-ms p99, 8.006-ms observed maximum and no callback above
10 ms. This is a measured desktop result, not a hard real-time guarantee.

On September 29 these were the embedded files. **Since September 30 the embedded
actor networks are their natural-bidding fine-tune and the ownership networks were
refitted on natural-bidding self-play** (see the human-style Master above and
`HUMAN_PLAY.md`); the September 29 files are frozen under
`artifacts/natural-20260930/sept29-weights` and `sept29-ownership` (local, not in git),
and the trainer rebuilds that Master exactly with `--opponent neural-master+own=<sept29-ownership>
--opponent-in <sept29-weights>`. `CreateNeuralMaster` keeps its configuration on the current
networks. `CreateFast` and hints retain three-trick/90-world endings.
`CreateRolloutMaster` preserves the previous 100-rollout/400-ms profile. All seven
embedded files total 3,523,482 bytes. Ownership inputs are public, and actor feature
layout remains 1.

The final September 29 `elo 20000 60` calibration completed 400,600 games in
1:03:02. Master is 1838 +/- 2.5, Expert 1611 +/- 2.2 and Skilled 1462 +/- 1.7,
each from 160,120 games; Expert therefore retains temperature 1.5 / MaxRegret 4.
The full rating table, uncertainty method and reproduction command are in
`NEURAL_NETWORK.md` section 16. V2 validation passes 741 engine, 361 AI,
74 UI and 78 Python tests. The post-rating UI tests pass again, and final Windows
and Android builds both have zero warnings and errors. All 46 changed C# files
have UTF-8 BOM and CRLF; engine production source and its NuGet API are unchanged.

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
- **Ten-millisecond research**: `FAST_BOT_EXPERIMENT.md` records the frozen
  baseline, all attempted variants, independent opponents and rejection results.
  The selected Master combines an exact constrained-hand sampler, separate
  ownership predictions and a bounded minimax transposition table. Interrupted
  sampled worlds contribute no root-action values; interrupted exact enumeration
  falls back entirely. Extra history, joint-loss ownership, diverse ownership
  data, control variates, truncated rollouts and late neural corrections did not
  justify replacing the selected profile. `MIXTURE_BELIEF_NOTE.md` records the
  offline K4 pilot; it has no runtime integration. Optional `CardSuitEnsemble`
  averages auction-preserving suit permutations only in ordinary network
  fallback. Master enables it after independent capped matches; the constructor,
  fast profile and Expert leave it off. Exact-teacher distillation into one
  network improved imitation metrics but tied the frozen actor in 20,000 games.
  `arena --player candidate` applies all trainer settings; `--opponent-config`
  accepts an independent checked settings JSON. `master`, `rollout-master`,
  `fast` and `expert` use shared factories in both `arena` and `bench --player`.
  `sampled4` remains the frozen four-trick development control. Use an otherwise
  idle machine for timing and any wall-clock-budget opponent.

## The MAUI app (`src/UI/Belot.UI`)

Android and Windows (`net10.0-android`; `net10.0-windows10.0.19041.0` only when building on
Windows), modelled file for file on the Santase engine's `Santase.UI`. The person plays South and
picks the level of each other seat separately: the partner (North) and the rivals West (on the
left) and East (on the right), from `Game/AiLevels.cs`: Random (`RandomPlayer`), Beginner
(`DummyPlayer`), Skilled (`SmartPlayer`), Expert (`ClaudePlayerProfiles.CreateExpert()`: the
network with bounded endgames choosing the most natural card within 5 game points of the best)
and Master (`ClaudePlayerProfiles.CreateMaster()`, the human-style Master above: early-trick
rollouts in 40 ms and belief-weighted endgames in 24 ms). Hints use
`AiLevels.CreateFastPlayer()`, the human-style neural/endgame profile. The Master keeps the id
`claude` it had as ClaudePlayerIsmcts, so people's history and records carry over. The app
references the three AI projects, so an `IPlayer` break in any of them breaks the app build too.

- **The game is one async flow on the UI thread, never a thread of its own.** `Game/GameSession.cs`
  drives a `BelotMatch`: the person's decision is an awaited `TaskCompletionSource`, registered
  before `TurnStarted` is raised (`TryBid`/`TryDeclare`/`TryPlay`, checked with
  `BelotMatch.Validate`; the person's belote is always claimed). The UI opts South into the
  internal `ManualCardPlaySeats` option: every card waits for a tap, including a single legal
  card and the last card. The engine's public API and default automatic moves are unchanged;
  friend access is limited to the UI and its tests. A computer seat decides with
  `Task.Run(() => player.Decide(view))` during the think pause (the only work off the UI thread;
  each seat has its own player, built per game by `Lineup.CreatePlayer`); a finished trick stays
  for the settle pause, the passes and cards the rules make wait for the auto-move pause, and a
  finished deal waits for `Continue`. The UI's manual seat interrupts forced-card replay before
  each human card. Once stopped or restarted, a run raises nothing more: it
  checks for the stop after every await and every raised event. **Don't reintroduce a game
  thread, blocking waits or `Thread.Sleep` pacing.**
- **One `Act` is many things at the table**: after a bid the seats that can only pass pass; a card
  is followed by every forced card, across tricks and to the end of the deal, which is then scored
  and the next one dealt. `Game/ActReplay.cs` (pure) turns an act into the ordered events by
  comparing South's view before and after it; a finished deal's auction and tricks come from
  `PreviousRounds[^1]` (`BelotRoundSummary.Bids`/`Tricks`). **`GameViewModel` renders from the
  events only** (the live view is already past them while they are replayed) and re-syncs from
  `GetView(South)` at each of the person's turns. Live cards and the last-trick miniature bind
  to each seat's chronological Z index; publish the index before the card/animation notification.
- **The game layer is MAUI-free**: all of `Game/` except `PreferencesSettingsStore.cs`, plus
  `Localization/AppStrings.cs` (English and Bulgarian, in code; `{loc:Tr Key}` in XAML) and
  `LocalizationManager.cs`. MAUI sits behind two seams: `ISettingsStore` (`SettingsStore.Current`,
  MAUI `Preferences`, set first thing in `MauiProgram`) and `IGameTableHost` (UI timers, vibration,
  leaving the page; `GamePage` implements it). The value converters live in `Converters/`.
- **Compact home and guarded navigation**: three native pickers select the partner, West and
  East levels; `Game/LineupSelection.cs` keeps the saved level ids. Beside each selector, an
  optional name is saved independently in `AppSettings`. `SeatNames.Create` supplies custom names
  or localized seat roles in engine order; difficulty labels never become in-game names. `Game/PageActions.cs`
  serializes navigation and dialogs on the supporting pages and rejects confirmations from an
  earlier page visit. `UiScale.StartColumnsFor` keeps the compact two-column setup at normal
  phone text size; larger text gives the title a full row and stacks each name above its picker.
  Reflow preserves the existing controls, text and selection, and observes scale changes only
  while the page is active. Keep Play ahead of optional history and explanatory text. The secondary
  online-play button uses `Game/OnlinePlay.cs` to open `https://ednaigra.com/play?game=belot`
  in the system browser, with a localized launch-failure notice tied to the current visit.
  This is a browser handoff; the app's game engine has no multiplayer/network integration.
- **Sizes follow the window with upper limits**: `{ui:Size n}` (scrolling pages) and
  `{ui:TableSize n}` (table geometry) bind design sizes to `Game/UiScale.cs`; code uses
  `Scaling/Ui.Size`. Page scale caps at 1.1 and table scale at 1.25. The 360 x 640 phone design
  stays centered within 480 x 760 design units. Sizes use these helpers rather than fixed
  pixels or `OnIdiom`. Explicit `SafeAreaEdges` respect system bars and the setup keyboard;
  the table measures its content host. Live table text uses `{ui:TableTextSize n}` with a
  system-font multiplier capped at 1.3 and native auto-scaling disabled to avoid applying it
  twice. Font scaling does not enlarge the cards. Supporting pages and result overlays retain
  full system font scaling. Settings choice grids and the statistics metrics grid reduce
  their column count when large text needs more room. With stacked metrics, the Statistics
  heading spans a separate full-width row to keep the Bulgarian title intact without truncation.
- **Decisions stay above the fixed hand**: `TableGrid` is a bounded direct child of `TableHost`
  and does not scroll; North, West and East retain their expanded `Backs` stacks. Side seats
  use `Auto,Auto,Auto,*` rows so `Controls/VerticalCardBackLayout` fits the backs below the
  labels. Its MAUI-free `VerticalCardBackGeometry` keeps 38 x 53 cards and a 13-unit step when
  space permits, reduces the step toward two units, then scales proportionally if necessary.
  North's horizontal backs and the person's hand are unchanged. Declared combinations keep
  their persistent summary and suppress the duplicate speech bubble; bidding bubbles remain.
  Bidding occupies an inline `Auto` row: suit buttons above a single action row, where Double and
  ReDouble share a slot between all trumps and Pass. Declarations keep their title and Declare
  button visible; only the choices list scrolls, up to 90 table design units. One choice keeps
  its natural height. Result overlays scroll. Bounded name/score columns keep Hint and Menu
  separate. Illegal hand faces use `FaceOpacity = 0.82` over an opaque rounded black backing;
  legal faces use 1.0. Dimming changes only the image, so overlapped faces cannot bleed through
  and the hand does not move. No black overlay covers the cards; `IsPlayable` and the engine
  enforce legality. A fixed three-unit hint border overlays the card inside its
  fixed-size grid, so showing a hint does not resize or shift the hand.
- **Accessible state follows the visible state**: native hand/suit/declaration controls have
  spoken labels. Current and last-trick cards include the seat name; expanded back stacks
  announce the seat and public card count, with decorative backs excluded. Settings language
  and speed buttons announce selected/not-selected state and refresh it after language changes.
- **Android font changes preserve the active game**: `MainActivity` handles `FontScale` without
  recreating the activity. After the base callback it updates `UiScale.SystemFontScale`,
  refreshes existing MAUI font mappings and invalidates measurement, including formatted spans.
  The current match survives; this does not restore a match after process death.
- **Rating**: an on-device ELO (`PlayerRatingStore`, start 1000, K = 32) by the team formula:
  expected = 1 / (1 + 10^((rivals − (person + partner) / 2) / 400)), the rivals rated as the
  average of their two levels (`Lineup.RivalsElo`). The levels' ratings in `AiLevels` are pair
  ratings from the simulator's `elo` suite (`EloTournament`: two of a level against two of
  another in mirrored pairs, a Bradley-Terry fit anchored at Dummy = 1200; ClaudePlayerIsmcts
  plays too, for reference). Latest run (September 30, 2026, human-style Expert and Master,
  `elo 20000 60 3000`, 1:26:04): Random 646 +/- 3.2, Beginner 1200 (fixed anchor),
  Skilled 1490 +/- 1.9, Expert 1647 +/- 2.5, Master 1886 +/- 4.6, and ClaudePlayerIsmcts
  1786 +/- 17.6. Errors are one standard deviation from 1,000 shared-seed mirrored-pair
  bootstrap samples. Fast matchups use 20,000 pairs / 40,000 games (the fast levels each
  played 126,120 games), the Master's 3,000 pairs / 6,000 games (24,120 in all: it searches in
  every trick) and ISMCTS's 60 pairs / 120 games (600). The local log is
  `artifacts/natural-20260930/final-elo.log`; the previous run (September 29, Master 1838)
  and its method are in section 16 of `NEURAL_NETWORK.md`, with the UTF-16LE conversion
  through `iconv`. Re-run the suite and re-paste ratings if the players change.
- **`src/Tests/Belot.UI.Tests`** compiles those files and plays whole games on a UI-like
  single-threaded `SynchronizationContext`: `ActReplayTests` (every act of 300 engine matches
  replays into exactly `GetRecord()`), `GameSessionTests` (every level, the table's event order,
  illegal decisions refused, stop and restart from any handler, hints, pacing, a player per seat),
  `GameTableTests` through `TableTester` (it plays through the view model's commands like a person
  and checks the screen against the person's view at every decision, then every deal's result
  screen, the game over, the rating, history and records), plus the strings (both languages, and
  every key the app uses), the hand order and the history. Regression tests cover immediate
  control closure after accepted moves, repeated or late commands, hint failures and disposal,
  declaration selection, stale page confirmations, picker persistence, damaged saved settings
  and statistics, chronological card layers, fixed hint bounds, semantic labels, bounded table
  text and adaptive supporting-page grids. `VerticalCardBackGeometryTests` checks 1-8 cards
  within short/full bounds; `SeatPresentationTests` covers declaration/bid visibility and
  `StartPageLayoutTests` covers home reflow, normal-size compactness and lifecycle wiring.
  `AndroidConfigurationTests` compiles
  the actual activity against test doubles to check callback order, font refresh and retained
  state; native builds and device checks verify Android behavior. Tests that touch static app
  state run in the non-parallel `AppState` collection on a fresh in-memory store. Keep the game
  and localization files MAUI-free. See `etc/AndroidUiReleaseCheck.md` for reproduction commands
  and the recorded verification scope. September 29, 2026 manual-card follow-up: UI 275/275,
  engine 746 and AI 361 tests passed; Android and Windows Release builds have zero warnings/errors.
  `ManualCardPlayTests` compares 72 complete records with default automatic behavior; UI tests
  verify all eight human taps per deal through replay. Native Test completed two deals with
  16 explicit human card taps, five-second waits on forced/final cards, and unchanged hands
  after an illegal tap. Illegal-card backgrounds measured RGB 209/209/209, legal backgrounds
  255/255/255. The report distinguishes the earlier full-game audit from these latest checks.

## Google Play release

- Store listing source, BG/EN screenshots and graphics live in `store/google-play/`.
- `tools/PlayRelease/build-android.ps1` requires an existing upload keystore and two
  protected password files. It publishes a signed AAB, rejects Android Debug keys,
  checks the signing certificate, package/version/API 36 and both arm64/x64 ABIs.
  Password values never belong in command arguments, logs or Git.
- `tools/PlayRelease/play-release.mjs` uses an explicitly supplied Play service-account
  key. It targets only `com.nksolutions.belot`; `listings`, `upload`, and `complete`
  are remote mutations. `inspect` and `verify` discard their temporary edit.
  `upload` prepares version 1 as a draft; `complete` selects a 100% production release.
  The API requires automatic review submission for this app. Always inspect Console
  afterward: saved drafts and completed rollout configuration do not prove approval
  or public availability. It refuses to cancel an existing review.
- The canonical publisher site is `https://belot.nksolutions.com/`; its BG/EN privacy
  page is `https://belot.nksolutions.com/privacy-policy.html`. Play Console uses these
  addresses. Settings in the signed 1.0 bundle retains its original
  `https://nksolutions.com/belot/privacy-policy.html` link through the page-action gate;
  the publisher Worker permanently redirects this legacy address to the canonical page.
  `PrivacyPolicyTests` covers launch success/failure, repeated taps and stale visits.
  The release-preparation UI suite passes 287 tests; Android and Windows builds have
  zero warnings/errors. Native Test checks the exact AAB content under its existing
  test signature to preserve local statistics; this is separate from Play delivery.
- Version 1.0 was submitted in Console on 29 September 2026: 13 changes under
  Changes in review, BG/EN listings, 100% production rollout and managed publishing off.
  Quick checks were still running at the final capture; approval/public availability
  remain unverified. The user completed the declarations with target audience 9+.
  Submission and artifact evidence: `store/google-play/releases/1.0.md`.

## NuGet engine release

- `src/Belot.Engine/Belot.Engine.csproj` is the only NuGet release project. Version 2.0.0
  keeps `netstandard2.0`, MIT and no runtime package dependencies. Its packaged README
  documents the push API and migration from 1.1; AI projects and weights are excluded.
- `.github/workflows/publish.yml` tests the engine and its consumers, packs only the engine,
  validates the archive and runs its README example through an isolated package consumer.
  A release tag must exactly match `<Version>` (for example `2.0.0`). Publication runs on
  a published GitHub release or manual dispatch on that tag, never on a branch push.
- NuGet trusted publishing is scoped to `NikolayIT/BelotGameEngine`, `publish.yml`, environment
  `release`, and only new versions of the exact `BelotGameEngine` package. The environment
  permits only tags matching `*.*.*`; only the publish job has `id-token: write`.
  No long-lived NuGet key is required. See `tools/NuGetRelease/README.md` for verification
  and release commands. Check the live NuGet version after the push succeeds.

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
