namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Samples publicly consistent assignments with mass equal to the product of card ownership weights.
    /// Dynamic programming conditions the product on exact hand sizes, known cards and exclusions.
    /// Fixed known cards are conditioned on first, so their weights do not affect the partition.
    /// </summary>
    internal sealed class WeightedWorldSampler
    {
        private const int HandSize = 8;
        private const int Width = HandSize + 1;
        private const int WeightCount = 32 * 3;

        private readonly int[] seats = new int[3];
        private readonly int[] needs = new int[3];
        private readonly uint[] known = new uint[3];
        private readonly int[] pool = new int[3 * HandSize];
        private readonly double[] weights = new double[3 * 3 * HandSize];
        private readonly double[] logWeights = new double[3 * 3 * HandSize];
        private readonly double[] completions = new double[((3 * HandSize) + 1) * Width * Width];
        private readonly bool[] computed = new bool[((3 * HandSize) + 1) * Width * Width];

        private int poolCount;
        private bool configured;
        private bool logarithmic;

        /// <summary>Gets the partition over unfixed cards; extreme finite inputs may overflow or underflow this value.</summary>
        public double TotalWeight { get; private set; }

        /// <summary>Gets the logarithm of the partition, or negative infinity if configuration failed.</summary>
        public double LogTotalWeight { get; private set; } = double.NegativeInfinity;

        /// <summary>
        /// The 96 weights are indexed by absolute card times three plus relative owner minus one.
        /// Relative owners one, two and three are the next seat, partner and preceding seat.
        /// All supplied weights must be finite and nonnegative; impossible constraints are never relaxed.
        /// </summary>
        public bool Configure(RoundKnowledge knowledge, ReadOnlySpan<float> ownershipWeights) => this.Configure(
            knowledge.Me, knowledge.MyHand, knowledge.Played, knowledge.Known, knowledge.Excluded, knowledge.HandCounts, ownershipWeights);

        /// <summary>Replaces only the other seats' hands, preserving the deciding hand and public state.</summary>
        public void Sample(ref SimState state, Random random) => this.SampleAt(ref state, random.NextDouble());

        /// <summary>Selects the assignment containing a fixed quantile in the interval [0, 1).</summary>
        public void SampleAt(ref SimState state, double quantile)
        {
            if (!double.IsFinite(quantile) || quantile < 0 || quantile >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantile));
            }

            if (!this.configured)
            {
                throw new InvalidOperationException("The sampler has no positive-weight valid worlds.");
            }

            var hand0 = this.known[0];
            var hand1 = this.known[1];
            var hand2 = this.known[2];
            var room0 = this.needs[0];
            var room1 = this.needs[1];
            for (var index = 0; index < this.poolCount; index++)
            {
                var offset = index * 3;
                double mass0;
                double mass1;
                double mass2;
                if (this.logarithmic)
                {
                    mass0 = this.logWeights[offset] + this.LogMass(index + 1, room0 - 1, room1);
                    mass1 = this.logWeights[offset + 1] + this.LogMass(index + 1, room0, room1 - 1);
                    mass2 = this.logWeights[offset + 2] + this.LogMass(index + 1, room0, room1);
                    var maximum = Math.Max(mass0, Math.Max(mass1, mass2));
                    mass0 = Math.Exp(mass0 - maximum);
                    mass1 = Math.Exp(mass1 - maximum);
                    mass2 = Math.Exp(mass2 - maximum);
                }
                else
                {
                    mass0 = this.weights[offset] * this.Mass(index + 1, room0 - 1, room1);
                    mass1 = this.weights[offset + 1] * this.Mass(index + 1, room0, room1 - 1);
                    mass2 = this.weights[offset + 2] * this.Mass(index + 1, room0, room1);
                }

                var total = mass0 + mass1 + mass2;
                var pick = Math.Min(Math.BitDecrement(total), quantile * total);
                var bit = 1u << this.pool[index];
                double selectedMass;
                if (mass0 > 0 && pick < mass0)
                {
                    hand0 |= bit;
                    room0--;
                    selectedMass = mass0;
                }
                else if (mass1 > 0 && (pick - mass0 < mass1 || mass2 == 0))
                {
                    pick -= mass0;
                    hand1 |= bit;
                    room1--;
                    selectedMass = mass1;
                }
                else
                {
                    pick -= mass0 + mass1;
                    hand2 |= bit;
                    selectedMass = mass2;
                }

                quantile = Math.Clamp(pick / selectedMass, 0, Math.BitDecrement(1d));
            }

            state.Hands[this.seats[0]] = hand0;
            state.Hands[this.seats[1]] = hand1;
            state.Hands[this.seats[2]] = hand2;
        }

        internal bool Configure(
            int me,
            uint ownHand,
            uint played,
            ReadOnlySpan<uint> knownCards,
            ReadOnlySpan<uint> excluded,
            ReadOnlySpan<int> handCounts,
            ReadOnlySpan<float> ownershipWeights)
        {
            this.configured = false;
            this.TotalWeight = 0;
            this.LogTotalWeight = double.NegativeInfinity;
            this.poolCount = 0;
            this.logarithmic = false;
            if (me < 0 || me >= 4 || knownCards.Length != 4 || excluded.Length != 4 || handCounts.Length != 4
                || ownershipWeights.Length != WeightCount || handCounts[me] != BitOperations.PopCount(ownHand)
                || handCounts[me] > HandSize || (ownHand & played) != 0)
            {
                return false;
            }

            foreach (var weight in ownershipWeights)
            {
                if (!float.IsFinite(weight) || weight < 0)
                {
                    return false;
                }
            }

            var occupied = ownHand | played;
            var needed = 0;
            for (var player = 0; player < 3; player++)
            {
                var seat = (me + 1 + player) & 3;
                var cards = knownCards[seat];
                if (handCounts[seat] < 0 || handCounts[seat] > HandSize || (cards & (occupied | excluded[seat])) != 0)
                {
                    return false;
                }

                this.seats[player] = seat;
                this.known[player] = cards;
                this.needs[player] = handCounts[seat] - BitOperations.PopCount(cards);
                if (this.needs[player] < 0)
                {
                    return false;
                }

                occupied |= cards;
                needed += this.needs[player];
            }

            var unseen = ~occupied;
            if (BitOperations.PopCount(unseen) != needed)
            {
                return false;
            }

            var logScale = 0d;
            for (var rest = unseen; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var offset = this.poolCount * 3;
                var maximum = 0d;
                for (var player = 0; player < 3; player++)
                {
                    var weight = this.needs[player] > 0 && (excluded[this.seats[player]] & (1u << card)) == 0
                        ? ownershipWeights[(card * 3) + player]
                        : 0;
                    this.weights[offset + player] = weight;
                    maximum = Math.Max(maximum, weight);
                }

                if (maximum == 0)
                {
                    return false;
                }

                // Per-card scaling leaves the conditional distribution unchanged and prevents overflow.
                logScale += Math.Log(maximum);
                for (var player = 0; player < 3; player++)
                {
                    this.weights[offset + player] /= maximum;
                    this.logWeights[offset + player] = Math.Log(this.weights[offset + player]);
                }

                this.pool[this.poolCount++] = card;
            }

            Array.Clear(this.computed);
            var mass = this.Mass(0, this.needs[0], this.needs[1]);
            if (mass > 0)
            {
                this.LogTotalWeight = Math.Log(mass) + logScale;
                this.TotalWeight = logScale == 0 ? mass : Math.Exp(this.LogTotalWeight);
            }
            else
            {
                // Finite positive weights can still underflow when cardinality forces unlikely cards.
                Array.Clear(this.computed);
                this.logarithmic = true;
                var logMass = this.LogMass(0, this.needs[0], this.needs[1]);
                if (double.IsNegativeInfinity(logMass))
                {
                    return false;
                }

                this.LogTotalWeight = logMass + logScale;
                this.TotalWeight = Math.Exp(this.LogTotalWeight);
            }

            this.configured = true;
            return true;
        }

        private double Mass(int index, int room0, int room1)
        {
            var room2 = this.poolCount - index - room0 - room1;
            if (room0 < 0 || room1 < 0 || room2 < 0 || room2 > this.needs[2])
            {
                return 0;
            }

            if (index == this.poolCount)
            {
                return 1;
            }

            var key = (((index * Width) + room0) * Width) + room1;
            if (this.computed[key])
            {
                return this.completions[key];
            }

            var offset = index * 3;
            var mass = (this.weights[offset] * this.Mass(index + 1, room0 - 1, room1))
                + (this.weights[offset + 1] * this.Mass(index + 1, room0, room1 - 1))
                + (this.weights[offset + 2] * this.Mass(index + 1, room0, room1));
            this.computed[key] = true;
            this.completions[key] = mass;
            return mass;
        }

        private double LogMass(int index, int room0, int room1)
        {
            var room2 = this.poolCount - index - room0 - room1;
            if (room0 < 0 || room1 < 0 || room2 < 0 || room2 > this.needs[2])
            {
                return double.NegativeInfinity;
            }

            if (index == this.poolCount)
            {
                return 0;
            }

            var key = (((index * Width) + room0) * Width) + room1;
            if (this.computed[key])
            {
                return this.completions[key];
            }

            var offset = index * 3;
            var first = this.logWeights[offset] + this.LogMass(index + 1, room0 - 1, room1);
            var second = this.logWeights[offset + 1] + this.LogMass(index + 1, room0, room1 - 1);
            var third = this.logWeights[offset + 2] + this.LogMass(index + 1, room0, room1);
            var maximum = Math.Max(first, Math.Max(second, third));
            var mass = double.IsNegativeInfinity(maximum)
                ? maximum
                : maximum + Math.Log(Math.Exp(first - maximum) + Math.Exp(second - maximum) + Math.Exp(third - maximum));
            this.computed[key] = true;
            this.completions[key] = mass;
            return mass;
        }
    }
}
