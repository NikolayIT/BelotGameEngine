namespace Belot.NeuralTrainer
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>
    /// One thread's share of a training step: the forward pass of a sample (keeping each
    /// layer's non-zero inputs), the loss over its labelled outputs, and the backward pass,
    /// adding the sample's gradients to its own. Everything is sparse: a layer's input is the
    /// list of its non-zero activations, which are exactly the units a ReLU lets the gradient
    /// through, so a weight row is touched only for an input that took part.
    /// </summary>
    internal sealed class MlpWorker
    {
        private readonly int[] sizes;
        private readonly float[][] outputs;
        private readonly float[][] deltas;
        private readonly int[][] activeIndices;
        private readonly float[][] activeValues;
        private readonly int[] activeCounts;

        public MlpWorker(int[] sizes)
        {
            this.sizes = (int[])sizes.Clone();
            var layers = sizes.Length - 1;
            this.Gradients = new float[layers * 2][];
            this.outputs = new float[layers][];
            this.deltas = new float[layers][];
            this.activeIndices = new int[layers][];
            this.activeValues = new float[layers][];
            this.activeCounts = new int[layers];
            for (var layer = 0; layer < layers; layer++)
            {
                this.Gradients[layer * 2] = new float[sizes[layer] * sizes[layer + 1]];
                this.Gradients[(layer * 2) + 1] = new float[sizes[layer + 1]];
                this.outputs[layer] = new float[sizes[layer + 1]];
                this.deltas[layer] = new float[sizes[layer + 1]];
                this.activeIndices[layer] = new int[Math.Max(sizes[layer], SampleBuffer.MaxFeatures)];
                this.activeValues[layer] = new float[Math.Max(sizes[layer], SampleBuffer.MaxFeatures)];
            }
        }

        /// <summary>Gets the summed gradients, laid out like <see cref="Mlp.Parameters"/>.</summary>
        public float[][] Gradients { get; }

        /// <summary>Gets the summed loss of the samples since <see cref="Clear"/>.</summary>
        public double Loss { get; private set; }

        public void Clear()
        {
            foreach (var gradient in this.Gradients)
            {
                Array.Clear(gradient);
            }

            this.Loss = 0;
        }

        /// <summary>Adds the sample's loss and gradients (scaled).</summary>
        public void Accumulate(Mlp network, Batch batch, int sample, float scale, float huber)
        {
            this.Forward(network, batch, sample);
            var last = this.sizes.Length - 2;
            var delta = this.deltas[last];
            this.LossAndDelta(batch, sample, huber, scale, delta);
            for (var layer = last; layer >= 0; layer--)
            {
                var outputs = this.sizes[layer + 1];
                delta = this.deltas[layer];
                Mlp.Add(this.Gradients[(layer * 2) + 1], delta);
                var gradient = this.Gradients[layer * 2];
                var weights = network.Weights(layer);
                var indices = this.activeIndices[layer];
                var values = this.activeValues[layer];
                var count = this.activeCounts[layer];
                for (var k = 0; k < count; k++)
                {
                    Axpy(gradient.AsSpan(indices[k] * outputs, outputs), delta, values[k]);
                }

                if (layer == 0)
                {
                    break;
                }

                // The delta of the layer below: only its active (positive) units pass it.
                var below = this.deltas[layer - 1];
                Array.Clear(below);
                for (var k = 0; k < count; k++)
                {
                    below[indices[k]] = Dot(weights.AsSpan(indices[k] * outputs, outputs), delta);
                }
            }
        }

        /// <summary>Adds the sample's loss only.</summary>
        public void Evaluate(Mlp network, Batch batch, int sample, float huber)
        {
            this.Forward(network, batch, sample);
            this.LossAndDelta(batch, sample, huber, 0, this.deltas[this.sizes.Length - 2]);
        }

        private static void Axpy(Span<float> target, ReadOnlySpan<float> source, float factor)
        {
            var width = Vector<float>.Count;
            var scaled = new Vector<float>(factor);
            var i = 0;
            for (; i + width <= target.Length; i += width)
            {
                (new Vector<float>(target.Slice(i)) + (new Vector<float>(source.Slice(i)) * scaled)).CopyTo(target.Slice(i));
            }

            for (; i < target.Length; i++)
            {
                target[i] += source[i] * factor;
            }
        }

        private static float Dot(ReadOnlySpan<float> first, ReadOnlySpan<float> second)
        {
            var width = Vector<float>.Count;
            var sum = Vector<float>.Zero;
            var i = 0;
            for (; i + width <= first.Length; i += width)
            {
                sum += new Vector<float>(first.Slice(i)) * new Vector<float>(second.Slice(i));
            }

            var result = Vector.Sum(sum);
            for (; i < first.Length; i++)
            {
                result += first[i] * second[i];
            }

            return result;
        }

        private void Forward(Mlp network, Batch batch, int sample)
        {
            var features = batch.FeatureCounts[sample];
            batch.Indices.AsSpan(sample * SampleBuffer.MaxFeatures, features).CopyTo(this.activeIndices[0]);
            batch.Values.AsSpan(sample * SampleBuffer.MaxFeatures, features).CopyTo(this.activeValues[0]);
            this.activeCounts[0] = features;
            var last = this.sizes.Length - 2;
            for (var layer = 0; layer <= last; layer++)
            {
                var count = this.activeCounts[layer];
                NeuralNetwork.Layer(
                    network.Weights(layer),
                    network.Biases(layer),
                    this.sizes[layer + 1],
                    this.activeIndices[layer].AsSpan(0, count),
                    this.activeValues[layer].AsSpan(0, count),
                    this.outputs[layer]);
                if (layer == last)
                {
                    break;
                }

                var output = this.outputs[layer];
                var nextIndices = this.activeIndices[layer + 1];
                var nextValues = this.activeValues[layer + 1];
                var active = 0;
                for (var i = 0; i < output.Length; i++)
                {
                    if (output[i] > 0)
                    {
                        nextIndices[active] = i;
                        nextValues[active] = output[i];
                        active++;
                    }
                }

                this.activeCounts[layer + 1] = active;
            }
        }

        // The Huber loss of the labelled outputs and its gradient (0 for the others).
        private void LossAndDelta(Batch batch, int sample, float huber, float scale, float[] delta)
        {
            var output = this.outputs[this.sizes.Length - 2];
            var labels = batch.Labels.AsSpan(sample * batch.Outputs, batch.Outputs);
            var mask = batch.Masks[sample];
            Array.Clear(delta);
            var loss = 0.0;
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                var o = BitOperations.TrailingZeroCount(rest);
                var error = output[o] - labels[o];
                var size = Math.Abs(error);
                if (size <= huber)
                {
                    loss += 0.5 * error * error;
                    delta[o] = error * scale;
                }
                else
                {
                    loss += huber * (size - (0.5 * huber));
                    delta[o] = Math.Sign(error) * huber * scale;
                }
            }

            this.Loss += loss;
        }
    }
}
