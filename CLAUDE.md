# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A C# engine for **Belot** (Bridge-Belote), a 4-player (2v2) 32-card trick-taking game. The
core engine ships as the `BelotGameEngine` NuGet package. The repository's real purpose is to
**evolve card-playing AIs and measure each change in ELO**. There are two: the hand-written
`SmartPlayer`, measured against its previously committed version (see "The ELO benchmark
workflow" below, the single most important thing to understand about it), and the much stronger
search player `ClaudePlayerIsmcts` (see "ClaudePlayerIsmcts design"), measured in mirrored
matches. The full rules are in `etc/Rules.md`.

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

# Play in the console (you are South vs three SmartPlayers)
dotnet run --project src/UI/Belot.UI.Console/Belot.UI.Console.csproj
```

Build cross-platform projects individually with `dotnet build`. Do **not** run `dotnet build` on
the whole `src/Belot.sln`: it includes `UI/Belot.UI.Windows`, a UWP (x86) project that only builds
with Visual Studio / full MSBuild. CI (`.github/workflows/build.yml`) builds the projects
individually for exactly this reason and leaves the UWP project out.

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

Everything flows through the `IPlayer` interface (`Belot.Engine/Players/IPlayer.cs`). The engine
drives the game and calls players via six callbacks: `GetBid`, `GetAnnounces`, `PlayCard`,
`EndOfTrick`, `EndOfRound`, `EndOfGame`. **To add an AI or a UI, implement `IPlayer`** — the engine
owns all rules and state; players only make decisions.

Control flow, outer to inner:

- `BelotGame.PlayGame(firstToPlay)` — loops rounds until a team reaches ≥151 points (with the
  capot/contract guards in the win check) and returns a `GameResult`. `new BelotGame(..., Random)`
  seeds the deals: every deal shuffles the whole deck once, so the n-th deal depends only on the
  seed and n, and the same seed with the teams swapped replays the same deals (a mirror match).
- `GameMechanics/RoundManager.PlayRound(...)` — deals 5 cards, runs bidding, deals 3 more, plays
  the tricks, scores. Owns the `Deck` and the four players' `CardCollection`s.
- `GameMechanics/ContractManager.GetContract(...)` — the bidding loop (suit ladder, no-trumps,
  all-trumps, double/redouble) until three consecutive passes.
- `GameMechanics/TricksManager.PlayTricks(...)` — 8 tricks. Handles announces (trick 1, resolved in
  trick 2 via `ValidAnnouncesService.UpdateActiveAnnounces`), Belote, trick winners, and
  accumulating each team's won cards.
- `GameMechanics/ScoreManager.GetScore(...)` — full scoring: no-trumps doubling, last-10, capot
  (+90), double/redouble coefficients, hanging points, and the suit/all-trumps **rounding** rules
  (`RoundPoints`).

Decisions returned by players are **always validated** by the engine; an illegal card or bid throws
`BelotGameException`. Rule logic lives in stateless services under `GameMechanics/`:
`ValidCardsService` (follow-suit / must-trump / must-overtrump → returns the legal `CardCollection`;
if only one card is legal the engine auto-plays it), `TrickWinnerService`, `ValidAnnouncesService`.

Player callbacks receive context objects that all extend `BasePlayerContext`
(`PlayerGetBidContext`, `PlayerGetAnnouncesContext`, `PlayerPlayCardContext`) carrying the player's
hand, the bids, the current contract, and the trick/round history.

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

## ClaudePlayerIsmcts design (the strongest AI)

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
  `PlayerPlayCardContext` (only the hanging points come from `EndOfRound`), so a player can be
  created for any position.
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
