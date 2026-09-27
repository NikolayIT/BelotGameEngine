namespace Belot.NeuralTrainer
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Masked softmax of point-valued Q outputs; no hidden information is accepted.</summary>
    internal static class PpoPolicy
    {
        public static int Sample(float[] values, uint mask, double temperature, double uniform, out float logProbability)
        {
            if (mask == 0 || !double.IsFinite(temperature) || temperature <= 0 || uniform < 0 || uniform >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature));
            }

            var maximum = float.NegativeInfinity;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var value = values[BitOperations.TrailingZeroCount(rest)];
                if (!float.IsFinite(value))
                {
                    throw new ArgumentException("Non-finite legal action value.", nameof(values));
                }

                maximum = Math.Max(maximum, value);
            }

            Span<double> weights = stackalloc double[FeatureEncoder.CardOutputs];
            var total = 0d;
            var scale = NeuralEvaluator.ValueScale / temperature;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var action = BitOperations.TrailingZeroCount(rest);
                weights[action] = Math.Exp(((double)values[action] - maximum) * scale);
                total += weights[action];
            }

            var threshold = uniform * total;
            var last = -1;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var action = BitOperations.TrailingZeroCount(rest);
                last = action;
                threshold -= weights[action];
                if (threshold < 0)
                {
                    logProbability = (float)((((double)values[action] - maximum) * scale) - Math.Log(total));
                    return action;
                }
            }

            logProbability = (float)((((double)values[last] - maximum) * scale) - Math.Log(total));
            return last;
        }
    }
}
