namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.NeuralTrainer;

    using Xunit;

    public class ConfiguredSearchDistillationTests
    {
        [Theory]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        [InlineData(BidType.Spades)]
        public void EndgameLabelsUseTheFullConfiguredSampledTeacher(BidType contract)
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-distill-ownership-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                SaveOwnership(directory);
                var settings = new TrainingSettings
                {
                    Teacher = "endgame",
                    SearchDeals = 7,
                    Endgame = false,
                    EndgameDeclarations = true,
                    EndgameTricks = 4,
                    EndgameWorlds = 1,
                    EndgameSampledWorlds = 16,
                    EndgameNodes = 100000,
                    EndgamePruning = true,
                    EndgameTranspositions = true,
                    EndgamePolicyActions = 2,
                    EndgamePolicyTemperature = 1.7,
                    EndgamePolicyUniformMix = 0.2,
                    EndgamePolicyPower = 0.6,
                    EndgameOwnership = directory,
                    EndgameOwnershipPower = 0.7,
                    EndgameOwnershipUniformMix = 0.15,
                    CardLabelChance = 1,
                };
                var context = Position(contract);
                var models = RandomModels.Create(935);
                var teacher = OpponentCatalog.Configured(settings, models, 936);
                teacher.UseEndgameSearch = true;
                teacher.SearchDeals = 0;
                CheckLabels(context, models, settings, teacher, new RejectBatchedPolicy());
                Assert.Equal(1, teacher.EndgameDecisions);
                Assert.Equal(settings.EndgameSampledWorlds, teacher.EndgameWorlds);
                Assert.True(teacher.EndgameSampleAttempts >= settings.EndgameSampledWorlds);
                Assert.True(teacher.EndgameLikelihoodEvaluations > 0);
                Assert.True(teacher.EndgameTranspositionProbes > 0);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Theory]
        [InlineData("plain")]
        [InlineData("prior")]
        [InlineData("prune")]
        [InlineData("time")]
        [InlineData("control")]
        [InlineData("rollout")]
        [InlineData("double-dummy")]
        [InlineData("declarations")]
        public void RolloutLabelsHonorConfiguredOptionsAndOnlyBatchPlainSearch(string option)
        {
            var settings = new TrainingSettings
            {
                Teacher = "neural",
                SearchDeals = 3,
                Endgame = true,
                EndgameTricks = 4,
                EndgameSampledWorlds = 16,
                CardLabelChance = 1,
            };
            switch (option)
            {
                case "prior": settings.SearchPriorDeals = 2; break;
                case "prune": settings.SearchPruneMargin = 1; break;
                case "time": settings.SearchMilliseconds = 100000; break;
                case "control": settings.SearchControlVariateDeals = 4; break;
                case "rollout": settings.SearchRolloutTricks = 1; break;
                case "double-dummy": settings.SearchDoubleDummyTricks = 2; break;
                case "declarations": settings.SearchDeclarations = true; break;
            }

            var models = RandomModels.Create(935);
            var teacher = OpponentCatalog.Configured(settings, models, 936);
            teacher.UseEndgameSearch = false;
            IBatchedCardPolicy policy = option == "plain" ? new ManagedBatchedCardPolicy(models) : new RejectBatchedPolicy();
            CheckLabels(Position(BidType.NoTrumps), models, settings, teacher, policy);
            Assert.Equal(0, teacher.EndgameDecisions);
        }

        private static void CheckLabels(
            PlayerPlayCardContext context, NeuralModels models, TrainingSettings settings, ClaudePlayerNeural teacher, IBatchedCardPolicy policy)
        {
            var buffers = Enumerable.Range(0, 4).Select(tag => new SampleBuffer(1, tag == 0 ? 9 : 32)).ToArray();
            var recorder = new SearchDistillPlayer(models, buffers, settings, 936, policy);
            var scores = teacher.EvaluateCards(context);
            var action = recorder.PlayCard(context);
            Assert.Equal(scores.OrderByDescending(score => score.Value).ThenBy(score => score.Card.GetHashCode()).First().Card, action.Card);
            Assert.True(NeuralDeal.FromPlayContext(context, new BelotSimulator(), out var deal));
            var buffer = buffers[1 + FeatureEncoder.CardNetwork(deal.Kind)];
            Assert.Equal(1, buffer.Written);
            Assert.Equal(1, buffers.Sum(item => item.Written));
            var sample = new Batch(1, 32);
            buffer.Take(sample, new[] { 0 });
            var mask = 0u;
            foreach (var score in scores)
            {
                var output = FeatureEncoder.ToNetwork(score.Card.GetHashCode(), FeatureEncoder.Rotation(deal.Kind));
                mask |= 1u << output;
                Assert.Equal((float)(Half)((float)score.Value / NeuralEvaluator.ValueScale), sample.Labels[output]);
            }

            Assert.Equal(mask, sample.Masks[0]);
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var count = FeatureEncoder.EncodeCard(in deal, NeuralDeal.ToMask(context.AvailableCardsToPlay), indices, values);
            Assert.Equal(count, sample.FeatureCounts[0]);
            for (var index = 0; index < count; index++)
            {
                Assert.Equal(indices[index], sample.Indices[index]);
                Assert.Equal((float)(Half)values[index], sample.Values[index]);
            }
        }

        private static PlayerPlayCardContext Position(BidType contract)
        {
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(937) });
            var smart = new SmartPlayer.SmartPlayer();
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    Assert.Equal(1, context.RoundNumber);
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
                    if (context.RoundActions.Count() >= 16 && context.AvailableCardsToPlay.Count > 1)
                    {
                        return context;
                    }

                    var action = smart.PlayCard(context);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(action.Card, action.Belote)));
                }
            }

            throw new InvalidOperationException("No non-forced ending found.");
        }

        private static void SaveOwnership(string directory)
        {
            var names = new[] { "trump.bin", "notrumps.bin", "alltrumps.bin" };
            for (var index = 0; index < names.Length; index++)
            {
                var sizes = new[] { 600, 128, 64, 96 };
                var weights = new[] { new float[600 * 128], new float[128 * 64], new float[64 * 96] };
                var biases = new[] { new float[128], new float[64], new float[96] };
                for (var output = 0; output < 96; output++)
                {
                    biases[2][output] = ((output * 17) % 11) * 0.4f;
                }

                using var stream = File.Create(Path.Combine(directory, names[index]));
                new NeuralNetwork(11 + index, 1, sizes, weights, biases).Write(stream);
            }
        }

        private sealed class RejectBatchedPolicy : IBatchedCardPolicy
        {
            public void Choose(NeuralDeal[] states, int[] slots, uint[] legal, int count, int[] chosen) =>
                throw new InvalidOperationException("Configured search cannot use the plain-rollout batch path.");
        }
    }
}
