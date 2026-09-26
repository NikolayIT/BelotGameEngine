namespace Belot.AI.ClaudePlayer.Neural
{
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;

    /// <summary>
    /// Turns what a seat knows (<see cref="NeuralDeal"/>) into the networks' inputs, as a sparse
    /// list (the indexes of the non-zero inputs and their values): most inputs are card planes
    /// of 32 ones and zeros. Seats are relative to the one deciding (0 itself, 1 the next to
    /// play, 2 the partner, 3 the previous). In a suit contract the suits are rotated so the
    /// trump suit is always the first (a card's index is suit * 8 + type, types from seven to
    /// ace), so one network plays all four suit contracts.
    ///
    /// Card networks (<see cref="CardInputs"/> inputs, one output per card):
    /// planes 0 the hand, 1 the legal cards, 2-5 the cards each seat played in the earlier
    /// tricks, 6-8 the card each other seat played to this trick, 9 the card holding the trick,
    /// 10-12 the unseen cards each other seat cannot hold (the play showed it), 13-15 the unseen
    /// cards each other seat surely holds; then the trick (8), the position in it (4), who holds
    /// it (3), the declarer (4), the doubling (3), each seat's bids (4 x 8: the suits, no
    /// trumps, all trumps, double, redouble), passes (4), declared combinations (4 x 6), and the
    /// points: each team's so far, the trick's, the tricks taken, and the hanging points.
    ///
    /// The bidding network (<see cref="BidInputs"/> inputs, <see cref="BidOutputs"/> outputs:
    /// pass, the six contracts, double, redouble): the five cards, the seat's place in the
    /// auction, each seat's bids and passes, the contract so far with its declarer and doubling,
    /// the bids open, the hanging points and how long the auction has been.
    /// </summary>
    internal static class FeatureEncoder
    {
        public const int LayoutVersion = 1;

        public const int CardInputs = 600;

        public const int CardOutputs = 32;

        public const int BidInputs = 97;

        public const int BidOutputs = 9;

        /// <summary>The most inputs that can be non-zero at once.</summary>
        public const int MaxActive = 256;

        private const int Scalars = 16 * 32;

        /// <summary>The card network of a contract kind: 0 suits, 1 no trumps, 2 all trumps.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CardNetwork(int kind) => kind < SimTables.NoTrumps ? 0 : kind - 3;

        /// <summary>How far the suits turn so that the trump suit comes first.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rotation(int kind) => kind < SimTables.NoTrumps ? kind : 0;

        /// <summary>The card's index as the network sees it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNetwork(int card, int rotation) => ((((card >> 3) - rotation) & 3) << 3) | (card & 7);

        /// <summary>The card of a network output.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromNetwork(int index, int rotation) => ((((index >> 3) + rotation) & 3) << 3) | (index & 7);

        /// <summary>The output of a bid: pass, clubs … all trumps, double, redouble.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BidIndex(BidType bid) => bid == BidType.Pass ? 0 : BitOperations.TrailingZeroCount((uint)bid) + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BidType BidOfIndex(int index) => index == 0 ? BidType.Pass : (BidType)(1 << (index - 1));

        /// <summary>The inputs of a card decision of the seat to play.</summary>
        /// <returns>How many inputs are non-zero.</returns>
        public static int EncodeCard(in NeuralDeal deal, uint legal, int[] indices, float[] values)
        {
            var me = deal.Play.Turn;
            var rotation = Rotation(deal.Kind);
            var hand = deal.Play.Hands[me];
            var trick = deal.TrickMask();
            var unseen = ~(hand | deal.Played);
            var count = 0;
            Plane(indices, values, ref count, 0, hand, rotation);
            Plane(indices, values, ref count, 1, legal, rotation);
            for (var relative = 0; relative < 4; relative++)
            {
                var seat = (me + relative) & 3;
                Plane(indices, values, ref count, 2 + relative, deal.PlayedBy[seat] & ~trick, rotation);
                if (relative == 0)
                {
                    continue;
                }

                if (deal.TrickCards[seat] != 0)
                {
                    Plane(indices, values, ref count, 5 + relative, 1u << (deal.TrickCards[seat] - 1), rotation);
                }

                Plane(indices, values, ref count, 9 + relative, deal.Excluded[seat] & unseen, rotation);
                Plane(indices, values, ref count, 12 + relative, deal.Known[seat] & unseen, rotation);
            }

            ref readonly var play = ref deal.Play;
            if (play.TrickCards > 0)
            {
                Plane(indices, values, ref count, 9, 1u << play.WinnerCard, rotation);
                Set(indices, values, ref count, Scalars + 12 + ((play.WinnerSeat - me) & 3) - 1, 1);
            }

            Set(indices, values, ref count, Scalars + play.TricksPlayed, 1);
            Set(indices, values, ref count, Scalars + 8 + play.TrickCards, 1);
            Set(indices, values, ref count, Scalars + 15 + ((deal.Declarer - me) & 3), 1);
            Set(indices, values, ref count, Scalars + 19 + Doubling(deal.Contract), 1);
            for (var relative = 0; relative < 4; relative++)
            {
                var seat = (me + relative) & 3;
                Flags(indices, values, ref count, Scalars + 22 + (relative * 8), RotateBids(deal.BidsMade[seat], rotation));
                Set(indices, values, ref count, Scalars + 54 + relative, deal.Passes[seat] / 3f);
                Flags(indices, values, ref count, Scalars + 58 + (relative * 6), deal.Declared[seat]);
            }

            var team = me & 1;
            var ours = team == 0 ? play.SouthNorthPoints : play.EastWestPoints;
            var theirs = team == 0 ? play.EastWestPoints : play.SouthNorthPoints;
            var ourTricks = team == 0 ? play.SouthNorthTricks : play.EastWestTricks;
            var theirTricks = team == 0 ? play.EastWestTricks : play.SouthNorthTricks;
            Set(indices, values, ref count, Scalars + 82, ours / 100f);
            Set(indices, values, ref count, Scalars + 83, theirs / 100f);
            Set(indices, values, ref count, Scalars + 84, play.TrickCards > 0 ? play.TrickPoints / 50f : 0);
            Set(indices, values, ref count, Scalars + 85, ourTricks / 8f);
            Set(indices, values, ref count, Scalars + 86, theirTricks / 8f);
            Set(indices, values, ref count, Scalars + 87, deal.Hanging / 30f);
            return count;
        }

        /// <summary>The inputs of a bid decision of the seat to bid.</summary>
        /// <returns>How many inputs are non-zero.</returns>
        public static int EncodeBid(in NeuralDeal deal, int[] indices, float[] values)
        {
            var me = deal.ToBid;
            var count = 0;
            Plane(indices, values, ref count, 0, deal.Play.Hands[me], 0);
            Set(indices, values, ref count, 32 + ((me - deal.First) & 3), 1);
            for (var relative = 0; relative < 4; relative++)
            {
                var seat = (me + relative) & 3;
                Flags(indices, values, ref count, 36 + (relative * 8), deal.BidsMade[seat]);
                Set(indices, values, ref count, 68 + relative, deal.Passes[seat] / 3f);
            }

            var plain = deal.Contract & ~(BidType.Double | BidType.ReDouble);
            Set(indices, values, ref count, 72 + BidIndex(plain), 1);
            if (plain != BidType.Pass)
            {
                Set(indices, values, ref count, 79 + ((deal.Declarer - me) & 3), 1);
                Set(indices, values, ref count, 83 + Doubling(deal.Contract), 1);
            }

            var available = (uint)deal.AvailableBids();
            Set(indices, values, ref count, 86, 1);
            for (var rest = available; rest != 0; rest &= rest - 1)
            {
                Set(indices, values, ref count, 87 + BitOperations.TrailingZeroCount(rest), 1);
            }

            Set(indices, values, ref count, 95, deal.Hanging / 30f);
            Set(indices, values, ref count, 96, deal.BidCount / 8f);
            return count;
        }

        // 0 as bid, 1 doubled, 2 redoubled.
        private static int Doubling(BidType contract) =>
            contract.HasFlag(BidType.ReDouble) ? 2 : contract.HasFlag(BidType.Double) ? 1 : 0;

        // The four suit bids turn with the suits.
        private static byte RotateBids(byte bids, int rotation)
        {
            var suits = bids & 0xF;
            suits = ((suits >> rotation) | (suits << (4 - rotation))) & 0xF;
            return (byte)((bids & 0xF0) | suits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set(int[] indices, float[] values, ref int count, int index, float value)
        {
            if (value != 0)
            {
                indices[count] = index;
                values[count] = value;
                count++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Plane(int[] indices, float[] values, ref int count, int plane, uint cards, int rotation)
        {
            var offset = plane * 32;
            for (var rest = BitOperations.RotateRight(cards, rotation * 8); rest != 0; rest &= rest - 1)
            {
                indices[count] = offset + BitOperations.TrailingZeroCount(rest);
                values[count] = 1;
                count++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Flags(int[] indices, float[] values, ref int count, int offset, uint flags)
        {
            for (var rest = flags; rest != 0; rest &= rest - 1)
            {
                indices[count] = offset + BitOperations.TrailingZeroCount(rest);
                values[count] = 1;
                count++;
            }
        }
    }
}
