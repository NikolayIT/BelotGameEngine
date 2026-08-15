namespace BelotV2
{
    /// <summary>A single card laid to the table, tagged with the seat that played it.</summary>
    public readonly record struct Play(int Seat, Card Card);

    /// <summary>
    /// Stateless rule helpers: trick-winner resolution and card point values,
    /// derived from the strength/point tables recovered from belot.exe (see Cards.cs).
    /// Valid-card enforcement lives in ValidCards (Rules.ValidCards.cs).
    /// </summary>
    public static partial class Rules
    {
        /// <summary>
        /// Index (into <paramref name="trick"/>, i.e. play order) of the winning card.
        /// Suit contract: a trump beats any non-trump; among trumps the higher TrumpOrder
        /// wins; with no trump played the higher card of the led suit wins.
        /// All-trumps: the highest TrumpOrder card of the led suit wins (other suits are
        /// discards and cannot win). No-trumps: the highest card of the led suit wins.
        /// </summary>
        public static int WinnerIndex(IReadOnlyList<Play> trick, Contract contract)
        {
            if (trick.Count == 0)
            {
                throw new ArgumentException("empty trick");
            }

            Suit led = trick[0].Card.Suit;

            switch (contract.Category)
            {
                case ContractCategory.Suit:
                    {
                        Suit trump = contract.TrumpSuit!.Value;
                        bool anyTrump = false;
                        for (int i = 0; i < trick.Count; i++)
                        {
                            if (trick[i].Card.Suit == trump)
                            {
                                anyTrump = true;
                                break;
                            }
                        }

                        return anyTrump
                            ? BestBy(trick, trump, Cards.TrumpOrder)
                            : BestBy(trick, led, Cards.NoTrumpOrder);
                    }

                case ContractCategory.AllTrumps:
                    return BestBy(trick, led, Cards.TrumpOrder);

                default: // NoTrumps
                    return BestBy(trick, led, Cards.NoTrumpOrder);
            }
        }

        public static int WinnerSeat(IReadOnlyList<Play> trick, Contract contract)
            => trick[WinnerIndex(trick, contract)].Seat;

        /// <summary>Point value of a card under a contract (trump table if the card is trump).</summary>
        public static int PointValue(Card c, Contract contract)
            => contract.IsTrump(c.Suit) ? Cards.TrumpPointValue(c) : Cards.PlainPointValue(c);

        /// <summary>Sum of card point values in a set under a contract (excludes last-trick bonus).</summary>
        public static int PointsOf(IEnumerable<Card> cards, Contract contract)
        {
            int total = 0;
            foreach (Card c in cards)
            {
                total += PointValue(c, contract);
            }

            return total;
        }

        // Highest card of a given suit by the supplied order table; returns play-order index.
        private static int BestBy(IReadOnlyList<Play> trick, Suit suit, int[] order)
        {
            int best = -1;
            int bestStrength = -1;
            for (int i = 0; i < trick.Count; i++)
            {
                Card c = trick[i].Card;
                if (c.Suit != suit)
                {
                    continue;
                }

                int s = order[c.RankIndex];
                if (s > bestStrength)
                {
                    bestStrength = s;
                    best = i;
                }
            }

            // The led card always matches `led`; for a trump query at least one card matches.
            return best;
        }
    }
}
