namespace Belot.NeuralTrainer
{
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Training labels only: two bits per rotated card identify its current owner relative
    /// to the deciding seat. Zero means our own card or a card already played, which is
    /// excluded from the hidden-card prediction objective. Never fed to the card policy.
    /// </summary>
    internal static class CardOwnership
    {
        public static ulong Encode(in NeuralDeal deal)
        {
            var result = 0UL;
            var me = deal.Play.Turn;
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            for (var relative = 1; relative < 4; relative++)
            {
                for (var rest = deal.Play.Hands[(me + relative) & 3]; rest != 0; rest &= rest - 1)
                {
                    var card = FeatureEncoder.ToNetwork(BitOperations.TrailingZeroCount(rest), rotation);
                    result |= (ulong)relative << (2 * card);
                }
            }

            return result;
        }
    }
}
