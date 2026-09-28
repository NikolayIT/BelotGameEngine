namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// Predicts an unseen card's owner using only the ordinary seat features. The three
    /// outputs per card are the next seat, partner and previous seat. These are local weights;
    /// a sampler must still enforce public exclusions, known cards and remaining hand sizes.
    /// </summary>
    internal sealed class CardOwnershipModel
    {
        public const int Outputs = 96;

        public const int FileBytes = 182884;

        private static readonly int[] Sizes = { FeatureEncoder.CardInputs, 128, 64, Outputs };

        private static readonly string[] Names = { "trump.bin", "notrumps.bin", "alltrumps.bin" };

        private static readonly ConcurrentDictionary<string, Lazy<CardOwnershipModel>> Cache =
            new ConcurrentDictionary<string, Lazy<CardOwnershipModel>>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private readonly NeuralNetwork[] networks;

        public CardOwnershipModel(NeuralNetwork trump, NeuralNetwork noTrumps, NeuralNetwork allTrumps)
        {
            this.networks = new[] { trump, noTrumps, allTrumps };
            for (var index = 0; index < this.networks.Length; index++)
            {
                var network = this.networks[index];
                if (network.Tag != 11 + index || network.Layout != FeatureEncoder.LayoutVersion || !network.GetSizes().SequenceEqual(Sizes))
                {
                    throw new InvalidDataException("Ownership network tag, feature layout or shape mismatch.");
                }

                for (var layer = 0; layer < network.LayerCount; layer++)
                {
                    if (network.GetWeights(layer).Any(value => !float.IsFinite(value))
                        || network.GetBiases(layer).Any(value => !float.IsFinite(value)))
                    {
                        throw new InvalidDataException("Ownership network parameters must be finite.");
                    }
                }
            }
        }

        public static CardOwnershipModel Load(string directory)
        {
            var networks = new NeuralNetwork[3];
            for (var index = 0; index < networks.Length; index++)
            {
                using var stream = File.OpenRead(Path.Combine(directory, Names[index]));
                networks[index] = Read(stream, 11 + index);
            }

            return new CardOwnershipModel(networks[0], networks[1], networks[2]);
        }

        /// <summary>Loads once per full folder path. The folder must be an immutable weight snapshot.</summary>
        public static CardOwnershipModel LoadCached(string directory)
        {
            var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            return Cache.GetOrAdd(path, key => new Lazy<CardOwnershipModel>(() => Load(key))).Value;
        }

        /// <summary>Reads only the exact checked ownership format, before allocating network arrays.</summary>
        public static NeuralNetwork Read(Stream stream, int expectedTag)
        {
            if (!stream.CanSeek || stream.Length - stream.Position != FileBytes || expectedTag < 11 || expectedTag > 13)
            {
                throw new InvalidDataException("Ownership network length or expected tag mismatch.");
            }

            var start = stream.Position;
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            if (reader.ReadInt32() != NeuralNetwork.FileMagic || reader.ReadInt32() != NeuralNetwork.FileVersion
                || reader.ReadInt32() != expectedTag || reader.ReadInt32() != FeatureEncoder.LayoutVersion || reader.ReadInt32() != 3)
            {
                throw new InvalidDataException("Ownership network header mismatch.");
            }

            foreach (var size in Sizes)
            {
                if (reader.ReadInt32() != size)
                {
                    throw new InvalidDataException("Ownership network shape mismatch.");
                }
            }

            stream.Position = start;
            var network = NeuralNetwork.Read(stream);
            for (var layer = 0; layer < network.LayerCount; layer++)
            {
                if (network.GetWeights(layer).Any(value => !float.IsFinite(value))
                    || network.GetBiases(layer).Any(value => !float.IsFinite(value)))
                {
                    throw new InvalidDataException("Ownership network parameters must be finite.");
                }
            }

            return network;
        }

        public Evaluator CreateEvaluator() => new Evaluator(this);

        /// <summary>One set of mutable inference buffers per player or thread.</summary>
        public sealed class Evaluator
        {
            private readonly CardOwnershipModel model;
            private readonly int[] indices = new int[FeatureEncoder.MaxActive];
            private readonly float[] features = new float[FeatureEncoder.MaxActive];

            public Evaluator(CardOwnershipModel model)
            {
                this.model = model;
            }

            /// <summary>
            /// Fills weights[physicalCard * 3 + relativeOwner - 1], where relative owners are 1–3
            /// from the deciding seat. Own and played cards get zero. Cards are unrotated here;
            /// probabilities are not masked, because the sampler owns the authoritative constraints.
            /// </summary>
            public void Evaluate(in NeuralDeal deal, uint legal, Span<float> weights)
            {
                if (weights.Length != Outputs)
                {
                    throw new ArgumentException("Ownership output must contain exactly 96 values.", nameof(weights));
                }

                Span<float> logits = stackalloc float[Outputs];
                var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.features);
                this.model.networks[FeatureEncoder.CardNetwork(deal.Kind)].Forward(this.indices.AsSpan(0, count), this.features.AsSpan(0, count), logits);
                weights.Clear();
                var rotation = FeatureEncoder.Rotation(deal.Kind);
                var unseen = ~(deal.Play.Hands[deal.Play.Turn] | deal.Played);
                for (var rest = unseen; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var source = FeatureEncoder.ToNetwork(card, rotation) * 3;
                    var maximum = Math.Max(logits[source], Math.Max(logits[source + 1], logits[source + 2]));
                    var first = MathF.Exp(logits[source] - maximum);
                    var second = MathF.Exp(logits[source + 1] - maximum);
                    var third = MathF.Exp(logits[source + 2] - maximum);
                    var total = first + second + third;
                    weights[card * 3] = first / total;
                    weights[(card * 3) + 1] = second / total;
                    weights[(card * 3) + 2] = third / total;
                }
            }
        }
    }
}
