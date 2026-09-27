namespace Belot.NeuralTrainer
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// Training-only client of the loopback Python inference server. A connection verifies
    /// the exact four weight files; each request sends only the deciding seats' features.
    /// </summary>
    internal sealed class GpuCardPolicy : IBatchedCardPolicy, IDisposable
    {
        private const int Magic = 0x314E4E47;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly NeuralEvaluator reference;
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] featureValues = new float[FeatureEncoder.MaxActive];
        private float[] inputs = Array.Empty<float>();
        private long verified;
        private long different;
        private float largestGap;

        public GpuCardPolicy(string modelDirectory, int port, NeuralModels verifyModels = null)
        {
            this.reference = verifyModels == null ? null : new NeuralEvaluator(verifyModels);
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("GPU transport requires little-endian floats.");
            }

            this.client = new TcpClient { NoDelay = true, ReceiveTimeout = 120_000, SendTimeout = 120_000 };
            try
            {
                this.client.Connect(IPAddress.Loopback, port);
                this.stream = this.client.GetStream();
                Span<byte> handshake = stackalloc byte[4 + (4 * 32)];
                BinaryPrimitives.WriteInt32LittleEndian(handshake, Magic);
                for (var tag = 0; tag < NeuralModels.FileNames.Length; tag++)
                {
                    var bytes = File.ReadAllBytes(Path.Combine(modelDirectory, NeuralModels.FileNames[tag]));
                    SHA256.HashData(bytes, handshake.Slice(4 + (32 * tag), 32));
                }

                this.stream.Write(handshake);
                Span<byte> response = stackalloc byte[8];
                this.stream.ReadExactly(response);
                if (BinaryPrimitives.ReadInt32LittleEndian(response) != Magic || BinaryPrimitives.ReadInt32LittleEndian(response[4..]) != 0)
                {
                    throw new InvalidDataException("GPU server rejected the protocol or weight hashes.");
                }
            }
            catch
            {
                this.client.Dispose();
                throw;
            }
        }

        public void Choose(NeuralDeal[] states, int[] slots, uint[] legal, int count, int[] chosen)
        {
            if (count < 1 || count > 4096)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var size = count * FeatureEncoder.CardInputs;
            if (this.inputs.Length < size)
            {
                this.inputs = new float[size];
            }

            Array.Clear(this.inputs, 0, size);
            var kind = states[slots[0]].Kind;
            for (var row = 0; row < count; row++)
            {
                ref readonly var state = ref states[slots[row]];
                if (state.Kind != kind)
                {
                    throw new ArgumentException("A GPU batch must have one contract kind.", nameof(states));
                }

                var active = FeatureEncoder.EncodeCard(in state, legal[row], this.indices, this.featureValues);
                for (var feature = 0; feature < active; feature++)
                {
                    this.inputs[(row * FeatureEncoder.CardInputs) + this.indices[feature]] = this.featureValues[feature];
                }
            }

            Span<byte> header = stackalloc byte[20];
            BinaryPrimitives.WriteInt32LittleEndian(header, Magic);
            BinaryPrimitives.WriteInt32LittleEndian(header[4..], 1 + FeatureEncoder.CardNetwork(kind));
            BinaryPrimitives.WriteInt32LittleEndian(header[8..], FeatureEncoder.Rotation(kind));
            BinaryPrimitives.WriteInt32LittleEndian(header[12..], count);
            BinaryPrimitives.WriteInt32LittleEndian(header[16..], FeatureEncoder.CardInputs);
            this.stream.Write(header);
            this.stream.Write(MemoryMarshal.AsBytes(this.inputs.AsSpan(0, size)));
            this.stream.ReadExactly(header[..8]);
            if (BinaryPrimitives.ReadInt32LittleEndian(header) != Magic || BinaryPrimitives.ReadInt32LittleEndian(header[4..]) != count)
            {
                throw new InvalidDataException("Invalid GPU response header.");
            }

            this.stream.ReadExactly(MemoryMarshal.AsBytes(chosen.AsSpan(0, count)));
            if (this.reference != null)
            {
                var values = new float[FeatureEncoder.CardOutputs];
                for (var row = 0; row < count; row++)
                {
                    var actual = chosen[row];
                    if ((uint)actual >= 32 || (legal[row] & (1u << actual)) == 0)
                    {
                        throw new InvalidDataException("GPU returned an illegal card.");
                    }

                    var expected = this.reference.BestCard(in states[slots[row]], legal[row], values);
                    this.verified++;
                    if (actual != expected)
                    {
                        this.different++;
                        this.largestGap = Math.Max(this.largestGap, values[expected] - values[actual]);
                        if (this.largestGap > 0.01f)
                        {
                            throw new InvalidDataException($"GPU disagrees with managed inference by {this.largestGap} game points.");
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            this.stream.Dispose();
            this.client.Dispose();
            if (this.reference != null)
            {
                Console.WriteLine($"GPU verification: {this.verified} decisions, {this.different} different choices, largest value gap {this.largestGap:F6} game points.");
            }
        }
    }
}
