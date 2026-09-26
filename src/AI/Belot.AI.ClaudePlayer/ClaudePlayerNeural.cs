namespace Belot.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays by neural networks trained with reinforcement learning (see NEURAL_NETWORK.md): a
    /// bidding network and a card network for each kind of contract (the suits, no trumps, all
    /// trumps) value every action open to the player, in game points (its team's points from the
    /// deal minus the other team's, as if everybody played on like the networks), and the player
    /// takes the best. A decision costs one small forward pass, not a search.
    ///
    /// <see cref="Temperature"/> and <see cref="MaxRegret"/> make it weaker on purpose: it then
    /// sometimes takes an action that is nearly as good (never one worse than the best by more
    /// than MaxRegret points). It declares every combination it is offered and every belote.
    /// </summary>
    public class ClaudePlayerNeural : IPlayer
    {
        private readonly SmartPlayer smartPlayer = new SmartPlayer();
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly NeuralEvaluator evaluator;
        private readonly float[] cardValues = new float[FeatureEncoder.CardOutputs];
        private readonly float[] bidValues = new float[FeatureEncoder.BidOutputs];

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaudePlayerNeural"/> class with the
        /// trained networks built into the assembly.
        /// </summary>
        public ClaudePlayerNeural()
            : this(NeuralModels.Embedded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaudePlayerNeural"/> class with the
        /// networks in a folder (the trainer's checkpoints: bid.bin, trump.bin, notrumps.bin,
        /// alltrumps.bin).
        /// </summary>
        public ClaudePlayerNeural(string weightsDirectory)
            : this(NeuralModels.LoadCached(weightsDirectory))
        {
        }

        internal ClaudePlayerNeural(NeuralModels models)
        {
            this.evaluator = new NeuralEvaluator(models);
        }

        public string Name => "Claude Player (neural)";

        /// <summary>
        /// Gets or sets how freely the player takes an action other than the best, in game points:
        /// an action worth d points less than the best is taken e^(-d / Temperature) times as
        /// often. 0 (the default) always takes the best.
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// Gets or sets the most, in game points, that an action the player takes may be worth
        /// less than the best (with a <see cref="Temperature"/> above 0).
        /// </summary>
        public double MaxRegret { get; set; } = double.PositiveInfinity;

        public Random Rng { get; set; } = new Random();

        /// <summary>Gets or sets a value indicating whether the player may double and redouble.</summary>
        public bool MayDouble { get; set; } = true;

        /// <summary>Gets how many decisions fell back to SmartPlayer (a context that did not add up).</summary>
        public int Fallbacks { get; private set; }

        internal NeuralModels Models => this.evaluator.Models;

        public BidType GetBid(PlayerGetBidContext context)
        {
            if (!NeuralDeal.FromBidContext(context, out var deal) || deal.AvailableBids() != context.AvailableBids)
            {
                this.Fallbacks++;
                return this.smartPlayer.GetBid(context);
            }

            this.evaluator.EvaluateBids(in deal, this.bidValues);
            var available = this.MayDouble ? context.AvailableBids : context.AvailableBids & ~(BidType.Double | BidType.ReDouble);
            return FeatureEncoder.BidOfIndex(this.Choose(this.bidValues, NeuralEvaluator.BidCandidates(available)));
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var available = context.AvailableCardsToPlay;
            if (available.Count == 1)
            {
                return new PlayCardAction(available.FirstOrDefault());
            }

            if (!this.Evaluate(context, out var legal))
            {
                this.Fallbacks++;
                return this.smartPlayer.PlayCard(context);
            }

            return new PlayCardAction(Card.AllCards[this.Choose(this.cardValues, legal)]);
        }

        /// <summary>
        /// Values every card the player may play, best first: the game points its team gets from
        /// the deal minus the other team's, if it plays that card.
        /// </summary>
        public IReadOnlyList<CardValue> EvaluateCards(PlayerPlayCardContext context)
        {
            if (!this.Evaluate(context, out var legal))
            {
                throw new ArgumentException("The context does not add up.", nameof(context));
            }

            var result = new List<CardValue>(BitOperations.PopCount(legal));
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                result.Add(new CardValue(Card.AllCards[card], this.cardValues[card]));
            }

            result.Sort((x, y) => y.Value.CompareTo(x.Value));
            return result;
        }

        /// <summary>
        /// Values every bid open to the player, best first: the game points its team gets from
        /// the deal minus the other team's, if it makes that bid.
        /// </summary>
        public IReadOnlyList<BidValue> EvaluateBids(PlayerGetBidContext context)
        {
            if (!NeuralDeal.FromBidContext(context, out var deal))
            {
                throw new ArgumentException("The context does not add up.", nameof(context));
            }

            this.evaluator.EvaluateBids(in deal, this.bidValues);
            var candidates = NeuralEvaluator.BidCandidates(context.AvailableBids);
            var result = new List<BidValue>(BitOperations.PopCount(candidates));
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                result.Add(new BidValue(FeatureEncoder.BidOfIndex(index), this.bidValues[index]));
            }

            result.Sort((x, y) => y.Value.CompareTo(x.Value));
            return result;
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }

        private bool Evaluate(PlayerPlayCardContext context, out uint legal)
        {
            legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            if (!NeuralDeal.FromPlayContext(context, this.simulator, out var deal)
                || this.simulator.LegalMoves(in deal.Play) != legal)
            {
                return false;
            }

            this.evaluator.EvaluateCards(in deal, legal, this.cardValues);
            return true;
        }

        // The best candidate, or with a temperature a random one near it.
        private int Choose(float[] values, uint candidates)
        {
            var best = NeuralEvaluator.Best(values, candidates);
            if (this.Temperature <= 0)
            {
                return best;
            }

            var floor = values[best] - this.MaxRegret;
            var total = 0d;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                if (values[index] >= floor)
                {
                    total += Math.Exp((values[index] - values[best]) / this.Temperature);
                }
            }

            var pick = this.Rng.NextDouble() * total;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                if (values[index] >= floor)
                {
                    pick -= Math.Exp((values[index] - values[best]) / this.Temperature);
                    if (pick < 0)
                    {
                        return index;
                    }
                }
            }

            return best;
        }
    }
}
