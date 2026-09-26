namespace Belot.UI.Game
{
    using System.Collections.Generic;

    using Belot.Engine.Players;

    /// <summary>The four seats of the table. The person always sits South, the partner North.</summary>
    public static class Seats
    {
        public const PlayerPosition Person = PlayerPosition.South;

        public const PlayerPosition Partner = PlayerPosition.North;

        /// <summary>Gets the seats in the order of play (South, East, North, West).</summary>
        public static IReadOnlyList<PlayerPosition> All { get; } = new[]
        {
            PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West,
        };

        /// <summary>Whether the seat is on the person's side (South-North).</summary>
        public static bool IsUs(PlayerPosition seat) => seat == PlayerPosition.South || seat == PlayerPosition.North;

        /// <summary>The seat before this one in the order of play: the dealer of a deal this seat opens.</summary>
        public static PlayerPosition Previous(PlayerPosition seat) => seat.Next().Next().Next();
    }
}
