namespace Belot.NeuralTrainer
{
    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Reference batch implementation for equivalence tests and CPU measurements.</summary>
    internal sealed class ManagedBatchedCardPolicy : IBatchedCardPolicy
    {
        private readonly NeuralEvaluator evaluator;
        private readonly float[] values = new float[FeatureEncoder.CardOutputs];

        public ManagedBatchedCardPolicy(NeuralModels models)
        {
            this.evaluator = new NeuralEvaluator(models);
        }

        public void Choose(NeuralDeal[] states, int[] slots, uint[] legal, int count, int[] chosen)
        {
            for (var item = 0; item < count; item++)
            {
                chosen[item] = this.evaluator.BestCard(in states[slots[item]], legal[item], this.values);
            }
        }
    }
}
