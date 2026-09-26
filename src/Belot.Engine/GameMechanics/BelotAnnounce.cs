namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// A declared combination or belote. During a deal the other seats see only what was declared
    /// (its <see cref="Type"/>); which cards make it (<see cref="Card"/>) and whether it scores
    /// (<see cref="IsScored"/>) show once the deal is over. A belote's card was played in the
    /// open, so it always shows.
    /// </summary>
    public sealed class BelotAnnounce
    {
        public PlayerPosition Player { get; set; }

        public AnnounceType Type { get; set; }

        /// <summary>
        /// Gets or sets the card that names the combination (a sequence's top card, a carre's
        /// rank in spades, the belote's king or queen); null while it is hidden.
        /// </summary>
        public Card Card { get; set; }

        /// <summary>Gets or sets whether it scored; null until known.</summary>
        public bool? IsScored { get; set; }

        /// <summary>Gets the points it is worth if it scores.</summary>
        public int Value => new Announce(this.Type, this.Card).Value;
    }
}
