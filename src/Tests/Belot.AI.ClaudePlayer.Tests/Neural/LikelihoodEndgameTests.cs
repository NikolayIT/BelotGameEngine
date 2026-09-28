namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class LikelihoodEndgameTests
    {
        [Theory]
        [InlineData(1, 2.0, 0.1, 0.5)]
        [InlineData(2, 4.0, 0.2, 1.0)]
        public void WeightedValuesMatchIndependentlySampledAndSolvedWorlds(int actions, double temperature, double uniformMix, double power)
        {
            var models = RandomModels.Create(401);
            var checkedPositions = 0;
            foreach (var position in Positions().Take(3))
            {
                var context = position.Context;
                var simulator = new BelotSimulator();
                Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var search = Search(actions, temperature, uniformMix, power);
                var values = Enumerable.Repeat(923f, 32).ToArray();
                Assert.True(search.Evaluate(context, in deal, legal, simulator, values, new Random(149), new NeuralEvaluator(models)));
                var expected = Reference(context, simulator, legal, models, search, 149);
                Assert.Equal(expected.Values, values);
                Assert.Equal(search.SampledWorlds, search.Worlds);
                Assert.Equal(search.SampledWorlds, search.SampleAttempts);
                Assert.Equal(expected.Evaluations, search.LikelihoodEvaluations);
                Assert.Equal(expected.EffectiveWorlds, search.EffectiveWorlds, 12);
                Assert.InRange(search.EffectiveWorlds, double.Epsilon, search.Worlds + 0.000000001);
                Assert.True(search.LikelihoodEvaluations > 0);
                for (var card = 0; card < 32; card++)
                {
                    if ((legal & (1u << card)) != 0)
                    {
                        Assert.NotEqual(923f, values[card]);
                        Assert.True(float.IsFinite(values[card]));
                    }
                    else
                    {
                        Assert.Equal(923f, values[card]);
                    }
                }

                var viewContext = position.View.CreatePlayCardContext();
                Assert.True(NeuralDeal.FromPlayContext(viewContext, simulator, out var viewDeal));
                var viewValues = Enumerable.Repeat(923f, 32).ToArray();
                Assert.True(search.Evaluate(viewContext, in viewDeal, legal, simulator, viewValues, new Random(149), new NeuralEvaluator(models)));
                Assert.Equal(values, viewValues);
                Assert.Equal(expected.EffectiveWorlds, search.EffectiveWorlds, 12);
                checkedPositions++;
            }

            Assert.Equal(3, checkedPositions);
        }

        [Fact]
        public void ZeroPowerPreservesUniformValuesAndRandomStreamExactly()
        {
            var models = RandomModels.Create(402);
            foreach (var position in Positions().Take(3))
            {
                var simulator = new BelotSimulator();
                Assert.True(NeuralDeal.FromPlayContext(position.Context, simulator, out var deal));
                var legal = NeuralDeal.ToMask(position.Context.AvailableCardsToPlay);
                var uniform = Search(0, 2, 0.1, 0.5);
                var disabled = Search(2, 2, 0.1, 0);
                var uniformValues = new float[32];
                var disabledValues = new float[32];
                var uniformRandom = new Random(351);
                var disabledRandom = new Random(351);
                Assert.True(uniform.Evaluate(position.Context, in deal, legal, simulator, uniformValues, uniformRandom));
                Assert.True(disabled.Evaluate(position.Context, in deal, legal, simulator, disabledValues, disabledRandom, new NeuralEvaluator(models)));
                Assert.Equal(uniformValues, disabledValues);
                Assert.Equal(uniform.Worlds, disabled.Worlds);
                Assert.Equal(uniform.Nodes, disabled.Nodes);
                Assert.Equal(uniform.EffectiveWorlds, disabled.EffectiveWorlds);
                Assert.Equal(0, disabled.LikelihoodEvaluations);
                Assert.Equal(uniformRandom.NextInt64(), disabledRandom.NextInt64());
            }
        }

        [Fact]
        public void InterruptedWeightedWorldDoesNotChangeCompletedAverageOrEffectiveCount()
        {
            var context = Positions().First().Context;
            var simulator = new BelotSimulator();
            var evaluator = new NeuralEvaluator(RandomModels.Create(403));
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = Search(2, 2, 0.1, 0.5);
            search.SampledWorlds = 1;
            var oneWorld = new float[32];
            Assert.True(search.Evaluate(context, in deal, legal, simulator, oneWorld, new Random(672), evaluator));
            Assert.True(search.Nodes > 1);
            Assert.Equal(1, search.EffectiveWorlds, 12);
            search.NodeLimit = search.Nodes + 1;
            search.SampledWorlds = 8;
            var partial = new float[32];
            Assert.True(search.Evaluate(context, in deal, legal, simulator, partial, new Random(672), evaluator));
            Assert.Equal(1, search.Worlds);
            Assert.Equal(1, search.IncompleteWorlds);
            Assert.Equal(1, search.EffectiveWorlds, 12);
            Assert.Equal(oneWorld, partial);
        }

        [Fact]
        public void MissingPolicyEvaluatorLeavesValuesUnchanged()
        {
            var context = Positions().First().Context;
            var simulator = new BelotSimulator();
            Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var search = Search(1, 2, 0.1, 0.5);
            var values = Enumerable.Repeat(923f, 32).ToArray();
            Assert.False(search.Evaluate(context, in deal, legal, simulator, values, new Random(89)));
            Assert.All(values, value => Assert.Equal(923f, value));
            Assert.Equal(0, search.Worlds);
            Assert.Equal(0, search.Nodes);
        }

        [Fact]
        public void PublicPolicySettingsHaveSafeDefaultsAndRejectInvalidValues()
        {
            var player = new ClaudePlayerNeural(RandomModels.Create(404));
            Assert.Equal(0, player.EndgamePolicyActions);
            Assert.Equal(2, player.EndgamePolicyTemperature);
            Assert.Equal(0.1, player.EndgamePolicyUniformMix);
            Assert.Equal(0.5, player.EndgamePolicyPower);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyActions = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyActions = 33);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyTemperature = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyTemperature = double.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyUniformMix = -0.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyUniformMix = 1.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyUniformMix = double.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyPower = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => player.EndgamePolicyPower = double.PositiveInfinity);
            player.EndgamePolicyActions = 32;
            player.EndgamePolicyTemperature = 4;
            player.EndgamePolicyUniformMix = 1;
            player.EndgamePolicyPower = 0;
            Assert.Equal(32, player.EndgamePolicyActions);
            Assert.Equal(4, player.EndgamePolicyTemperature);
            Assert.Equal(1, player.EndgamePolicyUniformMix);
            Assert.Equal(0, player.EndgamePolicyPower);
        }

        private static EndgameSearch Search(int actions, double temperature, double uniformMix, double power) =>
            new EndgameSearch
            {
                Tricks = 4,
                UseDeclarations = true,
                SampledWorlds = 8,
                PruneEquivalentCards = true,
                PolicyActions = actions,
                PolicyTemperature = temperature,
                PolicyUniformMix = uniformMix,
                PolicyPower = power,
            };

        private static (float[] Values, double EffectiveWorlds, int Evaluations) Reference(
            PlayerPlayCardContext context, BelotSimulator simulator, uint legal, NeuralModels models, EndgameSearch search, int seed)
        {
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(context, simulator, usePlayInference: true));
            Assert.Equal(SimTables.NoTrumps, simulator.Kind);
            var sampler = new UniformWorldSampler();
            Assert.True(sampler.Configure(knowledge));
            var likelihood = new PlayLikelihood
            {
                Temperature = search.PolicyTemperature,
                UniformMix = search.PolicyUniformMix,
                Power = search.PolicyPower,
            };
            Assert.True(likelihood.Configure(context, simulator, new NeuralEvaluator(models), search.PolicyActions));
            var random = new Random(seed);
            var sums = new double[32];
            var weightSum = 0.0;
            var squaredWeightSum = 0.0;
            for (var sample = 0; sample < search.SampledWorlds; sample++)
            {
                var state = knowledge.Root;
                sampler.Sample(ref state, random);
                var weight = likelihood.Weight(in state);
                Assert.True(weight > 0);
                weightSum += weight;
                squaredWeightSum += weight * weight;
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var child = state;
                    simulator.Play(ref child, card, legal);
                    sums[card] += weight * EndgameSearch.Solve(in child, simulator, knowledge.Me & 1, 0, 0);
                }
            }

            var values = Enumerable.Repeat(923f, 32).ToArray();
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                values[card] = (float)(sums[card] / weightSum);
            }

            return (values, (weightSum * weightSum) / squaredWeightSum, likelihood.Evaluations);
        }

        private static IEnumerable<(PlayerPlayCardContext Context, BelotSeatView View)> Positions()
        {
            var smart = new SmartPlayer.SmartPlayer();
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(2451) });
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = context.CurrentContract.Type == BidType.Pass ? BidType.NoTrumps : BidType.Pass;
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bid)));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    if (context.RoundActions.Count() / 4 == 4)
                    {
                        yield return (context, match.GetView(seat));
                    }

                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }
        }
    }
}
