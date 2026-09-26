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
                var player = new ClaudePlayerNeural(models) { MayDouble = settings.MayDouble };
                return settings.SmartBidding ? new SmartBiddingPlayer(player) : player;
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

        // The time of a decision: whole self-play deals with the networks, no labels.
        private static void Bench(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var bench = new TrainingSettings { CardLabelChance = 0, BidLabelChance = 0, CardExploration = 0, BidExploration = 0 };
            var actor = new SelfPlayActor(bench, settings.Seed);
            var seats = new[] { models, models, models, models };
            for (var i = 0; i < 2000; i++)
            {
                actor.PlayDeal(seats, 0, null);
            }

            var stopwatch = Stopwatch.StartNew();
            const int Deals = 20_000;
            for (var i = 0; i < Deals; i++)
            {
                actor.PlayDeal(seats, 0, null);
            }

            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(
                $"{Deals} deals in {elapsed.TotalSeconds:0.00} s: {elapsed.TotalMilliseconds * 1000 / Deals:0} µs a deal, "
                + $"{elapsed.TotalMilliseconds * 1000 / actor.Decisions:0.0} µs a decision ({actor.Decisions} decisions); "
                + $"networks {string.Join(", ", models.Networks.Select(n => $"{string.Join("-", n.GetSizes())} ({n.ParameterCount / 1000}k)"))}");
        }
    }
}
