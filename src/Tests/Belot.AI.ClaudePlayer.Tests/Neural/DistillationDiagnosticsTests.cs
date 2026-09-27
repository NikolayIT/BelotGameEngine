namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.NeuralTrainer;

    using Xunit;

    public class DistillationDiagnosticsTests
    {
        [Fact]
        public void ReportsPointErrorsAndLegalTeacherRegret()
        {
            var metrics = new SampleDiagnostics();
            var target = new[] { 1f, 3f, -999f, 5f };
            metrics.Add(new[] { 2f, 4f, 999f, 7f }, target, 0b1011);
            Assert.Equal(Math.Sqrt(2) * NeuralEvaluator.ValueScale, metrics.RootMeanSquaredError, 5);
            Assert.Equal(Math.Sqrt(2) / 3 * NeuralEvaluator.ValueScale, metrics.CentredRootMeanSquaredError, 5);
            Assert.Equal(0, metrics.MeanRegret);
            Assert.Equal(1, metrics.BestChoiceFraction);

            metrics.Add(new[] { 5f, 4f, 999f, 0f }, target, 0b1011);
            metrics.Add(new float[4], target, 0);
            Assert.Equal(2, metrics.Samples);
            Assert.Equal(2 * NeuralEvaluator.ValueScale, metrics.MeanRegret);
            Assert.Equal(0.5, metrics.BestChoiceFraction);
        }

        [Fact]
        public void EqualTeacherValuesAllowEitherChoice()
        {
            var metrics = new SampleDiagnostics();
            metrics.Add(new[] { 0f, 1f }, new[] { 3f, 3f }, 3);
            Assert.Equal(0, metrics.MeanRegret);
            Assert.Equal(1, metrics.BestChoiceFraction);
        }

        [Fact]
        public void ExpansionPreservesPredictionsIncludingAfterExport()
        {
            var source = new Mlp(1, new[] { 20, 12, 8, 4 }, new Random(31)).ToNetwork();
            var expanded = NetworkExpansion.Widen(source, new[] { 19, 15 }, new Random(32));
            using var originalStream = new MemoryStream();
            using var expandedStream = new MemoryStream();
            source.Write(originalStream);
            expanded.Write(expandedStream);
            originalStream.Position = 0;
            expandedStream.Position = 0;
            var originalFile = NeuralNetwork.Read(originalStream);
            var expandedFile = NeuralNetwork.Read(expandedStream);
            var random = new Random(33);
            var indices = Enumerable.Range(0, 20).Where(x => x % 3 != 0).ToArray();
            foreach (var pair in new[] { (source, expanded), (originalFile, expandedFile) })
            {
                for (var sample = 0; sample < 100; sample++)
                {
                    var values = indices.Select(_ => (float)((random.NextDouble() * 2) - 1)).ToArray();
                    var before = new float[4];
                    var after = new float[4];
                    pair.Item1.Forward(indices, values, before);
                    pair.Item2.Forward(indices, values, after);
                    for (var output = 0; output < before.Length; output++)
                    {
                        Assert.Equal(before[output], after[output], 5);
                    }
                }
            }

            Assert.Throws<ArgumentException>(() => NetworkExpansion.Widen(source, new[] { 11, 8 }, random));
            Assert.Throws<ArgumentException>(() => NetworkExpansion.Widen(source, new[] { 12, 8, 4 }, random));
        }

        [Fact]
        public void AddedUnitsCanLearnThroughInitiallyZeroConnections()
        {
            var source = new Mlp(1, new[] { 5, 3, 2 }, new Random(34)).ToNetwork();
            var network = new Mlp(NetworkExpansion.Widen(source, new[] { 8 }, new Random(35)));
            var batch = new Batch(1, 2) { Count = 1 };
            batch.FeatureCounts[0] = 5;
            batch.Masks[0] = 3;
            batch.Labels[0] = 2;
            batch.Labels[1] = -2;
            for (var i = 0; i < 5; i++)
            {
                batch.Indices[i] = i;
                batch.Values[i] = 1;
            }

            Assert.All(network.Weights(1).Skip(6), weight => Assert.Equal(0, weight));
            network.Train(batch, new[] { new MlpWorker(network.Sizes) }, 0.01f, 1, 1);
            Assert.Contains(network.Weights(1).Skip(6), weight => weight != 0);
        }
    }
}
