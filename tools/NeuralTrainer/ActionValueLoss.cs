namespace Belot.NeuralTrainer
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Fits action differences separately from the deal's common value. Shared deal luck
    /// cancels before clipping, while a smaller value term keeps the outputs in game points.
    /// </summary>
    internal static class ActionValueLoss
    {
        /// <summary>
        /// Returns the summed loss and writes its derivative (zero for unlabelled actions).
        /// A negative value weight selects the original independent Huber losses.
        /// </summary>
        public static double Evaluate(
            ReadOnlySpan<float> predictions,
            ReadOnlySpan<float> targets,
            uint mask,
            float huber,
            float valueWeight,
            Span<float> gradient)
        {
            gradient.Clear();
            var count = BitOperations.PopCount(mask);
            if (count == 0)
            {
                return 0;
            }

            var mean = 0.0;
            if (valueWeight >= 0)
            {
                for (var rest = mask; rest != 0; rest &= rest - 1)
                {
                    var action = BitOperations.TrailingZeroCount(rest);
                    mean += predictions[action] - targets[action];
                }

                mean /= count;
            }

            var loss = 0.0;
            var derivativeMean = 0.0;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var action = BitOperations.TrailingZeroCount(rest);
                var error = predictions[action] - targets[action] - mean;
                loss += Huber(error, huber);
                gradient[action] = (float)Math.Clamp(error, -huber, huber);
                derivativeMean += gradient[action];
            }

            if (valueWeight >= 0)
            {
                loss += count * valueWeight * Huber(mean, huber);
                var correction = (float)((valueWeight * Math.Clamp(mean, -huber, huber)) - (derivativeMean / count));
                for (var rest = mask; rest != 0; rest &= rest - 1)
                {
                    gradient[BitOperations.TrailingZeroCount(rest)] += correction;
                }
            }

            return loss;
        }

        private static double Huber(double error, float threshold)
        {
            var size = Math.Abs(error);
            return size <= threshold ? 0.5 * error * error : threshold * (size - (0.5 * threshold));
        }
    }
}
