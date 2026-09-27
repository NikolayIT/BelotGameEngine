namespace Belot.NeuralTrainer
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Players;

    /// <summary>
    /// Training-only version of plain neural search. Independent worlds advance together,
    /// so their next network decisions can be evaluated in one accelerator batch. Simulation,
    /// declarations, public information, legal moves and final scoring remain in C#.
    /// </summary>
    internal sealed class BatchedNeuralSearch
    {
        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly WorldSampler sampler = new WorldSampler();
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly double[] sums = new double[32];
        private readonly IBatchedCardPolicy policy;
        private NeuralDeal[] states = Array.Empty<NeuralDeal>();
        private int[] rootCards = Array.Empty<int>();
        private int[] slots = Array.Empty<int>();
        private uint[] legalCards = Array.Empty<uint>();
        private int[] chosen = Array.Empty<int>();

        public BatchedNeuralSearch(IBatchedCardPolicy policy)
        {
            this.policy = policy;
        }

        public bool Evaluate(
            PlayerPlayCardContext context,
            in NeuralDeal deal,
            uint legal,
            int worlds,
            BelotSimulator simulator,
            Random random,
            float[] values)
        {
            if (worlds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(worlds));
            }

            if (!this.knowledge.Build(context, simulator, usePlayInference: true) || !this.sampler.Configure(this.knowledge))
            {
                return false;
            }

            var count = checked(worlds * BitOperations.PopCount(legal));
            if (this.states.Length < count)
            {
                this.states = new NeuralDeal[count];
                this.rootCards = new int[count];
                this.slots = new int[count];
                this.legalCards = new uint[count];
                this.chosen = new int[count];
            }

            var next = 0;
            for (var world = 0; world < worlds; world++)
            {
                var sampled = this.knowledge.Root;
                this.sampler.Sample(ref sampled, random);
                var position = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    position.Play.Hands[seat] = sampled.Hands[seat];
                }

                this.Declarations(ref position, in sampled, random);
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    this.states[next] = position;
                    this.states[next].PlayCard(simulator, card, legal);
                    this.rootCards[next++] = card;
                }
            }

            while (true)
            {
                var pending = 0;
                for (var slot = 0; slot < count; slot++)
                {
                    ref var position = ref this.states[slot];
                    while (!position.IsFinished)
                    {
                        position.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in position.Play);
                        if ((moves & (moves - 1)) != 0)
                        {
                            this.slots[pending] = slot;
                            this.legalCards[pending++] = moves;
                            break;
                        }

                        position.PlayCard(simulator, BitOperations.TrailingZeroCount(moves), moves);
                    }
                }

                if (pending == 0)
                {
                    break;
                }

                this.policy.Choose(this.states, this.slots, this.legalCards, pending, this.chosen);
                for (var item = 0; item < pending; item++)
                {
                    var card = this.chosen[item];
                    if ((uint)card >= 32 || (this.legalCards[item] & (1u << card)) == 0)
                    {
                        throw new InvalidOperationException("Batched policy returned an illegal card.");
                    }

                    this.states[this.slots[item]].PlayCard(simulator, card, this.legalCards[item]);
                }
            }

            Array.Clear(this.sums);
            var team = deal.Play.Turn & 1;
            for (var slot = 0; slot < count; slot++)
            {
                this.states[slot].Score(simulator, out var southNorth, out var eastWest, out _);
                this.sums[this.rootCards[slot]] += team == 0 ? southNorth - eastWest : eastWest - southNorth;
            }

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                values[card] = (float)(this.sums[card] / worlds);
            }

            return true;
        }

        private void Declarations(ref NeuralDeal world, in SimState state, Random random)
        {
            var count = this.knowledge.AnnounceCount;
            Array.Copy(this.knowledge.Announces, this.announces, count);
            for (var seats = this.knowledge.SeatsToDeclare; seats != 0; seats &= seats - 1)
            {
                var seat = BitOperations.TrailingZeroCount(seats);
                var start = count;
                count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, this.announces, count);
                world.ToDeclare[seat] = 0;
                for (var i = start; i < count; i++)
                {
                    world.ToDeclare[seat] |= NeuralDeal.DeclarationBit(this.announces[i].Type);
                }
            }

            AnnounceScorer.ResolveHiddenRanks(this.announces, count, random);
            AnnounceScorer.GetPoints(this.announces, count, out world.SouthNorthAnnounces, out world.EastWestAnnounces);
        }
    }
}
