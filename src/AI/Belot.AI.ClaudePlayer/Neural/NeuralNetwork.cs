namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A multilayer perceptron (ReLU hidden layers, a linear output), inference only. It is
    /// immutable, so one instance serves every player on every thread. The weights of a layer
    /// are stored input-major (the outputs of one input side by side), so a layer is the bias
    /// plus, for every non-zero input, that input times its row: the sparse inputs cost only the
    /// rows they touch, and the rows are summed a SIMD vector of outputs at a time.
    ///
    /// The file (little-endian): the magic "BNN1", the version, the network's tag and feature
    /// layout, the number of layers and their sizes, then per layer its weights and biases as
    /// 16-bit floats.
    /// </summary>
    internal sealed class NeuralNetwork
    {
        public const int FileMagic = 0x314E4E42;

        public const int FileVersion = 1;

        private readonly int[] sizes;
        private readonly float[][] weights;
        private readonly float[][] biases;
        private readonly int maxWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeuralNetwork"/> class (the arrays are
        /// copied).
        /// </summary>
        /// <param name="tag">What the network is for (checked on loading).</param>
        /// <param name="layout">The feature layout it was trained on.</param>
        /// <param name="sizes">The sizes of the input, the hidden layers and the output.</param>
        /// <param name="weights">Per layer, input-major: weights[layer][input * outputs + output].</param>
        /// <param name="biases">Per layer, the biases.</param>
        public NeuralNetwork(int tag, int layout, int[] sizes, float[][] weights, float[][] biases)
        {
            if (sizes.Length < 2 || weights.Length != sizes.Length - 1 || biases.Length != sizes.Length - 1)
            {
                throw new ArgumentException("The layers do not match the sizes.");
            }

            this.Tag = tag;
            this.Layout = layout;
            this.sizes = (int[])sizes.Clone();
            this.weights = new float[weights.Length][];
            this.biases = new float[biases.Length][];
            for (var layer = 0; layer < weights.Length; layer++)
            {
                if (weights[layer].Length != sizes[layer] * sizes[layer + 1] || biases[layer].Length != sizes[layer + 1])
                {
                    throw new ArgumentException($"Layer {layer} has the wrong number of parameters.");
                }

                this.weights[layer] = (float[])weights[layer].Clone();
                this.biases[layer] = (float[])biases[layer].Clone();
            }

            for (var layer = 1; layer < sizes.Length; layer++)
            {
                this.maxWidth = Math.Max(this.maxWidth, sizes[layer]);
            }
        }

        public int Tag { get; }

        public int Layout { get; }

        public int InputSize => this.sizes[0];

        public int OutputSize => this.sizes[^1];

        public int LayerCount => this.weights.Length;

        public int ParameterCount
        {
            get
            {
                var count = 0;
                for (var layer = 0; layer < this.weights.Length; layer++)
                {
                    count += this.weights[layer].Length + this.biases[layer].Length;
                }

                return count;
            }
        }

        public static NeuralNetwork Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != FileMagic)
            {
                throw new InvalidDataException("Not a network file.");
            }

            var version = reader.ReadInt32();
            if (version != FileVersion)
            {
                throw new InvalidDataException($"Network file version {version}, expected {FileVersion}.");
            }

            var tag = reader.ReadInt32();
            var layout = reader.ReadInt32();
            var layers = reader.ReadInt32();
            if (layers < 1 || layers > 16)
            {
                throw new InvalidDataException($"{layers} layers.");
            }

            var sizes = new int[layers + 1];
            for (var i = 0; i <= layers; i++)
            {
                sizes[i] = reader.ReadInt32();
                if (sizes[i] < 1 || sizes[i] > 1 << 14)
                {
                    throw new InvalidDataException($"A layer of {sizes[i]}.");
                }
            }

            var weights = new float[layers][];
            var biases = new float[layers][];
            for (var layer = 0; layer < layers; layer++)
            {
                weights[layer] = ReadHalves(reader, sizes[layer] * sizes[layer + 1]);
                biases[layer] = ReadHalves(reader, sizes[layer + 1]);
            }

            return new NeuralNetwork(tag, layout, sizes, weights, biases);
        }

        public int[] GetSizes() => (int[])this.sizes.Clone();

        public float[] GetWeights(int layer) => (float[])this.weights[layer].Clone();

        public float[] GetBiases(int layer) => (float[])this.biases[layer].Clone();

        public void Write(Stream stream)
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write(FileMagic);
            writer.Write(FileVersion);
            writer.Write(this.Tag);
            writer.Write(this.Layout);
            writer.Write(this.weights.Length);
            foreach (var size in this.sizes)
            {
                writer.Write(size);
            }

            for (var layer = 0; layer < this.weights.Length; layer++)
            {
                WriteHalves(writer, this.weights[layer]);
                WriteHalves(writer, this.biases[layer]);
            }
        }

        /// <summary>
        /// Computes the outputs for the inputs given as a sparse list (every input not listed is 0).
        /// </summary>
        public void Forward(ReadOnlySpan<int> indices, ReadOnlySpan<float> values, Span<float> output)
        {
            Span<float> first = stackalloc float[this.maxWidth];
            Span<float> second = stackalloc float[this.maxWidth];
            Span<int> activeIndices = stackalloc int[this.maxWidth];
            Span<float> activeValues = stackalloc float[this.maxWidth];
            var last = this.weights.Length - 1;
            var input = first;
            Layer(this.weights[0], this.biases[0], this.sizes[1], indices, values, last == 0 ? output : input);
            for (var layer = 1; layer <= last; layer++)
            {
                // ReLU, keeping the positive activations as the next layer's sparse input.
                var width = this.sizes[layer];
                var active = 0;
                for (var i = 0; i < width; i++)
                {
                    if (input[i] > 0)
                    {
                        activeIndices[active] = i;
                        activeValues[active] = input[i];
                        active++;
                    }
                }

                var next = layer == last ? output : (layer % 2 == 1 ? second : first);
                Layer(this.weights[layer], this.biases[layer], this.sizes[layer + 1], activeIndices[..active], activeValues[..active], next);
                input = next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static void Layer(float[] weights, float[] bias, int outputs, ReadOnlySpan<int> indices, ReadOnlySpan<float> values, Span<float> result)
        {
            ref var w = ref MemoryMarshal.GetArrayDataReference(weights);
            ref var b = ref MemoryMarshal.GetArrayDataReference(bias);
            ref var r = ref MemoryMarshal.GetReference(result);
            var width = Vector<float>.Count;
            var count = indices.Length;
            var o = 0;
            for (; o + (4 * width) <= outputs; o += 4 * width)
            {
                var offset = (nuint)o;
                var acc0 = Vector.LoadUnsafe(ref b, offset);
                var acc1 = Vector.LoadUnsafe(ref b, offset + (nuint)width);
                var acc2 = Vector.LoadUnsafe(ref b, offset + (nuint)(2 * width));
                var acc3 = Vector.LoadUnsafe(ref b, offset + (nuint)(3 * width));
                for (var k = 0; k < count; k++)
                {
                    var x = new Vector<float>(values[k]);
                    var row = (nuint)((indices[k] * outputs) + o);
                    acc0 += Vector.LoadUnsafe(ref w, row) * x;
                    acc1 += Vector.LoadUnsafe(ref w, row + (nuint)width) * x;
                    acc2 += Vector.LoadUnsafe(ref w, row + (nuint)(2 * width)) * x;
                    acc3 += Vector.LoadUnsafe(ref w, row + (nuint)(3 * width)) * x;
                }

                acc0.StoreUnsafe(ref r, offset);
                acc1.StoreUnsafe(ref r, offset + (nuint)width);
                acc2.StoreUnsafe(ref r, offset + (nuint)(2 * width));
                acc3.StoreUnsafe(ref r, offset + (nuint)(3 * width));
            }

            for (; o + width <= outputs; o += width)
            {
                var acc = Vector.LoadUnsafe(ref b, (nuint)o);
                for (var k = 0; k < count; k++)
                {
                    acc += Vector.LoadUnsafe(ref w, (nuint)((indices[k] * outputs) + o)) * new Vector<float>(values[k]);
                }

                acc.StoreUnsafe(ref r, (nuint)o);
            }

            for (; o < outputs; o++)
            {
                var sum = bias[o];
                for (var k = 0; k < count; k++)
                {
                    sum += weights[(indices[k] * outputs) + o] * values[k];
                }

                result[o] = sum;
            }
        }

        private static float[] ReadHalves(BinaryReader reader, int count)
        {
            var bytes = reader.ReadBytes(count * 2);
            if (bytes.Length != count * 2)
            {
                throw new InvalidDataException("The network file is truncated.");
            }

            var halves = MemoryMarshal.Cast<byte, Half>(bytes);
            var result = new float[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = (float)halves[i];
            }

            return result;
        }

        private static void WriteHalves(BinaryWriter writer, float[] values)
        {
            var halves = new Half[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                halves[i] = (Half)values[i];
            }

            writer.Write(MemoryMarshal.AsBytes(halves.AsSpan()));
        }
    }
}
