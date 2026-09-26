namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;

    using Xunit;

    public class NeuralNetworkTests
    {
        // Layer widths that are and are not multiples of the SIMD width (and of four of them).
        [Theory]
        [InlineData(new[] { 600, 64, 32 })]
        [InlineData(new[] { 97, 37, 9 })]
        [InlineData(new[] { 20, 5 })]
        [InlineData(new[] { 600, 512, 256, 128, 32 })]
        public void TheForwardPassComputesTheNetwork(int[] sizes)
        {
            var random = new Random(sizes.Sum());
            var network = RandomModels.Network(0, sizes, random);
            for (var trial = 0; trial < 50; trial++)
            {
                var count = random.Next(0, Math.Min(sizes[0], 120));
                var indices = Enumerable.Range(0, sizes[0]).OrderBy(_ => random.Next()).Take(count).ToArray();
                var values = indices.Select(_ => random.Next(3) == 0 ? (float)random.NextDouble() : 1f).ToArray();
                var output = new float[sizes[^1]];
                network.Forward(indices, values, output);

                var expected = Reference(network, sizes, indices, values);
                for (var o = 0; o < output.Length; o++)
                {
                    Assert.Equal(expected[o], output[o], 1e-3 * (1 + Math.Abs(expected[o])));
                }
            }
        }

        // The file keeps 16-bit weights: once rounded, saving and loading changes nothing.
        [Fact]
        public void TheFileRoundTrips()
        {
            var network = RandomModels.Network(2, new[] { FeatureEncoder.CardInputs, 40, FeatureEncoder.CardOutputs }, new Random(5));
            var once = RoundTrip(network);
            var twice = RoundTrip(once);
            Assert.Equal(2, twice.Tag);
            Assert.Equal(FeatureEncoder.LayoutVersion, twice.Layout);
            Assert.Equal(network.GetSizes(), twice.GetSizes());
            for (var layer = 0; layer < network.LayerCount; layer++)
            {
                Assert.Equal(once.GetWeights(layer), twice.GetWeights(layer));
                Assert.Equal(once.GetBiases(layer), twice.GetBiases(layer));
                Assert.All(
                    network.GetWeights(layer).Zip(once.GetWeights(layer)),
                    x => Assert.Equal(x.First, x.Second, 1e-3));
            }
        }

        [Fact]
        public void ABrokenOrMismatchedFileIsRefused()
        {
            Assert.Throws<InvalidDataException>(() => NeuralNetwork.Read(new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));

            var bytes = Save(RandomModels.Network(1, new[] { FeatureEncoder.CardInputs, 8, FeatureEncoder.CardOutputs }, new Random(1)));
            Assert.ThrowsAny<Exception>(() => NeuralNetwork.Read(new MemoryStream(bytes.Take(bytes.Length - 10).ToArray())));

            // A card network where the bidding one belongs, or the wrong input size.
            var random = new Random(2);
            var card = RandomModels.Network(1, new[] { FeatureEncoder.CardInputs, 8, FeatureEncoder.CardOutputs }, random);
            Assert.Throws<InvalidDataException>(() => new NeuralModels(card, card, card, card));
            var small = RandomModels.Network(1, new[] { FeatureEncoder.CardInputs - 1, 8, FeatureEncoder.CardOutputs }, random);
            var models = RandomModels.Create(3);
            Assert.Throws<InvalidDataException>(() => new NeuralModels(models.Bid, small, models.Networks[2], models.Networks[3]));
        }

        private static float[] Reference(NeuralNetwork network, int[] sizes, int[] indices, float[] values)
        {
            var input = new double[sizes[0]];
            for (var k = 0; k < indices.Length; k++)
            {
                input[indices[k]] = values[k];
            }

            for (var layer = 0; layer < sizes.Length - 1; layer++)
            {
                var weights = network.GetWeights(layer);
                var biases = network.GetBiases(layer);
                var output = new double[sizes[layer + 1]];
                for (var o = 0; o < output.Length; o++)
                {
                    var sum = (double)biases[o];
                    for (var i = 0; i < input.Length; i++)
                    {
                        sum += weights[(i * output.Length) + o] * input[i];
                    }

                    output[o] = layer < sizes.Length - 2 ? Math.Max(0, sum) : sum;
                }

                input = output;
            }

            return input.Select(x => (float)x).ToArray();
        }

        private static byte[] Save(NeuralNetwork network)
        {
            using var stream = new MemoryStream();
            network.Write(stream);
            return stream.ToArray();
        }

        private static NeuralNetwork RoundTrip(NeuralNetwork network) => NeuralNetwork.Read(new MemoryStream(Save(network)));
    }
}
