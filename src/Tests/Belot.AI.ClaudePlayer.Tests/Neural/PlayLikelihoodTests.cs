namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class PlayLikelihoodTests
    {
        [Theory]
        [InlineData(BidType.Clubs, 41)]
        [InlineData(BidType.Diamonds, 42)]
        [InlineData(BidType.Hearts, 43)]
        [InlineData(BidType.Spades, 44)]
        [InlineData(BidType.NoTrumps, 45)]
        [InlineData(BidType.AllTrumps, 46)]
        public void HistoricalFeaturesMatchTheEngineAndSeatView(BidType contract, int seed)
        {
            var captured = Capture(contract, seed, 3);
            var byAction = captured.Decisions.ToDictionary(x => (x.Context.RoundNumber, x.ActionIndex));
            var models = RandomModels.Create(seed);
            var historicalDecisions = 0;
            var weightedDecisions = 0;
            foreach (var decision in captured.Decisions)
            {
                var context = decision.Context;
                var actionsBefore = context.RoundActions.ToArray();
                var announcesBefore = context.Announces.ToArray();
                var handBefore = context.MyCards.ToArray();
                var likelihood = new PlayLikelihood();
                var fromView = new PlayLikelihood();
                Assert.True(likelihood.Configure(context, new BelotSimulator(), new NeuralEvaluator(models), 32));
                Assert.True(fromView.Configure(decision.View.CreatePlayCardContext(), new BelotSimulator(), new NeuralEvaluator(models), 32));
                Assert.Equal(likelihood.SelectedActions, fromView.SelectedActions);
                var world = TrueWorld(captured.Record, context);
                var weight = likelihood.Weight(in world);
                Assert.InRange(weight, double.Epsilon, 1);
                Assert.Equal(weight, fromView.Weight(in world));
                weightedDecisions += likelihood.Evaluations;
                for (var i = 0; i < likelihood.SelectedActions; i++)
                {
                    var actionIndex = likelihood.GetActionIndex(i);
                    Assert.Equal(actionIndex, fromView.GetActionIndex(i));
                    Assert.True(likelihood.TryGetDecision(i, in world, out var deal, out var legal, out var card));
                    Assert.NotEqual(context.MyPosition.Index(), deal.Play.Turn);
                    Assert.Equal(actionsBefore[actionIndex].Card.GetHashCode(), card);
                    Assert.NotEqual(0u, legal & (1u << card));
                    Assert.True(deal.Play.TrickCards == 0 || (card >> 3) != deal.Play.LedSuit);
                    if (i > 0)
                    {
                        Assert.True(actionIndex < likelihood.GetActionIndex(i - 1));
                    }

                    for (var seat = 0; seat < 4; seat++)
                    {
                        if (seat != deal.Play.Turn)
                        {
                            Assert.Equal(0u, deal.Play.Hands[seat]);
                        }
                    }

                    if (byAction.TryGetValue((context.RoundNumber, actionIndex), out var historical))
                    {
                        Assert.Equal(NeuralDeal.ToMask(historical.Context.AvailableCardsToPlay), legal);
                        Assert.Equal(historical.Features, Encode(in deal, legal));
                        historicalDecisions++;
                    }
                    else
                    {
                        // The engine advances through a forced card without asking the player.
                        Assert.Equal(1, BitOperations.PopCount(legal));
                    }
                }

                Assert.Equal(actionsBefore, context.RoundActions);
                Assert.Equal(announcesBefore, context.Announces);
                Assert.Equal(handBefore, context.MyCards);
                Assert.Equal(decision.Features, Encode(context));
            }

            Assert.True(historicalDecisions > 50, $"Only {historicalDecisions} historical decisions were checked.");
            Assert.True(weightedDecisions > 50);
        }

        [Fact]
        public void FuturePublicFactsAndCurrentPrivateDataDoNotEnterAnEarlierDecision()
        {
            var captured = Capture(BidType.AllTrumps, 54, 2);
            var decision = captured.Decisions.First(x => x.ActionIndex >= 12 && x.Context.MyPosition != x.Context.FirstToPlayInTheRound);
            var model = new NeuralEvaluator(RandomModels.Create(1));
            var likelihood = new PlayLikelihood();
            Assert.True(likelihood.Configure(decision.Context, new BelotSimulator(), model, 32));
            var selected = Enumerable.Range(0, likelihood.SelectedActions).Single(i => likelihood.GetActionIndex(i) == 0);
            var world = TrueWorld(captured.Record, decision.Context);
            Assert.True(likelihood.TryGetDecision(selected, in world, out var expected, out var expectedLegal, out _));
            var firstSeat = expected.Play.Turn;
            var laterSeat = (firstSeat + 1) & 3;

            // This combination becomes public after the first action, irrespective of its rank.
            // The current caller's cards are also deliberately replaced with unrelated cards.
            var view = decision.View;
            view.Announces = view.Announces.Concat(new[]
            {
                new BelotAnnounce
                {
                    Player = (PlayerPosition)(1 << laterSeat),
                    Type = AnnounceType.FourJacks,
                    Card = Card.AllCards[4],
                    IsScored = true,
                },
            }).ToArray();
            view.Hand = Card.AllCards;
            var changed = new PlayLikelihood();
            Assert.True(changed.Configure(view.CreatePlayCardContext(), new BelotSimulator(), model, 32));
            var changedSelected = Enumerable.Range(0, changed.SelectedActions).Single(i => changed.GetActionIndex(i) == 0);
            var poisoned = world;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat != firstSeat)
                {
                    poisoned.Hands[seat] = uint.MaxValue;
                }
            }

            poisoned.Turn = laterSeat;
            poisoned.SouthNorthPoints = 9876;
            poisoned.EastWestPoints = 5432;
            poisoned.TrickCards = 3;
            poisoned.TricksPlayed = 7;
            Assert.True(changed.TryGetDecision(changedSelected, in poisoned, out var actual, out var actualLegal, out _));
            Assert.Equal(expectedLegal, actualLegal);
            Assert.Equal(Encode(in expected, expectedLegal), Encode(in actual, actualLegal));
            Assert.Equal(0u, actual.Played);
            Assert.Equal(0u, actual.Excluded[laterSeat]);
            Assert.Equal(0u, actual.Known[laterSeat]);
            Assert.Equal(0, actual.Declared[laterSeat]);
            Assert.Equal(0, actual.Play.SouthNorthPoints);
            Assert.Equal(0, actual.Play.EastWestPoints);
        }

        [Fact]
        public void WeightIsTheTemperedProductOfSoftenedPolicyProbabilities()
        {
            var captured = Capture(BidType.Clubs, 57, 2);
            var models = RandomModels.Create(2);
            var likelihood = new PlayLikelihood { Temperature = 3.5, UniformMix = 0.2, Power = 0.7 };
            var reference = new NeuralEvaluator(models);
            var values = new float[32];
            var forced = 0;
            var evaluated = 0;
            foreach (var decision in captured.Decisions.Where(x => x.ActionIndex >= 8))
            {
                Assert.True(likelihood.Configure(decision.Context, new BelotSimulator(), new NeuralEvaluator(models), 32));
                var world = TrueWorld(captured.Record, decision.Context);
                var product = 1.0;
                var expectedEvaluations = 0;
                var expectedForced = 0;
                for (var i = 0; i < likelihood.SelectedActions; i++)
                {
                    Assert.True(likelihood.TryGetDecision(i, in world, out var deal, out var legal, out var observed));
                    var cards = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
                    if (cards.Length == 1)
                    {
                        expectedForced++;
                        continue;
                    }

                    reference.EvaluateCards(in deal, legal, values);
                    var maximum = cards.Max(card => values[card]);
                    var denominator = cards.Sum(card => Math.Exp((values[card] - maximum) / likelihood.Temperature));
                    var probability = Math.Exp((values[observed] - maximum) / likelihood.Temperature) / denominator;
                    product *= (likelihood.UniformMix / cards.Length) + ((1 - likelihood.UniformMix) * probability);
                    expectedEvaluations++;
                }

                Assert.Equal(Math.Pow(product, likelihood.Power), likelihood.Weight(in world), 12);
                Assert.Equal(expectedEvaluations, likelihood.Evaluations);
                Assert.Equal(expectedForced, likelihood.ForcedActions);
                Assert.Equal(0, likelihood.InvalidWorlds);
                evaluated += expectedEvaluations;
                forced += expectedForced;
            }

            Assert.True(evaluated > 50);
            Assert.True(forced > 0);
        }

        [Fact]
        public void ZeroPowerAndUniformPolicyAvoidNeuralEvaluations()
        {
            var captured = Capture(BidType.Spades, 63, 1);
            var decision = captured.Decisions.First(x => x.ActionIndex >= 16);
            var world = TrueWorld(captured.Record, decision.Context);
            var likelihood = new PlayLikelihood { UniformMix = 1, Power = 0.5 };
            Assert.True(likelihood.Configure(decision.Context, new BelotSimulator(), new NeuralEvaluator(RandomModels.Create(3)), 2));
            Assert.Equal(2, likelihood.SelectedActions);
            var product = 1.0;
            for (var i = 0; i < likelihood.SelectedActions; i++)
            {
                Assert.True(likelihood.TryGetDecision(i, in world, out _, out var legal, out _));
                product /= BitOperations.PopCount(legal);
            }

            Assert.Equal(Math.Sqrt(product), likelihood.Weight(in world), 12);
            Assert.Equal(0, likelihood.Evaluations);
            likelihood.Power = 0;
            Assert.Equal(1, likelihood.Weight(in world));
            Assert.Equal(0, likelihood.Evaluations);
            Assert.True(likelihood.Configure(decision.Context, new BelotSimulator(), new NeuralEvaluator(RandomModels.Create(4)), 0));
            likelihood.Power = 0.5;
            Assert.Equal(1, likelihood.Weight(in world));
        }

        [Fact]
        public void InvalidWorldAndConfigurationAreRejectedAndCountersReset()
        {
            var captured = Capture(BidType.NoTrumps, 67, 1);
            var decision = captured.Decisions.First(x => x.ActionIndex >= 12);
            var world = TrueWorld(captured.Record, decision.Context);
            var likelihood = new PlayLikelihood();
            var evaluator = new NeuralEvaluator(RandomModels.Create(5));
            var simulator = new BelotSimulator();
            Assert.True(likelihood.Configure(decision.Context, simulator, evaluator, 1));
            Assert.True(likelihood.TryGetDecision(0, in world, out var deal, out _, out _));
            world.Hands[deal.Play.Turn] = 0;
            Assert.False(likelihood.TryGetDecision(0, in world, out _, out _, out _));
            Assert.Equal(0, likelihood.Weight(in world));
            Assert.Equal(1, likelihood.InvalidWorlds);
            Assert.True(likelihood.Configure(decision.Context, simulator, evaluator, 1));
            Assert.Equal(0, likelihood.InvalidWorlds);
            Assert.Equal(0, likelihood.Evaluations);
            Assert.Equal(0, likelihood.ForcedActions);
            var invalid = decision.View.CreatePlayCardContext();
            invalid.Bids = Array.Empty<Bid>();
            Assert.False(likelihood.Configure(invalid, simulator, evaluator, 2));
            Assert.Equal(0, likelihood.SelectedActions);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.GetActionIndex(0));
        }

        [Fact]
        public void CacheSharesActorHandsAcrossWorldsWithoutChangingWeights()
        {
            var captured = Capture(BidType.NoTrumps, 71, 6);
            var decision = captured.Decisions.First(x => x.ActionIndex == 21);
            var simulator = new BelotSimulator();
            var models = RandomModels.Create(9);
            var cached = new PlayLikelihood();
            var uncached = new PlayLikelihood();
            var evaluator = new NeuralEvaluator(models);
            Assert.True(cached.Configure(decision.Context, simulator, evaluator, 1));
            Assert.Equal(20, cached.GetActionIndex(0));
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(decision.Context, simulator, usePlayInference: false));
            var sampler = new UniformWorldSampler();
            Assert.True(sampler.Configure(knowledge));
            var random = new Random(1021);
            var forwardsWithoutCache = 0;
            for (var sample = 0; sample < 128; sample++)
            {
                var world = knowledge.Root;
                sampler.Sample(ref world, random);
                Assert.True(uncached.Configure(decision.Context, simulator, evaluator, 1));
                Assert.Equal(uncached.Weight(in world), cached.Weight(in world));
                forwardsWithoutCache += uncached.Evaluations;
            }

            // The last lead's actor now holds two of the eight unseen cards: at most C(8, 2)
            // distinct inputs, even though 128 full allocations of the other hands were sampled.
            Assert.Equal(128, forwardsWithoutCache);
            Assert.InRange(cached.Evaluations, 1, 28);
            Assert.Equal(128 - cached.Evaluations, cached.CacheHits);
            var next = captured.Decisions.First(x => x.Context.RoundNumber != decision.Context.RoundNumber && x.ActionIndex >= 12);
            Assert.True(cached.Configure(next.Context, simulator, evaluator, 1));
            Assert.Equal(0, cached.CacheHits);
            Assert.Equal(0, cached.Evaluations);
            Assert.True(uncached.Configure(next.Context, simulator, evaluator, 1));
            var nextWorld = TrueWorld(captured.Record, next.Context);
            Assert.Equal(uncached.Weight(in nextWorld), cached.Weight(in nextWorld));
            Assert.Equal(uncached.Evaluations, cached.Evaluations);
        }

        [Fact]
        public void CacheInvalidatesChangedPolicyParametersAndWeightsAllocateNothing()
        {
            var captured = Capture(BidType.AllTrumps, 73, 1);
            var decision = captured.Decisions.First(x => x.ActionIndex >= 12);
            var world = TrueWorld(captured.Record, decision.Context);
            var models = RandomModels.Create(10);
            var cached = new PlayLikelihood();
            Assert.True(cached.Configure(decision.Context, new BelotSimulator(), new NeuralEvaluator(models), 2));
            var first = cached.Weight(in world);
            var initialEvaluations = cached.Evaluations;
            Assert.Equal(2, initialEvaluations);
            Assert.Equal(first, cached.Weight(in world));
            Assert.Equal(initialEvaluations, cached.Evaluations);
            Assert.Equal(2, cached.CacheHits);

            cached.Temperature = 7;
            cached.UniformMix = 0.3;
            var fresh = new PlayLikelihood { Temperature = 7, UniformMix = 0.3 };
            Assert.True(fresh.Configure(decision.Context, new BelotSimulator(), new NeuralEvaluator(models), 2));
            var expected = fresh.Weight(in world);
            Assert.Equal(expected, cached.Weight(in world));
            Assert.Equal(initialEvaluations + fresh.Evaluations, cached.Evaluations);
            cached.Power = 1;
            fresh.Power = 1;
            Assert.Equal(fresh.Weight(in world), cached.Weight(in world));
            Assert.Equal(initialEvaluations + fresh.Evaluations, cached.Evaluations);

            for (var warmup = 0; warmup < 20; warmup++)
            {
                cached.Weight(in world);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var repetition = 0; repetition < 100; repetition++)
            {
                cached.Weight(in world);
            }

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Fact]
        public void SettingsMustBeFiniteAndInRange()
        {
            var likelihood = new PlayLikelihood();
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.Temperature = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.Temperature = double.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.UniformMix = -0.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.UniformMix = 1.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.UniformMix = double.PositiveInfinity);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.Power = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => likelihood.Power = double.PositiveInfinity);
        }

        private static (List<Decision> Decisions, BelotMatchRecord Record) Capture(BidType contract, int seed, int rounds)
        {
            var random = new Random(seed);
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed), FirstToPlay = (PlayerPosition)(1 << (seed & 3)) });
            var decisions = new List<Decision>();
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var view = match.GetView(seat);
                if (view.RoundNumber > rounds)
                {
                    match.Stop();
                    break;
                }

                switch (match.Decision)
                {
                    case BelotDecision.Bid:
                        var bidding = match.CreateBidContext();
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(bidding.CurrentContract.Type == BidType.Pass ? contract : BidType.Pass)));
                        break;
                    case BelotDecision.Announce:
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                        break;
                    default:
                        var context = match.CreatePlayCardContext();
                        decisions.Add(new Decision
                        {
                            Context = context,
                            View = view,
                            ActionIndex = context.RoundActions.Count(),
                            Features = Encode(context),
                        });
                        var cards = context.AvailableCardsToPlay.ToArray();
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(cards[random.Next(cards.Length)], random.Next(3) != 0)));
                        break;
                }
            }

            return (decisions, match.GetRecord());
        }

        private static SimState TrueWorld(BelotMatchRecord record, PlayerPlayCardContext context)
        {
            var round = record.Rounds.Single(x => x.RoundNumber == context.RoundNumber);
            var world = default(SimState);
            for (var i = 0; i < 32; i++)
            {
                world.Hands[i & 3] |= 1u << round.Deal[i].GetHashCode();
            }

            foreach (var action in context.RoundActions)
            {
                world.Hands[action.Player.Index()] &= ~(1u << action.Card.GetHashCode());
            }

            return world;
        }

        private static float[] Encode(PlayerPlayCardContext context)
        {
            Assert.True(NeuralDeal.FromPlayContext(context, new BelotSimulator(), out var deal));
            return Encode(in deal, NeuralDeal.ToMask(context.AvailableCardsToPlay));
        }

        private static float[] Encode(in NeuralDeal deal, uint legal)
        {
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var features = new float[FeatureEncoder.CardInputs];
            var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            for (var i = 0; i < count; i++)
            {
                features[indices[i]] = values[i];
            }

            return features;
        }

        private sealed class Decision
        {
            public PlayerPlayCardContext Context { get; set; }

            public BelotSeatView View { get; set; }

            public int ActionIndex { get; set; }

            public float[] Features { get; set; }
        }
    }
}
