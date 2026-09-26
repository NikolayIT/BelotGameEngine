namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays as ClaudePlayerIsmcts and records what its search found at every decision: the
    /// average result of each card it tried and of each bid it weighed, in game points, with the
    /// inputs the neural player would see there. These samples teach the networks to value
    /// actions the way the search does, the start of their training.
    /// </summary>
    internal sealed class DistillPlayer : IPlayer
    {
        private readonly ClaudePlayerIsmcts search;
        private readonly SampleBuffer[] buffers;
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly double[] cardValues = new double[32];
        private readonly int[] visits = new int[32];
        private readonly BidType[] bids = new BidType[16];
        private readonly double[] bidValues = new double[16];
        private readonly float[] labels = new float[FeatureEncoder.CardOutputs];
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] values = new float[FeatureEncoder.MaxActive];

        public DistillPlayer(ClaudePlayerIsmcts search, SampleBuffer[] buffers)
        {
            this.search = search;
            this.buffers = buffers;
        }

        public string Name => "Distill " + this.search.Name;

        public BidType GetBid(PlayerGetBidContext context)
        {
            var bid = this.search.GetBid(context);
            var count = this.search.GetBidValues(this.bids, this.bidValues);
            if (count > 1 && NeuralDeal.FromBidContext(context, out var deal))
            {
                Array.Clear(this.labels);
                var mask = 0u;
                for (var i = 0; i < count; i++)
                {
                    var index = FeatureEncoder.BidIndex(this.bids[i]);
                    this.labels[index] = (float)(this.bidValues[i] / NeuralEvaluator.ValueScale);
                    mask |= 1u << index;
                }

                var features = FeatureEncoder.EncodeBid(in deal, this.indices, this.values);
                this.buffers[NeuralModels.BidTag].Add(
                    this.indices.AsSpan(0, features), this.values.AsSpan(0, features), this.labels.AsSpan(0, FeatureEncoder.BidOutputs), mask);
            }

            return bid;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.search.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            if (context.AvailableCardsToPlay.Count == 1)
            {
                return this.search.PlayCard(context);
            }

            var card = this.search.Search(context);
            if (card < 0)
            {
                return this.search.PlayCard(context);
            }

            this.search.GetRootValues(this.cardValues, this.visits);
            if (NeuralDeal.FromPlayContext(context, this.simulator, out var deal))
            {
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                var rotation = FeatureEncoder.Rotation(deal.Kind);
                Array.Clear(this.labels);
                var mask = 0u;
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var legalCard = System.Numerics.BitOperations.TrailingZeroCount(rest);
                    if (this.visits[legalCard] > 0)
                    {
                        var output = FeatureEncoder.ToNetwork(legalCard, rotation);
                        this.labels[output] = (float)(this.cardValues[legalCard] / NeuralEvaluator.ValueScale);
                        mask |= 1u << output;
                    }
                }

                var features = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.values);
                this.buffers[1 + FeatureEncoder.CardNetwork(deal.Kind)].Add(
                    this.indices.AsSpan(0, features), this.values.AsSpan(0, features), this.labels, mask);
            }

            return new PlayCardAction(Card.AllCards[card]);
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
    }
}
