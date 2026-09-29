# BelotGameEngine

A managed C# engine for Bulgarian Bridge-Belote: four players, two teams and a
32-card deck. It handles bidding, declarations, legal cards, tricks, scoring and
the end of a game.

**This package contains only the engine.** It includes the `IPlayer` interface
and decision contexts, but no bots, neural networks, UI or networking code. It
targets .NET Standard 2.0 and has no third-party runtime dependencies. Licensed
under MIT.

## Install

```sh
dotnet add package BelotGameEngine --version 2.0.0
```

The assembly and namespaces use `Belot.Engine`.

## Drive a match from a UI or server

`BelotMatch` advances one decision at a time. It holds no thread while a player
chooses an action. This example starts a seeded match and passes the first bid:

```csharp
using System;
using Belot.Engine.Game;
using Belot.Engine.GameMechanics;
using Belot.Engine.Players;

var match = new BelotMatch(new BelotMatchOptions
{
    FirstToPlay = PlayerPosition.South,
    Random = new Random(1234),
});

match.Start();

PlayerPosition seat = match.ToMove;
BelotSeatView view = match.GetView(seat);
BelotAction action = BelotAction.Bid(BidType.Pass);
BelotActResult result = match.Act(seat, action);

if (result != BelotActResult.Ok)
{
    throw new InvalidOperationException(result.ToString());
}
```

Use `match.Decision` (`Bid`, `Announce` or `PlayCard`) and the current seat's view
to request the next action. Submit it with `BelotAction.Bid(...)`,
`BelotAction.Declare(...)` or `BelotAction.PlayCard(...)`. `Validate(seat, action)`
checks an action without applying it. Rejected actions do not change the match.

After an accepted action the engine automatically advances forced passes and
cards, completes tricks and deals, and stops at the next decision. Refresh the
view after each action. Drive a match from one thread at a time.

- `GetView(seat)` exposes that seat's hand and public information. Other hands
  and opponents' combination cards remain hidden during the deal.
- `CreateBidContext()`, `CreateAnnouncesContext()` and `CreatePlayCardContext()`
  provide copied contexts for the matching decision. A seat view can also
  recreate these contexts.
- `IsFinished`, `Winner` and `Result` describe the end of a completed game.
  `Stop()` ends a match early without a winner or a normal game result.
- `GetRecord()` and `GetFinalView()` expose the full history after completion or
  a stop. They require `BelotMatchOptions.RecordHistory`, which defaults to `true`.

## Run a whole game with your players

Implement `Belot.Engine.Players.IPlayer` for each of the four seats, then pass
the implementations to `BelotGame`. The constructor order is South, East, North,
West; South and North form one team. `PlayGame()` calls the players until the
game ends and returns a `GameResult`.

The existing four-player constructor remains available. Its overload taking a
`System.Random` allows reproducible deals. `IPlayer` retains its six callbacks:
`GetBid`, `GetAnnounces`, `PlayCard`, `EndOfTrick`, `EndOfRound` and `EndOfGame`.

## Version 2.0 and migration from 1.1

- Added `BelotMatch`, action validation, seat views and full match records for
  externally driven games. `BelotGame` uses the same rules and state machines.
- Added seeded random sources for games, rounds, decks and shuffles.
- Corrected belote scoring, declarations in the first trick, trump obligations,
  double/redouble capot scoring, hanging points through passed-out deals,
  equal-value four-of-a-kind ordering and long-sequence declarations.
- Preserved the declarer as the contract player after a double or redouble.
  `EndOfRound` is now called for passed-out deals too; consumers must handle a
  round whose contract is `BidType.Pass`.
- Declaration options can include a sequence and a four-of-a-kind that share a
  card. Only the first of overlapping declarations counts. The offered list puts
  four-of-a-kind first; select or reorder declarations to express a different
  preference.
- Reduced allocations in card iteration and rule evaluation.

**Recompile consumers when upgrading.** `CardCollection.GetEnumerator()` now
returns `CardCollection.Enumerator`, a struct, instead of `IEnumerator<Card>`.
Normal `foreach` and the `IEnumerable<Card>` interface remain supported, but the
method's binary signature changed.

`CardCollection.Highest<TKey>` and `Lowest<TKey>` now require
`IComparable<TKey>` instead of non-generic `IComparable`. Custom key types that
only implement the old interface must implement the generic interface, or
selectors must return a supported key such as `int`.

The rule fixes and additional passed-out round callbacks can change results and
callback sequences compared with 1.1. Recheck stored expectations on upgrade.

## Source and rules

- [Source, issues and tests](https://github.com/NikolayIT/BelotGameEngine)
- [Bulgarian game rules](https://github.com/NikolayIT/BelotGameEngine/blob/master/etc/Rules.md)
