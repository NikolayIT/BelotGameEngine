namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Bids by what each bid brought once the deals were over: for every natural bid the player
    /// may make (a suit with its jack or nine and another card, no trumps with an ace, all trumps
    /// with a jack, a double) the <see cref="BidModel"/> counts the cards and the auction into the
    /// game points the bid should bring over passing, and the best bid that brings more than
    /// <see cref="HeuristicSettings.BidMargin"/> is made. Situations the model does not cover,
    /// and redoubles, are left to <see cref="HeuristicBidding"/>.
    /// </summary>
    internal static class LearnedBidding
    {
        public static BidType Choose(PlayerGetBidContext context, HeuristicSettings settings, BidModel model)
        {
            var situation = BidSituation.From(context);
            var kind = BidFeatures.Class(situation);
            if (!model.Covers(kind))
            {
                return HeuristicBidding.Choose(context, settings);
            }

            Span<float> features = stackalloc float[BidFeatures.Count];
            var available = situation.Available;
            var best = BidType.Pass;
            var bestGain = settings.BidMargin;
            for (var suit = 0; suit < 4; suit++)
            {
                var bid = (BidType)(1 << suit);
                if (available.HasFlag(bid) && Natural(situation.Hand, bid))
                {
                    BidFeatures.ForSuit(situation, suit, features);
                    Consider(bid, model.Gain(BidFeatures.Suit, kind, BidFeatures.Cell(BidFeatures.Suit, situation, features), features), ref best, ref bestGain);
                }
            }

            if (available.HasFlag(BidType.NoTrumps) && Natural(situation.Hand, BidType.NoTrumps))
            {
                BidFeatures.ForNoTrumps(situation, features);
                Consider(BidType.NoTrumps, model.Gain(BidFeatures.NoTrumps, kind, BidFeatures.Cell(BidFeatures.NoTrumps, situation, features), features), ref best, ref bestGain);
            }

            if (available.HasFlag(BidType.AllTrumps) && Natural(situation.Hand, BidType.AllTrumps))
            {
                BidFeatures.ForAllTrumps(situation, features);
                Consider(BidType.AllTrumps, model.Gain(BidFeatures.AllTrumps, kind, BidFeatures.Cell(BidFeatures.AllTrumps, situation, features), features), ref best, ref bestGain);
            }

            if (available.HasFlag(BidType.Double) && settings.MayDouble)
            {
                BidFeatures.ForDouble(situation, features);
                Consider(BidType.Double, model.Gain(BidFeatures.Double, kind, BidFeatures.Cell(BidFeatures.Double, situation, features), features) - settings.DoubleBidMargin, ref best, ref bestGain);
            }

            if (best == BidType.Pass && available.HasFlag(BidType.ReDouble))
            {
                var ruled = HeuristicBidding.Choose(context, settings);
                return ruled == BidType.ReDouble ? ruled : BidType.Pass;
            }

            return best;
        }

        /// <summary>Whether a person reads the bid as what it is: a suit with its jack or nine and another card, no trumps with an ace, all trumps with a jack.</summary>
        public static bool Natural(uint hand, BidType bid)
        {
            switch (bid)
            {
                case BidType.Clubs:
                case BidType.Diamonds:
                case BidType.Hearts:
                case BidType.Spades:
                    var cards = (hand >> ((int)bid.ToCardSuit() * 8)) & 0xFFu;
                    return System.Numerics.BitOperations.PopCount(cards) >= 2 && (cards & ((1u << CardMemory.Nine) | (1u << CardMemory.Jack))) != 0;
                case BidType.NoTrumps:
                    return (hand & 0x80808080u) != 0;
                case BidType.AllTrumps:
                    return (hand & 0x10101010u) != 0;
                default:
                    return true;
            }
        }

        private static void Consider(BidType bid, double gain, ref BidType best, ref double bestGain)
        {
            if (gain > bestGain)
            {
                best = bid;
                bestGain = gain;
            }
        }
    }
}
