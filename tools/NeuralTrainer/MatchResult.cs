namespace Belot.NeuralTrainer
{
    using System;

    /// <summary>The outcome of mirrored games for team A: its share of the wins and points.</summary>
    internal sealed class MatchResult
    {
        public MatchResult(int games, double score, double sigma, double pointsPerGame)
        {
            this.Games = games;
            this.Score = score;
            this.Sigma = sigma;
            this.PointsPerGame = pointsPerGame;
        }

        public int Games { get; }

        /// <summary>Gets team A's share of the games won.</summary>
        public double Score { get; }

        public double Sigma { get; }

        /// <summary>Gets team A's points minus team B's, per game.</summary>
        public double PointsPerGame { get; }

        public double Elo => this.Score is <= 0 or >= 1 ? double.NaN : -400 * Math.Log10((1 / this.Score) - 1);

        public override string ToString() =>
            $"{this.Games} games: {this.Score:P1} ± {this.Sigma:P1}, {this.PointsPerGame:+0.0;-0.0} points a game, ELO {this.Elo:+0;-0}";
    }
}
