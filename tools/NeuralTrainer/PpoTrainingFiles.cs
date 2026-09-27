namespace Belot.NeuralTrainer
{
    using System;
    using System.IO;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Training-only full-precision snapshots; deployed weights still use BNN1.</summary>
    internal static class PpoTrainingFiles
    {
        public const int Magic = 0x31464E42; // BNF1.

        public static NeuralModels Load(string directory, bool fullPrecision)
        {
            if (!fullPrecision)
            {
                return NeuralModels.Load(directory);
            }

            var networks = new NeuralNetwork[4];
            for (var tag = 0; tag < networks.Length; tag++)
            {
                using var file = File.OpenRead(Path.Combine(directory, FileName(tag, fullPrecision)));
                networks[tag] = tag == 0 ? NeuralNetwork.Read(file) : Read(file, tag);
            }

            return new NeuralModels(networks[0], networks[1], networks[2], networks[3]);
        }

        public static string FileName(int tag, bool fullPrecision) =>
            tag > 0 && fullPrecision ? Path.ChangeExtension(NeuralModels.FileNames[tag], ".f32") : NeuralModels.FileNames[tag];

        public static NeuralNetwork Read(Stream stream, int expectedTag)
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            if (reader.ReadInt32() != Magic || reader.ReadInt32() != 1 || reader.ReadInt32() != expectedTag
                || reader.ReadInt32() != FeatureEncoder.LayoutVersion)
            {
                throw new InvalidDataException("Unsupported PPO snapshot header.");
            }

            var layers = reader.ReadInt32();
            if (layers < 1 || layers > 16)
            {
                throw new InvalidDataException("Invalid PPO layer count.");
            }

            var sizes = new int[layers + 1];
            long parameters = 0;
            for (var i = 0; i < sizes.Length; i++)
            {
                sizes[i] = reader.ReadInt32();
                if (sizes[i] < 1 || sizes[i] > 16384)
                {
                    throw new InvalidDataException("Invalid PPO layer width.");
                }

                if (i > 0)
                {
                    parameters += ((long)sizes[i - 1] + 1) * sizes[i];
                }
            }

            if (sizes[0] != FeatureEncoder.CardInputs || sizes[^1] != FeatureEncoder.CardOutputs
                || parameters > 20_000_000 || stream.Length - stream.Position != parameters * sizeof(float))
            {
                throw new InvalidDataException("Invalid PPO dimensions or parameter length.");
            }

            var weights = new float[layers][];
            var biases = new float[layers][];
            for (var layer = 0; layer < layers; layer++)
            {
                weights[layer] = ReadValues(reader, sizes[layer] * sizes[layer + 1]);
                biases[layer] = ReadValues(reader, sizes[layer + 1]);
            }

            return new NeuralNetwork(expectedTag, FeatureEncoder.LayoutVersion, sizes, weights, biases);
        }

        private static float[] ReadValues(BinaryReader reader, int count)
        {
            var result = new float[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = reader.ReadSingle();
                if (!float.IsFinite(result[i]))
                {
                    throw new InvalidDataException("Non-finite PPO parameter.");
                }
            }

            return result;
        }
    }
}
