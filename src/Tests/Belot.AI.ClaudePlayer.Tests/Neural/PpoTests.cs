namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.NeuralTrainer;

    using Xunit;

    public class PpoTests
    {
        [Fact]
        public void SamplingUsesOnlyLegalActionsAndPointTemperature()
        {
            var q = new float[32];
            q[2] = 1;
            q[7] = 1 + (1f / NeuralEvaluator.ValueScale);
            q[12] = 1000;
            var mask = (1u << 2) | (1u << 7);
            Assert.Equal(2, PpoPolicy.Sample(q, mask, 1, 0.2, out var firstLog));
            Assert.Equal(7, PpoPolicy.Sample(q, mask, 1, 0.8, out var secondLog));
            Assert.Equal(-Math.Log(1 + Math.E), firstLog, 5);
            Assert.Equal(1 - Math.Log(1 + Math.E), secondLog, 5);
            q[12] = float.NaN;
            Assert.Equal(7, PpoPolicy.Sample(q, mask, 1, 0.8, out var unchanged));
            Assert.Equal(secondLog, unchanged);
            Assert.Throws<ArgumentOutOfRangeException>(() => PpoPolicy.Sample(q, 0, 1, 0.5, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => PpoPolicy.Sample(q, mask, 0, 0.5, out _));
        }

        [Fact]
        public void FloatSnapshotsPreserveParametersAndRejectInvalidFiles()
        {
            var source = NeuralModels.Embedded.Networks[1];
            using var file = new MemoryStream();
            using (var writer = new BinaryWriter(file, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(PpoTrainingFiles.Magic);
                writer.Write(1);
                writer.Write(source.Tag);
                writer.Write(source.Layout);
                writer.Write(source.LayerCount);
                foreach (var size in source.GetSizes())
                {
                    writer.Write(size);
                }

                for (var layer = 0; layer < source.LayerCount; layer++)
                {
                    foreach (var value in source.GetWeights(layer).Concat(source.GetBiases(layer)))
                    {
                        writer.Write(value);
                    }
                }
            }

            file.Position = 0;
            var loaded = PpoTrainingFiles.Read(file, 1);
            for (var layer = 0; layer < source.LayerCount; layer++)
            {
                Assert.Equal(source.GetWeights(layer), loaded.GetWeights(layer));
                Assert.Equal(source.GetBiases(layer), loaded.GetBiases(layer));
            }

            var bytes = file.ToArray();
            Assert.Throws<InvalidDataException>(() => PpoTrainingFiles.Read(new MemoryStream(bytes), 2));
            Assert.Throws<InvalidDataException>(() => PpoTrainingFiles.Read(new MemoryStream(bytes[..^1]), 1));
            Assert.Throws<InvalidDataException>(() => PpoTrainingFiles.Read(new MemoryStream(bytes.Concat(new byte[1]).ToArray()), 1));
            BitConverter.GetBytes(float.NaN).CopyTo(bytes, bytes.Length - sizeof(float));
            Assert.Throws<InvalidDataException>(() => PpoTrainingFiles.Read(new MemoryStream(bytes), 1));
        }

        [Fact]
        public void RecordedProbabilitiesRewardsAndSeatTrajectoriesAreConsistent()
        {
            var models = NeuralModels.Embedded;
            var first = new PpoRecording.Worker(models, 1231, 0.7);
            var repeat = new PpoRecording.Worker(models, 1231, 0.7);
            for (var deal = 0; deal < 40; deal++)
            {
                first.PlayDeal(deal);
                repeat.PlayDeal(deal);
            }

            var q = new float[32];
            for (var kind = 0; kind < 3; kind++)
            {
                var samples = first.Samples[kind];
                Assert.NotEmpty(samples);
                Assert.Equal(samples.Count, repeat.Samples[kind].Count);
                for (var i = 0; i < samples.Count; i++)
                {
                    var sample = samples[i];
                    var copy = repeat.Samples[kind][i];
                    Assert.Equal(sample.Action, copy.Action);
                    Assert.Equal(sample.Outcome, copy.Outcome);
                    Assert.Equal(sample.Owners, copy.Owners);
                    Assert.Equal(sample.Features, copy.Features);
                    Assert.True((sample.Mask & (1u << sample.Action)) != 0);
                    models.Networks[kind + 1].Forward(sample.Indices, sample.Features, q);
                    Assert.Equal(q[sample.Action], sample.OldValue);
                    var maximum = NeuralEvaluator.Best(q, sample.Mask);
                    var sum = 0d;
                    for (var rest = sample.Mask; rest != 0; rest &= rest - 1)
                    {
                        sum += Math.Exp(((double)q[BitOperations.TrailingZeroCount(rest)] - q[maximum]) * NeuralEvaluator.ValueScale / 0.7);
                    }

                    var expected = (((double)q[sample.Action] - q[maximum]) * NeuralEvaluator.ValueScale / 0.7) - Math.Log(sum);
                    Assert.InRange(Math.Abs(expected - sample.LogProbability), 0, 2e-6);
                    Assert.InRange(sample.Indices.Length, 1, FeatureEncoder.MaxActive);
                    Assert.All(sample.Indices, index => Assert.InRange(index, 0, FeatureEncoder.CardInputs - 1));
                    if (sample.Next >= 0)
                    {
                        Assert.True(sample.Next > i);
                        Assert.Equal(sample.Deal, samples[sample.Next].Deal);
                        Assert.Equal(sample.Seat, samples[sample.Next].Seat);
                        Assert.Equal(sample.Outcome, samples[sample.Next].Outcome);
                    }
                }

                foreach (var deal in samples.GroupBy(sample => sample.Deal))
                {
                    var result = deal.First().Outcome * ((deal.First().Seat & 1) == 0 ? 1 : -1);
                    Assert.All(deal, sample => Assert.Equal(result, sample.Outcome * ((sample.Seat & 1) == 0 ? 1 : -1)));
                    foreach (var seat in deal.GroupBy(sample => sample.Seat))
                    {
                        Assert.Single(seat, sample => sample.Next < 0);
                    }
                }
            }
        }
    }
}
