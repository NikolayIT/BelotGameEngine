namespace Belot.Engine.Tests.Players
{
    using Belot.Engine.Players;

    using Xunit;

    public class PlayerPositionExtensionsTests
    {
        [Theory]
        [InlineData(PlayerPosition.South, PlayerPosition.East)]
        [InlineData(PlayerPosition.East, PlayerPosition.North)]
        [InlineData(PlayerPosition.North, PlayerPosition.West)]
        [InlineData(PlayerPosition.West, PlayerPosition.South)]
        public void NextShouldRotateSouthEastNorthWest(PlayerPosition position, PlayerPosition expectedNext)
        {
            Assert.Equal(expectedNext, position.Next());
        }

        [Fact]
        public void NextAppliedFourTimesShouldReturnTheStartingPosition()
        {
            foreach (var position in new[] { PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West })
            {
                Assert.Equal(position, position.Next().Next().Next().Next());
            }
        }

        [Theory]
        [InlineData(PlayerPosition.South, 0)]
        [InlineData(PlayerPosition.East, 1)]
        [InlineData(PlayerPosition.North, 2)]
        [InlineData(PlayerPosition.West, 3)]
        public void IndexShouldMapPositionsToArraySlots(PlayerPosition position, int expectedIndex)
        {
            Assert.Equal(expectedIndex, position.Index());
        }

        [Theory]
        [InlineData(PlayerPosition.South, PlayerPosition.North)]
        [InlineData(PlayerPosition.North, PlayerPosition.South)]
        [InlineData(PlayerPosition.East, PlayerPosition.West)]
        [InlineData(PlayerPosition.West, PlayerPosition.East)]
        public void GetTeammateShouldReturnThePlayerAcrossTheTable(PlayerPosition position, PlayerPosition expectedTeammate)
        {
            Assert.Equal(expectedTeammate, position.GetTeammate());
        }

        [Theory]
        [InlineData(PlayerPosition.South, PlayerPosition.South, true)]
        [InlineData(PlayerPosition.South, PlayerPosition.North, true)]
        [InlineData(PlayerPosition.South, PlayerPosition.East, false)]
        [InlineData(PlayerPosition.South, PlayerPosition.West, false)]
        [InlineData(PlayerPosition.East, PlayerPosition.South, false)]
        [InlineData(PlayerPosition.East, PlayerPosition.East, true)]
        [InlineData(PlayerPosition.East, PlayerPosition.North, false)]
        [InlineData(PlayerPosition.East, PlayerPosition.West, true)]
        [InlineData(PlayerPosition.North, PlayerPosition.South, true)]
        [InlineData(PlayerPosition.North, PlayerPosition.East, false)]
        [InlineData(PlayerPosition.North, PlayerPosition.North, true)]
        [InlineData(PlayerPosition.North, PlayerPosition.West, false)]
        [InlineData(PlayerPosition.West, PlayerPosition.South, false)]
        [InlineData(PlayerPosition.West, PlayerPosition.East, true)]
        [InlineData(PlayerPosition.West, PlayerPosition.North, false)]
        [InlineData(PlayerPosition.West, PlayerPosition.West, true)]
        public void IsInSameTeamWithShouldCoverAllOrderedPairs(
            PlayerPosition position,
            PlayerPosition otherPosition,
            bool expectedSameTeam)
        {
            Assert.Equal(expectedSameTeam, position.IsInSameTeamWith(otherPosition));
        }
    }
}
