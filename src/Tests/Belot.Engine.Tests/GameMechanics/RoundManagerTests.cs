namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Engine.Cards;
    using Moq;
    using Xunit;

    public class RoundManagerTests
    {
        [Theory]
        [InlineData(BidType.AllTrumps, BidType.Pass, BidType.Pass, BidType.Pass, 26)]
        [InlineData(BidType.NoTrumps, BidType.Pass, BidType.Pass, BidType.Pass, 26)]
        [InlineData(BidType.Clubs, BidType.Pass, BidType.Pass, BidType.Pass, 16)]
        [InlineData(BidType.Pass, BidType.Pass, BidType.Pass, BidType.Pass, 0)]
        public void PlayTricksShouldReturnValidSouthNorthAndEastWesPoints(
            BidType southBidType,
            BidType eastBidType,
            BidType northBidType,
            BidType westBidType,
            int expectedTotalPoints)
        {
            var southPlayer = this.MockObject(southBidType);
            var eastPlayer = this.MockObject(eastBidType);
            var northPlayer = this.MockObject(northBidType);
            var westPlayer = this.MockObject(westBidType);

            var roundManager = new RoundManager(southPlayer, eastPlayer, northPlayer, westPlayer);

            var roundResult = roundManager.PlayRound(1, PlayerPosition.South, 0, 0, 0);

            var acutalTotalPoints = roundResult.EastWestPoints + roundResult.SouthNorthPoints;

            Assert.True(acutalTotalPoints >= expectedTotalPoints);
        }

        private IPlayer MockObject(BidType southBidType)
        {
            var player = new Mock<IPlayer>();

            player.Setup(x => x.GetBid(It.IsAny<PlayerGetBidContext>()))
                .Returns(southBidType);

            player.Setup(x => x.GetAnnounces(It.IsAny<PlayerGetAnnouncesContext>()))
                .Returns(() => new List<Announce>());

            player.Setup(x => x.PlayCard(It.IsAny<PlayerPlayCardContext>()))
                .Returns<PlayerPlayCardContext>(x => new PlayCardAction(x.AvailableCardsToPlay.RandomElement()));

            return player.Object;
        }
    }
}
