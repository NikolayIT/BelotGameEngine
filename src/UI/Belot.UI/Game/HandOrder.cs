namespace Belot.UI.Game
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    /// <summary>
    /// How the person's hand is laid out: by suit, colours alternating (spades, hearts, clubs,
    /// diamonds), the trump suit first in a suit contract; within a suit from the strongest card
    /// down, in trump order for trumps (J 9 A 10 K Q 8 7; every suit in all trumps) and otherwise in
    /// plain order (A 10 K Q J 9 8 7). During the bidding every suit is in plain order.
    /// </summary>
    public static class HandOrder
    {
        private static readonly CardSuit[] SuitCycle = { CardSuit.Spade, CardSuit.Heart, CardSuit.Club, CardSuit.Diamond };

        public static IReadOnlyList<Card> Sort(IEnumerable<Card> cards, BidType contract)
        {
            var allTrumps = contract.HasFlag(BidType.AllTrumps);
            var noTrumps = contract.HasFlag(BidType.NoTrumps);
            var hasTrumpSuit = !allTrumps && !noTrumps && (contract & ~(BidType.Double | BidType.ReDouble)) != BidType.Pass;
            var trump = hasTrumpSuit ? contract.ToCardSuit() : CardSuit.Spade;
            var start = hasTrumpSuit ? System.Array.IndexOf(SuitCycle, trump) : 0;

            int SuitRank(CardSuit suit) => (System.Array.IndexOf(SuitCycle, suit) - start + 4) % 4;
            bool IsTrump(Card card) => allTrumps || (hasTrumpSuit && card.Suit == trump);

            return cards
                .OrderBy(card => SuitRank(card.Suit))
                .ThenByDescending(card => IsTrump(card) ? card.TrumpOrder : card.NoTrumpOrder)
                .ToArray();
        }
    }
}
