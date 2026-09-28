namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Weights a hypothetical deal by how likely other seats' observed card choices were under
    /// the neural policy. Each historical decision contains only that seat's hypothetical hand
    /// and the information public before its action. Buffers are reused between sampled worlds.
    /// </summary>
    internal sealed class PlayLikelihood
    {
        private const int CacheSize = 1024;

        private readonly NeuralDeal[] prefixes = new NeuralDeal[32];
        private readonly int[] cards = new int[32];
        private readonly int[] actionIndices = new int[32];
        private readonly uint[] laterCards = new uint[32];
        private readonly float[] values = new float[32];
        private readonly ulong[] cacheKeys = new ulong[CacheSize];
        private readonly double[] cacheProbabilities = new double[CacheSize];
        private readonly byte[] cacheChoices = new byte[CacheSize];

        private BelotSimulator simulator;
        private NeuralEvaluator evaluator;
        private uint played;
        private double temperature = 2;
        private double uniformMix = 0.1;
        private double power = 0.5;

        /// <summary>Gets or sets the softmax temperature, in game points.</summary>
        public double Temperature
        {
            get => this.temperature;
            set
            {
                if (!double.IsFinite(value) || value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (this.temperature != value)
                {
                    this.temperature = value;
                    Array.Clear(this.cacheKeys);
                }
            }
        }

        /// <summary>Gets or sets the probability of choosing uniformly among legal cards.</summary>
        public double UniformMix
        {
            get => this.uniformMix;
            set
            {
                if (!double.IsFinite(value) || value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (this.uniformMix != value)
                {
                    this.uniformMix = value;
                    Array.Clear(this.cacheKeys);
                }
            }
        }

        /// <summary>Gets or sets the exponent that tempers the combined likelihood (zero disables it).</summary>
        public double Power
        {
            get => this.power;
            set
            {
                if (!double.IsFinite(value) || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.power = value;
            }
        }

        public int SelectedActions { get; private set; }

        /// <summary>Gets the number of neural evaluations since the last configuration.</summary>
        public int Evaluations { get; private set; }

        public int ForcedActions { get; private set; }

        public int InvalidWorlds { get; private set; }

        public int CacheHits { get; private set; }

        /// <summary>
        /// Selects recent leads and off-suit plays by seats other than the current player.
        /// Whether a selected action was forced depends on the hypothetical hand, so it is
        /// checked separately for each world. The supplied simulator must remain on this contract.
        /// </summary>
        public bool Configure(PlayerPlayCardContext context, BelotSimulator simulator, NeuralEvaluator evaluator, int maxActions)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(simulator);
            ArgumentNullException.ThrowIfNull(evaluator);
            if (maxActions < 0 || maxActions > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(maxActions));
            }

            this.SelectedActions = 0;
            this.Evaluations = 0;
            this.ForcedActions = 0;
            this.InvalidWorlds = 0;
            this.CacheHits = 0;
            Array.Clear(this.cacheKeys);
            this.simulator = simulator;
            this.evaluator = evaluator;
            var deal = NeuralDeal.Start(context.FirstToPlayInTheRound.Index(), context.HangingPoints);
            foreach (var bid in context.Bids)
            {
                if (deal.AuctionFinished || bid.Player.Index() != deal.ToBid)
                {
                    return false;
                }

                deal.RecordBid(bid.Type);
            }

            if (!deal.AuctionFinished || deal.Contract == BidType.Pass || deal.Contract != context.CurrentContract.Type
                                      || deal.Declarer != context.CurrentContract.Player.Index())
            {
                return false;
            }

            deal.StartPlay(simulator);
            var declarations = default(SeatBytes);
            foreach (var announce in context.Announces)
            {
                if (announce.Type != AnnounceType.Belot)
                {
                    declarations[announce.Player.Index()] |= NeuralDeal.DeclarationBit(announce.Type);
                }
            }

            var count = 0;
            var actionIndex = 0;
            var currentSeat = context.MyPosition.Index();
            foreach (var action in context.RoundActions)
            {
                var seat = deal.Play.Turn;
                var card = action.Card.GetHashCode();
                if (actionIndex >= 32 || action.Player.Index() != seat || (deal.Played & (1u << card)) != 0)
                {
                    return false;
                }

                // Combinations become public immediately before their owner's first card.
                // In particular, a later seat's first-trick declarations must not leak back.
                if (deal.PlayedBy[seat] == 0)
                {
                    deal.Declare(seat, declarations[seat]);
                }

                if (seat != currentSeat && (deal.Play.TrickCards == 0 || (card >> 3) != deal.Play.LedSuit))
                {
                    this.prefixes[count] = deal;
                    this.cards[count] = card;
                    this.actionIndices[count] = actionIndex;
                    count++;
                }

                deal.PlayCard(simulator, card, action.Belote);
                actionIndex++;
            }

            if (deal.Play.Turn != currentSeat)
            {
                return false;
            }

            Array.Reverse(this.prefixes, 0, count);
            Array.Reverse(this.cards, 0, count);
            Array.Reverse(this.actionIndices, 0, count);
            this.played = deal.Played;
            this.SelectedActions = Math.Min(count, maxActions);
            for (var i = 0; i < this.SelectedActions; i++)
            {
                var seat = this.prefixes[i].Play.Turn;
                this.laterCards[i] = deal.PlayedBy[seat] & ~this.prefixes[i].PlayedBy[seat];
            }

            return true;
        }

        /// <summary>
        /// The product of softened action probabilities, raised to <see cref="Power"/>.
        /// Forced actions contribute one. An impossible observed card contributes zero.
        /// Only sampled remaining hands are read; no sampled public state is trusted.
        /// </summary>
        public double Weight(in SimState sampledWorld)
        {
            if (this.Power == 0)
            {
                return 1;
            }

            var logLikelihood = 0.0;
            for (var i = 0; i < this.SelectedActions; i++)
            {
                // Worlds with the same hand for this historical actor give exactly the same
                // policy inputs. Exact endings revisit those hands many times across worlds.
                var seat = this.prefixes[i].Play.Turn;
                var key = (((ulong)i << 32) | sampledWorld.Hands[seat]) + 1;
                var slot = this.CacheSlot(key);
                double probability;
                int choices;
                if (this.cacheKeys[slot] == key)
                {
                    probability = this.cacheProbabilities[slot];
                    choices = this.cacheChoices[slot];
                    this.CacheHits++;
                }
                else
                {
                    probability = this.ActionProbability(i, in sampledWorld, out choices);
                    this.cacheKeys[slot] = key;
                    this.cacheProbabilities[slot] = probability;
                    this.cacheChoices[slot] = (byte)choices;
                }

                if (choices == 0)
                {
                    this.InvalidWorlds++;
                    return 0;
                }

                if (choices == 1)
                {
                    this.ForcedActions++;
                    continue;
                }

                logLikelihood += Math.Log(probability);
            }

            return Math.Exp(this.Power * logLikelihood);
        }

        /// <summary>The zero-based position of a selected action in the public card history.</summary>
        public int GetActionIndex(int selectedIndex)
        {
            this.CheckIndex(selectedIndex);
            return this.actionIndices[selectedIndex];
        }

        /// <summary>Reconstructs one historical information set, also exposed for parity tests.</summary>
        public bool TryGetDecision(int selectedIndex, in SimState sampledWorld, out NeuralDeal deal, out uint legal, out int observedCard)
        {
            this.CheckIndex(selectedIndex);
            deal = this.prefixes[selectedIndex];
            observedCard = this.cards[selectedIndex];
            legal = 0;
            var seat = deal.Play.Turn;
            var remainingHand = sampledWorld.Hands[seat];
            var ownHand = remainingHand | this.laterCards[selectedIndex];
            if ((remainingHand & this.played) != 0 || BitOperations.PopCount(ownHand) != 8 - BitOperations.PopCount(deal.PlayedBy[seat]))
            {
                return false;
            }

            deal.Play.Hands[seat] = ownHand;
            legal = this.simulator.LegalMoves(in deal.Play);
            return (legal & (1u << observedCard)) != 0;
        }

        private double ActionProbability(int selectedIndex, in SimState sampledWorld, out int choices)
        {
            if (!this.TryGetDecision(selectedIndex, in sampledWorld, out var deal, out var legal, out var observedCard))
            {
                choices = 0;
                return 0;
            }

            choices = BitOperations.PopCount(legal);
            if (choices == 1 || this.UniformMix == 1)
            {
                return 1.0 / choices;
            }

            this.evaluator.EvaluateCards(in deal, legal, this.values);
            this.Evaluations++;
            var best = NeuralEvaluator.Best(this.values, legal);
            var maximum = this.values[best];
            var total = 0.0;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                total += Math.Exp((this.values[card] - maximum) / this.Temperature);
            }

            var neuralProbability = Math.Exp((this.values[observedCard] - maximum) / this.Temperature) / total;
            return (this.UniformMix / choices) + ((1 - this.UniformMix) * neuralProbability);
        }

        private int CacheSlot(ulong key)
        {
            var first = (int)(unchecked(key * 11400714819323198485UL) >> 54);
            for (var probe = 0; probe < 4; probe++)
            {
                var slot = (first + probe) & (CacheSize - 1);
                if (this.cacheKeys[slot] == 0 || this.cacheKeys[slot] == key)
                {
                    return slot;
                }
            }

            // Bounded work even if many actions exhaust the cache. Replacing an entry only
            // costs a future reevaluation; it cannot change a probability or the random stream.
            return first;
        }

        private void CheckIndex(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= this.SelectedActions)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }
        }
    }
}
