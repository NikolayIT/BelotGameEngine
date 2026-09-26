namespace Belot.Engine.Players
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    /// <summary>
    /// One decision for <see cref="GameMechanics.BelotMatch.Act"/>: a bid, the combinations to
    /// declare, or a card (with or without a belote claim).
    /// </summary>
    public sealed class BelotAction
    {
        private BelotAction(BelotActionType type, BidType bidType, IReadOnlyList<Announce> announces, Card card, bool belote)
        {
            this.Type = type;
            this.BidType = bidType;
            this.Announces = announces;
            this.Card = card;
            this.Belote = belote;
        }

        public BelotActionType Type { get; }

        /// <summary>Gets the bid (a single flag, or Pass) of a <see cref="BelotActionType.Bid"/>.</summary>
        public BidType BidType { get; }

        /// <summary>
        /// Gets the combinations of a <see cref="BelotActionType.Announce"/>, from the offered ones
        /// (see <see cref="PlayerGetAnnouncesContext.AvailableAnnounces"/>); empty to declare nothing.
        /// </summary>
        public IReadOnlyList<Announce> Announces { get; }

        /// <summary>Gets the card of a <see cref="BelotActionType.PlayCard"/>.</summary>
        public Card Card { get; }

        /// <summary>
        /// Gets a value indicating whether a <see cref="BelotActionType.PlayCard"/> claims the
        /// belote (ignored when the card cannot make one).
        /// </summary>
        public bool Belote { get; }

        public static BelotAction Bid(BidType bid) => new BelotAction(BelotActionType.Bid, bid, null, null, false);

        /// <summary>The answer to an announce decision: the combinations to declare.</summary>
        /// <param name="announces">Some of the offered combinations; null or empty for none. Of
        /// the declarations sharing a card only the first counts, and any combination not offered
        /// is ignored (like <see cref="IPlayer.GetAnnounces"/>'s answer).</param>
        /// <returns>The action.</returns>
        public static BelotAction Declare(IEnumerable<Announce> announces) =>
            new BelotAction(
                BelotActionType.Announce,
                BidType.Pass,
                announces == null ? Array.Empty<Announce>() : new List<Announce>(announces).ToArray(),
                null,
                false);

        public static BelotAction PlayCard(Card card, bool belote = true) =>
            new BelotAction(BelotActionType.PlayCard, BidType.Pass, null, card, belote);
    }
}
