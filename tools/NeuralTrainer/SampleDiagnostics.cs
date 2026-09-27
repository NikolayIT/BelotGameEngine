namespace Belot.NeuralTrainer
{
    using System;
    using System.Globalization;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Measures fit to labelled actions. Teacher regret is the teacher's best labelled value
    /// minus its value for the student's choice; it is not a whole-game strength estimate.
    /// </summary>
    internal sealed class SampleDiagnostics
    {
        private long labels;
        private double squaredError;
        private double centredSquaredError;
        private double regret;
        private long bestChoices;

        public int Samples { get; private set; }

        public double RootMeanSquaredError => Math.Sqrt(this.squaredError / Math.Max(1, this.labels)) * NeuralEvaluator.ValueScale;

        public double CentredRootMeanSquaredError => Math.Sqrt(this.centredSquaredError / Math.Max(1, this.labels)) * NeuralEvaluator.ValueScale;

        public double MeanRegret => this.regret / Math.Max(1, this.Samples) * NeuralEvaluator.ValueScale;

        public double BestChoiceFraction => (double)this.bestChoices / Math.Max(1, this.Samples);

        public static SampleDiagnostics Measure(NeuralNetwork network, SampleBuffer buffer, int[] slots, Batch batch)
        {
            var result = new SampleDiagnostics();
            var outputs = new float[network.OutputSize];
            for (var start = 0; start < slots.Length; start += batch.Capacity)
            {
                buffer.Take(batch, slots.AsSpan(start, Math.Min(batch.Capacity, slots.Length - start)));
                for (var sample = 0; sample < batch.Count; sample++)
                {
                    var offset = sample * SampleBuffer.MaxFeatures;
                    var count = batch.FeatureCounts[sample];
                    network.Forward(batch.Indices.AsSpan(offset, count), batch.Values.AsSpan(offset, count), outputs);
                    result.Add(outputs, batch.Labels.AsSpan(sample * batch.Outputs, batch.Outputs), batch.Masks[sample]);
                }
            }

            return result;
        }

        public void Add(ReadOnlySpan<float> prediction, ReadOnlySpan<float> target, uint mask)
        {
            if (mask == 0)
            {
                return;
            }

            var count = BitOperations.PopCount(mask);
            var chosen = BitOperations.TrailingZeroCount(mask);
            var bestTarget = target[chosen];
            var mean = 0.0;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var action = BitOperations.TrailingZeroCount(rest);
                var error = prediction[action] - target[action];
                mean += error / count;
                this.squaredError += error * error;
                bestTarget = Math.Max(bestTarget, target[action]);
                if (prediction[action] > prediction[chosen])
                {
                    chosen = action;
                }
            }

            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var action = BitOperations.TrailingZeroCount(rest);
                var error = prediction[action] - target[action] - mean;
                this.centredSquaredError += error * error;
            }

            this.Samples++;
            this.labels += count;
            this.regret += bestTarget - target[chosen];
            if (target[chosen] == bestTarget)
            {
                this.bestChoices++;
            }
        }

        public override string ToString() => string.Format(
            CultureInfo.InvariantCulture,
            "{0} samples; RMSE {1:0.000} points, centred RMSE {2:0.000}, teacher regret {3:0.000}, optimal choices {4:P1}",
            this.Samples,
            this.RootMeanSquaredError,
            this.CentredRootMeanSquaredError,
            this.MeanRegret,
            this.BestChoiceFraction);
    }
}
