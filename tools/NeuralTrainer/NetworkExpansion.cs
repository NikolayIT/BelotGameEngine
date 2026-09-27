namespace Belot.NeuralTrainer
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Adds hidden units without changing the initial function. Old units keep their incoming
    /// weights, new units start randomly, and connections from new to old units start at zero.
    /// The extra units can then learn through those connections during ordinary training.
    /// </summary>
    internal static class NetworkExpansion
    {
        public static NeuralNetwork Widen(NeuralNetwork source, int[] hiddenSizes, Random random)
        {
            var oldSizes = source.GetSizes();
            if (hiddenSizes.Length != oldSizes.Length - 2)
            {
                throw new ArgumentException("Expansion must keep the same number of hidden layers.", nameof(hiddenSizes));
            }

            for (var layer = 0; layer < hiddenSizes.Length; layer++)
            {
                if (hiddenSizes[layer] < oldSizes[layer + 1])
                {
                    throw new ArgumentException("Expansion cannot shrink a hidden layer.", nameof(hiddenSizes));
                }
            }

            var newSizes = new[] { source.InputSize }.Concat(hiddenSizes).Append(source.OutputSize).ToArray();
            var expanded = new Mlp(source.Tag, newSizes, random);
            for (var layer = 0; layer < source.LayerCount; layer++)
            {
                var oldWeights = source.GetWeights(layer);
                var weights = expanded.Weights(layer);
                for (var input = 0; input < newSizes[layer]; input++)
                {
                    var row = weights.AsSpan(input * newSizes[layer + 1], oldSizes[layer + 1]);
                    if (input < oldSizes[layer])
                    {
                        oldWeights.AsSpan(input * oldSizes[layer + 1], oldSizes[layer + 1]).CopyTo(row);
                    }
                    else
                    {
                        row.Clear();
                    }
                }

                source.GetBiases(layer).CopyTo(expanded.Biases(layer), 0);
            }

            return expanded.ToNetwork();
        }

        public static void Run(TrainingSettings settings)
        {
            var source = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var random = new Random(settings.Seed);
            var sizes = TrainingSettings.ParseSizes(settings.Sizes);
            var networks = source.Networks.Select((network, tag) => tag == NeuralModels.BidTag ? network : Widen(network, sizes, random)).ToArray();
            new NeuralModels(networks[0], networks[1], networks[2], networks[3]).Save(settings.Out);
            Console.WriteLine($"Expanded card networks to {string.Join("-", networks[1].GetSizes())}; saved to {settings.Out}. Bidding unchanged.");
        }
    }
}
