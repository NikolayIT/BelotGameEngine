namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Numerics;

    using Belot.Engine.Game;

    /// <summary>
    /// Asks the networks for the value of every action open to the seat deciding: the game
    /// points its team gets from the deal minus the other team's (the points hanging from
    /// earlier deals included), if it takes that action and everybody plays on as the networks
    /// would. Keeps its own buffers, so one per player (or per thread).
    /// </summary>
    internal sealed class NeuralEvaluator
    {
        /// <summary>Game points per unit of a network's output.</summary>
        public const float ValueScale = 26f;

        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] values = new float[FeatureEncoder.MaxActive];
        private readonly float[] outputs = new float[FeatureEncoder.CardOutputs];

        public NeuralEvaluator(NeuralModels models)
        {
            this.Models = models;
        }

        public NeuralModels Models { get; set; }

        /// <summary>The bid's value index for each available bid (as bits).</summary>
        public static uint BidCandidates(BidType available)
        {
            var candidates = 1u;
            for (var rest = (uint)available; rest != 0; rest &= rest - 1)
            {
                candidates |= 1u << (BitOperations.TrailingZeroCount(rest) + 1);
            }

            return candidates;
        }

        /// <summary>The index with the highest value among the candidates (bits); the lowest on a tie.</summary>
        public static int Best(float[] values, uint candidates)
        {
            var best = -1;
            var bestValue = float.NegativeInfinity;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                if (best < 0 || values[index] > bestValue)
                {
                    best = index;
                    bestValue = values[index];
                }
            }

            return best;
        }

        /// <summary>Fills the value of every legal card of the seat to play (indexed by card).</summary>
        public void EvaluateCards(in NeuralDeal deal, uint legal, float[] cardValues)
        {
            var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.values);
            this.Models.Cards(deal.Kind).Forward(this.indices.AsSpan(0, count), this.values.AsSpan(0, count), this.outputs);
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                cardValues[card] = this.outputs[FeatureEncoder.ToNetwork(card, rotation)] * ValueScale;
            }
        }

        /// <summary>Fills the value of every bid open to the seat to bid (indexed by <see cref="FeatureEncoder.BidIndex"/>).</summary>
        public void EvaluateBids(in NeuralDeal deal, float[] bidValues)
        {
            var count = FeatureEncoder.EncodeBid(in deal, this.indices, this.values);
            this.Models.Bid.Forward(this.indices.AsSpan(0, count), this.values.AsSpan(0, count), this.outputs.AsSpan(0, FeatureEncoder.BidOutputs));
            for (var i = 0; i < FeatureEncoder.BidOutputs; i++)
            {
                bidValues[i] = this.outputs[i] * ValueScale;
            }
        }

        /// <summary>The best legal card of the seat to play.</summary>
        public int BestCard(in NeuralDeal deal, uint legal, float[] cardValues)
        {
            if ((legal & (legal - 1)) == 0)
            {
                return BitOperations.TrailingZeroCount(legal);
            }

            this.EvaluateCards(in deal, legal, cardValues);
            return Best(cardValues, legal);
        }

        /// <summary>The best bid of the seat to bid.</summary>
        public BidType BestBid(in NeuralDeal deal, float[] bidValues)
        {
            this.EvaluateBids(in deal, bidValues);
            return FeatureEncoder.BidOfIndex(Best(bidValues, BidCandidates(deal.AvailableBids())));
        }
    }
}
