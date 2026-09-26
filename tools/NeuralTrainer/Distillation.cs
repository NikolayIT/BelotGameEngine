namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine;
    using Belot.Engine.Players;

    /// <summary>
    /// The trainer's first two commands. "distill" plays whole games of four ClaudePlayerIsmcts
    /// (<see cref="DistillPlayer"/>) and saves what their searches found; "fit" trains new
    /// networks on those samples (the start the self-play training improves on).
    /// </summary>
    internal static class Distillation
    {
        private static readonly string[] Names = { "bid", "trump", "notrumps", "alltrumps" };

        public static void Record(TrainingSettings settings)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(settings.Data));
            Directory.CreateDirectory(directory);
            var buffers = Enumerable.Range(0, 4)
                .Select(tag => new SampleBuffer(
                    Math.Max(1000, settings.Games * (tag == 0 ? 100 : 300)),
                    tag == 0 ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs))
                .ToArray();
            using var players = new ThreadLocal<IPlayer[]>(() => Enumerable.Range(0, 4)
                .Select(seat => (IPlayer)new DistillPlayer(new ClaudePlayerIsmcts { TimeLimitMilliseconds = settings.Milliseconds }, buffers))
                .ToArray());
            var stopwatch = Stopwatch.StartNew();
            var lastSave = Stopwatch.StartNew();
            var done = 0;
            Parallel.For(
                0,
                settings.Games,
                new ParallelOptions { MaxDegreeOfParallelism = settings.Threads },
                i =>
                {
                    var p = players.Value;
                    new BelotGame(p[0], p[1], p[2], p[3], new Random(settings.Seed + i)).PlayGame((PlayerPosition)(1 << (i % 4)));
                    var finished = Interlocked.Increment(ref done);
                    if (finished % 50 == 0)
                    {
                        Console.WriteLine($"{stopwatch.Elapsed:hh\\:mm\\:ss} {finished} games: {Counts(buffers)}");
                    }

                    if (lastSave.Elapsed > TimeSpan.FromMinutes(15))
                    {
                        lock (lastSave)
                        {
                            if (lastSave.Elapsed > TimeSpan.FromMinutes(15))
                            {
                                Save(buffers, settings.Data);
                                lastSave.Restart();
                            }
                        }
                    }
                });

            Save(buffers, settings.Data);
            Console.WriteLine($"{stopwatch.Elapsed:hh\\:mm\\:ss} done: {Counts(buffers)}");
        }

        public static void Fit(TrainingSettings settings)
        {
            var networks = TrainingRun.CreateNetworks(settings);
            var random = new Random(settings.Seed);
            for (var tag = 0; tag < 4; tag++)
            {
                var buffer = SampleBuffer.Load($"{settings.Data}.{Names[tag]}.samples");
                var count = buffer.Count;
                var slots = Enumerable.Range(0, count).OrderBy(_ => random.Next()).ToArray();
                var validation = slots.Take(Math.Max(1, count / 20)).ToArray();
                var training = slots.Skip(validation.Length).ToArray();
                var network = networks[tag];
                var workers = Enumerable.Range(0, settings.Learners).Select(_ => new MlpWorker(network.Sizes)).ToArray();
                var batch = new Batch(settings.Batch, buffer.Outputs);
                Console.WriteLine($"{Names[tag]}: {count} samples, {string.Join("-", network.Sizes)}; validation loss {ValidationLoss(network, buffer, validation, batch, workers[0], settings):0.0000}");
                for (var epoch = 1; epoch <= settings.Epochs; epoch++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    Shuffle(training, random);
                    var rate = (float)(settings.FitLearningRate * (epoch > settings.Epochs * 0.7 ? 0.3 : 1));
                    var loss = 0.0;
                    var batches = 0;
                    for (var start = 0; start + settings.Batch <= training.Length; start += settings.Batch)
                    {
                        buffer.Take(batch, training.AsSpan(start, settings.Batch));
                        loss += network.Train(batch, workers, rate, (float)settings.MaxNorm, (float)settings.Huber);
                        batches++;
                    }

                    Console.WriteLine(
                        $"  epoch {epoch}: training loss {loss / Math.Max(1, batches):0.0000}, validation loss "
                        + $"{ValidationLoss(network, buffer, validation, batch, workers[0], settings):0.0000} ({stopwatch.Elapsed:mm\\:ss})");
                }

                // An action the samples never measured (ClaudePlayerIsmcts does not double) starts
                // out clearly bad, a deal lost, until self-play measures it.
                var labelled = Labelled(buffer, batch);
                for (var output = 0; output < buffer.Outputs; output++)
                {
                    if ((labelled & (1u << output)) == 0)
                    {
                        var last = network.Layers - 1;
                        var weights = network.Weights(last);
                        for (var input = 0; input < network.Sizes[last]; input++)
                        {
                            weights[(input * buffer.Outputs) + output] = 0;
                        }

                        network.Biases(last)[output] = -1;
                        Console.WriteLine($"  output {output} was never labelled: it starts at -{NeuralEvaluator.ValueScale} game points");
                    }
                }
            }

            TrainingRun.ToModels(networks).Save(settings.Out);
            Console.WriteLine($"Saved to {settings.Out}");
        }

        private static double ValidationLoss(Mlp network, SampleBuffer buffer, int[] validation, Batch batch, MlpWorker worker, TrainingSettings settings)
        {
            var loss = 0.0;
            var batches = 0;
            for (var start = 0; start < validation.Length; start += batch.Capacity)
            {
                buffer.Take(batch, validation.AsSpan(start, Math.Min(batch.Capacity, validation.Length - start)));
                loss += network.Loss(batch, worker, (float)settings.Huber);
                batches++;
            }

            return loss / Math.Max(1, batches);
        }

        // The outputs labelled in any sample.
        private static uint Labelled(SampleBuffer buffer, Batch batch)
        {
            var labelled = 0u;
            for (var start = 0; start < buffer.Count; start += batch.Capacity)
            {
                var slots = Enumerable.Range(start, Math.Min(batch.Capacity, buffer.Count - start)).ToArray();
                buffer.Take(batch, slots);
                for (var i = 0; i < batch.Count; i++)
                {
                    labelled |= batch.Masks[i];
                }
            }

            return labelled;
        }

        private static void Shuffle(int[] items, Random random)
        {
            for (var i = items.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static void Save(SampleBuffer[] buffers, string prefix)
        {
            for (var tag = 0; tag < buffers.Length; tag++)
            {
                buffers[tag].Save($"{prefix}.{Names[tag]}.samples");
            }
        }

        private static string Counts(SampleBuffer[] buffers) =>
            string.Join(", ", Enumerable.Range(0, buffers.Length).Select(t => $"{Names[t]} {buffers[t].Count}"));
    }
}
