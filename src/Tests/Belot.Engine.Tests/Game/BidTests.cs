namespace Belot.Engine.Tests.Game
{
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using Xunit;

    public class BidTests
    {
        [Fact]
        public void ConstructorShouldSetCorrectPropertyValues()
        {
            var bid = new Bid(PlayerPosition.East, BidType.Hearts | BidType.Double);
            Assert.Equal(PlayerPosition.East, bid.Player);
            Assert.True(bid.Type.HasFlag(BidType.Hearts));
            Assert.True(bid.Type.HasFlag(BidType.Double));
        }

        [Fact]
        public void ToStringShouldWorkCorrectly()
        {
            var bid = new Bid(PlayerPosition.East, BidType.Diamonds | BidType.ReDouble);
            var toStringValue = bid.ToString();
            Assert.Contains("East", toStringValue);
            Assert.Contains("Diamonds", toStringValue);
            Assert.Contains("ReDouble", toStringValue);
        }
    }
}
