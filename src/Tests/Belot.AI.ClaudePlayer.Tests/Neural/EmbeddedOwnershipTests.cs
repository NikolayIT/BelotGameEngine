namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    using Belot.AI.ClaudePlayer.Neural;

    using Xunit;

    public class EmbeddedOwnershipTests
    {
        [Fact]
        public void ActorAndOwnershipResourcesKeepThePromotedWeightBudget()
        {
            var assembly = typeof(CardOwnershipModel).Assembly;
            var names = assembly.GetManifestResourceNames().Where(name =>
                name.StartsWith("Belot.AI.ClaudePlayer.Neural.Weights.", StringComparison.Ordinal)
                && name.EndsWith(".bin", StringComparison.Ordinal)).ToArray();
            Assert.Equal(7, names.Length);
            var bytes = 0L;
            foreach (var name in names)
            {
                using var stream = assembly.GetManifestResourceStream(name);
                Assert.NotNull(stream);
                bytes += stream.Length;
            }

            Assert.Equal(3523482, bytes);
        }

        [Theory]
        [InlineData("trump.bin", 11, "FC3AE184C8DCD88F36CE4604EE508E66192F697327A20A9FE6D6464E5EE98F77")]
        [InlineData("notrumps.bin", 12, "193C2C8B0F02B4E95A47FA2887C2B9CBEFA505775F099D1A01ECC1412844CC67")]
        [InlineData("alltrumps.bin", 13, "0C4C4F84603861A816E9906ACB3CAA1A2F9257939A4C4E30F3516DF7255E8A9E")]
        public void ResourcesAreTheExactCheckedNaturalBiddingExports(string name, int tag, string expectedHash)
        {
            using var stream = typeof(CardOwnershipModel).Assembly.GetManifestResourceStream(
                "Belot.AI.ClaudePlayer.Neural.Weights.Ownership." + name);
            Assert.NotNull(stream);
            Assert.Equal(CardOwnershipModel.FileBytes, stream.Length);
            Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(stream)));
            stream.Position = 0;
            var network = CardOwnershipModel.Read(stream, tag);
            Assert.Equal(tag, network.Tag);
            Assert.Equal(FeatureEncoder.LayoutVersion, network.Layout);
            Assert.Equal(new[] { 600, 128, 64, 96 }, network.GetSizes());
        }

        [Fact]
        public void LazySharedModelMatchesIndependentCheckedResourcesWithSeparateBuffers()
        {
            var model = CardOwnershipModel.Embedded;
            Assert.Same(model, CardOwnershipModel.Embedded);
            var networks = new NeuralNetwork[3];
            var names = new[] { "trump.bin", "notrumps.bin", "alltrumps.bin" };
            for (var index = 0; index < networks.Length; index++)
            {
                using var stream = typeof(CardOwnershipModel).Assembly.GetManifestResourceStream(
                    "Belot.AI.ClaudePlayer.Neural.Weights.Ownership." + names[index]);
                Assert.NotNull(stream);
                networks[index] = CardOwnershipModel.Read(stream, 11 + index);
            }

            var first = model.CreateEvaluator();
            var second = model.CreateEvaluator();
            Assert.NotSame(first, second);
            var independent = new CardOwnershipModel(networks[0], networks[1], networks[2]).CreateEvaluator();
            var expected = new float[96];
            var actual = new float[96];
            var repeated = new float[96];
            for (var kind = 0; kind < 6; kind++)
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    var deal = default(NeuralDeal);
                    deal.Kind = kind;
                    deal.Play.Turn = seat;
                    deal.Play.Hands[seat] = 255u << (seat * 8);
                    var legal = deal.Play.Hands[seat];
                    independent.Evaluate(in deal, legal, expected);
                    first.Evaluate(in deal, legal, actual);
                    second.Evaluate(in deal, legal, repeated);
                    Assert.Equal(expected, actual);
                    Assert.Equal(expected, repeated);
                    Assert.All(actual, value => Assert.InRange(value, 0f, 1f));
                }
            }
        }
    }
}
