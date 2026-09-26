namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Players;

    /// <summary>A finished trick.</summary>
    public sealed class BelotTrick
    {
        /// <summary>Gets or sets the four cards, in the order they were played (the leader's first).</summary>
        public IReadOnlyList<BelotPlayedCard> Cards { get; set; }

        public PlayerPosition Winner { get; set; }
    }
}
