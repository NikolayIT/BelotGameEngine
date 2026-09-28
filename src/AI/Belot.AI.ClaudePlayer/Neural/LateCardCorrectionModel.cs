namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    /// <summary>A separate, optional correction to frozen card values in the final four tricks.</summary>
    internal sealed class LateCardCorrectionModel
    {
        public const int FileBytes = 81120;

        private static readonly int[] Sizes = { FeatureEncoder.CardInputs, 64, FeatureEncoder.CardOutputs };

        private static readonly string[] Names = { "trump.bin", "notrumps.bin", "alltrumps.bin" };

        private static readonly ConcurrentDictionary<string, Lazy<LateCardCorrectionModel>> Cache =
            new ConcurrentDictionary<string, Lazy<LateCardCorrectionModel>>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private readonly NeuralNetwork[] networks;

        public LateCardCorrectionModel(NeuralNetwork trump, NeuralNetwork noTrumps, NeuralNetwork allTrumps)
        {
            this.networks = new[] { trump, noTrumps, allTrumps };
            for (var index = 0; index < this.networks.Length; index++)
            {
                Validate(this.networks[index], 21 + index);
            }
        }

        public static LateCardCorrectionModel Load(string directory)
        {
            var networks = new NeuralNetwork[3];
            for (var index = 0; index < networks.Length; index++)
            {
                using var stream = File.OpenRead(Path.Combine(directory, Names[index]));
                networks[index] = Read(stream, 21 + index);
            }

            return new LateCardCorrectionModel(networks[0], networks[1], networks[2]);
        }

        /// <summary>Loads once per full folder path; callers must use immutable weight snapshots.</summary>
        public static LateCardCorrectionModel LoadCached(string directory)
        {
            var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            return Cache.GetOrAdd(path, key => new Lazy<LateCardCorrectionModel>(() => Load(key))).Value;
        }

        /// <summary>Checks the entire fixed header and length before allocating parameter arrays.</summary>
        public static NeuralNetwork Read(Stream stream, int expectedTag)
        {
            if (!stream.CanSeek || stream.Length - stream.Position != FileBytes || expectedTag < 21 || expectedTag > 23)
            {
                throw new InvalidDataException("Late card correction length or expected tag mismatch.");
            }

            var start = stream.Position;
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            if (reader.ReadInt32() != NeuralNetwork.FileMagic || reader.ReadInt32() != NeuralNetwork.FileVersion
                || reader.ReadInt32() != expectedTag || reader.ReadInt32() != FeatureEncoder.LayoutVersion || reader.ReadInt32() != 2)
            {
                throw new InvalidDataException("Late card correction header mismatch.");
            }

            foreach (var size in Sizes)
            {
                if (reader.ReadInt32() != size)
                {
                    throw new InvalidDataException("Late card correction shape mismatch.");
                }
            }

            stream.Position = start;
            var network = NeuralNetwork.Read(stream);
            Validate(network, expectedTag);
            return network;
        }

        public Evaluator CreateEvaluator() => new Evaluator(this);

        private static void Validate(NeuralNetwork network, int expectedTag)
        {
            if (network == null || network.Tag != expectedTag || network.Layout != FeatureEncoder.LayoutVersion || !network.GetSizes().SequenceEqual(Sizes))
            {
                throw new InvalidDataException("Late card correction tag, layout or shape mismatch.");
            }

            for (var layer = 0; layer < network.LayerCount; layer++)
            {
                if (network.GetWeights(layer).Any(value => !float.IsFinite(value)) || network.GetBiases(layer).Any(value => !float.IsFinite(value)))
                {
                    throw new InvalidDataException("Late card correction parameters must be finite.");
                }
            }
        }

        /// <summary>One set of mutable inference buffers per player or thread.</summary>
        public sealed class Evaluator
        {
            private readonly LateCardCorrectionModel model;
            private readonly int[] indices = new int[FeatureEncoder.MaxActive];
            private readonly float[] features = new float[FeatureEncoder.MaxActive];

            public Evaluator(LateCardCorrectionModel model)
            {
                this.model = model;
            }

            /// <summary>
            /// Adds centered corrections in game points to legal physical-card slots. Earlier
            /// tricks and illegal slots are untouched; the base network remains immutable.
            /// </summary>
            public void Apply(in NeuralDeal deal, uint legal, Span<float> cardValues)
            {
                if (cardValues.Length != FeatureEncoder.CardOutputs)
                {
                    throw new ArgumentException("Card values must contain exactly 32 values.", nameof(cardValues));
                }

                if (deal.Play.TricksPlayed < 4 || BitOperations.PopCount(legal) <= 1)
                {
                    return;
                }

                Span<float> correction = stackalloc float[FeatureEncoder.CardOutputs];
                var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.features);
                this.model.networks[FeatureEncoder.CardNetwork(deal.Kind)].Forward(this.indices.AsSpan(0, count), this.features.AsSpan(0, count), correction);
                var rotation = FeatureEncoder.Rotation(deal.Kind);
                var sum = 0d;
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    sum += correction[FeatureEncoder.ToNetwork(card, rotation)];
                }

                var mean = (float)(sum / BitOperations.PopCount(legal));
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var delta = correction[FeatureEncoder.ToNetwork(card, rotation)] - mean;
                    if (delta != 0)
                    {
                        cardValues[card] += NeuralEvaluator.ValueScale * delta;
                    }
                }
            }
        }
    }
}
