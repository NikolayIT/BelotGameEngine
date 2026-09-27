namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;

    using Belot.NeuralTrainer;

    using Xunit;

    public class ActionValueLossTests
    {
        [Theory]
        [InlineData(-1f)]
        [InlineData(0f)]
        [InlineData(0.05f)]
        [InlineData(1f)]
        public void OutputGradientsMatchFiniteDifferences(float valueWeight)
        {
            var predictions = new[] { -0.3f, 1.5f, 0.15f, -0.9f, 0.7f };
            var targets = new[] { 0.4f, -0.7f, 999f, 0.1f, 0.6f };
            var gradient = new float[predictions.Length];
            var scratch = new float[predictions.Length];
            const uint Mask = 0b11011;
            ActionValueLoss.Evaluate(predictions, targets, Mask, 0.5f, valueWeight, gradient);
            for (var action = 0; action < predictions.Length; action++)
            {
                const float Step = 0.001f;
                var saved = predictions[action];
                predictions[action] = saved + Step;
                var up = ActionValueLoss.Evaluate(predictions, targets, Mask, 0.5f, valueWeight, scratch);
                predictions[action] = saved - Step;
                var down = ActionValueLoss.Evaluate(predictions, targets, Mask, 0.5f, valueWeight, scratch);
                predictions[action] = saved;
                Assert.Equal((up - down) / (2 * Step), gradient[action], 0.0001);
            }

            Assert.Equal(0, gradient[2]);
        }

        [Fact]
        public void CentredLossIgnoresTheDealsSharedLuck()
        {
            var predictions = new[] { 0.2f, 0.5f, 0.75f };
            var targets = new[] { -0.5f, 0.3f, 0.6f };
            var shifted = targets.Select(x => x + 32).ToArray();
            var first = new float[3];
            var second = new float[3];
            var loss = ActionValueLoss.Evaluate(predictions, targets, 7, 1, 0, first);
            var shiftedLoss = ActionValueLoss.Evaluate(predictions, shifted, 7, 1, 0, second);
            Assert.Equal(loss, shiftedLoss, 0.00001);
            Assert.Equal(0, first.Sum(), 0.00001);
            for (var action = 0; action < first.Length; action++)
            {
                Assert.Equal(first[action], second[action], 0.00001);
            }
        }

        [Fact]
        public void ValueTermPreservesAbsolutePointCalibration()
        {
            var gradient = new float[3];
            var loss = ActionValueLoss.Evaluate(new[] { 0f, 1f, 2f }, new[] { 0.5f, 1.5f, 2.5f }, 7, 1, 0.1f, gradient);
            Assert.Equal(3 * 0.1 * 0.5 * 0.5 * 0.5, loss, 0.000001);
            Assert.All(gradient, g => Assert.Equal(-0.05, g, 0.000001));
        }

        [Fact]
        public void UnitValueWeightRecoversSquaredErrorBelowClipping()
        {
            var predictions = new[] { 0f, 1f, 2f };
            var targets = new[] { 0.5f, 0.5f, 2.5f };
            var first = new float[3];
            var second = new float[3];
            var independent = ActionValueLoss.Evaluate(predictions, targets, 7, 10, -1, first);
            var centred = ActionValueLoss.Evaluate(predictions, targets, 7, 10, 1, second);
            Assert.Equal(independent, centred, 0.000001);
            for (var action = 0; action < first.Length; action++)
            {
                Assert.Equal(first[action], second[action], 0.000001);
            }
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(1u)]
        public void EmptyOrSingleActionHasNoAdvantageGradient(uint mask)
        {
            var gradient = new[] { 99f, 99f };
            var loss = ActionValueLoss.Evaluate(new[] { 0f, 10f }, new[] { 3f, -10f }, mask, 1, 0, gradient);
            Assert.Equal(0, loss);
            Assert.All(gradient, g => Assert.Equal(0, g));
        }
    }
}
