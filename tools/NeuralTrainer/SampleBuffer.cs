namespace Belot.NeuralTrainer
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// A ring of training samples for one network: the non-zero inputs of a decision and the
    /// measured value of each action that was labelled (in units of
    /// <c>NeuralEvaluator.ValueScale</c>). The newest samples replace the oldest. Thread-safe:
    /// many actors add while a learner draws batches.
    /// </summary>
    internal sealed class SampleBuffer
    {
        /// <summary>The most non-zero inputs a sample may have (a sample with more is dropped).</summary>
        public const int MaxFeatures = 192;

        private const int FileMagic = 0x53504E42;

        private readonly object sync = new object();
        private readonly ushort[] indices;
        private readonly Half[] values;
        private readonly byte[] featureCounts;
        private readonly Half[] labels;
        private readonly uint[] masks;
        private long written;
        private long dropped;

        public SampleBuffer(int capacity, int outputs)
        {
            this.Capacity = capacity;
            this.Outputs = outputs;
            this.indices = new ushort[(long)capacity * MaxFeatures];
            this.values = new Half[(long)capacity * MaxFeatures];
            this.featureCounts = new byte[capacity];
            this.labels = new Half[(long)capacity * outputs];
            this.masks = new uint[capacity];
        }

        public int Capacity { get; }

        public int Outputs { get; }

        /// <summary>Gets how many samples were ever added.</summary>
        public long Written => Interlocked.Read(ref this.written);

        public long Dropped => Interlocked.Read(ref this.dropped);

        /// <summary>Gets how many samples the ring holds.</summary>
        public int Count => (int)Math.Min(this.Written, this.Capacity);

        public static SampleBuffer Load(string path, int? capacity = null)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            if (reader.ReadInt32() != FileMagic)
            {
                throw new InvalidDataException($"{path} is not a sample file.");
            }

            var outputs = reader.ReadInt32();
            var count = reader.ReadInt32();
            var buffer = new SampleBuffer(Math.Max(1, capacity ?? count), outputs);
            var sampleIndices = new int[MaxFeatures];
            var sampleValues = new float[MaxFeatures];
            var sampleLabels = new float[outputs];
            for (var i = 0; i < count; i++)
            {
                var features = reader.ReadByte();
                for (var k = 0; k < features; k++)
                {
                    sampleIndices[k] = reader.ReadUInt16();
                    sampleValues[k] = (float)reader.ReadHalf();
                }

                var mask = reader.ReadUInt32();
                for (var o = 0; o < outputs; o++)
                {
                    sampleLabels[o] = (mask & (1u << o)) != 0 ? (float)reader.ReadHalf() : 0;
                }

                buffer.Add(sampleIndices.AsSpan(0, features), sampleValues.AsSpan(0, features), sampleLabels, mask);
            }

            return buffer;
        }

        /// <summary>Adds a sample; false (and dropped) when it has too many non-zero inputs.</summary>
        public bool Add(ReadOnlySpan<int> sampleIndices, ReadOnlySpan<float> sampleValues, ReadOnlySpan<float> sampleLabels, uint mask)
        {
            if (sampleIndices.Length > MaxFeatures || mask == 0)
            {
                Interlocked.Increment(ref this.dropped);
                return false;
            }

            lock (this.sync)
            {
                var slot = (int)(this.written % this.Capacity);
                var start = (long)slot * MaxFeatures;
                for (var k = 0; k < sampleIndices.Length; k++)
                {
                    this.indices[start + k] = (ushort)sampleIndices[k];
                    this.values[start + k] = (Half)sampleValues[k];
                }

                this.featureCounts[slot] = (byte)sampleIndices.Length;
                var labelStart = (long)slot * this.Outputs;
                for (var o = 0; o < this.Outputs; o++)
                {
                    this.labels[labelStart + o] = (Half)sampleLabels[o];
                }

                this.masks[slot] = mask;
                Interlocked.Increment(ref this.written);
            }

            return true;
        }

        /// <summary>Fills the batch with random samples of the ring.</summary>
        public void Sample(Batch batch, int size, Random random)
        {
            lock (this.sync)
            {
                var count = this.Count;
                batch.Count = Math.Min(size, batch.Capacity);
                for (var i = 0; i < batch.Count; i++)
                {
                    this.CopyTo(batch, i, random.Next(count));
                }
            }
        }

        /// <summary>Fills the batch with the samples at these slots.</summary>
        public void Take(Batch batch, ReadOnlySpan<int> slots)
        {
            lock (this.sync)
            {
                batch.Count = Math.Min(slots.Length, batch.Capacity);
                for (var i = 0; i < batch.Count; i++)
                {
                    this.CopyTo(batch, i, slots[i]);
                }
            }
        }

        public void Save(string path)
        {
            lock (this.sync)
            {
                using var writer = new BinaryWriter(File.Create(path));
                var count = this.Count;
                writer.Write(FileMagic);
                writer.Write(this.Outputs);
                writer.Write(count);
                for (var slot = 0; slot < count; slot++)
                {
                    var features = this.featureCounts[slot];
                    writer.Write(features);
                    var start = (long)slot * MaxFeatures;
                    for (var k = 0; k < features; k++)
                    {
                        writer.Write(this.indices[start + k]);
                        writer.Write(this.values[start + k]);
                    }

                    var mask = this.masks[slot];
                    writer.Write(mask);
                    for (var o = 0; o < this.Outputs; o++)
                    {
                        if ((mask & (1u << o)) != 0)
                        {
                            writer.Write(this.labels[((long)slot * this.Outputs) + o]);
                        }
                    }
                }
            }
        }

        private void CopyTo(Batch batch, int row, int slot)
        {
            var features = this.featureCounts[slot];
            var start = (long)slot * MaxFeatures;
            var target = row * MaxFeatures;
            for (var k = 0; k < features; k++)
            {
                batch.Indices[target + k] = this.indices[start + k];
                batch.Values[target + k] = (float)this.values[start + k];
            }

            batch.FeatureCounts[row] = features;
            var labelStart = (long)slot * this.Outputs;
            for (var o = 0; o < this.Outputs; o++)
            {
                batch.Labels[(row * this.Outputs) + o] = (float)this.labels[labelStart + o];
            }

            batch.Masks[row] = this.masks[slot];
        }
    }
}
