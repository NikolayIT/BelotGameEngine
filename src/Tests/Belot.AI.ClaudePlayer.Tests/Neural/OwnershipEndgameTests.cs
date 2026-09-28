namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class OwnershipEndgameTests
    {
        [Fact]
        public void ExactWorldsUseTheProductPosteriorOnce()
        {
            var ownership = Model().CreateEvaluator();
            var search = new EndgameSearch { Ownership = ownership, Tricks = 2 };
            var checkedPositions = 0;
            Positions((context, deal, simulator, legal) =>
            {
                if (deal.Play.TricksPlayed != 6 || checkedPositions >= 12)
                {
                    return;
                }

                var actual = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, simulator, actual));
                var knowledge = new RoundKnowledge();
                Assert.True(knowledge.Build(context, simulator, true));
                var weights = Weights(ownership, in deal, legal);
                var cards = Enumerable.Range(0, 32).Where(card => ((knowledge.MyHand | knowledge.Played) & (1u << card)) == 0).ToArray();
                var sums = new double[32];
                var mass = 0d;
                var worlds = 0;
                for (var assignment = 0; assignment < (int)Math.Pow(3, cards.Length); assignment++)
                {
                    var state = knowledge.Root;
                    var remaining = assignment;
                    for (var index = 0; index < cards.Length; index++)
                    {
                        state.Hands[(knowledge.Me + 1 + (remaining % 3)) & 3] |= 1u << cards[index];
                        remaining /= 3;
                    }

                    var valid = true;
                    var weight = 1d;
                    for (var seat = 0; seat < 4; seat++)
                    {
                        valid &= BitOperations.PopCount(state.Hands[seat]) == knowledge.HandCounts[seat]
                            && (state.Hands[seat] & knowledge.Excluded[seat]) == 0
                            && (state.Hands[seat] & knowledge.Known[seat]) == knowledge.Known[seat];
                        if (seat != knowledge.Me)
                        {
                            var relative = (seat - knowledge.Me + 4) & 3;
                            foreach (var card in cards)
                            {
                                if ((state.Hands[seat] & ~knowledge.Known[seat] & (1u << card)) != 0)
                                {
                                    weight *= weights[(card * 3) + relative - 1];
                                }
                            }
                        }
                    }

                    if (!valid)
                    {
                        continue;
                    }

                    worlds++;
                    mass += weight;
                    AddReturns(in state, simulator, legal, knowledge.Me & 1, weight, sums);
                }

                Assert.Equal(worlds, search.Worlds);
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    Assert.Equal((float)(sums[card] / mass), actual[card], 5);
                }

                checkedPositions++;
            });
            Assert.True(checkedPositions >= 6);
        }

        [Fact]
        public void SampledWorldsAreNotWeightedAgainAndZeroPowerKeepsRandomStream()
        {
            var ownership = Model().CreateEvaluator();
            var search = new EndgameSearch { Ownership = ownership, Tricks = 3, ThreeTrickWorldLimit = 1, SampledWorlds = 128 };
            var zero = new EndgameSearch { Ownership = ownership, OwnershipPower = 0, Tricks = 3, ThreeTrickWorldLimit = 1, SampledWorlds = 128 };
            var uniform = new EndgameSearch { Tricks = 3, ThreeTrickWorldLimit = 1, SampledWorlds = 128 };
            var checkedPositions = 0;
            Positions((context, deal, simulator, legal) =>
            {
                if (deal.Play.TricksPlayed != 5 || checkedPositions >= 8)
                {
                    return;
                }

                var random = new Random(912);
                var actual = new float[32];
                Assert.True(search.Evaluate(context, in deal, legal, simulator, actual, random));
                if (search.SampleAttempts == 0)
                {
                    return;
                }

                var knowledge = new RoundKnowledge();
                Assert.True(knowledge.Build(context, simulator, true));
                var sampler = new WeightedWorldSampler();
                Assert.True(sampler.Configure(knowledge, Weights(ownership, in deal, legal)));
                var referenceRandom = new Random(912);
                var sums = new double[32];
                for (var sample = 0; sample < 128; sample++)
                {
                    var state = knowledge.Root;
                    sampler.Sample(ref state, referenceRandom);
                    AddReturns(in state, simulator, legal, knowledge.Me & 1, 1, sums);
                }

                Assert.Equal(128, search.Worlds);
                Assert.Equal(referenceRandom.Next(), random.Next());
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    Assert.Equal((float)(sums[card] / 128), actual[card]);
                }

                var zeroValues = new float[32];
                var uniformValues = new float[32];
                var zeroRandom = new Random(913);
                var uniformRandom = new Random(913);
                Assert.True(zero.Evaluate(context, in deal, legal, simulator, zeroValues, zeroRandom));
                Assert.True(uniform.Evaluate(context, in deal, legal, simulator, uniformValues, uniformRandom));
                Assert.Equal(uniformValues, zeroValues);
                Assert.Equal(uniformRandom.Next(), zeroRandom.Next());
                checkedPositions++;
            });
            Assert.True(checkedPositions >= 4);
        }

        private static void AddReturns(in SimState state, BelotSimulator simulator, uint legal, int team, double weight, double[] sums)
        {
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var copy = state;
                simulator.Play(ref copy, card, legal);
                sums[card] += weight * EndgameSearch.Solve(in copy, simulator, team, 0, 0);
            }
        }

        private static float[] Weights(CardOwnershipModel.Evaluator ownership, in NeuralDeal deal, uint legal)
        {
            var result = new float[96];
            ownership.Evaluate(in deal, legal, result);
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = (float)Math.Sqrt((0.1 / 3) + (0.9 * result[index]));
            }

            return result;
        }

        private static CardOwnershipModel Model()
        {
            var sizes = new[] { 600, 128, 64, 96 };
            var weights = new[] { new float[600 * 128], new float[128 * 64], new float[64 * 96] };
            var biases = new[] { new float[128], new float[64], new float[96] };
            for (var index = 0; index < 96; index++)
            {
                biases[2][index] = ((index * 17) % 11) * 0.4f;
            }

            return new CardOwnershipModel(
                new NeuralNetwork(11, 1, sizes, weights, biases),
                new NeuralNetwork(12, 1, sizes, weights, biases),
                new NeuralNetwork(13, 1, sizes, weights, biases));
        }

        private static void Positions(Action<PlayerPlayCardContext, NeuralDeal, BelotSimulator, uint> check)
        {
            var smart = new SmartPlayer.SmartPlayer();
            var simulator = new BelotSimulator();
            for (var seed = 0; seed < 12; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(920 + seed) });
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
                        Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                        check(context, deal, simulator, NeuralDeal.ToMask(context.AvailableCardsToPlay));
                        var action = smart.PlayCard(context);
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                    }
                }
            }
        }
    }
}
