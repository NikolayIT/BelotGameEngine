namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;

    using Xunit;

    public class CardOwnershipModelTests
    {
        [Fact]
        public void CheckedReaderAcceptsOnlyExactOwnershipFiles()
        {
            var network = Network(11);
            using var stream = new MemoryStream();
            network.Write(stream);
            var bytes = stream.ToArray();
            Assert.Equal(CardOwnershipModel.FileBytes, bytes.Length);
            using var valid = new MemoryStream(bytes);
            var restored = CardOwnershipModel.Read(valid, 11);
            Assert.Equal(11, restored.Tag);
            Assert.Equal(new[] { 600, 128, 64, 96 }, restored.GetSizes());
            for (var offset = 0; offset < 36; offset += 4)
            {
                var corrupt = (byte[])bytes.Clone();
                BitConverter.GetBytes(999).CopyTo(corrupt, offset);
                Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(corrupt), 11));
            }

            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes[..^1]), 11));
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes.Concat(new byte[] { 0 }).ToArray()), 11));
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes), 12));
            bytes[36] = 0;
            bytes[37] = 0x7e;
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes), 11));
            Assert.Throws<InvalidDataException>(() => new CardOwnershipModel(Network(12), Network(12), Network(13)));
        }

        [Fact]
        public void UnrotatesCardsAndKeepsRelativeOwnersForEveryContractAndSeat()
        {
            var model = new CardOwnershipModel(Network(11), Network(12), Network(13)).CreateEvaluator();
            for (var kind = 0; kind < 6; kind++)
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    var deal = default(NeuralDeal);
                    deal.Kind = kind;
                    deal.Play.Turn = seat;
                    deal.Play.Hands[seat] = 3;
                    deal.Played = 12;
                    var weights = new float[96];
                    model.Evaluate(in deal, 3, weights);
                    for (var card = 0; card < 32; card++)
                    {
                        if (card < 4)
                        {
                            Assert.Equal(new float[3], weights.Skip(card * 3).Take(3));
                            continue;
                        }

                        var rotated = FeatureEncoder.ToNetwork(card, FeatureEncoder.Rotation(kind));
                        var sum = 0f;
                        for (var owner = 0; owner < 3; owner++)
                        {
                            var expected = (owner == rotated % 3 ? MathF.Exp(2) : 1) / (MathF.Exp(2) + 2);
                            Assert.Equal(expected, weights[(card * 3) + owner], 6);
                            sum += weights[(card * 3) + owner];
                        }

                        Assert.Equal(1f, sum, 6);
                    }
                }
            }
        }

        [Fact]
        public void HiddenCardsCannotEnterThePrediction()
        {
            var random = new Random(894);
            var sizes = new[] { 600, 128, 64, 96 };
            var model = new CardOwnershipModel(
                RandomModels.Network(11, sizes, random), RandomModels.Network(12, sizes, random), RandomModels.Network(13, sizes, random)).CreateEvaluator();
            var deal = default(NeuralDeal);
            deal.Kind = 5;
            deal.Play.Hands[0] = 255;
            var expected = new float[96];
            var actual = new float[96];
            model.Evaluate(in deal, 255, expected);
            for (var seat = 1; seat < 4; seat++)
            {
                deal.Play.Hands[seat] = uint.MaxValue;
                deal.LastThree[seat] = uint.MaxValue;
            }

            model.Evaluate(in deal, 255, actual);
            Assert.Equal(expected, actual);
            Assert.Throws<ArgumentException>(() => model.Evaluate(in deal, 255, new float[95]));
        }

        private static NeuralNetwork Network(int tag)
        {
            var sizes = new[] { 600, 128, 64, 96 };
            var weights = new[] { new float[600 * 128], new float[128 * 64], new float[64 * 96] };
            var biases = new[] { new float[128], new float[64], new float[96] };
            for (var card = 0; card < 32; card++)
            {
                biases[2][(card * 3) + (card % 3)] = 2;
            }

            return new NeuralNetwork(tag, FeatureEncoder.LayoutVersion, sizes, weights, biases);
        }
    }
}
