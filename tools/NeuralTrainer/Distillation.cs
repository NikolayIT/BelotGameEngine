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
    /// The trainer's supervised commands. "distill" records searches from ClaudePlayerIsmcts
    /// or the neural search teacher; "fit" trains new networks or refines a warm start on those
    /// samples. Neural search records cards only, preserving the warm start's bidding network.
    /// </summary>
    internal static class Distillation
    {
        private static readonly string[] Names = { "bid", "trump", "notrumps", "alltrumps" };

        public static void Record(TrainingSettings settings)
        {
            if (settings.Teacher != "ismcts" && settings.Teacher != "neural" && settings.Teacher != "neural-batch" && settings.Teacher != "neural-gpu")
            {
                throw new ArgumentException("--teacher must be ismcts, neural, neural-batch or neural-gpu.", nameof(settings));
            }

            if (settings.Teacher == "neural-gpu" && string.IsNullOrEmpty(settings.In))
            {
                throw new ArgumentException("The GPU teacher requires --in so its weight hashes can be checked.", nameof(settings));
            }

            var models = settings.Teacher != "ismcts"
                ? (string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In))
                : null;
            Console.WriteLine($"distill: {settings}");
            var directory = Path.GetDirectoryName(Path.GetFullPath(settings.Data));
            Directory.CreateDirectory(directory);
            var buffers = Enumerable.Range(0, 4)
                .Select(tag => new SampleBuffer(
                    Math.Max(1000, settings.Games * (tag == 0 ? 100 : 300)),
                    tag == 0 ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs))
                .ToArray();
            var playerSeed = settings.Seed * 1000;
            using var policies = new ThreadLocal<IBatchedCardPolicy>(
                () => settings.Teacher switch
                {
                    "neural-gpu" => new GpuCardPolicy(settings.In, settings.GpuPort, settings.GpuVerify ? models : null),
                    "neural-batch" => new ManagedBatchedCardPolicy(models),
                    _ => null,
                },
                trackAllValues: true);
            using var players = new ThreadLocal<IPlayer[]>(() => Enumerable.Range(0, 4)
                .Select(seat => models == null
                    ? (IPlayer)new DistillPlayer(new ClaudePlayerIsmcts { TimeLimitMilliseconds = settings.Milliseconds }, buffers)
                    : new SearchDistillPlayer(models, buffers, settings, Interlocked.Increment(ref playerSeed), policies.Value))
                .ToArray());
            var stopwatch = Stopwatch.StartNew();
            var lastSave = Stopwatch.StartNew();
            var done = 0;
            try
            {
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
            }
            finally
            {
                foreach (var policy in policies.Values.OfType<IDisposable>())
                {
                    policy.Dispose();
                }
            }

            Save(buffers, settings.Data);
            Console.WriteLine($"{stopwatch.Elapsed:hh\\:mm\\:ss} done: {Counts(buffers)}");
        }

        public static void Fit(TrainingSettings settings)
        {
            var networks = TrainingRun.CreateNetworks(settings);
            var random = new Random(settings.Seed);
            Console.WriteLine($"fit: {settings}");
            if (settings.FitCheckpoints)
            {
                for (var epoch = 1; epoch <= settings.Epochs; epoch++)
                {
                    TrainingRun.ToModels(networks).Save(Path.Combine(settings.Out, $"epoch-{epoch:000}"));
                }
            }

            for (var tag = 0; tag < 4; tag++)
            {
                var buffer = SampleBuffer.Load($"{settings.Data}.{Names[tag]}.samples");
                var count = buffer.Count;
                if (count == 0)
                {
                    if (string.IsNullOrEmpty(settings.In))
                    {
                        throw new InvalidOperationException($"{Names[tag]} has no samples; supply --in to preserve a trained network.");
                    }

                    Console.WriteLine($"{Names[tag]}: no samples; keeping the input network unchanged.");
                    continue;
                }

                var slots = Enumerable.Range(0, count).OrderBy(_ => random.Next()).ToArray();
                var independent = !string.IsNullOrEmpty(settings.ValidationData);
                var validationBuffer = independent ? SampleBuffer.Load($"{settings.ValidationData}.{Names[tag]}.samples") : buffer;
                var validation = independent
                    ? Enumerable.Range(0, validationBuffer.Count).ToArray()
                    : slots.Take(Math.Max(1, count / 20)).ToArray();
                var training = independent ? slots : slots.Skip(validation.Length).ToArray();
                var network = networks[tag];
                var workers = Enumerable.Range(0, settings.Learners).Select(_ => new MlpWorker(network.Sizes)).ToArray();
                var batch = new Batch(settings.Batch, buffer.Outputs);
                Console.WriteLine($"{Names[tag]}: {count} samples, {string.Join("-", network.Sizes)}; validation loss {ValidationLoss(network, validationBuffer, validation, batch, workers[0], settings):0.0000}");
                Console.WriteLine($"  validation: {SampleDiagnostics.Measure(network.ToNetwork(), validationBuffer, validation, batch)}");

                // An action the samples never measured (ClaudePlayerIsmcts does not double) starts
                // out clearly bad, a deal lost, until self-play measures it.
                var labelled = Labelled(buffer, batch);
                for (var output = 0; output < buffer.Outputs; output++)
                {
                    if (string.IsNullOrEmpty(settings.In) && (labelled & (1u << output)) == 0)
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

                for (var epoch = 1; epoch <= settings.Epochs; epoch++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    Shuffle(training, random);
                    var rate = (float)(settings.FitLearningRate * (epoch > settings.Epochs * 0.7 ? 0.3 : 1));
                    var loss = 0.0;
                    var batches = 0;
                    for (var start = 0; start < training.Length; start += settings.Batch)
                    {
                        buffer.Take(batch, training.AsSpan(start, Math.Min(settings.Batch, training.Length - start)));
                        loss += network.Train(batch, workers, rate, (float)settings.MaxNorm, (float)settings.Huber, tag == NeuralModels.BidTag ? -1 : (float)settings.CardValueWeight);
                        batches++;
                    }

                    Console.WriteLine(
                        $"  epoch {epoch}: training loss {loss / Math.Max(1, batches):0.0000}, validation loss "
                        + $"{ValidationLoss(network, validationBuffer, validation, batch, workers[0], settings):0.0000} ({stopwatch.Elapsed:mm\\:ss})");
                    Console.WriteLine($"  validation: {SampleDiagnostics.Measure(network.ToNetwork(), validationBuffer, validation, batch)}");
                    if (settings.FitCheckpoints)
                    {
                        using var stream = File.Create(Path.Combine(settings.Out, $"epoch-{epoch:000}", NeuralModels.FileNames[tag]));
                        network.ToNetwork().Write(stream);
                    }
                }
            }

            TrainingRun.ToModels(networks).Save(settings.Out);
            Console.WriteLine($"Saved to {settings.Out}");
        }

        public static void Diagnose(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            Console.WriteLine($"diagnose: {settings}");
            for (var tag = 0; tag < models.Networks.Length; tag++)
            {
                var buffer = SampleBuffer.Load($"{settings.Data}.{Names[tag]}.samples");
                var slots = Enumerable.Range(0, buffer.Count).ToArray();
                Console.WriteLine($"{Names[tag]}: {SampleDiagnostics.Measure(models.Networks[tag], buffer, slots, new Batch(settings.Batch, buffer.Outputs))}");
            }
        }

        private static double ValidationLoss(Mlp network, SampleBuffer buffer, int[] validation, Batch batch, MlpWorker worker, TrainingSettings settings)
        {
            var loss = 0.0;
            var labelCount = 0;
            for (var start = 0; start < validation.Length; start += batch.Capacity)
            {
                buffer.Take(batch, validation.AsSpan(start, Math.Min(batch.Capacity, validation.Length - start)));
                var labels = batch.Masks.Take(batch.Count).Sum(mask => System.Numerics.BitOperations.PopCount(mask));
                loss += labels * network.Loss(batch, worker, (float)settings.Huber, network.Tag == NeuralModels.BidTag ? -1 : (float)settings.CardValueWeight);
                labelCount += labels;
            }

            return loss / Math.Max(1, labelCount);
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
