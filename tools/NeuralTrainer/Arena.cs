namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine;
    using Belot.Engine.Players;
    using BelotLegacy;

    /// <summary>Mirrored whole games with fresh seeded players, independent of worker scheduling.</summary>
    internal static class Arena
    {
        public static void Run(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var opponentModels = string.IsNullOrEmpty(settings.OpponentIn) ? models : NeuralModels.Load(settings.OpponentIn);
            Console.WriteLine($"arena: {settings.Player} vs {settings.Opponent}, {settings.Pairs} pairs, seed {settings.Seed}, threads {settings.Threads}, weights {settings.In}");
            var clock = Stopwatch.StartNew();
            var result = Play(
                OpponentCatalog.Factory(settings.Player, models),
                OpponentCatalog.Factory(settings.Opponent, opponentModels),
                settings.Pairs,
                settings.Threads,
                checked(settings.Seed * 100_000),
                done => Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {done}/{settings.Pairs} pairs"));
            Console.WriteLine($"{settings.Player} vs {settings.Opponent}: {result.Match} ({clock.Elapsed})");
            Console.WriteLine($"legacy diagnostics A: {JsonSerializer.Serialize(result.TeamA)}; B: {JsonSerializer.Serialize(result.TeamB)}");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
            var report = new
            {
                settings.Player,
                settings.Opponent,
                settings.In,
                settings.OpponentIn,
                settings.Seed,
                settings.Pairs,
                settings.Threads,
                Result = result,
                Seconds = clock.Elapsed.TotalSeconds,
            };
            var options = new JsonSerializerOptions { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
            File.WriteAllText(settings.Data + ".arena.json", JsonSerializer.Serialize(report, options));
        }

        public static Result Play(Func<int, IPlayer> teamA, Func<int, IPlayer> teamB, int pairs, int threads, int seed, Action<int> progress = null)
        {
            if (pairs < 2 || threads <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pairs), "Use at least two mirrored pairs and a positive thread count.");
            }

            var scores = new double[pairs];
            var points = new int[pairs];
            var diagnosticsA = new Diagnostics();
            var diagnosticsB = new Diagnostics();
            var completed = 0;
            Parallel.For(0, pairs, new ParallelOptions { MaxDegreeOfParallelism = threads }, pair =>
            {
                var pairSeed = unchecked(seed + pair);
                var firstToPlay = (PlayerPosition)(1 << (pair % 4));
                for (var leg = 0; leg < 2; leg++)
                {
                    var a = new[] { teamA(pairSeed), teamA(unchecked(pairSeed + 104729)) };
                    var b = new[] { teamB(pairSeed), teamB(unchecked(pairSeed + 104729)) };
                    var game = leg == 0 ? new BelotGame(a[0], b[0], a[1], b[1], new Random(pairSeed))
                        : new BelotGame(b[0], a[0], b[1], a[1], new Random(pairSeed));
                    var outcome = game.PlayGame(firstToPlay);
                    var side = leg == 0 ? PlayerPosition.SouthNorthTeam : PlayerPosition.EastWestTeam;
                    scores[pair] += outcome.Winner == side ? .5 : 0;
                    var difference = outcome.SouthNorthPoints - outcome.EastWestPoints;
                    points[pair] += leg == 0 ? difference : -difference;
                    foreach (var player in a)
                    {
                        diagnosticsA.Add(player);
                    }

                    foreach (var player in b)
                    {
                        diagnosticsB.Add(player);
                    }
                }

                var done = Interlocked.Increment(ref completed);
                if (done % Math.Max(10, pairs / 10) == 0 || done == pairs)
                {
                    progress?.Invoke(done);
                }
            });

            var mean = scores.Average();
            var sigma = Math.Sqrt(scores.Sum(score => (score - mean) * (score - mean)) / (pairs - 1) / pairs);
            return new Result
            {
                Match = new MatchResult(pairs * 2, mean, sigma, points.Average() / 2),
                PairScores = scores,
                PairPoints = points,
                TeamA = diagnosticsA,
                TeamB = diagnosticsB,
            };
        }

        internal sealed class Result
        {
            public MatchResult Match { get; init; }

            public double[] PairScores { get; init; }

            public int[] PairPoints { get; init; }

            public Diagnostics TeamA { get; init; }

            public Diagnostics TeamB { get; init; }
        }

        internal sealed class Diagnostics
        {
            private readonly long[] counts = new long[5];

            public long BidDecisions => this.counts[0];

            public long CardDecisions => this.counts[1];

            public long RejectedBids => this.counts[2];

            public long CardFallbacks => this.counts[3];

            public long LegalSetDifferences => this.counts[4];

            public void Add(IPlayer player)
            {
                if (player is ILegacyDiagnostics legacy)
                {
                    Interlocked.Add(ref this.counts[0], legacy.BidDecisions);
                    Interlocked.Add(ref this.counts[1], legacy.CardDecisions);
                    Interlocked.Add(ref this.counts[2], legacy.RejectedBids);
                    Interlocked.Add(ref this.counts[3], legacy.CardFallbacks);
                    Interlocked.Add(ref this.counts[4], legacy.LegalSetDifferences);
                }
            }
        }
    }
}
