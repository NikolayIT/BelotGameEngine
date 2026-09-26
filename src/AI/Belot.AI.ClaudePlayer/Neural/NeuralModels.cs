namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    /// <summary>
    /// The neural player's four networks: the bidding one and one card network per kind of
    /// contract (the suits, no trumps, all trumps). The trained ones are embedded in the
    /// assembly (Neural/Weights/*.bin); the trainer and the simulator also load them from a
    /// folder. Immutable and shared by every player.
    /// </summary>
    internal sealed class NeuralModels
    {
        public const int BidTag = 0;

        public static readonly string[] FileNames = { "bid.bin", "trump.bin", "notrumps.bin", "alltrumps.bin" };

        private static readonly Lazy<NeuralModels> EmbeddedModels = new Lazy<NeuralModels>(LoadEmbedded);

        private static readonly ConcurrentDictionary<string, NeuralModels> LoadedModels = new ConcurrentDictionary<string, NeuralModels>();

        public NeuralModels(NeuralNetwork bid, NeuralNetwork trump, NeuralNetwork noTrumps, NeuralNetwork allTrumps)
        {
            this.Networks = new[] { bid, trump, noTrumps, allTrumps };
            for (var tag = 0; tag < this.Networks.Length; tag++)
            {
                Check(this.Networks[tag], tag, FileNames[tag]);
            }
        }

        /// <summary>Gets the trained networks built into the assembly.</summary>
        public static NeuralModels Embedded => EmbeddedModels.Value;

        /// <summary>Gets the networks by tag: the bidding one, then the card ones (see <see cref="FeatureEncoder.CardNetwork"/>).</summary>
        public NeuralNetwork[] Networks { get; }

        public NeuralNetwork Bid => this.Networks[BidTag];

        public static NeuralModels Load(string directory)
        {
            var networks = new NeuralNetwork[FileNames.Length];
            for (var tag = 0; tag < networks.Length; tag++)
            {
                using var stream = File.OpenRead(Path.Combine(directory, FileNames[tag]));
                networks[tag] = NeuralNetwork.Read(stream);
            }

            return new NeuralModels(networks[0], networks[1], networks[2], networks[3]);
        }

        /// <summary>Loads the networks of a folder once per process.</summary>
        public static NeuralModels LoadCached(string directory) =>
            LoadedModels.GetOrAdd(Path.GetFullPath(directory), Load);

        /// <summary>The card network of a contract kind.</summary>
        public NeuralNetwork Cards(int kind) => this.Networks[1 + FeatureEncoder.CardNetwork(kind)];

        public void Save(string directory)
        {
            Directory.CreateDirectory(directory);
            for (var tag = 0; tag < this.Networks.Length; tag++)
            {
                using var stream = File.Create(Path.Combine(directory, FileNames[tag]));
                this.Networks[tag].Write(stream);
            }
        }

        private static void Check(NeuralNetwork network, int tag, string name)
        {
            var inputs = tag == BidTag ? FeatureEncoder.BidInputs : FeatureEncoder.CardInputs;
            var outputs = tag == BidTag ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs;
            if (network.Tag != tag || network.Layout != FeatureEncoder.LayoutVersion
                                   || network.InputSize != inputs || network.OutputSize != outputs)
            {
                throw new InvalidDataException(
                    $"{name}: tag {network.Tag}, layout {network.Layout}, {network.InputSize} -> {network.OutputSize}; "
                    + $"expected tag {tag}, layout {FeatureEncoder.LayoutVersion}, {inputs} -> {outputs}.");
            }
        }

        private static NeuralModels LoadEmbedded()
        {
            var assembly = typeof(NeuralModels).Assembly;
            var networks = new NeuralNetwork[FileNames.Length];
            for (var tag = 0; tag < networks.Length; tag++)
            {
                var name = "Belot.AI.ClaudePlayer.Neural.Weights." + FileNames[tag];
                using var stream = assembly.GetManifestResourceStream(name)
                                   ?? throw new InvalidOperationException($"The network {name} is not embedded in {assembly.GetName().Name}.");
                networks[tag] = NeuralNetwork.Read(stream);
            }

            return new NeuralModels(networks[0], networks[1], networks[2], networks[3]);
        }
    }
}
