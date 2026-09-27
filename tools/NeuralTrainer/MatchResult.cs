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

        public double Elo => this.Score is <= 0 or >= 1 ? double.NaN : 400 * Math.Log10(this.Score / (1 - this.Score));

        /// <summary>Gets the approximate Elo standard error by the delta method.</summary>
        public double EloSigma => this.Score is <= 0 or >= 1
            ? double.NaN
            : 400 * this.Sigma / (Math.Log(10) * this.Score * (1 - this.Score));

        public double Lower95 => Math.Max(0, this.Score - (1.959963984540054 * this.Sigma));

        public double Upper95 => Math.Min(1, this.Score + (1.959963984540054 * this.Sigma));

        public override string ToString() =>
            $"{this.Games} games: {this.Score:P3} ± {this.Sigma:P3}, 95% [{this.Lower95:P3}, {this.Upper95:P3}], "
            + $"{this.PointsPerGame:+0.0;-0.0} points a game, ELO {this.Elo:+0;-0} ± {this.EloSigma:0}";
    }
}
