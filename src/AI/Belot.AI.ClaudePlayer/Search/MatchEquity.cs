namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// The chance that a team wins the match (to 151) from given scores at the start of a deal,
    /// between equal teams: a dynamic program over the distribution of the game points a deal
    /// awards, measured in self-play (Search/MatchOutcomes.csv). A deal ends the match as
    /// <c>BelotMatch</c> ends it: a team with 151 or more and more points than the other, that
    /// scored in the deal, which was played and was no capot. With it, the search plays for the
    /// match as people do (the score decides how much a risk or a capot is worth), not for points.
    /// </summary>
    internal sealed class MatchEquity
    {
        /// <summary>The highest score the table holds; higher scores count as this one.</summary>
        public const int MaxScore = 400;

        public const int PointsToWin = 151;

        private const int Width = MaxScore + 1;

        private static readonly Lazy<MatchEquity> EmbeddedTable = new Lazy<MatchEquity>(() => FromCsv(ReadEmbedded()));

        // [ours * Width + theirs]: the chance that the team with 'ours' points wins.
        private readonly float[] table = new float[Width * Width];

        private MatchEquity(IReadOnlyList<(int Ours, int Theirs, bool Capot, bool Passed, double Weight)> outcomes)
        {
            // Symmetric in the teams: every outcome also happens the other way round.
            var list = new List<(int Ours, int Theirs, bool Capot, bool Passed, double Weight)>();
            var total = 0.0;
            foreach (var outcome in outcomes)
            {
                list.Add(outcome);
                list.Add((outcome.Theirs, outcome.Ours, outcome.Capot, outcome.Passed, outcome.Weight));
                total += 2 * outcome.Weight;
            }

            // A deal that awards nothing to anybody leaves the scores as they were.
            var still = 0.0;
            var moves = new List<(int Ours, int Theirs, bool Capot, double Probability)>();
            foreach (var outcome in list)
            {
                if (outcome.Passed || (outcome.Ours == 0 && outcome.Theirs == 0))
                {
                    still += outcome.Weight / total;
                }
                else
                {
                    moves.Add((outcome.Ours, outcome.Theirs, outcome.Capot, outcome.Weight / total));
                }
            }

            var scale = 1 / (1 - still);
            for (var sum = 2 * MaxScore; sum >= 0; sum--)
            {
                for (var ours = Math.Min(MaxScore, sum); ours >= 0 && sum - ours <= MaxScore; ours--)
                {
                    var theirs = sum - ours;
                    if (ours == MaxScore && theirs == MaxScore)
                    {
                        this.table[(ours * Width) + theirs] = 0.5f;
                        continue;
                    }

                    var win = 0.0;
                    foreach (var move in moves)
                    {
                        win += move.Probability * After(ours, theirs, move.Ours, move.Theirs, move.Capot);
                    }

                    this.table[(ours * Width) + theirs] = (float)(win * scale);
                }
            }

            double After(int ours, int theirs, int gained, int lost, bool capot)
            {
                var end = Ends(ours + gained, theirs + lost, gained, lost, capot);
                if (end != 0)
                {
                    return end > 0 ? 1 : 0;
                }

                var a = Math.Min(MaxScore, ours + gained);
                var b = Math.Min(MaxScore, theirs + lost);
                return this.table[(a * Width) + b];
            }
        }

        /// <summary>Gets the table from the embedded self-play outcomes.</summary>
        public static MatchEquity Embedded => EmbeddedTable.Value;

        /// <summary>
        /// Whether a played deal ends the match: 1 when the team wins it, -1 when the other team
        /// does, 0 when the match goes on. The scores include the deal.
        /// </summary>
        public static int Ends(int ours, int theirs, int gained, int lost, bool capot)
        {
            if (capot)
            {
                return 0;
            }

            if (ours >= PointsToWin && ours > theirs && gained > 0)
            {
                return 1;
            }

            if (theirs >= PointsToWin && theirs > ours && lost > 0)
            {
                return -1;
            }

            return 0;
        }

        public static MatchEquity FromCsv(TextReader reader)
        {
            var outcomes = new List<(int, int, bool, bool, double)>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length != 5)
                {
                    continue;
                }

                outcomes.Add((
                    int.Parse(parts[0], CultureInfo.InvariantCulture),
                    int.Parse(parts[1], CultureInfo.InvariantCulture),
                    parts[2] == "1",
                    parts[3] == "1",
                    double.Parse(parts[4], CultureInfo.InvariantCulture)));
            }

            return new MatchEquity(outcomes);
        }

        /// <summary>The chance that the team with <paramref name="ours"/> points wins, at the start of a deal.</summary>
        public double Win(int ours, int theirs) => this.table[(Math.Min(MaxScore, Math.Max(0, ours)) * Width) + Math.Min(MaxScore, Math.Max(0, theirs))];

        /// <summary>
        /// The team's chance to win the match after a deal that took the scores from
        /// (<paramref name="ours"/>, <paramref name="theirs"/>) by the points gained.
        /// </summary>
        public double AfterDeal(int ours, int theirs, int gained, int lost, bool capot)
        {
            var end = Ends(ours + gained, theirs + lost, gained, lost, capot);
            return end > 0 ? 1 : end < 0 ? 0 : this.Win(ours + gained, theirs + lost);
        }

        /// <summary>How many game points a whole match won is worth at these scores, at the margin (to give values in points).</summary>
        public double PointsPerEquity(int ours, int theirs)
        {
            var slope = (this.Win(ours + 10, theirs) - this.Win(ours, theirs + 10)) / 20;
            return 1 / Math.Max(0.0005, slope);
        }

        private static TextReader ReadEmbedded()
        {
            var stream = typeof(MatchEquity).GetTypeInfo().Assembly.GetManifestResourceStream("Belot.AI.ClaudePlayer.Search.MatchOutcomes.csv")
                ?? throw new InvalidOperationException("The match outcomes are not embedded.");
            return new StreamReader(stream);
        }
    }
}
