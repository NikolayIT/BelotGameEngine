namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Numerics;

    using Belot.Engine.Players;

    /// <summary>
    /// Optional public chronology for the separate ownership model. The actor's 600 inputs and
    /// layout stay unchanged. Layout 2 appends the same two time planes as the history dataset:
    /// the trick number divided by eight, then the position within that trick divided by four.
    /// </summary>
    internal static class OwnershipFeatureEncoder
    {
        public const int HistoryLayout = 2;

        public const int HistoryInputs = 664;

        public const int MaxActive = FeatureEncoder.MaxActive + 64;

        public static int Encode(in NeuralDeal deal, uint legal, int layout, PlayerPlayCardContext context, int[] indices, float[] values)
        {
            if (layout == FeatureEncoder.LayoutVersion)
            {
                return FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            }

            if (layout != HistoryLayout)
            {
                throw new ArgumentOutOfRangeException(nameof(layout), "Unsupported ownership feature layout.");
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "Ownership layout 2 requires the public play-card context.");
            }

            if (context.RoundActions == null || context.MyPosition.Index() != deal.Play.Turn)
            {
                throw new ArgumentException("Ownership history must belong to the deciding seat's public context.", nameof(context));
            }

            Span<byte> history = stackalloc byte[32];
            history.Clear();
            var played = 0u;
            var order = 0;
            foreach (var action in context.RoundActions)
            {
                if (action?.Card == null)
                {
                    throw new ArgumentException("Ownership history contains a missing public card.", nameof(context));
                }

                var card = action.Card.GetHashCode();
                var bit = 1u << card;
                if (order >= 32 || (played & bit) != 0)
                {
                    throw new ArgumentException("Ownership history contains duplicate or excess public cards.", nameof(context));
                }

                played |= bit;
                history[card] = (byte)++order;
            }

            if (played != deal.Played || order != (4 * deal.Play.TricksPlayed) + deal.Play.TrickCards)
            {
                throw new ArgumentException("Ownership history does not match the public deal position.", nameof(context));
            }

            var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            for (var rest = played; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var previous = history[card] - 1;
                var rotated = FeatureEncoder.ToNetwork(card, rotation);
                indices[count] = 600 + rotated;
                values[count++] = ((previous / 4) + 1) / 8f;
                indices[count] = 632 + rotated;
                values[count++] = ((previous % 4) + 1) / 4f;
            }

            return count;
        }
    }
}
