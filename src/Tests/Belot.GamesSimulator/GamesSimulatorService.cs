namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Players;

    public class GamesSimulatorService
    {
        public const int LineLength = 70;

        public void Run(int parallelism)
        {
            // Warmup
            new BelotGame(new SmartPlayer(), new SmartPlayerPreviousVersion(), new DummyPlayer(), new RandomPlayer())
                .PlayGame();

            var totalStopwatch = Stopwatch.StartNew();
            SimulateGames(TwoSmartVsTwoPreviousVersionGames, 200_000, parallelism);
            SimulateGames(TwoSmartVsTwoDummyGames, 200_000, parallelism);
            SimulateGames(OneSmartVsThreeDummyGames, 200_000, parallelism);
            SimulateGames(TwoSmartVsTwoRandomGames, 200_000, parallelism);
            SimulateGames(OneSmartVsThreeRandomGames, 200_000, parallelism);
            //// SimulateGames(FourSmartGames, 200_000, parallelism);
            //// SimulateGames(FourRandomGames, 200_000, parallelism);
            //// SimulateGames(TwoDummyVsTwoRandomGames, 200_000, parallelism);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}");
        }

        public void RunDetailedGames(int count)
        {
            SimulateGames(SmartVsSmartGamesWithLogging, count, 1, true);
        }

        private static void SimulateGames(Func<BelotGame> simulation, int games, int parallelism, bool detailedLog = false)
        {
            Console.WriteLine($"Running {simulation.Method.Name}...");
            GlobalCounters.Counters = new long[GlobalCounters.CountersCount];
            var game = new ThreadLocal<BelotGame>(simulation);
            var southNorthWins = 0;
            var southNorthPoints = 0;
            var eastWestPoints = 0;
            var rounds = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(
                1,
                games + 1,
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

            var eastWestWins = games - southNorthWins;
            Console.WriteLine(
                $"{southNorthWins + eastWestWins} Games: {southNorthWins}-{eastWestWins} (Δ {southNorthWins - eastWestWins}) (Rounds: {rounds}) ELO: {CalculateElo(southNorthWins, eastWestWins):0.00}");
            Console.WriteLine(stopwatch.Elapsed + $" => Points: {southNorthPoints / 1000}k-{eastWestPoints / 1000}k => Counters: " + string.Join(",", GlobalCounters.Counters));
            Console.WriteLine(new string('=', LineLength));
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

        private static BelotGame SmartVsSmartGamesWithLogging() =>
            new BelotGame(
                new LoggingPlayerDecorator(new SmartPlayer()),
                new SmartPlayer(),
                new SmartPlayer(),
                new SmartPlayer());

        private static double CalculateElo(int wins, int loses)
        {
            var percentage = (double)wins / (wins + loses);
            var eloDifference = -400 * Math.Log((1 / percentage) - 1) / 2.302585092994046;
            return eloDifference;
        }
    }
}
