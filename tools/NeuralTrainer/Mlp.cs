namespace Belot.NeuralTrainer
{
    using System;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// A trainable copy of a network (<see cref="NeuralNetwork"/>'s shape and input-major
    /// weights, ReLU hidden layers, a linear output). It learns a value per action: the loss is
    /// the Huber loss over the labelled outputs of each sample only (the actions whose value
    /// was measured), optionally separating the common value from action differences, and
    /// minimised with Adam. The gradients are summed over the batch by several
    /// threads (<see cref="MlpWorker"/>), each over its share of the samples.
    /// </summary>
    internal sealed class Mlp
    {
        private const float Beta1 = 0.9f;
        private const float Beta2 = 0.999f;
        private const float Epsilon = 1e-8f;
        private const int Chunk = 1 << 14;

        private readonly float[][] firstMoments;
        private readonly float[][] secondMoments;
        private int step;

        public Mlp(int tag, int[] sizes, Random random)
        {
            this.Tag = tag;
            this.Sizes = (int[])sizes.Clone();
            var layers = sizes.Length - 1;
            this.Parameters = new float[layers * 2][];
            for (var layer = 0; layer < layers; layer++)
            {
                // He initialisation for the ReLU layers; the output starts small.
                var inputs = sizes[layer];
                var outputs = sizes[layer + 1];
                var scale = layer == layers - 1 ? 0.1 * Math.Sqrt(1.0 / inputs) : Math.Sqrt(2.0 / inputs);
                var weights = new float[inputs * outputs];
                for (var i = 0; i < weights.Length; i++)
                {
                    weights[i] = (float)(Gaussian(random) * scale);
                }

                this.Parameters[layer * 2] = weights;
                this.Parameters[(layer * 2) + 1] = new float[outputs];
            }

            this.firstMoments = this.Parameters.Select(x => new float[x.Length]).ToArray();
            this.secondMoments = this.Parameters.Select(x => new float[x.Length]).ToArray();
        }

        public Mlp(NeuralNetwork network)
        {
            this.Tag = network.Tag;
            this.Sizes = network.GetSizes();
            var layers = network.LayerCount;
            this.Parameters = new float[layers * 2][];
            for (var layer = 0; layer < layers; layer++)
            {
                this.Parameters[layer * 2] = network.GetWeights(layer);
                this.Parameters[(layer * 2) + 1] = network.GetBiases(layer);
            }

            this.firstMoments = this.Parameters.Select(x => new float[x.Length]).ToArray();
            this.secondMoments = this.Parameters.Select(x => new float[x.Length]).ToArray();
        }

        public int Tag { get; }

        public int[] Sizes { get; }

        public int Layers => this.Sizes.Length - 1;

        /// <summary>Gets the weights and biases: [2 * layer] the weights (input-major), [2 * layer + 1] the biases.</summary>
        public float[][] Parameters { get; }

        public float[] Weights(int layer) => this.Parameters[layer * 2];

        public float[] Biases(int layer) => this.Parameters[(layer * 2) + 1];

        public NeuralNetwork ToNetwork()
        {
            var weights = new float[this.Layers][];
            var biases = new float[this.Layers][];
            for (var layer = 0; layer < this.Layers; layer++)
            {
                weights[layer] = this.Weights(layer);
                biases[layer] = this.Biases(layer);
            }

            return new NeuralNetwork(this.Tag, FeatureEncoder.LayoutVersion, this.Sizes, weights, biases);
        }

        /// <summary>One step of Adam on the batch.</summary>
        /// <returns>The batch's mean loss per labelled output.</returns>
        public double Train(Batch batch, MlpWorker[] workers, float learningRate, float maxNorm, float huber, float valueWeight = -1)
        {
            var labels = 0;
            for (var i = 0; i < batch.Count; i++)
            {
                labels += BitOperations.PopCount(batch.Masks[i]);
            }

            if (labels == 0)
            {
                return 0;
            }

            var scale = 1f / labels;
            Parallel.For(
                0,
                workers.Length,
                w =>
                {
                    var worker = workers[w];
                    worker.Clear();
                    for (var sample = w; sample < batch.Count; sample += workers.Length)
                    {
                        worker.Accumulate(this, batch, sample, scale, huber, valueWeight);
                    }
                });

            // Sum the workers' gradients into the first one, then clip and step.
            var gradients = workers[0].Gradients;
            var chunks = 0;
            foreach (var parameter in this.Parameters)
            {
                chunks += (parameter.Length + Chunk - 1) / Chunk;
            }

            var squares = new double[chunks];
            Parallel.For(
                0,
                chunks,
                c =>
                {
                    var (index, start, end) = this.Locate(c);
                    var sum = gradients[index].AsSpan(start, end - start);
                    for (var w = 1; w < workers.Length; w++)
                    {
                        Add(sum, workers[w].Gradients[index].AsSpan(start, end - start));
                    }

                    var square = 0.0;
                    foreach (var g in sum)
                    {
                        square += g * g;
                    }

                    squares[c] = square;
                });

            var norm = Math.Sqrt(squares.Sum());
            var clip = norm > maxNorm ? (float)(maxNorm / norm) : 1f;
            this.step++;
            var correction = (float)(learningRate * Math.Sqrt(1 - Math.Pow(Beta2, this.step)) / (1 - Math.Pow(Beta1, this.step)));
            Parallel.For(
                0,
                chunks,
                c =>
                {
                    var (index, start, end) = this.Locate(c);
                    Adam(
                        this.Parameters[index].AsSpan(start, end - start),
                        gradients[index].AsSpan(start, end - start),
                        this.firstMoments[index].AsSpan(start, end - start),
                        this.secondMoments[index].AsSpan(start, end - start),
                        clip,
                        correction);
                });

            var loss = 0.0;
            foreach (var worker in workers)
            {
                loss += worker.Loss;
            }

            return loss / labels;
        }

        /// <summary>The mean loss per labelled output, without learning.</summary>
        public double Loss(Batch batch, MlpWorker worker, float huber, float valueWeight = -1)
        {
            worker.Clear();
            var labels = 0;
            for (var sample = 0; sample < batch.Count; sample++)
            {
                labels += BitOperations.PopCount(batch.Masks[sample]);
                worker.Evaluate(this, batch, sample, huber, valueWeight);
            }

            return labels == 0 ? 0 : worker.Loss / labels;
        }

        internal static void Add(Span<float> sum, ReadOnlySpan<float> other)
        {
            var width = Vector<float>.Count;
            var i = 0;
            for (; i + width <= sum.Length; i += width)
            {
                (new Vector<float>(sum.Slice(i)) + new Vector<float>(other.Slice(i))).CopyTo(sum.Slice(i));
            }

            for (; i < sum.Length; i++)
            {
                sum[i] += other[i];
            }
        }

        private static void Adam(Span<float> weights, ReadOnlySpan<float> gradients, Span<float> first, Span<float> second, float clip, float rate)
        {
            var width = Vector<float>.Count;
            var i = 0;
            var beta1 = new Vector<float>(Beta1);
            var beta2 = new Vector<float>(Beta2);
            var oneMinusBeta1 = new Vector<float>(1 - Beta1);
            var oneMinusBeta2 = new Vector<float>(1 - Beta2);
            var epsilon = new Vector<float>(Epsilon);
            var clipVector = new Vector<float>(clip);
            var rateVector = new Vector<float>(rate);
            for (; i + width <= weights.Length; i += width)
            {
                var g = new Vector<float>(gradients.Slice(i)) * clipVector;
                var m = (beta1 * new Vector<float>(first.Slice(i))) + (oneMinusBeta1 * g);
                var v = (beta2 * new Vector<float>(second.Slice(i))) + (oneMinusBeta2 * g * g);
                m.CopyTo(first.Slice(i));
                v.CopyTo(second.Slice(i));
                (new Vector<float>(weights.Slice(i)) - (rateVector * m / (Vector.SquareRoot(v) + epsilon))).CopyTo(weights.Slice(i));
            }

            for (; i < weights.Length; i++)
            {
                var g = gradients[i] * clip;
                first[i] = (Beta1 * first[i]) + ((1 - Beta1) * g);
                second[i] = (Beta2 * second[i]) + ((1 - Beta2) * g * g);
                weights[i] -= rate * first[i] / (MathF.Sqrt(second[i]) + Epsilon);
            }
        }

        private static double Gaussian(Random random)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        // The parameter array and the range of a chunk.
        private (int Index, int Start, int End) Locate(int chunk)
        {
            for (var index = 0; index < this.Parameters.Length; index++)
            {
                var chunks = (this.Parameters[index].Length + Chunk - 1) / Chunk;
                if (chunk < chunks)
                {
                    var start = chunk * Chunk;
                    return (index, start, Math.Min(start + Chunk, this.Parameters[index].Length));
                }

                chunk -= chunks;
            }

            throw new ArgumentOutOfRangeException(nameof(chunk));
        }
    }
}
