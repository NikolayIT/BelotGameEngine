# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A C# engine for **Belot** (Bridge-Belote), a 4-player (2v2) 32-card trick-taking game. The
core engine ships as the `BelotGameEngine` NuGet package. The repository's real purpose is to
**evolve a card-playing AI (`SmartPlayer`) and measure each change in ELO** against the
previously committed version — see "The ELO benchmark workflow" below, which is the single most
important thing to understand here. The full rules are in `etc/Rules.md`.

## Commands

The solution lives in `src/`. Library projects target `netstandard2.0`; runnable/test projects
target `net8.0`.

```bash
# Run the unit tests (xUnit)
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj

# Run a single test or class (xUnit filter on fully-qualified name)
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj --filter "FullyQualifiedName~ScoreManagerTests"

# Run the ELO benchmark / simulator — ALWAYS in Release, and it needs internet (see below)
dotnet run -c Release --project src/Tests/Belot.GamesSimulator/Belot.GamesSimulator.csproj

# Play in the console (you are South vs three SmartPlayers)
dotnet run --project src/UI/Belot.UI.Console/Belot.UI.Console.csproj
```

Build cross-platform projects individually with `dotnet build`. Do **not** run `dotnet build` on
the whole `src/Belot.sln`: it includes `UI/Belot.UI.Windows`, a UWP (x86) project that only builds
with Visual Studio / full MSBuild. CI (`azure-pipelines.yml`) uses `VSBuild` on `windows-latest`
for exactly this reason.

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
  capot/contract guards in the win check) and returns a `GameResult`.
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
