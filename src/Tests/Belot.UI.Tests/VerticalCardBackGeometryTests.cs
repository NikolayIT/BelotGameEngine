namespace Belot.UI.Tests
{
    using System;

    using Belot.UI.Game;

    using Xunit;

    public class VerticalCardBackGeometryTests
    {
        [Theory]
        [InlineData(0.8)]
        [InlineData(1)]
        [InlineData(1.25)]
        public void EveryCardShouldStayInsideShortAndFullSideSeats(double scale)
        {
            for (var count = 1; count <= 8; count++)
            {
                foreach (var width in new[] { 12.0, 38.0, 80.0 })
                {
                    foreach (var height in new[] { 0.5, 8, 20, 40, 62, 90, 144, 240 })
                    {
                        var availableWidth = width * scale;
                        var availableHeight = height * scale;
                        var result = VerticalCardBackGeometry.Fit(count, availableWidth, availableHeight, scale);
                        Assert.InRange(result.CardWidth, double.Epsilon, Math.Min(38 * scale, availableWidth) + 1e-9);
                        Assert.InRange(result.CardHeight, double.Epsilon, (53 * scale) + 1e-9);
                        Assert.Equal(38.0 / 53, result.CardWidth / result.CardHeight, 10);
                        Assert.InRange(result.Height, 0, availableHeight + 1e-9);
                        Assert.InRange(result.Step, 0, 13 * scale);
                        if (count > 1)
                        {
                            Assert.True(result.Step > 0);
                        }

                        for (var index = 0; index < count; index++)
                        {
                            var top = index * result.Step;
                            Assert.InRange(top, 0, availableHeight);
                            Assert.True(top + result.CardHeight <= availableHeight + 1e-9);
                        }
                    }
                }
            }
        }

        [Fact]
        public void EnoughRoomShouldPreserveTheOriginalCardsAndOverlap()
        {
            for (var count = 1; count <= 8; count++)
            {
                var result = VerticalCardBackGeometry.Fit(count, 80, 300, 1);
                Assert.Equal(38, result.CardWidth);
                Assert.Equal(53, result.CardHeight);
                Assert.Equal(count == 1 ? 0 : 13, result.Step);
                Assert.Equal(53 + ((count - 1) * 13), result.Height);
            }
        }

        [Fact]
        public void OverlapShouldCompressBeforeTheCardsShrink()
        {
            var compressed = VerticalCardBackGeometry.Fit(8, 80, 88, 1);
            Assert.Equal(38, compressed.CardWidth);
            Assert.Equal(53, compressed.CardHeight);
            Assert.Equal(5, compressed.Step);
            Assert.Equal(88, compressed.Height);

            var minimumPeek = VerticalCardBackGeometry.Fit(8, 80, 67, 1);
            Assert.Equal(53, minimumPeek.CardHeight);
            Assert.Equal(2, minimumPeek.Step);

            var shorter = VerticalCardBackGeometry.Fit(8, 80, 33.5, 1);
            Assert.Equal(19, shorter.CardWidth);
            Assert.Equal(26.5, shorter.CardHeight);
            Assert.Equal(1, shorter.Step);
            Assert.Equal(33.5, shorter.Height);
        }

        [Fact]
        public void OneCardShouldOnlyShrinkEnoughToFitAndNeverGrowBeyondNormalSize()
        {
            Assert.Equal(new VerticalCardBackGeometry(38, 53, 0, 53), VerticalCardBackGeometry.Fit(1, 80, 300, 1));
            Assert.Equal(new VerticalCardBackGeometry(19, 26.5, 0, 26.5), VerticalCardBackGeometry.Fit(1, 80, 26.5, 1));
        }

        [Fact]
        public void UnconstrainedMeasureShouldUseNaturalSizeAndInvalidOrEmptySpaceShouldBeSafe()
        {
            Assert.Equal(new VerticalCardBackGeometry(38, 53, 13, 144), VerticalCardBackGeometry.Fit(8, double.PositiveInfinity, double.PositiveInfinity, 1));
            Assert.Equal(default, VerticalCardBackGeometry.Fit(0, 80, 300, 1));
            Assert.Equal(default, VerticalCardBackGeometry.Fit(-1, 80, 300, 1));
            foreach (var invalid in new[] { 0.0, -1, double.NaN, double.NegativeInfinity })
            {
                Assert.Equal(default, VerticalCardBackGeometry.Fit(8, invalid, 100, 1));
                Assert.Equal(default, VerticalCardBackGeometry.Fit(8, 80, invalid, 1));
            }

            Assert.Equal(VerticalCardBackGeometry.Fit(8, 80, 100, 1), VerticalCardBackGeometry.Fit(8, 80, 100, double.NaN));
        }
    }
}
