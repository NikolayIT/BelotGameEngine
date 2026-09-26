namespace Belot.UI.Game
{
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>How a deal ended, as the engine scored it.</summary>
    public sealed record RoundEndInfo(
        int RoundNumber,
        PlayerPosition Declarer,
        BidType Contract,
        RoundOutcome Outcome,
        int SouthNorthInDeal,
        int EastWestInDeal,
        int SouthNorthCombinations,
        int EastWestCombinations,
        int SouthNorthTricks,
        int EastWestTricks,
        bool IsCapot,
        int SouthNorthAwarded,
        int EastWestAwarded,
        int SouthNorthGamePoints,
        int EastWestGamePoints,
        int HangingBefore,
        int HangingAfter,
        IReadOnlyList<Combination> Combinations,
        bool IsGameOver) : GameEvent;
}
