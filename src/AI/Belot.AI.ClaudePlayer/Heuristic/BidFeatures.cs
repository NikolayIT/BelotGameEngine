namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;
    using System.Numerics;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// The numbers the learned bidding counts for each bid it may make: a block about the cards
    /// for that kind of contract (<see cref="Hand"/> entries) and a block about the auction
    /// (<see cref="Context"/> on). A person would call the cards' block a point count: the trump
    /// jack and nine, the aces and tens, the jacks for all trumps; the auction's block says who
    /// bid what, who leads and what the bidder holds in the suits the others bid.
    /// </summary>
    internal static class BidFeatures
    {
        public const int Count = 48;

        /// <summary>Where the auction's block starts.</summary>
        public const int Context = 24;

        public const int Suit = 0;

        public const int NoTrumps = 1;

        public const int AllTrumps = 2;

        public const int Double = 3;

        public const int Kinds = 4;

        /// <summary>The situations a model is kept for: who holds the contract (nobody, me, the partner, an opponent) times its kind (none, suit, no trumps, all trumps).</summary>
        public const int Classes = 16;

        /// <summary>The names of the cards' block, per kind, and of the auction's block (reports and the trainer's model files).</summary>
        public static readonly string[][] HandNames =
        {
            new[] { "bias", "tJ", "t9", "tJ9", "tA", "t10", "tK", "tQ", "tKQ", "tLow", "tCount3", "tCount4", "sideA", "sideA10", "side10", "sideAK", "sideK", "voids", "combos", "singletons" },
            new[] { "bias", "A", "A2", "A3", "A10", "T", "KwithA10", "KwithOne", "KQ", "lenA", "J", "voids", "Q", "N9" },
            new[] { "bias", "J", "J2", "J3", "J9", "lone9", "bare9", "AwithJ9", "Alone", "TwithHonours", "KQ", "lenJ", "combos", "A", "N9" },
            new[] { "bias", "tJ", "t9", "tA", "t10", "tCount", "J", "N9", "A", "A10", "sideA", "overcalled", "tJ9", "tK", "tQ" },
        };

        public static readonly string[] ContextNames =
        {
            "pos0", "pos1", "pos2", "pos3", "partnerSuit", "partnerNT", "partnerAT", "partnerPassed", "oppSuits", "oppNT",
            "oppAT", "oppPassed", "mine", "doubled", "pJ", "p9", "pA", "p10", "oJ", "o9", "oA", "o10", "laterTurn", "oppBids",
        };

        private const int Seven = CardMemory.Seven;
        private const int Eight = CardMemory.Eight;
        private const int Nine = CardMemory.Nine;
        private const int Ten = CardMemory.Ten;
        private const int Jack = CardMemory.Jack;
        private const int Queen = CardMemory.Queen;
        private const int King = CardMemory.King;
        private const int Ace = CardMemory.Ace;

        private static readonly string[] Sides = { "open", "mine", "partner", "opp" };

        private static readonly string[] ContractKinds = { "none", "suit", "NT", "AT" };

        /// <summary>The model's situation: who holds the contract times its kind.</summary>
        public static int Class(in BidSituation situation)
        {
            var side = situation.Holder < 0 ? 0 : situation.Holder == 0 ? 1 : situation.Holder == 2 ? 2 : 3;
            var kind = situation.Contract == BidType.Pass ? 0 : situation.Contract == BidType.NoTrumps ? 2 : situation.Contract == BidType.AllTrumps ? 3 : 1;
            return (side * 4) + kind;
        }

        /// <summary>A short name of the situation, as the trainer's reports and model files use it.</summary>
        public static string ClassName(int situation) => Sides[situation >> 2] + "-" + ContractKinds[situation & 3];

        /// <summary>The numbers for bidding <paramref name="trump"/> as trumps.</summary>
        public static void ForSuit(in BidSituation situation, int trump, Span<float> features)
        {
            features.Clear();
            var hand = situation.Hand;
            var trumps = Cards(hand, trump);
            var count = BitOperations.PopCount(trumps);
            var jack = Has(trumps, Jack);
            var nine = Has(trumps, Nine);
            var king = Has(trumps, King);
            var queen = Has(trumps, Queen);
            features[0] = 1;
            features[1] = jack;
            features[2] = nine;
            features[3] = jack * nine;
            features[4] = Has(trumps, Ace);
            features[5] = Has(trumps, Ten);
            features[6] = king;
            features[7] = queen;
            features[8] = king * queen;
            features[9] = Has(trumps, Seven) + Has(trumps, Eight);
            features[10] = count >= 3 ? 1 : 0;
            features[11] = count >= 4 ? 1 : 0;
            for (var suit = 0; suit < 4; suit++)
            {
                if (suit == trump)
                {
                    continue;
                }

                var cards = Cards(hand, suit);
                var ace = Has(cards, Ace);
                var ten = Has(cards, Ten);
                features[12] += ace;
                features[13] += ace * ten;
                features[14] += ten * (1 - ace);
                features[15] += Has(cards, King) * ace;
                features[16] += Has(cards, King) * (1 - ace);
                features[17] += cards == 0 && count >= 3 ? 1 : 0;
                features[19] += BitOperations.PopCount(cards) == 1 && count >= 3 ? 1 : 0;
            }

            features[18] = (float)(HeuristicBidding.Combinations(hand) / 10);
            AddContext(situation, features);
        }

        public static void ForNoTrumps(in BidSituation situation, Span<float> features)
        {
            features.Clear();
            var hand = situation.Hand;
            features[0] = 1;
            var aces = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = Cards(hand, suit);
                var count = BitOperations.PopCount(cards);
                var ace = Has(cards, Ace);
                var ten = Has(cards, Ten);
                var king = Has(cards, King);
                aces += ace;
                features[4] += ace * ten;
                features[5] += ten * (1 - ace);
                features[6] += king * ace * ten;
                features[7] += king * Math.Max(ace, ten) * (1 - (ace * ten));
                features[8] += king * Has(cards, Queen);
                features[9] += count >= 3 ? ace : 0;
                features[10] += Has(cards, Jack);
                features[11] += cards == 0 ? 1 : 0;
                features[12] += Has(cards, Queen);
                features[13] += Has(cards, Nine);
            }

            features[1] = aces;
            features[2] = aces >= 2 ? 1 : 0;
            features[3] = aces >= 3 ? 1 : 0;
            AddContext(situation, features);
        }

        public static void ForAllTrumps(in BidSituation situation, Span<float> features)
        {
            features.Clear();
            var hand = situation.Hand;
            features[0] = 1;
            var jacks = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = Cards(hand, suit);
                var count = BitOperations.PopCount(cards);
                var jack = Has(cards, Jack);
                var nine = Has(cards, Nine);
                var ace = Has(cards, Ace);
                jacks += jack;
                features[4] += jack * nine;
                features[5] += nine * (1 - jack) * (count >= 2 ? 1 : 0);
                features[6] += nine * (count == 1 ? 1 : 0);
                features[7] += ace * Math.Max(jack, nine);
                features[8] += ace * (1 - Math.Max(jack, nine));
                features[9] += Has(cards, Ten) * Math.Max(Math.Max(jack, nine), ace);
                features[10] += Has(cards, King) * Has(cards, Queen);
                features[11] += count >= 3 ? jack : 0;
                features[13] += ace;
                features[14] += nine;
            }

            features[1] = jacks;
            features[2] = jacks >= 2 ? 1 : 0;
            features[3] = jacks >= 3 ? 1 : 0;
            features[12] = (float)(HeuristicBidding.Combinations(hand) / 10);
            AddContext(situation, features);
        }

        /// <summary>The numbers for doubling the opponents' contract: the defence against it.</summary>
        public static void ForDouble(in BidSituation situation, Span<float> features)
        {
            features.Clear();
            var hand = situation.Hand;
            features[0] = 1;
            var contract = situation.Contract;
            var trump = contract == BidType.NoTrumps || contract == BidType.AllTrumps || contract == BidType.Pass ? -1 : (int)contract.ToCardSuit();
            if (trump >= 0)
            {
                var trumps = Cards(hand, trump);
                features[1] = Has(trumps, Jack);
                features[2] = Has(trumps, Nine);
                features[3] = Has(trumps, Ace);
                features[4] = Has(trumps, Ten);
                features[5] = BitOperations.PopCount(trumps);
                features[12] = features[1] * features[2];
                features[13] = Has(trumps, King);
                features[14] = Has(trumps, Queen);
            }

            for (var suit = 0; suit < 4; suit++)
            {
                var cards = Cards(hand, suit);
                var ace = Has(cards, Ace);
                features[6] += Has(cards, Jack);
                features[7] += Has(cards, Nine);
                features[8] += ace;
                features[9] += ace * Has(cards, Ten);
                features[10] += suit != trump ? ace : 0;
            }

            features[11] = situation.MyBids + (situation.PartnerSuits != 0 || situation.PartnerNoTrumps || situation.PartnerAllTrumps ? 1 : 0) > 0 ? 1 : 0;
            AddContext(situation, features);
        }

        /// <summary>
        /// The bid's cell: the few holdings a person looks at first, counted from the numbers of
        /// the kind: for a suit its jack, nine and length; for no trumps the aces and the tens
        /// with them; for all trumps the jacks and the nines with them; for a double the
        /// trumps' jack, ace and ten, the length and whether the opponents took the contract
        /// from this side (their no trumps: the aces, their all trumps: the jacks and nines).
        /// </summary>
        public static int Cell(int kind, in BidSituation situation, ReadOnlySpan<float> features)
        {
            switch (kind)
            {
                case Suit:
                    return (int)features[1] + (2 * (int)features[2]) + (4 * ((int)features[10] + (int)features[11]));
                case NoTrumps:
                    return Math.Min((int)features[1], 3) + (4 * Math.Min((int)features[4], 2));
                case AllTrumps:
                    return Math.Min((int)features[1], 4) + (5 * Math.Min((int)features[4], 2));
                default:
                    if (situation.Contract == BidType.NoTrumps)
                    {
                        return Math.Min((int)features[8], 3) + (4 * Math.Min((int)features[9], 1));
                    }

                    if (situation.Contract == BidType.AllTrumps)
                    {
                        return Math.Min((int)features[6], 3) + (4 * Math.Min((int)features[7], 3));
                    }

                    return (int)features[1] + (2 * Math.Min((int)(features[3] + features[4]), 2)) + (6 * Math.Min((int)features[5], 4)) + (30 * (int)features[11]);
            }
        }

        private static void AddContext(in BidSituation situation, Span<float> features)
        {
            var at = Context;
            features[at + situation.Position] = 1;
            features[at + 4] = situation.PartnerSuits != 0 ? 1 : 0;
            features[at + 5] = situation.PartnerNoTrumps ? 1 : 0;
            features[at + 6] = situation.PartnerAllTrumps ? 1 : 0;
            var partnerBid = situation.PartnerSuits != 0 || situation.PartnerNoTrumps || situation.PartnerAllTrumps;
            features[at + 7] = situation.PartnerTurns > 0 && !partnerBid ? 1 : 0;
            features[at + 8] = BitOperations.PopCount((uint)situation.OpponentSuits);
            features[at + 9] = situation.OpponentNoTrumps ? 1 : 0;
            features[at + 10] = situation.OpponentAllTrumps ? 1 : 0;
            features[at + 11] = situation.OpponentTurns >= 2 && situation.OpponentBids == 0 ? 1 : 0;
            features[at + 12] = situation.MyBids > 0 ? 1 : 0;
            features[at + 13] = situation.Doubled > 0 ? 1 : 0;
            var hand = situation.Hand;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = Cards(hand, suit);
                if ((situation.PartnerSuits & (1 << suit)) != 0)
                {
                    features[at + 14] += Has(cards, Jack);
                    features[at + 15] += Has(cards, Nine);
                    features[at + 16] += Has(cards, Ace);
                    features[at + 17] += Has(cards, Ten);
                }

                if ((situation.OpponentSuits & (1 << suit)) != 0)
                {
                    features[at + 18] += Has(cards, Jack);
                    features[at + 19] += Has(cards, Nine);
                    features[at + 20] += Has(cards, Ace);
                    features[at + 21] += Has(cards, Ten);
                }
            }

            features[at + 22] = situation.MyTurns > 0 ? 1 : 0;
            features[at + 23] = situation.OpponentBids;
        }

        private static uint Cards(uint hand, int suit) => (hand >> (suit * 8)) & 0xFFu;

        private static int Has(uint cards, int type) => (int)((cards >> type) & 1);
    }
}
