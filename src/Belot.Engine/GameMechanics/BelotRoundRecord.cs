namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>Everything that happened in one deal, hidden cards included: for replays and for checking a deal afterwards.</summary>
    public sealed class BelotRoundRecord
    {
        public int RoundNumber { get; set; }

        /// <summary>Gets or sets who bid and led first.</summary>
        public PlayerPosition FirstToPlay { get; set; }

        /// <summary>
        /// Gets or sets the 32 cards in the order they came off the shuffled deck: 0-19 five
        /// rounds of one card each to South, East, North and West, then (after a contract) 20-31
        /// three more rounds in the same order.
        /// </summary>
        public IReadOnlyList<Card> Deal { get; set; }

        /// <summary>Gets or sets the auction, in order (the passes of seats that could only pass included).</summary>
        public IReadOnlyList<Bid> Bids { get; set; }

        /// <summary>Gets or sets the contract: its declarer and type (Pass when passed out).</summary>
        public Bid Contract { get; set; }

        /// <summary>Gets or sets the combinations and belotes declared, with their cards.</summary>
        public IReadOnlyList<BelotAnnounce> Announces { get; set; }

        /// <summary>
        /// Gets or sets the finished tricks, in order. In the deal a match was stopped in, the
        /// cards of an unfinished trick come last, as a trick with fewer than four cards and no
        /// winner (<see cref="PlayerPosition.Unknown"/>).
        /// </summary>
        public IReadOnlyList<BelotTrick> Tricks { get; set; }

        /// <summary>Gets or sets how the deal was scored; null for the deal a match was stopped in.</summary>
        public BelotRoundSummary Result { get; set; }
    }
}
