namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
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
            var opponent = ResolveOpponent(settings, models);
            Console.WriteLine($"arena: {settings.Player} vs {opponent.Name}, {settings.Pairs} pairs, seed {settings.Seed}, threads {settings.Threads}, weights {settings.In}");
            Console.WriteLine(settings);
            Console.WriteLine($"resolved opponent: {opponent.Name}, weights {(string.IsNullOrEmpty(opponent.In) ? "embedded" : opponent.In)}");
            if (opponent.Settings != null)
            {
                Console.WriteLine($"opponent settings: {opponent.Settings}");
            }

            var clock = Stopwatch.StartNew();
            var result = Play(
                settings.Player == "candidate" ? seed => OpponentCatalog.Configured(settings, models, seed) : OpponentCatalog.Factory(settings.Player, models),
                opponent.Create,
                settings.Pairs,
                settings.Threads,
                checked(settings.Seed * 100_000),
                done => Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {done}/{settings.Pairs} pairs"));
            Console.WriteLine($"{settings.Player} vs {opponent.Name}: {result.Match} ({clock.Elapsed})");
            Console.WriteLine($"legacy diagnostics A: {JsonSerializer.Serialize(result.TeamA)}; B: {JsonSerializer.Serialize(result.TeamB)}");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
            var report = new
            {
                settings.Player,
                settings.Opponent,
                settings.In,
                settings.OpponentIn,
                settings.OpponentConfig,
                ResolvedOpponent = opponent,
                settings.Seed,
                settings.Pairs,
                settings.Threads,
                Settings = settings,
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
                var firstPlayerSeed = PlayerSeed(pairSeed, 0);
                var secondPlayerSeed = PlayerSeed(pairSeed, 1);
                for (var leg = 0; leg < 2; leg++)
                {
                    var a = new[] { teamA(firstPlayerSeed), teamA(secondPlayerSeed) };
                    var b = new[] { teamB(firstPlayerSeed), teamB(secondPlayerSeed) };
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

        /// <summary>Resolves an independent configured neural opponent, or preserves the named catalog path.</summary>
        internal static OpponentConfiguration ResolveOpponent(TrainingSettings settings, NeuralModels models)
        {
            TrainingSettings configured = null;
            if (!string.IsNullOrEmpty(settings.OpponentConfig))
            {
                if (!string.Equals(settings.Opponent, TrainingSettings.DefaultOpponent, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(settings.Opponent, "configured", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("--opponent-config conflicts with a named --opponent; omit --opponent or use configured.", nameof(settings));
                }

                using var document = JsonDocument.Parse(File.ReadAllText(settings.OpponentConfig));
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Opponent configuration must be a TrainingSettings JSON object.");
                }

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new JsonException($"Duplicate opponent setting: {property.Name}.");
                    }
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                };
                configured = document.RootElement.Deserialize<TrainingSettings>(options);
                if (!string.IsNullOrEmpty(configured.OpponentConfig))
                {
                    throw new ArgumentException("Opponent configuration cannot load another opponent configuration.", nameof(settings));
                }
            }

            var input = !string.IsNullOrEmpty(configured?.In) ? configured.In : settings.OpponentIn;
            var opponentModels = string.IsNullOrEmpty(input) ? models : NeuralModels.Load(input);
            var resolvedInput = string.IsNullOrEmpty(input) ? settings.In : input;
            resolvedInput = string.IsNullOrEmpty(resolvedInput) ? string.Empty : Path.GetFullPath(resolvedInput);
            if (configured == null)
            {
                return new OpponentConfiguration
                {
                    Name = settings.Opponent,
                    In = resolvedInput,
                    Create = OpponentCatalog.Factory(settings.Opponent, opponentModels),
                };
            }

            configured.In = resolvedInput;
            configured.Player = "candidate";
            return new OpponentConfiguration
            {
                Name = "configured",
                In = resolvedInput,
                Settings = configured,
                Create = seed => OpponentCatalog.Configured(configured, opponentModels, seed),
            };
        }

        // Do not restart a player's RNG at the same sequence used to shuffle its deal.
        // Both teams still share player seeds for common random numbers in comparisons.
        private static int PlayerSeed(int dealSeed, int partner)
        {
            var value = unchecked((uint)dealSeed + 0x9E3779B9u + ((uint)partner * 0x85EBCA6Bu));
            value = unchecked((value ^ (value >> 16)) * 0x85EBCA6Bu);
            value = unchecked((value ^ (value >> 13)) * 0xC2B2AE35u);
            return unchecked((int)(value ^ (value >> 16)));
        }

        internal sealed class OpponentConfiguration
        {
            public string Name { get; init; }

            public string In { get; init; }

            public TrainingSettings Settings { get; init; }

            [JsonIgnore]
            public Func<int, IPlayer> Create { get; init; }
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
