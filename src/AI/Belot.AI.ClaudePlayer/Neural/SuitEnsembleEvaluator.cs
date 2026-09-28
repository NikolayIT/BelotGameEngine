namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;

    /// <summary>
    /// Averages card values over suit maps that preserve the legal public auction. A learned
    /// bidding policy may still treat unbid suits differently, so its inferred probabilities
    /// need not be invariant under these maps; strength must be measured empirically.
    /// </summary>
    internal sealed class SuitEnsembleEvaluator
    {
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly int[] mappedIndices = new int[FeatureEncoder.MaxActive];
        private readonly float[] features = new float[FeatureEncoder.MaxActive];

        /// <summary>Gets the number of network views used by the most recent evaluation.</summary>
        public int Views { get; private set; }

        /// <summary>
        /// Writes the complete group as consecutive four-byte source-to-destination suit maps.
        /// Maps are in network coordinates, so a suit contract fixes slot zero. Every suit
        /// mentioned in any seat's auction is fixed as well. The identity is always first.
        /// </summary>
        public static int Permutations(in NeuralDeal deal, Span<byte> maps)
        {
            if (maps.Length < 96)
            {
                throw new ArgumentException("Suit maps need room for 24 permutations of four suits.", nameof(maps));
            }

            var rotation = FeatureEncoder.Rotation(deal.Kind);
            var fixedSuits = deal.Kind < SimTables.NoTrumps ? 1 : 0;
            var bids = deal.BidsMade[0] | deal.BidsMade[1] | deal.BidsMade[2] | deal.BidsMade[3];
            for (var suit = 0; suit < 4; suit++)
            {
                if ((bids & (1 << suit)) != 0)
                {
                    fixedSuits |= 1 << ((suit - rotation + 4) & 3);
                }
            }

            var count = 0;
            for (var first = 0; first < 4; first++)
            {
                if ((fixedSuits & 1) != 0 && first != 0)
                {
                    continue;
                }

                for (var second = 0; second < 4; second++)
                {
                    if (second == first || ((fixedSuits & 2) != 0 && second != 1))
                    {
                        continue;
                    }

                    for (var third = 0; third < 4; third++)
                    {
                        var fourth = 6 - first - second - third;
                        if (third == first || third == second || ((fixedSuits & 4) != 0 && third != 2)
                            || ((fixedSuits & 8) != 0 && fourth != 3))
                        {
                            continue;
                        }

                        var offset = count++ * 4;
                        maps[offset] = (byte)first;
                        maps[offset + 1] = (byte)second;
                        maps[offset + 2] = (byte)third;
                        maps[offset + 3] = (byte)fourth;
                    }
                }
            }

            return count;
        }

        /// <summary>Fills legal physical-card slots; neither illegal slots nor random state changes.</summary>
        public void EvaluateCards(in NeuralDeal deal, uint legal, NeuralEvaluator evaluator, float[] cardValues)
        {
            Span<byte> maps = stackalloc byte[96];
            this.Views = Permutations(in deal, maps);
            if (this.Views == 1)
            {
                evaluator.EvaluateCards(in deal, legal, cardValues);
                return;
            }

            var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.features);
            Span<float> outputs = stackalloc float[FeatureEncoder.CardOutputs];
            Span<double> sums = stackalloc double[FeatureEncoder.CardOutputs];
            sums.Clear();
            var network = evaluator.Models.Cards(deal.Kind);
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            for (var view = 0; view < this.Views; view++)
            {
                var map = maps.Slice(view * 4, 4);
                for (var feature = 0; feature < count; feature++)
                {
                    var index = this.indices[feature];
                    this.mappedIndices[feature] = index < 512 ? (index & ~31) + MapCard(index & 31, map) : index;
                }

                network.Forward(this.mappedIndices.AsSpan(0, count), this.features.AsSpan(0, count), outputs);
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    sums[card] += outputs[MapCard(FeatureEncoder.ToNetwork(card, rotation), map)];
                }
            }

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                cardValues[card] = (float)(sums[card] / this.Views) * NeuralEvaluator.ValueScale;
            }
        }

        private static int MapCard(int card, ReadOnlySpan<byte> map) => (map[card >> 3] * 8) + (card & 7);
    }
}
