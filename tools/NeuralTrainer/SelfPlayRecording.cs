namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Records frozen-policy Monte Carlo targets and optional privileged training labels.</summary>
    internal static class SelfPlayRecording
    {
        private static readonly string[] Names = { "bid", "trump", "notrumps", "alltrumps" };

        public static void Run(TrainingSettings settings)
        {
            if (settings.Deals <= 0 || settings.Threads <= 0 || settings.BidLabelChance != 0)
            {
                throw new ArgumentException("record-selfplay requires positive --deals and --threads, and --bid-label-chance 0.", nameof(settings));
            }

            Console.WriteLine($"record-selfplay: {settings}");
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var seats = new[] { models, models, models, models };
            var buffers = Enumerable.Range(0, 4).Select(tag => new SampleBuffer(
                tag == 0 ? 1 : settings.Capacity,
                tag == 0 ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs,
                trackOwners: tag != 0)).ToArray();
            var clock = Stopwatch.StartNew();
            var done = 0;
            Parallel.For(0, settings.Threads, new ParallelOptions { MaxDegreeOfParallelism = settings.Threads }, worker =>
            {
                var actor = new SelfPlayActor(settings, (settings.Seed * 1000) + worker);
                for (var deal = worker; deal < settings.Deals; deal += settings.Threads)
                {
                    actor.PlayDeal(seats, 0b1111, buffers);
                    var completed = Interlocked.Increment(ref done);
                    if (completed % 10000 == 0)
                    {
                        Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {completed}/{settings.Deals} deals");
                    }
                }
            });
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
            for (var tag = 0; tag < buffers.Length; tag++)
            {
                var buffer = buffers[tag];
                if (buffer.Written > buffer.Capacity || buffer.Dropped != 0)
                {
                    throw new InvalidOperationException($"{Names[tag]} overflow: {buffer.Written} written, {buffer.Capacity} capacity, {buffer.Dropped} dropped. Increase --capacity.");
                }

                buffer.Save($"{settings.Data}.{Names[tag]}.samples");
                Console.WriteLine($"{Names[tag]}: {buffer.Count} samples");
            }

            Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} done");
        }
    }
}
