namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class OwnershipHistoryTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void PublicChronologyMatchesTheHistoryDatasetAndPreservesAllActorInputs(int contract)
        {
            var checkedDecisions = 0;
            foreach (var position in Positions(contract))
            {
                var context = position.Context;
                var simulator = new BelotSimulator();
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var indices = new int[OwnershipFeatureEncoder.MaxActive];
                var values = new float[OwnershipFeatureEncoder.MaxActive];
                var count = OwnershipFeatureEncoder.Encode(in deal, legal, 2, context, indices, values);
                var originalIndices = new int[FeatureEncoder.MaxActive];
                var originalValues = new float[FeatureEncoder.MaxActive];
                var originalCount = FeatureEncoder.EncodeCard(in deal, legal, originalIndices, originalValues);
                Assert.Equal(originalIndices.Take(originalCount), indices.Take(originalCount));
                Assert.Equal(originalValues.Take(originalCount), values.Take(originalCount));
                Assert.Equal(1, FeatureEncoder.LayoutVersion);
                Assert.Equal(600, FeatureEncoder.CardInputs);
                var cards = context.RoundActions.Select(action => action.Card.GetHashCode()).ToArray();
                Assert.Equal(originalCount + (2 * cards.Length), count);
                var dense = Dense(indices, values, count);
                for (var card = 0; card < 32; card++)
                {
                    var order = Array.IndexOf(cards, card);
                    var rotated = FeatureEncoder.ToNetwork(card, FeatureEncoder.Rotation(deal.Kind));
                    Assert.Equal(order < 0 ? 0 : ((order / 4) + 1) / 8f, dense[600 + rotated]);
                    Assert.Equal(order < 0 ? 0 : ((order % 4) + 1) / 4f, dense[632 + rotated]);
                }

                // The branch wrote sparse history in physical-card order, regardless of play order.
                var expectedOrder = cards.OrderBy(card => card).SelectMany(card => new[]
                {
                    600 + FeatureEncoder.ToNetwork(card, FeatureEncoder.Rotation(deal.Kind)),
                    632 + FeatureEncoder.ToNetwork(card, FeatureEncoder.Rotation(deal.Kind)),
                });
                Assert.Equal(expectedOrder, indices.Skip(originalCount).Take(count - originalCount));
                var viewContext = position.View.CreatePlayCardContext();
                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
                var viewIndices = new int[OwnershipFeatureEncoder.MaxActive];
                var viewValues = new float[OwnershipFeatureEncoder.MaxActive];
                var viewCount = OwnershipFeatureEncoder.Encode(in viewDeal, legal, 2, viewContext, viewIndices, viewValues);
                Assert.Equal(count, viewCount);
                Assert.Equal(indices.Take(count), viewIndices.Take(count));
                Assert.Equal(values.Take(count), viewValues.Take(count));
                checkedDecisions++;
            }

            Assert.True(checkedDecisions >= 8);
        }

        [Fact]
        public void HistoryOwnershipUsesPublicContextAndIgnoresOtherHands()
        {
            var position = Positions(4).First(x => x.Context.RoundActions.Count() >= 12);
            var context = position.Context;
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var model = new CardOwnershipModel(Network(11, 2), Network(12, 2), Network(13, 2));
            var evaluator = model.CreateEvaluator();
            var expected = new float[96];
            evaluator.Evaluate(in deal, legal, expected, context);
            Assert.Contains(expected, value => value > 0.34f);
            var viewContext = position.View.CreatePlayCardContext();
            Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
            var fromView = new float[96];
            evaluator.Evaluate(in viewDeal, legal, fromView, viewContext);
            Assert.Equal(expected, fromView);
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat != deal.Play.Turn)
                {
                    deal.Play.Hands[seat] = uint.MaxValue;
                    deal.LastThree[seat] = uint.MaxValue;
                }
            }

            var changed = new float[96];
            evaluator.Evaluate(in deal, legal, changed, context);
            Assert.Equal(expected, changed);
            var error = Assert.Throws<ArgumentNullException>(() => evaluator.Evaluate(in deal, legal, changed));
            Assert.Contains("layout 2 requires the public", error.Message);
        }

        [Fact]
        public void CheckedOwnershipFilesSupportBothLayoutsAndRejectMixedModels()
        {
            var network = Network(11, 2);
            using var stream = new MemoryStream();
            network.Write(stream);
            var bytes = stream.ToArray();
            Assert.Equal(CardOwnershipModel.HistoryFileBytes, bytes.Length);
            var restored = CardOwnershipModel.Read(new MemoryStream(bytes), 11);
            Assert.Equal(2, restored.Layout);
            Assert.Equal(new[] { 664, 128, 64, 96 }, restored.GetSizes());
            for (var offset = 0; offset < 36; offset += 4)
            {
                var corrupt = (byte[])bytes.Clone();
                BitConverter.GetBytes(999).CopyTo(corrupt, offset);
                Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(corrupt), 11));
            }

            var wrongLayout = (byte[])bytes.Clone();
            BitConverter.GetBytes(1).CopyTo(wrongLayout, 12);
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(wrongLayout), 11));
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes[..^1]), 11));
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes.Concat(new byte[] { 0 }).ToArray()), 11));
            bytes[36] = 0;
            bytes[37] = 0x7e;
            Assert.Throws<InvalidDataException>(() => CardOwnershipModel.Read(new MemoryStream(bytes), 11));
            Assert.Throws<InvalidDataException>(() => new CardOwnershipModel(Network(11, 1), Network(12, 2), Network(13, 1)));
            Assert.Throws<InvalidDataException>(() => new CardOwnershipModel(Network(11, 2), Network(12, 2), Network(13, 1)));
            Assert.Equal(1, new CardOwnershipModel(Network(11, 1), Network(12, 1), Network(13, 1)).Layout);
            Assert.Equal(2, new CardOwnershipModel(Network(11, 2), Network(12, 2), Network(13, 2)).Layout);
        }

        [Fact]
        public void HistoryRejectsMissingRepeatedAndMismatchedPublicCards()
        {
            var context = Positions(1).First(x => x.Context.RoundActions.Count() >= 12).Context;
            Assert.True(NeuralDeal.FromPlayContext(context, new BelotSimulator(), out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var indices = new int[OwnershipFeatureEncoder.MaxActive];
            var values = new float[OwnershipFeatureEncoder.MaxActive];
            Assert.Throws<ArgumentNullException>(() => OwnershipFeatureEncoder.Encode(in deal, legal, 2, null, indices, values));
            Assert.Throws<ArgumentOutOfRangeException>(() => OwnershipFeatureEncoder.Encode(in deal, legal, 3, context, indices, values));
            var actions = context.RoundActions.ToArray();
            context.RoundActions = actions.Skip(1).ToArray();
            Assert.Throws<ArgumentException>(() => OwnershipFeatureEncoder.Encode(in deal, legal, 2, context, indices, values));
            context.RoundActions = actions.Append(actions[0]).ToArray();
            Assert.Throws<ArgumentException>(() => OwnershipFeatureEncoder.Encode(in deal, legal, 2, context, indices, values));
            context.RoundActions = null;
            Assert.Throws<ArgumentException>(() => OwnershipFeatureEncoder.Encode(in deal, legal, 2, context, indices, values));

            // Legacy ownership uses only the unchanged actor features and needs no context.
            var legacyCount = OwnershipFeatureEncoder.Encode(in deal, legal, 1, null, indices, values);
            Assert.All(indices.Take(legacyCount), index => Assert.InRange(index, 0, 599));
        }

        private static NeuralNetwork Network(int tag, int layout)
        {
            var inputs = layout == 2 ? 664 : 600;
            var sizes = new[] { inputs, 128, 64, 96 };
            var weights = new[] { new float[inputs * 128], new float[128 * 64], new float[64 * 96] };
            var biases = new[] { new float[128], new float[64], new float[96] };
            for (var input = 600; input < inputs; input++)
            {
                weights[0][input * 128] = ((input % 7) + 1) * 0.1f;
            }

            weights[1][0] = 1;
            for (var card = 0; card < 32; card++)
            {
                weights[2][card * 3] = 1;
            }

            return new NeuralNetwork(tag, layout, sizes, weights, biases);
        }

        private static float[] Dense(int[] indices, float[] values, int count)
        {
            var result = new float[664];
            for (var i = 0; i < count; i++)
            {
                result[indices[i]] = values[i];
            }

            return result;
        }

        private static IEnumerable<(PlayerPlayCardContext Context, BelotSeatView View)> Positions(int contract)
        {
            var random = new Random(374 + contract);
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(1761 + contract) });
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var view = match.GetView(seat);
                if (view.RoundNumber > 1)
                {
                    match.Stop();
                    yield break;
                }

                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = context.CurrentContract.Type == BidType.Pass ? FeatureEncoder.BidOfIndex(contract) : BidType.Pass;
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    yield return (context, view);
                    var cards = context.AvailableCardsToPlay.ToArray();
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(cards[random.Next(cards.Length)])));
                }
            }
        }
    }
}
