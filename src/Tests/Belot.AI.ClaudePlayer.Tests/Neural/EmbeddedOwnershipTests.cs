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
        [InlineData("trump.bin", 11, "20D7BEC4D4BF20F8FEEBDA74D3AD40A422CC50C8342FE3131E352A8B678D6239")]
        [InlineData("notrumps.bin", 12, "1CA6B99A48A163F3B36BBE2F89EF0B8EC54B5AD787D30BFAA4074B00666463CC")]
        [InlineData("alltrumps.bin", 13, "57F12BCA5A310EC1985880F196BF6B7F3D8722789ED1C1BFE2124550FF4D8F1C")]
        public void ResourcesAreTheExactCheckedCe12Exports(string name, int tag, string expectedHash)
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
