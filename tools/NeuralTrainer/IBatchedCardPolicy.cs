namespace Belot.NeuralTrainer
{
    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>A training-only batch of independent information-set card decisions.</summary>
    internal interface IBatchedCardPolicy
    {
        void Choose(NeuralDeal[] states, int[] slots, uint[] legal, int count, int[] chosen);
    }
}
