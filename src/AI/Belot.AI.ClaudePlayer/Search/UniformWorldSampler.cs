namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Samples uniformly from every assignment consistent with known cards, exclusions and hand sizes.
    /// A dynamic program counts the valid completions of each partial assignment. A uniform integer
    /// selects one complete assignment, so restrictions do not bias the distribution toward easier deals.
    /// Declaration and bidding likelihoods are not part of these constraints.
    /// </summary>
    internal sealed class UniformWorldSampler
    {
        private const int HandSize = 8;
        private const int Width = HandSize + 1;

        private readonly int[] seats = new int[3];
        private readonly int[] needs = new int[3];
        private readonly uint[] known = new uint[3];
        private readonly int[] pool = new int[3 * HandSize];
        private readonly int[] groups = new int[3 * HandSize];

        // Zero means uncomputed; a stored value is one plus the number of completions.
        // The third hand's remaining size follows from the first two and the card index.
        private readonly ulong[] completions = new ulong[((3 * HandSize) + 1) * Width * Width];

        private int poolCount;

        /// <summary>Gets the number of valid assignments, or zero if configuration failed.</summary>
        public ulong WorldCount { get; private set; }

        /// <summary>Configures the sampler without weakening inconsistent constraints.</summary>
        public bool Configure(RoundKnowledge knowledge) => this.Configure(
            knowledge.Me, knowledge.MyHand, knowledge.Played, knowledge.Known, knowledge.Excluded, knowledge.HandCounts);

        /// <summary>Replaces the other three hands, retaining the deciding seat's hand and all public state.</summary>
        public void Sample(ref SimState state, Random random)
        {
            if (this.WorldCount == 0)
            {
                throw new InvalidOperationException("The sampler has no valid worlds.");
            }

            this.SampleAt(ref state, (ulong)random.NextInt64((long)this.WorldCount));
        }

        /// <summary>
        /// Selects the uniquely indexed valid assignment. Enumerating all indices produces each world once.
        /// </summary>
        public void SampleAt(ref SimState state, ulong worldIndex)
        {
            if (worldIndex >= this.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(worldIndex));
            }

            var hand0 = this.known[0];
            var hand1 = this.known[1];
            var hand2 = this.known[2];
            var room0 = this.needs[0];
            var room1 = this.needs[1];
            for (var index = 0; index < this.poolCount; index++)
            {
                var group = this.groups[index];
                var bit = 1u << this.pool[index];
                var count = (group & 1) != 0 ? this.Count(index + 1, room0 - 1, room1) : 0;
                if (worldIndex < count)
                {
                    hand0 |= bit;
                    room0--;
                    continue;
                }

                worldIndex -= count;
                count = (group & 2) != 0 ? this.Count(index + 1, room0, room1 - 1) : 0;
                if (worldIndex < count)
                {
                    hand1 |= bit;
                    room1--;
                    continue;
                }

                worldIndex -= count;
                hand2 |= bit;
            }

            state.Hands[this.seats[0]] = hand0;
            state.Hands[this.seats[1]] = hand1;
            state.Hands[this.seats[2]] = hand2;
        }

        internal bool Configure(int me, uint ownHand, uint played, ReadOnlySpan<uint> knownCards, ReadOnlySpan<uint> excluded, ReadOnlySpan<int> handCounts)
        {
            this.WorldCount = 0;
            this.poolCount = 0;
            if (me < 0 || me >= 4 || knownCards.Length != 4 || excluded.Length != 4 || handCounts.Length != 4
                || handCounts[me] != BitOperations.PopCount(ownHand) || handCounts[me] > HandSize || (ownHand & played) != 0)
            {
                return false;
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

            for (var rest = unseen; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var group = 0;
                for (var player = 0; player < 3; player++)
                {
                    if ((excluded[this.seats[player]] & (1u << card)) == 0)
                    {
                        group |= 1 << player;
                    }
                }

                if (group == 0)
                {
                    return false;
                }

                this.pool[this.poolCount] = card;
                this.groups[this.poolCount++] = group;
            }

            Array.Clear(this.completions);
            this.WorldCount = this.Count(0, this.needs[0], this.needs[1]);
            return this.WorldCount != 0;
        }

        private ulong Count(int index, int room0, int room1)
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
            var cached = this.completions[key];
            if (cached != 0)
            {
                return cached - 1;
            }

            var group = this.groups[index];
            var count = (group & 1) != 0 ? this.Count(index + 1, room0 - 1, room1) : 0;
            count += (group & 2) != 0 ? this.Count(index + 1, room0, room1 - 1) : 0;
            count += (group & 4) != 0 ? this.Count(index + 1, room0, room1) : 0;
            this.completions[key] = count + 1;
            return count;
        }
    }
}
