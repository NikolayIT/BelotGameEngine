namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Players;

    public class GamesSimulatorService
    {
        public const int LineLength = 70;

        // The neural player decides in microseconds: its games against SmartPlayer are cheap.
        private const int NeuralFastPairs = 10_000;

        public void Run(int parallelism)
        {
            // Warmup
            for (var i = 0; i < 10; i++)
            {
                new BelotGame(
                    new SmartPlayer(),
                    new SmartPlayerPreviousVersion(),
                    new DummyPlayer(),
                    new RandomPlayer()).PlayGame();
            }

            var totalStopwatch = Stopwatch.StartNew();
            var elo = 0.0;
            SimulateGames(TwoSmartVsTwoPreviousVersionGames, 200_000, parallelism);
            elo += SimulateGames(TwoSmartVsTwoDummyGames, 200_000, parallelism);
            elo += SimulateGames(OneSmartVsThreeDummyGames, 200_000, parallelism);
            elo += SimulateGames(TwoSmartVsTwoRandomGames, 200_000, parallelism);
            elo += SimulateGames(OneSmartVsThreeRandomGames, 200_000, parallelism);
            //// elo += SimulateGames(FourSmartGames, 200_000, parallelism);
            //// elo += SimulateGames(FourRandomGames, 200_000, parallelism);
            //// elo += SimulateGames(TwoDummyVsTwoRandomGames, 200_000, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}. Total ELO: {elo:0.00}.");
        }

        public void RunClaude(int parallelism, int pairs, int budgetMilliseconds)
        {
            Console.WriteLine($"ClaudePlayerIsmcts at {budgetMilliseconds} ms per card, mirrored pairs of games");
            Console.WriteLine(new string('=', LineLength));
            IPlayer Claude() => CreateClaude(budgetMilliseconds, "-");
            IPlayer Smart() => new SmartPlayer();

            var totalStopwatch = Stopwatch.StartNew();
            MirrorMatch("TwoClaudeIsmctsVsTwoSmart", Claude, Claude, Smart, Smart, pairs, parallelism);
            MirrorMatch("ClaudeIsmctsAndSmartVsTwoSmart", Claude, Smart, Smart, Smart, pairs, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}.");
        }

        public void RunClaudeAb(int parallelism, int pairs, int budgetMilliseconds, string candidate, string baseline)
        {
            Console.WriteLine($"ClaudePlayerIsmcts [{candidate}] vs [{baseline}] at {budgetMilliseconds} ms per card, mirrored pairs");
            Console.WriteLine(new string('=', LineLength));
            IPlayer Candidate() => CreateClaude(budgetMilliseconds, candidate);
            IPlayer Baseline() => CreateClaude(budgetMilliseconds, baseline);

            var totalStopwatch = Stopwatch.StartNew();
            MirrorMatch("CandidateVsBaseline", Candidate, Candidate, Baseline, Baseline, pairs, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}.");
        }

        /// <summary>
        /// ClaudePlayerNeural (the embedded networks, or a folder of them) against SmartPlayer
        /// (fast, so many pairs) and against ClaudePlayerIsmcts at the given budget.
        /// </summary>
        public void RunNeural(int parallelism, int pairs, int budgetMilliseconds, string weights)
        {
            Console.WriteLine($"ClaudePlayerNeural ({weights ?? "embedded networks"}), mirrored pairs of games");
            Console.WriteLine(new string('=', LineLength));
            IPlayer Neural() => weights == null ? new ClaudePlayerNeural() : new ClaudePlayerNeural(weights);
            IPlayer Smart() => new SmartPlayer();
            IPlayer Claude() => CreateClaude(budgetMilliseconds, "-");

            var totalStopwatch = Stopwatch.StartNew();
            MirrorMatch("TwoNeuralVsTwoSmart", Neural, Neural, Smart, Smart, NeuralFastPairs, parallelism);
            MirrorMatch("NeuralAndSmartVsTwoSmart", Neural, Smart, Smart, Smart, NeuralFastPairs, parallelism);
            MirrorMatch($"TwoNeuralVsTwoClaudeIsmcts ({budgetMilliseconds} ms)", Neural, Neural, Claude, Claude, pairs, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}.");
        }

        /// <summary>Two sets of networks (folders, or "-" for the embedded ones) against each other.</summary>
        public void RunNeuralAb(int parallelism, int pairs, string candidate, string baseline)
        {
            Console.WriteLine($"ClaudePlayerNeural [{candidate}] vs [{baseline}], mirrored pairs");
            Console.WriteLine(new string('=', LineLength));
            IPlayer Candidate() => candidate == "-" ? new ClaudePlayerNeural() : new ClaudePlayerNeural(candidate);
            IPlayer Baseline() => baseline == "-" ? new ClaudePlayerNeural() : new ClaudePlayerNeural(baseline);

            var totalStopwatch = Stopwatch.StartNew();
            MirrorMatch("CandidateVsBaseline", Candidate, Candidate, Baseline, Baseline, pairs, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}.");
        }

        public void RunDetailedGames(int count)
        {
            SimulateGames(
                () => new BelotGame(
                    new LoggingPlayerDecorator(new SmartPlayer(), ConsoleColor.White),
                    new LoggingPlayerDecorator(new SmartPlayer(), ConsoleColor.Yellow),
                    new LoggingPlayerDecorator(new SmartPlayer(), ConsoleColor.Cyan),
                    new LoggingPlayerDecorator(new SmartPlayer(), ConsoleColor.DarkYellow)),
                count,
                1,
                true);
        }

        private static ClaudePlayerIsmcts CreateClaude(int budgetMilliseconds, string options)
        {
            var player = new ClaudePlayerIsmcts { TimeLimitMilliseconds = budgetMilliseconds };
            foreach (var option in options.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = option.Split('=');
                switch (parts[0])
                {
                    case "-":
                        break;
                    case "ms":
                        player.TimeLimitMilliseconds = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        break;
                    case "c":
                        player.ExplorationConstant = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        break;
                    case "eps":
                        player.RolloutRandomness = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        break;
                    case "inf":
                        player.UsePlayInference = parts[1] != "0";
                        break;
                    case "bidinf":
                        player.UseBidInference = parts[1] != "0";
                        break;
                    case "mcbid":
                        player.UseMonteCarloBidding = parts[1] != "0";
                        break;
                    case "dbl":
                        player.MayDouble = parts[1] != "0";
                        break;
                    case "margin":
                        player.BidMargin = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        break;
                    case "deals":
                        player.BiddingDeals = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ArgumentException($"Unknown option {option}");
                }
            }

            return player;
        }

        // Every pair plays the same deals twice (one seeded deck), the teams swapping seats, so
        // the luck of the cards cancels out. Team A is South-North in the first game of a pair.
        private static void MirrorMatch(
            string name,
            Func<IPlayer> teamA1,
            Func<IPlayer> teamA2,
            Func<IPlayer> teamB1,
            Func<IPlayer> teamB2,
            int pairs,
            int parallelism)
        {
            Console.WriteLine($"Running {name}...");
            var players = new ThreadLocal<IPlayer[]>(() => new[] { teamA1(), teamA2(), teamB1(), teamB2() });
            var pairScores = new double[pairs];
            long pointsA = 0;
            long pointsB = 0;
            long rounds = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(
                0,
                pairs,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                i =>
                {
                    var p = players.Value;
                    var firstToPlay = (PlayerPosition)(1 << (i % 4));
                    var first = new BelotGame(p[0], p[2], p[1], p[3], new Random(i)).PlayGame(firstToPlay);
                    var second = new BelotGame(p[2], p[0], p[3], p[1], new Random(i)).PlayGame(firstToPlay);
                    var winsA = (first.Winner == PlayerPosition.SouthNorthTeam ? 1 : 0)
                                + (second.Winner == PlayerPosition.EastWestTeam ? 1 : 0);
                    pairScores[i] = winsA / 2.0;
                    lock (lockObject)
                    {
                        pointsA += first.SouthNorthPoints + second.EastWestPoints;
                        pointsB += first.EastWestPoints + second.SouthNorthPoints;
                        rounds += first.RoundsPlayed + second.RoundsPlayed;
                    }
                });

            var elapsed = stopwatch.Elapsed;
            var mean = 0.0;
            foreach (var score in pairScores)
            {
                mean += score;
            }

            mean /= pairs;
            var variance = 0.0;
            foreach (var score in pairScores)
            {
                variance += (score - mean) * (score - mean);
            }

            var sigma = Math.Sqrt(variance / Math.Max(1, pairs - 1) / pairs);
            var gamesA = (int)Math.Round(mean * pairs * 2);
            var gamesB = (pairs * 2) - gamesA;
            Console.WriteLine(
                $"{pairs * 2} games: {gamesA}-{gamesB} ({mean:P1} ± {sigma:P1}) (Rounds: {rounds}) ELO: {CalculateElo(gamesA, gamesB):0.00}");
            Console.WriteLine(
                $"{elapsed}; Points: {pointsA}-{pointsB} ({(double)(pointsA - pointsB) / (pairs * 2):+0.0;-0.0} a game)");
            Console.WriteLine(new string('=', LineLength));
        }

        private static double SimulateGames(Func<BelotGame> simulation, int games, int parallelism, bool detailedLog = false, string name = null)
        {
            Console.WriteLine($"Running {name ?? simulation.Method.Name}...");
            GlobalCounters.Counters = new long[GlobalCounters.CountersCount];
            var game = new ThreadLocal<BelotGame>(simulation);
            var southNorthWins = 0;
            var southNorthPoints = 0;
            var eastWestPoints = 0;
            var rounds = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(
                0,
                games,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                i =>
                {
                    var firstToPlay = (PlayerPosition)(1 << (i % 4));
                    var result = game.Value.PlayGame(firstToPlay);
                    lock (lockObject)
                    {
                        if (result.Winner == PlayerPosition.SouthNorthTeam)
                        {
                            southNorthWins++;
                        }

                        southNorthPoints += result.SouthNorthPoints;
                        eastWestPoints += result.EastWestPoints;
                        rounds += result.RoundsPlayed;
                    }

                    if (detailedLog)
                    {
                        Console.WriteLine(
                            $"Game #{i + 1}: Winner: {result.Winner}; Result(SN-EW): {result.SouthNorthPoints} - {result.EastWestPoints} (Rounds: {result.RoundsPlayed})");
                    }
                });

            var elapsed = stopwatch.Elapsed;
            var eastWestWins = games - southNorthWins;
            var elo = CalculateElo(southNorthWins, eastWestWins);
            Console.WriteLine(
                $"{southNorthWins + eastWestWins} games: {southNorthWins}-{eastWestWins} (Δ {southNorthWins - eastWestWins}) (Rounds: {rounds}) ELO: {elo:0.00}");
            Console.WriteLine(
                $"{elapsed:mm\\:ss\\.fffffff} (~{(double)elapsed.Ticks / rounds:0.00}); Points: {southNorthPoints / 1000}k-{eastWestPoints / 1000}k; Counters: {string.Join(",", GlobalCounters.Counters)}");
            Console.WriteLine(new string('=', LineLength));
            return elo;
        }

        private static BelotGame FourSmartGames() =>
            new BelotGame(new SmartPlayer(), new SmartPlayer(), new SmartPlayer(), new SmartPlayer());

        private static BelotGame TwoSmartVsTwoPreviousVersionGames() =>
            new BelotGame(
                new SmartPlayer(),
                new SmartPlayerPreviousVersion(),
                new SmartPlayer(),
                new SmartPlayerPreviousVersion());

        private static BelotGame TwoSmartVsTwoDummyGames() =>
            new BelotGame(new SmartPlayer(), new DummyPlayer(), new SmartPlayer(), new DummyPlayer());

        private static BelotGame OneSmartVsThreeDummyGames() =>
            new BelotGame(new SmartPlayer(), new DummyPlayer(), new DummyPlayer(), new DummyPlayer());

        private static BelotGame TwoSmartVsTwoRandomGames() =>
            new BelotGame(new SmartPlayer(), new RandomPlayer(), new SmartPlayer(), new RandomPlayer());

        private static BelotGame OneSmartVsThreeRandomGames() =>
            new BelotGame(new SmartPlayer(), new RandomPlayer(), new RandomPlayer(), new RandomPlayer());

        private static BelotGame FourRandomGames() =>
            new BelotGame(new RandomPlayer(), new RandomPlayer(), new RandomPlayer(), new RandomPlayer());

        private static BelotGame TwoDummyVsTwoRandomGames() =>
            new BelotGame(new DummyPlayer(), new RandomPlayer(), new DummyPlayer(), new RandomPlayer());

        private static double CalculateElo(int wins, int loses)
        {
            var percentage = (double)wins / (wins + loses);
            var eloDifference = -400 * Math.Log((1 / percentage) - 1) / 2.302585092994046;
            return eloDifference;
        }
    }
}
