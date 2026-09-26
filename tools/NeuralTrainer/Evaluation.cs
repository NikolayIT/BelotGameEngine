namespace Belot.NeuralTrainer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.Engine;
    using Belot.Engine.Players;

    /// <summary>
    /// Mirrored pairs of whole games through the engine (as the simulator's suites play them):
    /// every pair plays the same deals twice, the teams swapping seats, so the luck of the cards
    /// cancels out.
    /// </summary>
    internal static class Evaluation
    {
        public static MatchResult MirrorMatch(
            Func<IPlayer> teamA1,
            Func<IPlayer> teamA2,
            Func<IPlayer> teamB1,
            Func<IPlayer> teamB2,
            int pairs,
            int parallelism,
            int seed = 0)
        {
            using var players = new ThreadLocal<IPlayer[]>(() => new[] { teamA1(), teamA2(), teamB1(), teamB2() });
            var pairScores = new double[pairs];
            long pointsA = 0;
            long pointsB = 0;
            Parallel.For(
                0,
                pairs,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                i =>
                {
                    var p = players.Value;
                    var firstToPlay = (PlayerPosition)(1 << (i % 4));
                    var first = new BelotGame(p[0], p[2], p[1], p[3], new Random(seed + i)).PlayGame(firstToPlay);
                    var second = new BelotGame(p[2], p[0], p[3], p[1], new Random(seed + i)).PlayGame(firstToPlay);
                    var winsA = (first.Winner == PlayerPosition.SouthNorthTeam ? 1 : 0)
                                + (second.Winner == PlayerPosition.EastWestTeam ? 1 : 0);
                    pairScores[i] = winsA / 2.0;
                    Interlocked.Add(ref pointsA, first.SouthNorthPoints + second.EastWestPoints);
                    Interlocked.Add(ref pointsB, first.EastWestPoints + second.SouthNorthPoints);
                });

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
            return new MatchResult(pairs * 2, mean, sigma, (double)(pointsA - pointsB) / (pairs * 2));
        }
    }
}
