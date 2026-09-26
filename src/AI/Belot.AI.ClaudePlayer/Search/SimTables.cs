namespace Belot.AI.ClaudePlayer.Search
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    /// <summary>
    /// Precomputed tables for the search's bitmask simulator. A card is its engine hash
    /// (suit * 8 + type), so a hand is the same 32-bit mask a CardCollection holds. A contract is
    /// a "kind": 0-3 a trump suit (clubs to spades), 4 no trumps, 5 all trumps.
    /// </summary>
    internal static class SimTables
    {
        public const int NoTrumps = 4;

        public const int AllTrumps = 5;

        public const int Queen = (int)CardType.Queen;

        public const int King = (int)CardType.King;

        public static readonly uint[] SuitMasks = { 0x000000FFu, 0x0000FF00u, 0x00FF0000u, 0xFF000000u };

        // [kind * 32 + card]: the card's points in the contract.
        public static readonly int[] Values = new int[6 * 32];

        // [(kind * 4 + ledSuit) * 32 + card]: how strongly the card bids for the trick (the
        // highest wins); 0 when it cannot win it.
        public static readonly int[] Strengths = new int[24 * 32];

        // [(kind * 4 + ledSuit) * 32 + card]: the cards that beat this card when it leads the trick.
        public static readonly uint[] BeatMasks = new uint[24 * 32];

        // [card]: the cards of the same suit that are higher in trump order.
        public static readonly uint[] HigherTrumps = new uint[32];

        static SimTables()
        {
            for (var kind = 0; kind < 6; kind++)
            {
                var contract = ToBidType(kind);
                for (var card = 0; card < 32; card++)
                {
                    Values[(kind * 32) + card] = Card.AllCards[card].GetValue(contract);
                }

                for (var led = 0; led < 4; led++)
                {
                    var row = ((kind * 4) + led) * 32;
                    for (var card = 0; card < 32; card++)
                    {
                        Strengths[row + card] = Strength(kind, led, Card.AllCards[card]);
                    }

                    for (var card = 0; card < 32; card++)
                    {
                        for (var other = 0; other < 32; other++)
                        {
                            if (Strengths[row + other] > Strengths[row + card])
                            {
                                BeatMasks[row + card] |= 1u << other;
                            }
                        }
                    }
                }
            }

            for (var card = 0; card < 32; card++)
            {
                for (var other = 0; other < 32; other++)
                {
                    if (other >> 3 == card >> 3 && Card.AllCards[other].TrumpOrder > Card.AllCards[card].TrumpOrder)
                    {
                        HigherTrumps[card] |= 1u << other;
                    }
                }
            }
        }

        public static BidType ToBidType(int kind) =>
            kind switch
                {
                    0 => BidType.Clubs,
                    1 => BidType.Diamonds,
                    2 => BidType.Hearts,
                    3 => BidType.Spades,
                    NoTrumps => BidType.NoTrumps,
                    _ => BidType.AllTrumps,
                };

        public static int ToKind(BidType contract) =>
            contract.HasFlag(BidType.AllTrumps) ? AllTrumps :
            contract.HasFlag(BidType.NoTrumps) ? NoTrumps : (int)contract.ToCardSuit();

        // Mirrors TrickWinnerService: in a suit contract any trump beats any other card.
        private static int Strength(int kind, int led, Card card)
        {
            var suit = (int)card.Suit;
            if (kind == AllTrumps)
            {
                return suit == led ? card.TrumpOrder : 0;
            }

            if (kind == NoTrumps)
            {
                return suit == led ? card.NoTrumpOrder : 0;
            }

            if (suit == kind)
            {
                return 100 + card.TrumpOrder;
            }

            return suit == led ? card.NoTrumpOrder : 0;
        }
    }
}
