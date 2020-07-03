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
        public static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.WriteLine("Belot Games Simulator 1.0");
            Console.WriteLine(new string('=', 75));
            Console.Write(DateTime.Now.ToShortDateString());
#if DEBUG
            Console.Write(", Mode=Debug");
#elif RELEASE
            Console.Write(", Mode=Release");
#endif
            Console.Write($", CPUs={Environment.ProcessorCount}, OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', 75));

            SimulateGames(SmartVsSmartGame, 200_000, 12);
            SimulateGames(SmartVsRandomGame, 200_000, 12);
        }

        private static void SimulateGames(Func<BelotGame> simulation, int games, int parallelism)
        {
            Console.WriteLine($"Running {simulation.Method.Name}...");
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
                            else if (result.Winner == PlayerPosition.EastWestTeam)
                            {
                                eastWestWins++;
                            }

                            rounds += result.RoundsPlayed;
                        }

                        //// Console.WriteLine(
                        ////     $"Game #{i + 1}: Winner: {result.Winner}; Result(SN-EW): {result.SouthNorthPoints} - {result.EastWestPoints} (Rounds: {result.RoundsPlayed})");
                    });

            Console.WriteLine(stopwatch.Elapsed);
            Console.WriteLine(
                $"Games: (SN-EW): {southNorthWins}-{eastWestWins} (Total: {southNorthWins + eastWestWins}) (Rounds: {rounds})");
            Console.WriteLine(new string('=', 75));
        }

        private static BelotGame SmartVsSmartGame()
        {
            return new BelotGame(
                new SmartPlayer(),
                new SmartPlayer(),
                new SmartPlayer(),
                new SmartPlayer());
        }

        private static BelotGame SmartVsPreviousVersionGames()
        {
            return new BelotGame(
                new SmartPlayer(),
                new SmartPlayerPreviousVersion(),
                new SmartPlayer(),
                new SmartPlayerPreviousVersion());
        }

        private static BelotGame SmartVsRandomGame()
        {
            return new BelotGame(new SmartPlayer(), new RandomPlayer(), new SmartPlayer(), new RandomPlayer());
        }

        private static BelotGame SmartVsSmartGameWithLogging()
        {
            return new BelotGame(
                new LoggingPlayerDecorator(new SmartPlayer()),
                new SmartPlayer(),
                new SmartPlayer(),
                new SmartPlayer());
        }
    }
}
