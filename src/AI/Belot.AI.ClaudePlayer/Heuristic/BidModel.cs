namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// The learned bidding's weights: for each kind of bid (<see cref="BidFeatures.Suit"/>, no
    /// trumps, all trumps, double) and each situation (<see cref="BidFeatures.Class"/>), what each
    /// of <see cref="BidFeatures"/>' numbers adds to the game points the bid is expected to bring
    /// over passing. Fitted to how the bids did once the deals were over (see HEURISTIC_PLAYER.md).
    /// </summary>
    public sealed class BidModel
    {
        private static readonly Lazy<BidModel> Embedded = new Lazy<BidModel>(() => Parse(BidModelWeights.Text));

        private readonly float[][][] weights = new float[BidFeatures.Kinds][][];

        // The cells a bid may be made in, per kind and situation (all when unset).
        private readonly ulong?[][] gates = new ulong?[BidFeatures.Kinds][];

        // A cell's own value, used instead of the weights when given (the doubles).
        private readonly float?[][][] cellValues = new float?[BidFeatures.Kinds][][];

        public BidModel()
        {
            for (var kind = 0; kind < BidFeatures.Kinds; kind++)
            {
                this.weights[kind] = new float[BidFeatures.Classes][];
                this.gates[kind] = new ulong?[BidFeatures.Classes];
                this.cellValues[kind] = new float?[BidFeatures.Classes][];
            }
        }

        /// <summary>Gets the model the heuristic player bids by unless told otherwise (see HEURISTIC_PLAYER.md).</summary>
        public static BidModel Default => Embedded.Value;

        /// <summary>
        /// Reads a model in the trainer's text format: one line per kind and situation, the kind's
        /// name (suit, nt, at, double), the situation's name (as <see cref="BidFeatures.ClassName"/>)
        /// and <see cref="BidFeatures.Count"/> weights.
        /// </summary>
        public static BidModel Parse(string text)
        {
            var model = new BidModel();
            using var reader = new StringReader(text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] == "gate" || parts[0] == "cell")
                {
                    // gate <kind> <situation> <hex mask of the cells allowed>; cell <kind> <situation> <cell> <value>
                    var gateKind = Kind(parts[1]);
                    var gateSituation = Situation(parts[2]);
                    if (gateKind < 0 || gateSituation < 0)
                    {
                        throw new FormatException($"Bad bid model line: {line}");
                    }

                    if (parts[0] == "gate")
                    {
                        model.gates[gateKind][gateSituation] = ulong.Parse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        var values = model.cellValues[gateKind][gateSituation] ??= new float?[64];
                        values[int.Parse(parts[3], CultureInfo.InvariantCulture)] = float.Parse(parts[4], CultureInfo.InvariantCulture);
                    }

                    continue;
                }

                var kind = Kind(parts[0]);
                var situation = Situation(parts[1]);
                if (kind < 0 || situation < 0 || parts.Length != 2 + BidFeatures.Count)
                {
                    throw new FormatException($"Bad bid model line: {line}");
                }

                var row = new float[BidFeatures.Count];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = float.Parse(parts[2 + i], CultureInfo.InvariantCulture);
                }

                model.weights[kind][situation] = row;
            }

            return model;
        }

        public static BidModel Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>Whether the model lets the bid be made in its cell.</summary>
        internal bool Allows(int kind, int situation, int cell)
        {
            var gate = this.gates[kind][situation];
            return gate == null || (cell >= 0 && cell < 64 && (gate.Value & (1UL << cell)) != 0);
        }

        /// <summary>Whether the model decides any kind of bid in the situation (weights, cells or a gate).</summary>
        internal bool Covers(int situation)
        {
            for (var kind = 0; kind < BidFeatures.Kinds; kind++)
            {
                if (this.weights[kind][situation] != null || this.cellValues[kind][situation] != null || this.gates[kind][situation] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The expected game points of the bid over passing, or negative infinity when the model never makes it there.</summary>
        internal double Gain(int kind, int situation, int cell, ReadOnlySpan<float> features)
        {
            var values = this.cellValues[kind][situation];
            if (values != null)
            {
                return cell >= 0 && cell < 64 && values[cell].HasValue ? values[cell].Value : double.NegativeInfinity;
            }

            if (!this.Allows(kind, situation, cell))
            {
                return double.NegativeInfinity;
            }

            var row = this.weights[kind][situation];
            if (row == null)
            {
                return double.NegativeInfinity;
            }

            var sum = 0.0;
            for (var i = 0; i < row.Length; i++)
            {
                sum += row[i] * features[i];
            }

            return sum;
        }

        private static int Kind(string name) => Array.IndexOf(new[] { "suit", "nt", "at", "double" }, name);

        private static int Situation(string name)
        {
            for (var i = 0; i < BidFeatures.Classes; i++)
            {
                if (BidFeatures.ClassName(i) == name)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
