namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

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
            Console.OutputEncoding = new UTF8Encoding(false);
            if (args.Length == 0)
            {
                Console.WriteLine("Commands: distill, record-selfplay, record-ppo, fit, diagnose, expand, train, validate, arena, bench (see Program.cs and NEURAL_NETWORK.md).");
                return 1;
            }

            PowerThrottling.Disable();
            var settings = TrainingSettings.Parse(args.Skip(1).ToArray(), new TrainingSettings());
            switch (args[0])
            {
                case "distill":
                    Distillation.Record(settings);
                    break;
                case "record-selfplay":
                    SelfPlayRecording.Run(settings);
                    break;
                case "record-ppo":
                    PpoRecording.Run(settings);
                    break;
                case "fit":
                    Distillation.Fit(settings);
                    break;
                case "diagnose":
                    Distillation.Diagnose(settings);
                    break;
                case "expand":
                    NetworkExpansion.Run(settings);
                    break;
                case "train":
                    new TrainingRun(settings).Run();
                    break;
                case "validate":
                    Validate(settings);
                    break;
                case "arena":
                    Arena.Run(settings);
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

        internal static ClaudePlayerNeural CreateBenchmarkPlayer(TrainingSettings settings, NeuralModels models, int seed)
        {
            return settings.Player.ToLowerInvariant() switch
            {
                "candidate" or "neural" => OpponentCatalog.Configured(settings, models, seed),
                "master" or "rollout-master" or "fast" or "expert" => (ClaudePlayerNeural)OpponentCatalog.Factory(settings.Player, models)(seed),
                _ => throw new ArgumentException($"Unknown benchmark player '{settings.Player}'. Use candidate, neural, master, rollout-master, fast or expert.", nameof(settings)),
            };
        }

        // Two of the networks against two of the opponent, in mirrored pairs of games.
        private static void Validate(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            IPlayer Neural(int seed)
            {
                var player = OpponentCatalog.Configured(settings, models, seed);
                return settings.Bidding switch
                    {
                        "smart" => new MixedPlayer(new SmartPlayer(), player),
                        "ismcts" => new MixedPlayer(new ClaudePlayerIsmcts { Rng = new Random(seed) }, player),
                        _ => player,
                    };
            }

            Func<int, IPlayer> opponent;
            if (settings.Opponent == "smart")
            {
                opponent = _ => new SmartPlayer();
            }
            else if (settings.Opponent.Equals("sharpbelot", StringComparison.OrdinalIgnoreCase)
                     || settings.Opponent.Equals("belot206", StringComparison.OrdinalIgnoreCase))
            {
                opponent = OpponentCatalog.Factory(settings.Opponent);
            }
            else if (settings.Opponent.StartsWith("ismcts", StringComparison.Ordinal))
            {
                var parts = settings.Opponent.Split(':');
                var milliseconds = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 100;
                opponent = seed => new ClaudePlayerIsmcts { Rng = new Random(seed), TimeLimitMilliseconds = milliseconds };
            }
            else
            {
                var baseline = NeuralModels.Load(settings.Opponent);
                opponent = seed => new ClaudePlayerNeural(baseline)
                {
                    Rng = new Random(seed),
                    SearchDeals = settings.OpponentSearchDeals,
                    SearchTimeLimitMilliseconds = settings.OpponentSearchMilliseconds,
                    UseEndgameSearch = settings.OpponentEndgame,
                    EndgameUseDeclarations = settings.EndgameDeclarations,
                    EndgameTricks = settings.EndgameTricks,
                    EndgameThreeTrickWorldLimit = settings.EndgameWorlds,
                };
            }

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"validate: {settings}");
            var result = Arena.Play(
                Neural,
                opponent,
                settings.Pairs,
                settings.Threads,
                checked(settings.Seed * 100_000),
                done => Console.WriteLine($"{stopwatch.Elapsed:hh\\:mm\\:ss} {done}/{settings.Pairs} mirrored pairs"));
            Console.WriteLine($"{settings.In} vs {settings.Opponent}: {result.Match} ({stopwatch.Elapsed})");
            Console.WriteLine($"legacy diagnostics A: {JsonSerializer.Serialize(result.TeamA)}; B: {JsonSerializer.Serialize(result.TeamB)}");
        }

        // Card decisions through the engine, after warming up; excludes bidding and forced cards.
        private static void BenchCards(ClaudePlayerNeural[] players, TrainingSettings settings)
        {
            var player = players[0];
            var warmupGames = player.SearchDeals > 0 ? 1 : 20;
            for (var game = 0; game < warmupGames; game++)
            {
                new Belot.Engine.BelotGame(players[0], players[1], players[2], players[3], new Random(-game - 1)).PlayGame();
            }

            var timed = players.Select(x => new TimedPlayer(x)).ToArray();
            var warmupEndgames = players.Sum(x => x.EndgameDecisions);
            var warmupNodes = players.Sum(x => x.EndgameNodes);
            var warmupWorlds = players.Sum(x => x.EndgameWorlds);
            var warmupAttempts = players.Sum(x => x.EndgameSampleAttempts);
            var warmupIncomplete = players.Sum(x => x.EndgameIncompleteWorlds);
            var warmupLikelihood = players.Sum(x => x.EndgameLikelihoodEvaluations);
            var warmupEffective = players.Sum(x => x.EndgameEffectiveWorlds);
            var warmupProbes = players.Sum(x => x.EndgameTranspositionProbes);
            var warmupHits = players.Sum(x => x.EndgameTranspositionHits);
            var warmupCutoffs = players.Sum(x => x.EndgameTranspositionCutoffs);
            var warmupNeuralVariance = players.Sum(x => x.SearchNeuralVariance);
            var warmupResidualVariance = players.Sum(x => x.SearchResidualVariance);
            var games = settings.BenchGames > 0 ? settings.BenchGames : player.SearchDeals > 0 ? 4 : 100;
            for (var game = 0; game < games; game++)
            {
                new Belot.Engine.BelotGame(timed[0], timed[1], timed[2], timed[3], new Random(game)).PlayGame();
            }

            var decisions = timed.Sum(x => x.Decisions);
            var ticks = timed.Sum(x => x.Ticks);
            var samples = timed.SelectMany(x => x.DecisionTicks).Order().ToArray();
            double Micros(long value) => value * 1_000_000.0 / Stopwatch.Frequency;
            double Percentile(double quantile) => Micros(samples[(int)Math.Ceiling(quantile * samples.Length) - 1]);
            Console.WriteLine($"card latency us: p50={Percentile(.5):0.0}, p95={Percentile(.95):0.0}, p99={Percentile(.99):0.0}, max={Micros(samples[^1]):0.0}, over10ms={samples.Count(x => Micros(x) > 10000)}/{samples.Length}");
            Console.WriteLine(settings);
            Console.WriteLine($"effective player {settings.Player}: temperature {player.Temperature}, regret {player.MaxRegret}, suit ensemble {player.CardSuitEnsemble}");
            Console.WriteLine($"endgame totals: {players.Sum(x => x.EndgameNodes) - warmupNodes} nodes, {players.Sum(x => x.EndgameWorlds) - warmupWorlds} worlds");
            Console.WriteLine($"endgame sampling: {players.Sum(x => x.EndgameSampleAttempts) - warmupAttempts} attempts, {players.Sum(x => x.EndgameIncompleteWorlds) - warmupIncomplete} incomplete worlds");
            Console.WriteLine($"endgame likelihood: {players.Sum(x => x.EndgameLikelihoodEvaluations) - warmupLikelihood} evaluations, {players.Sum(x => x.EndgameEffectiveWorlds) - warmupEffective:0.0} effective worlds");
            Console.WriteLine($"endgame transpositions: {players.Sum(x => x.EndgameTranspositionProbes) - warmupProbes} probes, {players.Sum(x => x.EndgameTranspositionHits) - warmupHits} hits, {players.Sum(x => x.EndgameTranspositionCutoffs) - warmupCutoffs} cutoffs");
            var neuralVariance = players.Sum(x => x.SearchNeuralVariance) - warmupNeuralVariance;
            var residualVariance = players.Sum(x => x.SearchResidualVariance) - warmupResidualVariance;
            Console.WriteLine($"control variate variance sums: neural={neuralVariance:0.000}, residual={residualVariance:0.000}, ratio={residualVariance / neuralVariance:0.000}");
            Console.WriteLine(
                $"search {player.SearchDeals} deals (prior {player.SearchPriorDeals}, prune {player.SearchPruneMargin}, "
                + $"limit {player.SearchTimeLimitMilliseconds} ms, endgame {player.UseEndgameSearch}/{player.EndgameTricks}/{player.EndgameThreeTrickWorldLimit}, declarations {player.EndgameUseDeclarations}, "
                + $"sampled {player.EndgameSampledWorlds}, nodes {player.EndgameNodeLimit}, endgame limit {player.EndgameTimeLimitMilliseconds} ms, transpositions {player.EndgameUseTranspositions}, "
                + $"ownership power {player.EndgameOwnershipPower}, ownership mix {player.EndgameOwnershipUniformMix}, "
                + $"{players.Sum(x => x.EndgameDecisions) - warmupEndgames} endgames): {decisions} card decisions, "
                + $"{ticks * 1_000_000.0 / Stopwatch.Frequency / decisions:0.0} µs per card through the engine ({games} games, warmup excluded)");
        }

        // The time of a decision: whole self-play deals with the networks, no labels.
        private static void Bench(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var players = Enumerable.Range(0, 4).Select(seat => CreateBenchmarkPlayer(settings, models, seat)).ToArray();
            var namedProfile = !settings.Player.Equals("candidate", StringComparison.OrdinalIgnoreCase)
                && !settings.Player.Equals("neural", StringComparison.OrdinalIgnoreCase);
            if (namedProfile || players[0].SearchDeals > 0 || players[0].UseEndgameSearch || players[0].CardSuitEnsemble || !string.IsNullOrEmpty(settings.CardCorrection))
            {
                BenchCards(players, settings);
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

            var warmupDecisions = actor.Decisions;
            var stopwatch = Stopwatch.StartNew();
            var count = deals;
            for (var i = 0; i < count; i++)
            {
                actor.PlayDeal(seats, 0b1111, buffers);
            }

            stopwatch.Stop();

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
                + $"{elapsed.TotalMilliseconds * 1000 / (actor.Decisions - warmupDecisions):0.0} µs a decision ({actor.Decisions - warmupDecisions} decisions, warmup excluded); "
                + $"networks {string.Join(", ", models.Networks.Select(n => $"{string.Join("-", n.GetSizes())} ({n.ParameterCount / 1000}k)"))}");
            if (!labels)
            {
                BenchCards(players, settings);
            }
        }
    }
}
