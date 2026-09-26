namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Players;

    /// <summary>A finished (or stopped) match in full, hidden cards included. Only available once it is over.</summary>
    public sealed class BelotMatchRecord
    {
        /// <summary>Gets or sets the deals, in order.</summary>
        public IReadOnlyList<BelotRoundRecord> Rounds { get; set; }

        /// <summary>Gets or sets the winning team; <see cref="PlayerPosition.Unknown"/> for a stopped match.</summary>
        public PlayerPosition Winner { get; set; }

        public int SouthNorthPoints { get; set; }

        public int EastWestPoints { get; set; }

        /// <summary>Gets or sets the points left hanging at the end.</summary>
        public int HangingPoints { get; set; }
    }
}
