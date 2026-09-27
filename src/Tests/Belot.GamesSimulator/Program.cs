namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Belot.NeuralTrainer;

    public static class Program
    {
        public const int LineLength = 70;

        /// <summary>
        /// Without arguments, runs the SmartPlayer ELO benchmark. The other suites play mirrored
        /// pairs of games (the same deals, the teams swapped) with ClaudePlayerIsmcts, whose every
        /// decision spends its budget, so keep the counts small:
        ///   claude [pairs] [budgetMs]: against SmartPlayer (default 100 pairs at 100 ms);
        ///   claude-ab [pairs] [budgetMs] [candidate] [baseline]: two configurations against each
        ///   other, each given as comma-separated options (ms=100 its own budget, c=0.3 the
        ///   exploration constant, inf=0 no play inference, bidinf=1 deals that explain the
        ///   auction, mcbid=0 SmartPlayer's bidding, dbl=0 no doubling, margin=1.5 and deals=300
        ///   for the Monte Carlo bidding; "-" for the defaults);
        ///   neural [pairs] [budgetMs] [weights]: ClaudePlayerNeural (the embedded networks, or a
        ///   folder of them) against SmartPlayer (10000 pairs) and against ClaudePlayerIsmcts
        ///   (default 200 pairs at 100 ms);
        ///   neural-ab [pairs] [candidate] [baseline]: two folders of networks ("-" the embedded)
        ///   against each other;
        ///   elo [fastPairs] [slowPairs]: the MAUI app's levels in a pair-vs-pair round robin
        ///   (default 20000 mirrored pairs a matchup, 150 with ISMCTS), printing the ratings to
        ///   paste into the app's level list.
        /// </summary>
        /// <param name="args">The optional suite name and its arguments.</param>
        public static void Main(string[] args)
        {
            var parallelism = Environment.ProcessorCount / 2;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            PowerThrottling.Disable();
            Console.WriteLine(new string('=', LineLength));
            Console.WriteLine("Belot Games Simulator");
#if DEBUG
            Console.Write("Mode=Debug");
#elif RELEASE
            Console.Write("Mode=Release");
#endif
            Console.Write(
                $", CPUs={Environment.ProcessorCount}({parallelism}), OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', LineLength));

            if (args.Length > 0 && args[0] == "elo")
            {
                EloTournament.Run(
                    parallelism,
                    args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20_000,
                    args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 150);
                return;
            }

            if (args.Length > 0 && args[0] == "neural")
            {
                new GamesSimulatorService().RunNeural(
                    parallelism,
                    args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 200,
                    args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 100,
                    args.Length > 3 ? args[3] : null);
                return;
            }

            if (args.Length > 0 && args[0] == "neural-ab")
            {
                new GamesSimulatorService().RunNeuralAb(
                    parallelism,
                    args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 2000,
                    args.Length > 2 ? args[2] : "-",
                    args.Length > 3 ? args[3] : "-");
                return;
            }

            if (args.Length > 0 && (args[0] == "claude" || args[0] == "claude-ab"))
            {
                var pairs = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 100;
                var budget = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 100;
                if (args[0] == "claude")
                {
                    new GamesSimulatorService().RunClaude(parallelism, pairs, budget);
                }
                else
                {
                    new GamesSimulatorService().RunClaudeAb(
                        parallelism,
                        pairs,
                        budget,
                        args.Length > 3 ? args[3] : "-",
                        args.Length > 4 ? args[4] : "-");
                }

                return;
            }

            new GamesSimulatorService().Run(parallelism);
            //// new GamesSimulatorService().RunDetailedGames(2);
        }
    }
}
