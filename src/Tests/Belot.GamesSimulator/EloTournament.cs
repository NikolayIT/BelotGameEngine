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
    /// The round robin behind the MAUI app's levels (its <c>AiLevels</c>, plus ClaudePlayerIsmcts
    /// for reference, which the app no longer uses): every two levels play pair against pair, in
    /// mirrored pairs of games (the same deals, the teams swapped, so the cards' luck cancels),
    /// then the ratings are fit with the Bradley-Terry model (which is what
    /// ELO's logistic 400-point scale models) and anchored so the Dummy (the app's Beginner) sits
    /// at <see cref="AnchorElo"/>. The printed numbers are pasted into the app's level list.
    /// </summary>
    public static class EloTournament
    {
        private const double AnchorElo = 1200d;

        // How long a level's games take, which decides its matchups' pair counts.
        private enum Tier
        {
            Fast,
            Master,
            Slow,
        }

        /// <summary>Plays the round robin and prints the ratings.</summary>
        /// <param name="parallelism">How many games run at once.</param>
        /// <param name="fastPairs">Mirrored pairs per matchup of the fast levels.</param>
        /// <param name="slowPairs">Mirrored pairs per matchup with ISMCTS, which searches on every card.</param>
        /// <param name="masterPairs">Mirrored pairs per matchup with the Master, whose early tricks search.</param>
        public static void Run(int parallelism, int fastPairs, int slowPairs, int masterPairs)
        {
            var levels = new[]
            {
                new Level("dummy", "DummyPlayer", () => new DummyPlayer(), Tier.Fast),
                new Level("random", "RandomPlayer", () => new RandomPlayer(), Tier.Fast),
                new Level("smart", "SmartPlayer", () => new SmartPlayer(), Tier.Fast),
                new Level(
                    "expert",
                    "Human-style Expert",
                    ClaudePlayerProfiles.CreateExpert,
                    Tier.Fast),
                new Level(
                    "claude",
                    "Human-style Master",
                    ClaudePlayerProfiles.CreateMaster,
                    Tier.Master),
                new Level("ismcts", "ClaudePlayerIsmcts", () => new ClaudePlayerIsmcts(), Tier.Slow),
            };

            var n = levels.Length;
            var wins = new double[n, n];
            var games = new double[n, n];
            var pairScores = new double[n * n][];
            Console.WriteLine($"ELO round robin of the app's levels, pair vs pair: {fastPairs} mirrored pairs a matchup ({masterPairs} with the Master, {slowPairs} with ISMCTS)");
            Console.WriteLine(new string('=', Program.LineLength));

            var total = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var tier = (Tier)Math.Max((int)levels[i].Tier, (int)levels[j].Tier);
                    var pairs = tier == Tier.Slow ? slowPairs : tier == Tier.Master ? masterPairs : fastPairs;
                    var stopwatch = Stopwatch.StartNew();
                    var (winsI, scores) = PlayMirroredPairs(levels[i].Create, levels[j].Create, pairs, parallelism);
                    pairScores[(i * n) + j] = scores;
                    var sigma = EloStatistics.StandardError(scores);
                    var played = pairs * 2;
                    wins[i, j] += winsI;
                    wins[j, i] += played - winsI;
                    games[i, j] += played;
                    games[j, i] += played;
                    Console.WriteLine(
                        $"  {levels[i].Name,-20} {winsI,6} - {played - winsI,-6} {levels[j].Name,-20} "
                        + $"({100.0 * winsI / played,7:0.000}% +/- {100 * sigma:0.000} pp; {played} games | {stopwatch.Elapsed:hh\\:mm\\:ss\\.f})");
                }
            }

            var ratings = EloStatistics.Fit(wins, games, n, AnchorElo);
            var errors = EloStatistics.BootstrapErrors(pairScores, n, 1000, 63841, AnchorElo);
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
                var played = 0d;
                for (var j = 0; j < n; j++)
                {
                    played += games[i, j];
                }

                Console.WriteLine($"  {levels[i].Id,-8} {levels[i].Name,-20} {ratings[i],6:0} +/- {errors[i]:0.0} ({played:0} games)");
            }

            Console.WriteLine("Rating errors: 1 sigma from 1,000 shared-seed mirrored-pair bootstrap samples; anchor fixed.");
            Console.WriteLine(new string('=', Program.LineLength));
        }

        // Two of level A against two of level B. The independent units for uncertainty are pairs.
        private static (int Wins, double[] Scores) PlayMirroredPairs(Func<IPlayer> levelA, Func<IPlayer> levelB, int pairs, int parallelism)
        {
            using var players = new ThreadLocal<IPlayer[]>(() => new[] { levelA(), levelA(), levelB(), levelB() });
            var pairScores = new double[pairs];
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
                    pairScores[i] = won / 2.0;
                    Interlocked.Add(ref winsA, won);
                });
            return (winsA, pairScores);
        }

        private sealed class Level
        {
            public Level(string id, string name, Func<IPlayer> create, Tier tier)
            {
                this.Id = id;
                this.Name = name;
                this.Create = create;
                this.Tier = tier;
            }

            public string Id { get; }

            public string Name { get; }

            public Func<IPlayer> Create { get; }

            public Tier Tier { get; }
        }
    }
}
