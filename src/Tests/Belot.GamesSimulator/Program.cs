namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Players;

    public static class Program
    {
        public const int LineLength = 70;

        public static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.WriteLine(new string('=', LineLength));
            Console.WriteLine("Belot Games Simulator");
            Console.Write(DateTime.Now.ToShortDateString());
#if DEBUG
            Console.Write(", Mode=Debug");
#elif RELEASE
            Console.Write(", Mode=Release");
#endif
            Console.Write(
                $", CPUs={Environment.ProcessorCount}, OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', LineLength));

            var totalStopwatch = Stopwatch.StartNew();
            SimulateGames(TwoSmartVsTwoPreviousVersionGames, 200_000, 12);
            SimulateGames(TwoSmartVsTwoDummyGames, 200_000, 12);
            SimulateGames(OneSmartVsThreeDummyGames, 200_000, 12);
            SimulateGames(TwoSmartVsTwoRandomGames, 200_000, 12);
            SimulateGames(OneSmartVsThreeRandomGames, 200_000, 12);
            //// SimulateGames(SmartVsSmartGames, 200_000, 12);
            //// SimulateGames(SmartVsSmartGamesWithLogging, 10, 1, true);
            Console.WriteLine($"Total tests time: {totalStopwatch.Elapsed}");
        }

        private static void SimulateGames(Func<BelotGame> simulation, int games, int parallelism, bool detailedLog = false)
        {
            Console.WriteLine($"Running {simulation.Method.Name}...");
            GlobalCounters.Counters = new int[GlobalCounters.CountersCount];
            var game = new ThreadLocal<BelotGame>(simulation);
            var southNorthWins = 0;
            var eastWestWins = 0;
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
                            else
                            {
                                eastWestWins++;
                            }

                            rounds += result.RoundsPlayed;
                        }

                        if (detailedLog)
                        {
                            Console.WriteLine(
                                $"Game #{i + 1}: Winner: {result.Winner}; Result(SN-EW): {result.SouthNorthPoints} - {result.EastWestPoints} (Rounds: {result.RoundsPlayed})");
                        }
                    });

            Console.WriteLine(
                $"Games: {southNorthWins}-{eastWestWins} (Total: {southNorthWins + eastWestWins}, Diff: {southNorthWins - eastWestWins}) (Rounds: {rounds}) ELO: {CalculateElo(southNorthWins, eastWestWins):0.00}");
            Console.WriteLine(stopwatch.Elapsed + " => Counters: " + string.Join(",", GlobalCounters.Counters));
            Console.WriteLine(new string('=', LineLength));
        }

        private static BelotGame TwoSmartVsTwoSmartGames() =>
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
