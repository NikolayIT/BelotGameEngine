namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class LateCardCorrectionTests
    {
        [Fact]
        public void CheckedReaderRejectsEveryWrongHeaderAndInvalidParameter()
        {
            using var stream = new MemoryStream();
            Network(21).Write(stream);
            var bytes = stream.ToArray();
            Assert.Equal(LateCardCorrectionModel.FileBytes, bytes.Length);
            var restored = LateCardCorrectionModel.Read(new MemoryStream(bytes), 21);
            Assert.Equal(new[] { 600, 64, 32 }, restored.GetSizes());
            for (var offset = 0; offset < 32; offset += 4)
            {
                var corrupt = (byte[])bytes.Clone();
                BitConverter.GetBytes(999).CopyTo(corrupt, offset);
                Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(corrupt), 21));
            }

            Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(bytes[..^1]), 21));
            Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(bytes.Concat(new byte[] { 0 }).ToArray()), 21));
            Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(bytes), 22));
            Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(bytes), 20));
            foreach (var half in new[] { 0x7e00, 0x7c00, 0xfc00 })
            {
                BitConverter.GetBytes((ushort)half).CopyTo(bytes, 32);
                Assert.Throws<InvalidDataException>(() => LateCardCorrectionModel.Read(new MemoryStream(bytes), 21));
            }

            Assert.Throws<InvalidDataException>(() => new LateCardCorrectionModel(Network(21), Network(23), Network(22)));
            Assert.Throws<InvalidDataException>(() => new LateCardCorrectionModel(Network(21, layout: 2), Network(22), Network(23)));
        }

        [Fact]
        public void EarlierTricksRemainBitIdenticalAndSkipTheEncoder()
        {
            var evaluator = Model().CreateEvaluator();
            for (var tricks = 0; tricks < 4; tricks++)
            {
                var deal = default(NeuralDeal);
                deal.Play.TricksPlayed = tricks;
                deal.Kind = int.MaxValue;
                var values = Enumerable.Range(0, 32).Select(card => BitConverter.Int32BitsToSingle(unchecked((int)0x80000000) + card)).ToArray();
                var expected = values.Select(BitConverter.SingleToInt32Bits).ToArray();
                evaluator.Apply(in deal, uint.MaxValue, values);
                Assert.Equal(expected, values.Select(BitConverter.SingleToInt32Bits));
            }
        }

        [Fact]
        public void EveryContractUnrotatesAndCentersOnlyLegalCorrections()
        {
            var evaluator = Model().CreateEvaluator();
            for (var kind = 0; kind < 6; kind++)
            {
                var deal = default(NeuralDeal);
                deal.Kind = kind;
                deal.Play.TricksPlayed = 4;
                var legal = (1u << 1) | (1u << 8) | (1u << 19) | (1u << 31);
                var values = Enumerable.Range(0, 32).Select(card => (float)card).ToArray();
                var before = (float[])values.Clone();
                evaluator.Apply(in deal, legal, values);
                var rotation = FeatureEncoder.Rotation(kind);
                var cards = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
                var mean = cards.Average(card => (double)FeatureEncoder.ToNetwork(card, rotation));
                foreach (var card in Enumerable.Range(0, 32))
                {
                    var expected = (legal & (1u << card)) == 0
                        ? before[card]
                        : before[card] + (NeuralEvaluator.ValueScale * (FeatureEncoder.ToNetwork(card, rotation) - (float)mean));
                    Assert.Equal(expected, values[card]);
                }

                Assert.Equal(cards.Sum(card => before[card]), cards.Sum(card => values[card]));
            }
        }

        [Fact]
        public void ZeroOutputAndForcedDecisionsPreserveEveryBit()
        {
            var zero = new LateCardCorrectionModel(Network(21, zero: true), Network(22, zero: true), Network(23, zero: true)).CreateEvaluator();
            var nonzero = Model().CreateEvaluator();
            var deal = default(NeuralDeal);
            deal.Play.TricksPlayed = 4;
            var values = Enumerable.Range(0, 32).Select(card => card == 0 ? BitConverter.Int32BitsToSingle(int.MinValue) : card / 7f).ToArray();
            var expected = values.Select(BitConverter.SingleToInt32Bits).ToArray();
            zero.Apply(in deal, uint.MaxValue, values);
            Assert.Equal(expected, values.Select(BitConverter.SingleToInt32Bits));
            nonzero.Apply(in deal, 1, values);
            Assert.Equal(expected, values.Select(BitConverter.SingleToInt32Bits));
            nonzero.Apply(in deal, 0, values);
            Assert.Equal(expected, values.Select(BitConverter.SingleToInt32Bits));
            Assert.Throws<ArgumentException>(() => zero.Apply(in deal, 3, new float[31]));
        }

        [Fact]
        public void HiddenHandsCannotChangeCorrectionsAndInferenceAddsNoEncoderAllocations()
        {
            var random = new Random(8112);
            var sizes = new[] { 600, 64, 32 };
            var evaluator = new LateCardCorrectionModel(
                RandomModels.Network(21, sizes, random), RandomModels.Network(22, sizes, random), RandomModels.Network(23, sizes, random)).CreateEvaluator();
            var deal = default(NeuralDeal);
            deal.Kind = 3;
            deal.Play.TricksPlayed = 5;
            deal.Play.Hands[0] = 7;
            var expected = new float[32];
            evaluator.Apply(in deal, 7, expected);
            for (var seat = 1; seat < 4; seat++)
            {
                deal.Play.Hands[seat] = uint.MaxValue;
                deal.LastThree[seat] = uint.MaxValue;
            }

            var actual = new float[32];
            evaluator.Apply(in deal, 7, actual);
            Assert.Equal(expected, actual);
            for (var iteration = 0; iteration < 100; iteration++)
            {
                evaluator.Apply(in deal, 7, actual);
            }

            var indices = new int[FeatureEncoder.MaxActive];
            var features = new float[FeatureEncoder.MaxActive];
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 100; iteration++)
            {
                FeatureEncoder.EncodeCard(in deal, 7, indices, features);
            }

            var encoderBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 100; iteration++)
            {
                evaluator.Apply(in deal, 7, actual);
            }

            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, encoderBytes);
        }

        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.Diamonds)]
        [InlineData(BidType.Hearts)]
        [InlineData(BidType.Spades)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        public void ConfiguredPlayerHasEngineAndSeatViewParity(BidType contract)
        {
            var models = RandomModels.Create(8811);
            var baseline = new ClaudePlayerNeural(models);
            var corrected = new ClaudePlayerNeural(models) { CardCorrectionModel = Model() };
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(8812) });
            var smart = new SmartPlayer.SmartPlayer();
            var late = 0;
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    if (context.RoundNumber > 1)
                    {
                        break;
                    }

                    var bid = context.CurrentContract.Type == BidType.Pass ? contract : BidType.Pass;
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    var viewContext = match.GetView(seat).CreatePlayCardContext();
                    var scores = corrected.EvaluateCards(context);
                    Assert.Equal(scores.Select(score => score.Value), corrected.EvaluateCards(viewContext).Select(score => score.Value));
                    Assert.Equal(corrected.PlayCard(context).Card, corrected.PlayCard(viewContext).Card);
                    if (context.RoundActions.Count() < 16)
                    {
                        Assert.Equal(baseline.EvaluateCards(context).Select(score => score.Value), scores.Select(score => score.Value));
                    }
                    else
                    {
                        late++;
                    }

                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }

            Assert.True(late > 0);
        }

        private static LateCardCorrectionModel Model() => new LateCardCorrectionModel(Network(21), Network(22), Network(23));

        private static NeuralNetwork Network(int tag, int layout = 1, bool zero = false)
        {
            var weights = new[] { new float[600 * 64], new float[64 * 32] };
            var biases = new[] { new float[64], new float[32] };
            if (!zero)
            {
                for (var card = 0; card < 32; card++)
                {
                    biases[1][card] = card;
                }
            }

            return new NeuralNetwork(tag, layout, new[] { 600, 64, 32 }, weights, biases);
        }
    }
}
