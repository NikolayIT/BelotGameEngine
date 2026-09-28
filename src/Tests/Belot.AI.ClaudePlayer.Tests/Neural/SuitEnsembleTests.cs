namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.NeuralTrainer;

    using Xunit;

    public class SuitEnsembleTests
    {
        [Theory]
        [InlineData(5, 0, 24)]
        [InlineData(4, 0x40, 24)]
        [InlineData(4, 1, 6)]
        [InlineData(4, 5, 2)]
        [InlineData(4, 7, 1)]
        [InlineData(2, 0, 6)]
        [InlineData(2, 4, 6)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 3, 1)]
        public void GroupFixesTrumpAndEverySuitBidByAnySeat(int kind, int bids, int expected)
        {
            var deal = Position(kind, bids);
            var maps = new byte[96];
            var count = SuitEnsembleEvaluator.Permutations(in deal, maps);
            Assert.Equal(expected, count);
            Assert.Equal(new byte[] { 0, 1, 2, 3 }, maps.Take(4));
            var fixedMask = FixedMask(kind, bids);
            var reference = Permutations(fixedMask).Select(map => string.Join(',', map)).ToArray();
            var actual = Enumerable.Range(0, count).Select(index => string.Join(',', maps.Skip(index * 4).Take(4))).ToArray();
            Assert.Equal(reference, actual);
            Assert.Equal(count, actual.Distinct().Count());
            Assert.Throws<ArgumentException>(() => SuitEnsembleEvaluator.Permutations(in deal, new byte[95]));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 0)]
        [InlineData(5, 0)]
        public void ValuesMatchAnIndependentDensePermutationAverageForAllSixContracts(int kind, int bids)
        {
            var deal = Position(kind, bids);
            var models = RandomModels.Create(7195);
            var legal = deal.Play.Hands[0];
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            var dense = new float[600];
            for (var feature = 0; feature < count; feature++)
            {
                dense[indices[feature]] = values[feature];
            }

            for (var plane = 0; plane < 16; plane++)
            {
                Assert.Contains(dense.Skip(plane * 32).Take(32), value => value != 0);
            }

            var sums = new double[32];
            var group = Permutations(FixedMask(kind, bids)).ToArray();
            var rotation = FeatureEncoder.Rotation(kind);
            foreach (var map in group)
            {
                var mapped = (float[])dense.Clone();
                for (var plane = 0; plane < 16; plane++)
                {
                    for (var card = 0; card < 32; card++)
                    {
                        mapped[(plane * 32) + (map[card / 8] * 8) + (card % 8)] = dense[(plane * 32) + card];
                    }
                }

                Assert.Equal(dense.Skip(512), mapped.Skip(512));
                var active = Enumerable.Range(0, 600).Where(index => mapped[index] != 0).ToArray();
                var outputs = new float[32];
                models.Cards(kind).Forward(active, active.Select(index => mapped[index]).ToArray(), outputs);
                foreach (var card in Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0))
                {
                    var canonical = FeatureEncoder.ToNetwork(card, rotation);
                    sums[card] += outputs[(map[canonical / 8] * 8) + (canonical % 8)];
                }
            }

            var actual = Enumerable.Repeat(923f, 32).ToArray();
            var evaluator = new SuitEnsembleEvaluator();
            evaluator.EvaluateCards(in deal, legal, new NeuralEvaluator(models), actual);
            Assert.Equal(group.Length, evaluator.Views);
            for (var card = 0; card < 32; card++)
            {
                if ((legal & (1u << card)) == 0)
                {
                    Assert.Equal(923f, actual[card]);
                }
                else
                {
                    var expected = (float)(sums[card] / group.Length) * NeuralEvaluator.ValueScale;
                    Assert.InRange(Math.Abs(expected - actual[card]), 0, 0.0005f);
                }
            }
        }

        [Fact]
        public void IdentityOnlyKeepsBaselineBitsAndHiddenHandsCannotAffectTheAverage()
        {
            var models = RandomModels.Create(7196);
            var evaluator = new NeuralEvaluator(models);
            var ensemble = new SuitEnsembleEvaluator();
            var deal = Position(5, 15);
            var legal = deal.Play.Hands[0];
            var expected = Enumerable.Repeat(-923f, 32).ToArray();
            var actual = (float[])expected.Clone();
            evaluator.EvaluateCards(in deal, legal, expected);
            ensemble.EvaluateCards(in deal, legal, evaluator, actual);
            Assert.Equal(1, ensemble.Views);
            Assert.Equal(expected.Select(BitConverter.SingleToInt32Bits), actual.Select(BitConverter.SingleToInt32Bits));
            deal = Position(5, 0);
            ensemble.EvaluateCards(in deal, legal, evaluator, expected);
            for (var seat = 1; seat < 4; seat++)
            {
                deal.Play.Hands[seat] = uint.MaxValue;
                deal.LastThree[seat] = uint.MaxValue;
            }

            ensemble.EvaluateCards(in deal, legal, evaluator, actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.Diamonds)]
        [InlineData(BidType.Hearts)]
        [InlineData(BidType.Spades)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        public void EnabledPlayerPreservesViewParityBiddingAndRandomState(BidType contract)
        {
            var models = RandomModels.Create(7197);
            var baseline = new ClaudePlayerNeural(models);
            var ensemble = new ClaudePlayerNeural(models) { CardSuitEnsemble = true, Rng = new Random(7198) };
            var expectedRandom = new Random(7198);
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(7199) });
            var smart = new SmartPlayer.SmartPlayer();
            var decisions = 0;
            var endgames = 0;
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

                    Assert.Equal(baseline.EvaluateBids(context).Select(value => value.Value), ensemble.EvaluateBids(context).Select(value => value.Value));
                    Assert.Equal(baseline.GetBid(context), ensemble.GetBid(context));
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
                    var view = match.GetView(seat).CreatePlayCardContext();
                    var scores = ensemble.EvaluateCards(context);
                    Assert.Equal(scores.Select(value => value.Value), ensemble.EvaluateCards(view).Select(value => value.Value));
                    Assert.Equal(ensemble.PlayCard(context).Card, ensemble.PlayCard(view).Card);
                    var disabled = new ClaudePlayerNeural(models) { CardSuitEnsemble = false };
                    Assert.Equal(baseline.EvaluateCards(context).Select(value => value.Value), disabled.EvaluateCards(context).Select(value => value.Value));
                    if (decisions++ == 0)
                    {
                        var search = new ClaudePlayerNeural(models) { SearchDeals = 2, Rng = new Random(7200) };
                        var searchEnsemble = new ClaudePlayerNeural(models) { SearchDeals = 2, CardSuitEnsemble = true, Rng = new Random(7200) };
                        Assert.Equal(search.EvaluateCards(context).Select(value => value.Value), searchEnsemble.EvaluateCards(context).Select(value => value.Value));
                        Assert.Equal(search.Rng.Next(), searchEnsemble.Rng.Next());
                    }

                    if (context.RoundActions.Count() >= 24)
                    {
                        var search = new ClaudePlayerNeural(models) { UseEndgameSearch = true };
                        var searchEnsemble = new ClaudePlayerNeural(models) { UseEndgameSearch = true, CardSuitEnsemble = true };
                        var values = search.EvaluateCards(context);
                        if (search.EndgameDecisions > 0)
                        {
                            Assert.Equal(values.Select(value => value.Value), searchEnsemble.EvaluateCards(context).Select(value => value.Value));
                            Assert.Equal(1, searchEnsemble.EndgameDecisions);
                            endgames++;
                        }
                    }

                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }

            Assert.True(decisions > 5);
            Assert.True(endgames > 0);
            Assert.Equal(expectedRandom.Next(), ensemble.Rng.Next());
        }

        [Fact]
        public void OptionDefaultsOffAndConfiguredTrainerPreservesIt()
        {
            Assert.False(new ClaudePlayerNeural(RandomModels.Create(7201)).CardSuitEnsemble);
            Assert.False(new TrainingSettings().CardSuitEnsemble);
            var settings = TrainingSettings.Parse(new[] { "--card-suit-ensemble", "true" }, new TrainingSettings());
            Assert.True(OpponentCatalog.Configured(settings, RandomModels.Create(7202), 7203).CardSuitEnsemble);
        }

        private static int FixedMask(int kind, int bids)
        {
            var rotation = kind < 4 ? kind : 0;
            var fixedMask = kind < 4 ? 1 : 0;
            for (var suit = 0; suit < 4; suit++)
            {
                if ((bids & (1 << suit)) != 0)
                {
                    fixedMask |= 1 << ((suit - rotation + 4) % 4);
                }
            }

            return fixedMask;
        }

        private static IEnumerable<int[]> Permutations(int fixedMask)
        {
            for (var packed = 0; packed < 256; packed++)
            {
                var map = new[] { (packed / 64) % 4, (packed / 16) % 4, (packed / 4) % 4, packed % 4 };
                if (map.Distinct().Count() == 4 && Enumerable.Range(0, 4).All(suit => (fixedMask & (1 << suit)) == 0 || map[suit] == suit))
                {
                    yield return map;
                }
            }
        }

        private static NeuralDeal Position(int kind, int bids)
        {
            var deal = default(NeuralDeal);
            deal.Kind = kind;
            deal.Contract = (BidType)(1 << kind);
            deal.Declarer = 1;
            deal.Play.Hands[0] = 1u | (1u << 8) | (1u << 16) | (1u << 24);
            deal.Play.TricksPlayed = 4;
            deal.Play.TrickCards = 3;
            deal.Play.WinnerSeat = 2;
            deal.Play.WinnerCard = 11;
            deal.PlayedBy[0] = 1u << 25;
            for (var seat = 1; seat < 4; seat++)
            {
                var suit = seat - 1;
                var current = (suit * 8) + 3;
                deal.TrickCards[seat] = (byte)(current + 1);
                deal.PlayedBy[seat] = (1u << ((suit * 8) + 1)) | (1u << current);
                deal.Excluded[seat] = 1u << ((suit * 8) + 6);
                deal.Known[seat] = 1u << ((suit * 8) + 4);
            }

            for (var seat = 0; seat < 4; seat++)
            {
                deal.Played |= deal.PlayedBy[seat];
                deal.BidsMade[seat] = (byte)(bids & (1 << seat));
            }

            deal.BidsMade[0] |= (byte)(bids & ~15);
            return deal;
        }
    }
}
