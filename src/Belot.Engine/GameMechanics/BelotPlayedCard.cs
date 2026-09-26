namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    /// <summary>A card on the table: who played it, and whether it made a belote.</summary>
    public sealed class BelotPlayedCard
    {
        public PlayerPosition Player { get; set; }

        public Card Card { get; set; }

        /// <summary>Gets or sets a value indicating whether the card was played with a belote that counted.</summary>
        public bool Belote { get; set; }
    }
}
