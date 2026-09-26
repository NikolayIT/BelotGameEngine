namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.SmartPlayer;
    using Belot.Engine.Players;

    /// <summary>
    /// Trains ClaudePlayerNeural's networks (see NEURAL_NETWORK.md). Always run in Release:
    /// <code>
    /// distill  --data data/distill --games 3000 --milliseconds 50   record ClaudePlayerIsmcts's searches
    /// fit      --data data/distill --out checkpoints/distilled      train new networks on them
    /// train    --in checkpoints/distilled --out checkpoints/run1 --hours 10   self-play training
    /// validate --in checkpoints/run1/best --opponent ismcts:100 --pairs 200   mirrored games
    /// bench    --in checkpoints/run1/best                           time per decision
    /// </code>
    /// Every setting of <see cref="TrainingSettings"/> may be given as --name value.
    /// </summary>
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Commands: distill, fit, train, validate, bench (see Program.cs and NEURAL_NETWORK.md).");
                return 1;
            }

            PowerThrottling.Disable();
            var settings = TrainingSettings.Parse(args.Skip(1).ToArray(), new TrainingSettings());
            switch (args[0])
            {
                case "distill":
                    Distillation.Record(settings);
                    break;
                case "fit":
                    Distillation.Fit(settings);
                    break;
                case "train":
                    new TrainingRun(settings).Run();
                    break;
                case "validate":
                    Validate(settings);
                    break;
                case "bench":
                    Bench(settings);
                    break;
                default:
                    Console.WriteLine($"Unknown command {args[0]}.");
                    return 1;
            }

            return 0;
        }

        // Two of the networks against two of the opponent, in mirrored pairs of games.
        private static void Validate(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            IPlayer Neural()
            {
                var player = new ClaudePlayerNeural(models)
                {
                    MayDouble = settings.MayDouble,
                    Temperature = settings.Temperature,
                    MaxRegret = settings.MaxRegret,
                    SearchDeals = settings.SearchDeals,
                    SearchPriorDeals = settings.SearchPriorDeals,
                    SearchPruneMargin = settings.SearchPruneMargin,
                    SearchTimeLimitMilliseconds = settings.SearchMilliseconds,
                    Rng = new Random(Environment.CurrentManagedThreadId),
                };
                return settings.Bidding switch
                    {
                        "smart" => new MixedPlayer(new SmartPlayer(), player),
                        "ismcts" => new MixedPlayer(new ClaudePlayerIsmcts(), player),
                        _ => player,
                    };
            }

            Func<IPlayer> opponent;
            if (settings.Opponent == "smart")
            {
                opponent = () => new SmartPlayer();
            }
            else if (settings.Opponent.StartsWith("ismcts", StringComparison.Ordinal))
            {
                var parts = settings.Opponent.Split(':');
                var milliseconds = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 100;
                opponent = () => new ClaudePlayerIsmcts { TimeLimitMilliseconds = milliseconds };
            }
            else
            {
                var baseline = NeuralModels.Load(settings.Opponent);
                opponent = () => new ClaudePlayerNeural(baseline);
            }

            var stopwatch = Stopwatch.StartNew();
            var result = Evaluation.MirrorMatch(Neural, Neural, opponent, opponent, settings.Pairs, settings.Threads, settings.Seed * 100_000);
            Console.WriteLine($"{settings.In} vs {settings.Opponent}: {result} ({stopwatch.Elapsed})");
        }

        // The time of a searched card decision: whole games of four searching players on one thread.
        private static void BenchSearch(NeuralModels models, TrainingSettings settings)
        {
            var players = Enumerable.Range(0, 4).Select(seat => new ClaudePlayerNeural(models)
            {
                SearchDeals = settings.SearchDeals,
                SearchPriorDeals = settings.SearchPriorDeals,
                SearchPruneMargin = settings.SearchPruneMargin,
                SearchTimeLimitMilliseconds = settings.SearchMilliseconds,
                Rng = new Random(seat),
            }).ToArray();
            var timed = players.Select(x => new TimedPlayer(x)).ToArray();
            for (var game = 0; game < 4; game++)
            {
                new Belot.Engine.BelotGame(timed[0], timed[1], timed[2], timed[3], new Random(game)).PlayGame();
            }

            var decisions = timed.Sum(x => x.Decisions);
            var ticks = timed.Sum(x => x.Ticks);
            Console.WriteLine(
                $"search {settings.SearchDeals} deals (prior {settings.SearchPriorDeals}, prune {settings.SearchPruneMargin}, "
                + $"limit {settings.SearchMilliseconds} ms): {decisions} card decisions, "
                + $"{ticks * 1000.0 / Stopwatch.Frequency / decisions:0.0} ms a decision");
        }

        // The time of a decision: whole self-play deals with the networks, no labels.
        private static void Bench(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            if (settings.SearchDeals > 0)
            {
                BenchSearch(models, settings);
                return;
            }

            var labels = settings.Deals > 0;
            var bench = new TrainingSettings
            {
                CardLabelChance = labels ? settings.CardLabelChance : 0,
                BidLabelChance = labels ? settings.BidLabelChance : 0,
                CardExploration = 0,
                BidExploration = 0,
            };
            var actor = new SelfPlayActor(bench, settings.Seed);
            var seats = new[] { models, models, models, models };
            var buffers = Enumerable.Range(0, 4).Select(t => new SampleBuffer(100_000, t == 0 ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs)).ToArray();
            var deals = labels ? settings.Deals : 20_000;
            for (var i = 0; i < deals / 10; i++)
            {
                actor.PlayDeal(seats, 0b1111, buffers);
            }

            var stopwatch = Stopwatch.StartNew();
            var count = deals;
            for (var i = 0; i < count; i++)
            {
                actor.PlayDeal(seats, 0b1111, buffers);
            }

            if (labels)
            {
                Console.WriteLine($"labelled: {stopwatch.Elapsed.TotalMilliseconds / count:0.0} ms a deal; samples {string.Join(", ", buffers.Select(x => x.Written))}; rollout decisions {actor.RolloutDecisions}");
                if (settings.Threads > 1)
                {
                    // The same with an actor per thread, as a training run has them.
                    var parallel = Stopwatch.StartNew();
                    System.Threading.Tasks.Parallel.For(
                        0,
                        settings.Threads,
                        new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = settings.Threads },
                        t =>
                        {
                            var threadActor = new SelfPlayActor(bench, settings.Seed + t);
                            for (var i = 0; i < count; i++)
                            {
                                threadActor.PlayDeal(seats, 0b1111, buffers);
                            }
                        });
                    Console.WriteLine($"{settings.Threads} threads: {settings.Threads * count / parallel.Elapsed.TotalSeconds:0} labelled deals a second");
                }
            }

            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(
                $"{count} deals in {elapsed.TotalSeconds:0.00} s: {elapsed.TotalMilliseconds * 1000 / count:0} µs a deal, "
                + $"{elapsed.TotalMilliseconds * 1000 / actor.Decisions:0.0} µs a decision ({actor.Decisions} decisions); "
                + $"networks {string.Join(", ", models.Networks.Select(n => $"{string.Join("-", n.GetSizes())} ({n.ParameterCount / 1000}k)"))}");
        }
    }
}
