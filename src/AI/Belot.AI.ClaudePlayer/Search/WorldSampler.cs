namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Deals the unseen cards to the other three players, consistently with what the play has
    /// shown (<see cref="RoundKnowledge"/>): each gets the cards it surely holds, the right number
    /// of cards, and none it cannot hold. Cards go one by one, in random order, to a random player
    /// that may hold them (weighted by the room left in the hands, which makes the deal uniform
    /// when nothing is known), and a player is only chosen when the rest can still be dealt:
    /// with three players that is Hall's condition over the seven groups of players, so a deal
    /// never gets stuck.
    /// </summary>
    internal sealed class WorldSampler
    {
        private readonly int[] seats = new int[3];
        private readonly int[] needs = new int[3];
        private readonly uint[] known = new uint[3];
        private readonly int[] pool = new int[32];

        // [card]: the players (as bits over this.seats) that may hold the card.
        private readonly int[] cardGroups = new int[32];

        // [group]: how many pool cards may go exactly to that group of players.
        private readonly int[] groupCounts = new int[8];

        private readonly int[] counts = new int[8];
        private readonly int[] room = new int[3];

        private int poolCount;
        private bool constrained;

        /// <summary>Gets a value indicating whether the last Configure had to drop the constraints.</summary>
        public bool DroppedConstraints { get; private set; }

        /// <summary>
        /// Prepares the deals of one decision.
        /// </summary>
        /// <returns>False when the hand sizes do not add up.</returns>
        public bool Configure(RoundKnowledge knowledge)
        {
            var me = knowledge.Me;
            var unseen = ~(knowledge.MyHand | knowledge.Played);
            var needed = 0;
            for (var i = 0; i < 3; i++)
            {
                var seat = (me + 1 + i) & 3;
                this.seats[i] = seat;
                this.known[i] = knowledge.Known[seat];
                unseen &= ~this.known[i];
                this.needs[i] = knowledge.HandCounts[seat] - BitOperations.PopCount(this.known[i]);
                needed += this.needs[i];
                if (this.needs[i] < 0)
                {
                    return false;
                }
            }

            this.poolCount = 0;
            for (var rest = unseen; rest != 0; rest &= rest - 1)
            {
                this.pool[this.poolCount++] = BitOperations.TrailingZeroCount(rest);
            }

            if (this.poolCount != needed)
            {
                return false;
            }

            this.DroppedConstraints = !this.SetGroups(knowledge, true);
            if (this.DroppedConstraints)
            {
                this.SetGroups(knowledge, false);
            }

            return true;
        }

        /// <summary>Deals the other hands into the state (the searching player's stays).</summary>
        public void Sample(ref SimState state, Random random)
        {
            var pool = this.pool;
            var count = this.poolCount;
            for (var i = count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var hand0 = this.known[0];
            var hand1 = this.known[1];
            var hand2 = this.known[2];
            this.room[0] = this.needs[0];
            this.room[1] = this.needs[1];
            this.room[2] = this.needs[2];
            if (this.constrained)
            {
                Array.Copy(this.groupCounts, this.counts, 8);
            }

            for (var i = 0; i < count; i++)
            {
                var card = pool[i];
                var player = this.constrained ? this.ChoosePlayer(this.cardGroups[card], random) : this.ChooseAnyPlayer(random);
                switch (player)
                {
                    case 0:
                        hand0 |= 1u << card;
                        break;
                    case 1:
                        hand1 |= 1u << card;
                        break;
                    default:
                        hand2 |= 1u << card;
                        break;
                }
            }

            state.Hands[this.seats[0]] = hand0;
            state.Hands[this.seats[1]] = hand1;
            state.Hands[this.seats[2]] = hand2;
        }

        private bool SetGroups(RoundKnowledge knowledge, bool useExclusions)
        {
            Array.Clear(this.groupCounts, 0, 8);
            this.constrained = false;
            for (var i = 0; i < this.poolCount; i++)
            {
                var card = this.pool[i];
                var group = 0;
                for (var player = 0; player < 3; player++)
                {
                    if (!useExclusions || (knowledge.Excluded[this.seats[player]] & (1u << card)) == 0)
                    {
                        group |= 1 << player;
                    }
                }

                if (group == 0)
                {
                    return false;
                }

                this.cardGroups[card] = group;
                this.groupCounts[group]++;
                this.constrained |= group != 7;
            }

            this.room[0] = this.needs[0];
            this.room[1] = this.needs[1];
            this.room[2] = this.needs[2];
            return !this.constrained || this.CanDeal(this.groupCounts);
        }

        // Hall's condition: for every group of players, the cards that can only go to them fit.
        private bool CanDeal(int[] groupCounts)
        {
            for (var players = 1; players < 8; players++)
            {
                var cards = 0;
                for (var group = 1; group < 8; group++)
                {
                    if ((group & ~players) == 0)
                    {
                        cards += groupCounts[group];
                    }
                }

                var space = ((players & 1) != 0 ? this.room[0] : 0)
                            + ((players & 2) != 0 ? this.room[1] : 0)
                            + ((players & 4) != 0 ? this.room[2] : 0);
                if (cards > space)
                {
                    return false;
                }
            }

            return true;
        }

        private int ChooseAnyPlayer(Random random)
        {
            var pick = random.Next(this.room[0] + this.room[1] + this.room[2]);
            var player = pick < this.room[0] ? 0 : pick < this.room[0] + this.room[1] ? 1 : 2;
            this.room[player]--;
            return player;
        }

        private int ChoosePlayer(int group, Random random)
        {
            this.counts[group]--;
            while (true)
            {
                var room0 = (group & 1) != 0 ? this.room[0] : 0;
                var room1 = (group & 2) != 0 ? this.room[1] : 0;
                var room2 = (group & 4) != 0 ? this.room[2] : 0;
                var pick = random.Next(room0 + room1 + room2);
                var player = pick < room0 ? 0 : pick < room0 + room1 ? 1 : 2;
                this.room[player]--;
                if ((group & (group - 1)) == 0 || this.CanDeal(this.counts))
                {
                    return player;
                }

                // The rest could not be dealt: this player must not take the card.
                this.room[player]++;
                group &= ~(1 << player);
            }
        }
    }
}
