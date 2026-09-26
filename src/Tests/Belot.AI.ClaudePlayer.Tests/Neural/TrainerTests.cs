namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;

    using Belot.NeuralTrainer;

    using Xunit;

    /// <summary>The trainer's hand-written backpropagation and Adam.</summary>
    public class TrainerTests
    {
        // Every gradient the workers sum is the loss's derivative, measured by nudging the weight.
        [Fact]
        public void TheGradientsAreTheLossDerivatives()
        {
            var random = new Random(1);
            var sizes = new[] { 30, 12, 8, 5 };
            var network = new Mlp(1, sizes, random);
            var batch = RandomBatch(sizes, 6, random);
            var worker = new MlpWorker(sizes);
            const float Huber = 0.3f;
            worker.Clear();
            for (var sample = 0; sample < batch.Count; sample++)
            {
                worker.Accumulate(network, batch, sample, 1, Huber);
            }

            var checkedWeights = 0;
            for (var index = 0; index < network.Parameters.Length; index++)
            {
                var parameters = network.Parameters[index];
                for (var trial = 0; trial < 12; trial++)
                {
                    var i = random.Next(parameters.Length);
                    var saved = parameters[i];
                    const float Step = 1e-3f;
                    parameters[i] = saved + Step;
                    var up = network.Loss(batch, new MlpWorker(sizes), Huber);
                    parameters[i] = saved - Step;
                    var down = network.Loss(batch, new MlpWorker(sizes), Huber);
                    parameters[i] = saved;

                    // Loss is per label; the worker's gradients are summed (scale 1).
                    var labels = batch.Masks.Take(batch.Count).Sum(x => System.Numerics.BitOperations.PopCount(x));
                    var numeric = (up - down) / (2 * Step) * labels;
                    Assert.Equal(numeric, worker.Gradients[index][i], Math.Max(2e-3, Math.Abs(numeric) * 0.03));
                    checkedWeights++;
                }
            }

            Assert.Equal(12 * network.Parameters.Length, checkedWeights);
        }

        // A few hundred steps of Adam learn a simple target: each labelled output is the sum of
        // two of the inputs.
        [Fact]
        public void TrainingLearnsASimpleTarget()
        {
            var random = new Random(2);
            var sizes = new[] { 40, 32, 16, 6 };
            var network = new Mlp(1, sizes, random);
            var workers = Enumerable.Range(0, 3).Select(_ => new MlpWorker(sizes)).ToArray();
            var test = RandomBatch(sizes, 256, random);
            var before = network.Loss(test, workers[0], 1);
            for (var step = 0; step < 400; step++)
            {
                network.Train(RandomBatch(sizes, 64, random), workers, 3e-3f, 1, 1);
            }

            var after = network.Loss(test, workers[0], 1);
            Assert.True(after < before / 10, $"loss {before:0.0000} -> {after:0.0000}");
        }

        // Sparse inputs, some outputs labelled with a known function of them.
        private static Batch RandomBatch(int[] sizes, int count, Random random)
        {
            var inputs = sizes[0];
            var outputs = sizes[^1];
            var batch = new Batch(count, outputs) { Count = count };
            for (var sample = 0; sample < count; sample++)
            {
                var active = Enumerable.Range(0, inputs).OrderBy(_ => random.Next()).Take(random.Next(3, 12)).OrderBy(x => x).ToArray();
                var dense = new float[inputs];
                for (var k = 0; k < active.Length; k++)
                {
                    var value = random.Next(4) == 0 ? (float)random.NextDouble() : 1f;
                    batch.Indices[(sample * SampleBuffer.MaxFeatures) + k] = active[k];
                    batch.Values[(sample * SampleBuffer.MaxFeatures) + k] = value;
                    dense[active[k]] = value;
                }

                batch.FeatureCounts[sample] = active.Length;
                var mask = 0u;
                for (var o = 0; o < outputs; o++)
                {
                    if (random.Next(2) == 0)
                    {
                        batch.Labels[(sample * outputs) + o] = dense[o] + dense[o + outputs] - (0.5f * dense[inputs - 1 - o]);
                        mask |= 1u << o;
                    }
                }

                batch.Masks[sample] = mask == 0 ? 1u : mask;
            }

            return batch;
        }
    }
}
