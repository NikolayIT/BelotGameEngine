namespace Belot.AI.ClaudePlayer.Tests.TestHelpers
{
    using System;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Networks of the right shapes with random weights, for tests that need a neural player of any strength.</summary>
    public static class RandomModels
    {
        internal static NeuralModels Create(int seed)
        {
            var random = new Random(seed);
            return new NeuralModels(
                Network(0, new[] { FeatureEncoder.BidInputs, 24, FeatureEncoder.BidOutputs }, random),
                Network(1, new[] { FeatureEncoder.CardInputs, 32, 16, FeatureEncoder.CardOutputs }, random),
                Network(2, new[] { FeatureEncoder.CardInputs, 32, 16, FeatureEncoder.CardOutputs }, random),
                Network(3, new[] { FeatureEncoder.CardInputs, 32, 16, FeatureEncoder.CardOutputs }, random));
        }

        internal static NeuralNetwork Network(int tag, int[] sizes, Random random)
        {
            var weights = new float[sizes.Length - 1][];
            var biases = new float[sizes.Length - 1][];
            for (var layer = 0; layer < weights.Length; layer++)
            {
                weights[layer] = new float[sizes[layer] * sizes[layer + 1]];
                biases[layer] = new float[sizes[layer + 1]];
                for (var i = 0; i < weights[layer].Length; i++)
                {
                    weights[layer][i] = (float)((random.NextDouble() * 2) - 1) * 0.3f;
                }

                for (var i = 0; i < biases[layer].Length; i++)
                {
                    biases[layer][i] = (float)((random.NextDouble() * 2) - 1) * 0.1f;
                }
            }

            return new NeuralNetwork(tag, FeatureEncoder.LayoutVersion, sizes, weights, biases);
        }
    }
}
