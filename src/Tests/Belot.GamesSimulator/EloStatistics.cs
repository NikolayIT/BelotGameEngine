namespace Belot.GamesSimulator
{
    using System;
    using System.Linq;

    /// <summary>Mirrored-pair uncertainty and Bradley-Terry ratings for the app's round robin.</summary>
    internal static class EloStatistics
    {
        private const double EloPerDecade = 400d;

        // Same 1% fifty-fifty prior as the original rating fit: avoids infinite blow-out gaps.
        private const double PriorFraction = 0.01d;

        public static double StandardError(double[] scores)
        {
            if (scores.Length < 2)
            {
                return 0;
            }

            var mean = scores.Average();
            var variance = 0d;
            foreach (var score in scores)
            {
                variance += (score - mean) * (score - mean);
            }

            return Math.Sqrt(variance / (scores.Length - 1) / scores.Length);
        }

        // Resample complete mirrored pairs, sharing seed indexes across matchups. The common
        // slow range and the additional fast range are separate strata, preserving their sizes.
        public static double[] BootstrapErrors(double[][] pairScores, int n, int repetitions, int seed, double anchor)
        {
            if (repetitions < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(repetitions));
            }

            var boundaries = pairScores.Where(x => x != null).Select(x => x.Length).Distinct().OrderBy(x => x).ToArray();
            var sampled = new int[boundaries[boundaries.Length - 1]];
            var wins = new double[n, n];
            var games = new double[n, n];
            var means = new double[n];
            var sums = new double[n];
            var rng = new Random(seed);
            for (var repeat = 0; repeat < repetitions; repeat++)
            {
                var start = 0;
                foreach (var end in boundaries)
                {
                    for (var k = start; k < end; k++)
                    {
                        sampled[k] = rng.Next(start, end);
                    }

                    start = end;
                }

                for (var i = 0; i < n; i++)
                {
                    for (var j = i + 1; j < n; j++)
                    {
                        var scores = pairScores[(i * n) + j];
                        var won = 0d;
                        for (var k = 0; k < scores.Length; k++)
                        {
                            won += 2 * scores[sampled[k]];
                        }

                        wins[i, j] = won;
                        wins[j, i] = (2 * scores.Length) - won;
                        games[i, j] = games[j, i] = 2 * scores.Length;
                    }
                }

                var ratings = Fit(wins, games, n, anchor);
                for (var i = 0; i < n; i++)
                {
                    var delta = ratings[i] - means[i];
                    means[i] += delta / (repeat + 1);
                    sums[i] += delta * (ratings[i] - means[i]);
                }
            }

            return sums.Select(sum => Math.Sqrt(Math.Max(0, sum) / (repetitions - 1))).ToArray();
        }

        // Bradley-Terry strengths by the minorization-maximization iteration, on the ELO scale,
        // shifted so the anchor (index 0) sits at the requested rating.
        public static double[] Fit(double[,] wins, double[,] games, int n, double anchor)
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

            var shift = anchor - ratings[0];
            for (var i = 0; i < n; i++)
            {
                ratings[i] += shift;
            }

            return ratings;
        }
    }
}
