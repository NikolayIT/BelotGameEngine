namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.NeuralTrainer;

    using Xunit;

    public class SearchDistillationTests
    {
        [Fact]
        public void RecordsExactlyThePublicSearchValuesAndSeatFeatures()
        {
            var models = RandomModels.Create(27);
            var settings = new TrainingSettings { SearchDeals = 3, CardLabelChance = 1 };
            var buffers = Enumerable.Range(0, 4).Select(tag => new SampleBuffer(1000, tag == 0 ? 9 : 32)).ToArray();
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(28) });
            var ordinary = new SmartPlayer.SmartPlayer();
            var simulator = new BelotSimulator();
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var checkedSamples = 0;
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.Bid)
                {
                    match.Act(seat, BelotAction.Bid(ordinary.GetBid(match.CreateBidContext())));
                }
                else if (match.Decision == BelotDecision.Announce)
                {
                    match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces));
                }
                else
                {
                    var context = match.CreatePlayCardContext();
                    var recorder = new SearchDistillPlayer(models, buffers, settings, checkedSamples);
                    var teacher = new ClaudePlayerNeural(models) { SearchDeals = 3, Rng = new Random(checkedSamples) };
                    var scores = teacher.EvaluateCards(context);
                    var chosen = recorder.PlayCard(context);
                    var expected = scores.OrderByDescending(x => x.Value).ThenBy(x => x.Card.GetHashCode()).First();
                    Assert.Equal(expected.Card, chosen.Card);
                    Assert.True(NeuralDeal.FromPlayContext(context, simulator, out var deal));
                    var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                    var rotation = FeatureEncoder.Rotation(deal.Kind);
                    var buffer = buffers[1 + FeatureEncoder.CardNetwork(deal.Kind)];
                    var sample = new Batch(1, 32);
                    buffer.Take(sample, new[] { buffer.Count - 1 });
                    var mask = 0u;
                    foreach (var score in scores)
                    {
                        var output = FeatureEncoder.ToNetwork(score.Card.GetHashCode(), rotation);
                        mask |= 1u << output;
                        Assert.Equal((float)(Half)(score.Value / NeuralEvaluator.ValueScale), sample.Labels[output]);
                    }

                    Assert.Equal(mask, sample.Masks[0]);
                    var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
                    Assert.Equal(count, sample.FeatureCounts[0]);
                    for (var feature = 0; feature < count; feature++)
                    {
                        Assert.Equal(indices[feature], sample.Indices[feature]);
                        Assert.Equal((float)(Half)values[feature], sample.Values[feature]);
                    }

                    checkedSamples++;
                    match.Act(seat, BelotAction.PlayCard(chosen.Card));
                }
            }

            Assert.True(checkedSamples > 30);
            Assert.Equal(0, buffers[0].Written);
            Assert.Equal(checkedSamples, buffers.Sum(x => x.Written));
        }

        [Fact]
        public void IndependentValidationSamplesAreNotUsedForTraining()
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-holdout-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var input = Path.Combine(directory, "input");
                var output = Path.Combine(directory, "output");
                var training = Path.Combine(directory, "train");
                var validation = Path.Combine(directory, "validation");
                RandomModels.Create(41).Save(input);
                foreach (var name in NeuralModels.FileNames)
                {
                    var tag = Array.IndexOf(NeuralModels.FileNames, name);
                    foreach (var (prefix, target) in new[] { (training, 10f), (validation, -10f) })
                    {
                        var buffer = new SampleBuffer(1, tag == 0 ? 9 : 32);
                        if (tag == 1)
                        {
                            var labels = new float[32];
                            labels[0] = target;
                            buffer.Add(new[] { 0 }, new[] { 1f }, labels, 1);
                        }

                        buffer.Save(prefix + "." + Path.GetFileNameWithoutExtension(name) + ".samples");
                    }
                }

                Distillation.Fit(new TrainingSettings
                {
                    In = input,
                    Out = output,
                    Data = training,
                    ValidationData = validation,
                    Epochs = 1,
                    Batch = 64,
                    Learners = 1,
                    FitLearningRate = 0.01,
                });
                var before = NeuralModels.Load(input).Networks[1];
                var after = NeuralModels.Load(output).Networks[1];
                Assert.True(after.GetBiases(after.LayerCount - 1)[0] > before.GetBiases(before.LayerCount - 1)[0]);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void EmptySamplesKeepWarmStartWeightsAndRefuseRandomOnes()
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-distill-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var input = Path.Combine(directory, "input");
                var output = Path.Combine(directory, "output");
                var prefix = Path.Combine(directory, "samples");
                RandomModels.Create(29).Save(input);
                foreach (var name in NeuralModels.FileNames)
                {
                    var tag = Array.IndexOf(NeuralModels.FileNames, name);
                    new SampleBuffer(1, tag == 0 ? 9 : 32).Save(prefix + "." + Path.GetFileNameWithoutExtension(name) + ".samples");
                }

                Distillation.Fit(new TrainingSettings { In = input, Out = output, Data = prefix });
                foreach (var name in NeuralModels.FileNames)
                {
                    Assert.Equal(File.ReadAllBytes(Path.Combine(input, name)), File.ReadAllBytes(Path.Combine(output, name)));
                }

                Assert.Throws<InvalidOperationException>(() => Distillation.Fit(new TrainingSettings { Out = output, Data = prefix }));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void FitsPartialBatchesAndKeepsUnlabelledWarmStartOutputs()
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-partial-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var input = Path.Combine(directory, "input");
                var output = Path.Combine(directory, "output");
                var prefix = Path.Combine(directory, "samples");
                RandomModels.Create(31).Save(input);
                foreach (var name in NeuralModels.FileNames)
                {
                    var tag = Array.IndexOf(NeuralModels.FileNames, name);
                    var buffer = new SampleBuffer(8, tag == 0 ? 9 : 32);
                    if (tag == 1)
                    {
                        var targets = new float[32];
                        targets[0] = 10;
                        for (var sample = 0; sample < 8; sample++)
                        {
                            buffer.Add(new[] { sample }, new[] { 1f }, targets, 1);
                        }
                    }

                    buffer.Save(prefix + "." + Path.GetFileNameWithoutExtension(name) + ".samples");
                }

                Distillation.Fit(new TrainingSettings
                {
                    In = input,
                    Out = output,
                    Data = prefix,
                    Epochs = 1,
                    Batch = 64,
                    Learners = 1,
                    FitLearningRate = 0.01,
                    FitCheckpoints = true,
                });
                var before = NeuralModels.Load(input).Networks[1];
                var after = NeuralModels.Load(output).Networks[1];
                foreach (var name in NeuralModels.FileNames)
                {
                    Assert.Equal(File.ReadAllBytes(Path.Combine(output, name)), File.ReadAllBytes(Path.Combine(output, "epoch-001", name)));
                }

                var last = before.LayerCount - 1;
                Assert.NotEqual(before.GetBiases(last)[0], after.GetBiases(last)[0]);
                Assert.Equal(before.GetBiases(last).Skip(1), after.GetBiases(last).Skip(1));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void SearchTeacherRequiresPositiveDealCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SearchDistillPlayer(RandomModels.Create(30), null, new TrainingSettings(), 1));
        }
    }
}
