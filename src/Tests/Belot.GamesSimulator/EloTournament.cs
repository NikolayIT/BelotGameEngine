namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Players;

    /// <summary>
    /// The round robin behind the MAUI app's levels (its <c>AiLevels</c>): every two levels play
    /// pair against pair, in mirrored pairs of games (the same deals, the teams swapped, so the
    /// cards' luck cancels), then the ratings are fit with the Bradley-Terry model (which is what
    /// ELO's logistic 400-point scale models) and anchored so the Dummy (the app's Beginner) sits
    /// at <see cref="AnchorElo"/>. The printed numbers are pasted into the app's level list.
    /// </summary>
    public static class EloTournament
    {
        private const double AnchorElo = 1200d;

        // ELO is Bradley-Terry on a base-10, 400-point scale: gap = 400 * log10(strength ratio).
        private const double EloPerDecade = 400d;

        // A 50/50 prior worth this fraction of each matchup's games keeps a blow-out (the Master
        // against Random wins nearly every game) from giving an infinite gap; close matchups are
        // left essentially untouched.
        private const double PriorFraction = 0.01d;

        /// <summary>Plays the round robin and prints the ratings.</summary>
        /// <param name="parallelism">How many games run at once.</param>
        /// <param name="fastPairs">Mirrored pairs per matchup of the fast levels.</param>
        /// <param name="slowPairs">Mirrored pairs per matchup with the ISMCTS level (it spends its time budget on every card).</param>
        public static void Run(int parallelism, int fastPairs, int slowPairs)
        {
            var levels = new[]
            {
                new Level("dummy", "DummyPlayer", () => new DummyPlayer(), isSlow: false),
                new Level("random", "RandomPlayer", () => new RandomPlayer(), isSlow: false),
                new Level("smart", "SmartPlayer", () => new SmartPlayer(), isSlow: false),
                new Level("claude", "ClaudePlayerIsmcts", () => new ClaudePlayerIsmcts(), isSlow: true),
            };

            var n = levels.Length;
            var wins = new double[n, n];
            var games = new double[n, n];
            Console.WriteLine($"ELO round robin of the app's levels, pair vs pair: {fastPairs} mirrored pairs a matchup ({slowPairs} with ISMCTS)");
            Console.WriteLine(new string('=', Program.LineLength));

            var total = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var pairs = levels[i].IsSlow || levels[j].IsSlow ? slowPairs : fastPairs;
                    var stopwatch = Stopwatch.StartNew();
                    var winsI = PlayMirroredPairs(levels[i].Create, levels[j].Create, pairs, parallelism);
                    var played = pairs * 2;
                    wins[i, j] += winsI;
                    wins[j, i] += played - winsI;
                    games[i, j] += played;
                    games[j, i] += played;
                    Console.WriteLine(
                        $"  {levels[i].Name,-20} {winsI,6} - {played - winsI,-6} {levels[j].Name,-20} ({100.0 * winsI / played,5:0.0}% | {stopwatch.Elapsed:hh\\:mm\\:ss\\.f})");
                }
            }

            var ratings = FitElo(wins, games, n);
            var order = new int[n];
            for (var i = 0; i < n; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (a, b) => ratings[b].CompareTo(ratings[a]));
            Console.WriteLine(new string('=', Program.LineLength));
            Console.WriteLine($"Pair ratings (anchor {levels[0].Name} = {AnchorElo:0}), total time {total.Elapsed:hh\\:mm\\:ss}:");
            foreach (var i in order)
            {
                Console.WriteLine($"  {levels[i].Id,-8} {levels[i].Name,-20} {(int)Math.Round(ratings[i]),6}");
            }

            Console.WriteLine(new string('=', Program.LineLength));
        }

        // Two of level A against two of level B; returns A's wins out of 2 * pairs games.
        private static int PlayMirroredPairs(Func<IPlayer> levelA, Func<IPlayer> levelB, int pairs, int parallelism)
        {
            var players = new ThreadLocal<IPlayer[]>(() => new[] { levelA(), levelA(), levelB(), levelB() });
            var winsA = 0;
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
                    var won = (first.Winner == PlayerPosition.SouthNorthTeam ? 1 : 0) + (second.Winner == PlayerPosition.EastWestTeam ? 1 : 0);
                    Interlocked.Add(ref winsA, won);
                });
            return winsA;
        }

        // Bradley-Terry strengths by the minorization-maximization iteration, on the ELO scale,
        // shifted so the anchor (index 0) sits at AnchorElo.
        private static double[] FitElo(double[,] wins, double[,] games, int n)
        {
            var winsWithPrior = new double[n, n];
            var gamesWithPrior = new double[n, n];
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        var prior = PriorFraction * games[i, j];
                        winsWithPrior[i, j] = wins[i, j] + (0.5 * prior);
                        gamesWithPrior[i, j] = games[i, j] + prior;
                    }
                }
            }

            var strength = new double[n];
            Array.Fill(strength, 1d);
            for (var iteration = 0; iteration < 10000; iteration++)
            {
                var next = new double[n];
                for (var i = 0; i < n; i++)
                {
                    double totalWins = 0, denominator = 0;
                    for (var j = 0; j < n; j++)
                    {
                        if (i != j)
                        {
                            totalWins += winsWithPrior[i, j];
                            denominator += gamesWithPrior[i, j] / (strength[i] + strength[j]);
                        }
                    }

                    next[i] = denominator > 0 ? totalWins / denominator : strength[i];
                }

                // Geometric mean 1 keeps the iteration stable.
                var logSum = 0d;
                for (var i = 0; i < n; i++)
                {
                    logSum += Math.Log(next[i]);
                }

                var scale = Math.Exp(-logSum / n);
                var maxDelta = 0d;
                for (var i = 0; i < n; i++)
                {
                    next[i] *= scale;
                    maxDelta = Math.Max(maxDelta, Math.Abs(next[i] - strength[i]));
                    strength[i] = next[i];
                }

                if (maxDelta < 1e-12)
                {
                    break;
                }
            }

            var ratings = new double[n];
            for (var i = 0; i < n; i++)
            {
                ratings[i] = EloPerDecade * Math.Log10(strength[i]);
            }

            var shift = AnchorElo - ratings[0];
            for (var i = 0; i < n; i++)
            {
                ratings[i] += shift;
            }

            return ratings;
        }

        private sealed class Level
        {
            public Level(string id, string name, Func<IPlayer> create, bool isSlow)
            {
                this.Id = id;
                this.Name = name;
                this.Create = create;
                this.IsSlow = isSlow;
            }

            public string Id { get; }

            public string Name { get; }

            public Func<IPlayer> Create { get; }

            public bool IsSlow { get; }
        }
    }
}
