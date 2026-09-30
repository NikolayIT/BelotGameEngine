namespace Belot.AI.ClaudePlayer.Human
{
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;

    /// <summary>
    /// The contracts a person could read from a bid, by the five cards it is made on: a suit with
    /// its jack or its nine and another card of it, no trumps with an ace, all trumps with a jack.
    /// In self-play the networks learned to bid clubs as a relay that shows a strong hand (30% of
    /// their suit bids held neither the suit's jack nor its nine); a human partner reads such a
    /// bid as a wish for clubs, and a human opponent as nonsense. Pass, double and redouble stay.
    /// </summary>
    internal static class NaturalBidding
    {
        public static BidType Allowed(uint hand)
        {
            var bids = BidType.Double | BidType.ReDouble;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = hand & SimTables.SuitMasks[suit];
                var honour = (cards & ((1u << ((suit * 8) + 4)) | (1u << ((suit * 8) + 2)))) != 0;
                if (honour && BitOperations.PopCount(cards) >= 2)
                {
                    bids |= (BidType)(1 << suit);
                }
            }

            if ((hand & 0x80808080u) != 0)
            {
                bids |= BidType.NoTrumps;
            }

            if ((hand & 0x10101010u) != 0)
            {
                bids |= BidType.AllTrumps;
            }

            return bids;
        }

        /// <summary>The bid indexes (see <see cref="Neural.NeuralEvaluator.BidCandidates"/>) open to a natural bidder.</summary>
        public static uint Candidates(BidType available, uint hand) => Neural.NeuralEvaluator.BidCandidates(available & Allowed(hand));
    }
}
