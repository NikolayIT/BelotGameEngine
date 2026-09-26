namespace Belot.NeuralTrainer
{
    /// <summary>Samples copied out of a <see cref="SampleBuffer"/> for one training step.</summary>
    internal sealed class Batch
    {
        public Batch(int capacity, int outputs)
        {
            this.Capacity = capacity;
            this.Outputs = outputs;
            this.Indices = new int[capacity * SampleBuffer.MaxFeatures];
            this.Values = new float[capacity * SampleBuffer.MaxFeatures];
            this.FeatureCounts = new int[capacity];
            this.Labels = new float[capacity * outputs];
            this.Masks = new uint[capacity];
        }

        public int Capacity { get; }

        public int Outputs { get; }

        public int Count { get; set; }

        /// <summary>Gets each sample's non-zero inputs, <see cref="SampleBuffer.MaxFeatures"/> slots a sample.</summary>
        public int[] Indices { get; }

        public float[] Values { get; }

        public int[] FeatureCounts { get; }

        /// <summary>Gets each sample's target outputs (in units of <c>NeuralEvaluator.ValueScale</c>).</summary>
        public float[] Labels { get; }

        /// <summary>Gets which outputs of each sample are labelled.</summary>
        public uint[] Masks { get; }
    }
}
