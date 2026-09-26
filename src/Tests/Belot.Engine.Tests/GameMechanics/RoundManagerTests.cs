namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

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

            // On a hanging (equal) round only the defenders' half is banked and the declarer's
            // half hangs, so the invariant must count the hanging points as well.
            var acutalTotalPoints =
                roundResult.EastWestPoints + roundResult.SouthNorthPoints + roundResult.HangingPoints;

            Assert.True(acutalTotalPoints >= expectedTotalPoints);
        }

        [Fact]
        public void AllPassRoundNotifiesEveryPlayerOfTheRoundEnd()
        {
            // A thrown-in deal is still a round: every player must get EndOfRound, carrying the
            // Pass contract and the hanging points that stay on the table.
            var players = new[]
            {
                new Mock<IPlayer>(), new Mock<IPlayer>(), new Mock<IPlayer>(), new Mock<IPlayer>(),
            };
            var results = new List<RoundResult>();
            foreach (var player in players)
            {
                player.Setup(x => x.GetBid(It.IsAny<PlayerGetBidContext>())).Returns(BidType.Pass);
                player.Setup(x => x.EndOfRound(It.IsAny<RoundResult>())).Callback<RoundResult>(results.Add);
            }

            var roundManager = new RoundManager(players[0].Object, players[1].Object, players[2].Object, players[3].Object);
            var roundResult = roundManager.PlayRound(3, PlayerPosition.West, 40, 50, 17);

            Assert.Equal(BidType.Pass, roundResult.Contract.Type);
            Assert.Equal(0, roundResult.SouthNorthPoints);
            Assert.Equal(0, roundResult.EastWestPoints);
            Assert.Equal(17, roundResult.HangingPoints);
            Assert.Equal(4, results.Count);
            Assert.All(results, x => Assert.Same(roundResult, x));
            foreach (var player in players)
            {
                player.Verify(x => x.EndOfRound(It.IsAny<RoundResult>()), Times.Once);
                player.Verify(x => x.PlayCard(It.IsAny<PlayerPlayCardContext>()), Times.Never);
            }
        }

        [Theory]
        [InlineData(BidType.Clubs)]
        [InlineData(BidType.NoTrumps)]
        [InlineData(BidType.AllTrumps)]
        public void PlayedRoundNotifiesEveryPlayerOnceWithTheReturnedResult(BidType contract)
        {
            var players = new[]
            {
                this.MockPlayer(contract), this.MockPlayer(BidType.Pass), this.MockPlayer(BidType.Pass), this.MockPlayer(BidType.Pass),
            };
            var results = new List<RoundResult>();
            foreach (var player in players)
            {
                player.Setup(x => x.EndOfRound(It.IsAny<RoundResult>())).Callback<RoundResult>(results.Add);
            }

            var roundManager = new RoundManager(players[0].Object, players[1].Object, players[2].Object, players[3].Object);
            var roundResult = roundManager.PlayRound(1, PlayerPosition.South, 0, 0, 0);

            Assert.Equal(contract, roundResult.Contract.Type);
            Assert.Equal(PlayerPosition.South, roundResult.Contract.Player);
            Assert.Equal(4, results.Count);
            Assert.All(results, x => Assert.Same(roundResult, x));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void BiddingSeesFiveCardsAndThePlayUsesTheWholeDealOfEight(int repetition)
        {
            // Deal 5 cards, bid, then deal 3 more: bidders hold exactly 5 cards, every card-play
            // decision sees the 8 dealt cards minus those already played, and the round plays
            // each player's own 8 cards - the 32 cards exactly once.
            Assert.True(repetition > 0);
            var players = new[]
            {
                this.MockPlayer(BidType.Hearts), this.MockPlayer(BidType.Pass), this.MockPlayer(BidType.Pass), this.MockPlayer(BidType.Pass),
            };
            var handsAtBidding = new CardCollection[4];
            var playedCards = new[] { new CardCollection(), new CardCollection(), new CardCollection(), new CardCollection() };
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                players[i].Setup(x => x.GetBid(It.IsAny<PlayerGetBidContext>()))
                    .Callback<PlayerGetBidContext>(c => handsAtBidding[index] ??= new CardCollection(c.MyCards))
                    .Returns<PlayerGetBidContext>(c => c.AvailableBids.HasFlag(BidType.Hearts) && index == 0 ? BidType.Hearts : BidType.Pass);
                players[i].Setup(x => x.PlayCard(It.IsAny<PlayerPlayCardContext>()))
                    .Callback<PlayerPlayCardContext>(c => Assert.Equal(9 - c.CurrentTrickNumber, c.MyCards.Count))
                    .Returns<PlayerPlayCardContext>(c => new PlayCardAction(c.AvailableCardsToPlay.RandomElement()));
            }

            players[0].Setup(x => x.EndOfTrick(It.IsAny<IEnumerable<PlayCardAction>>()))
                .Callback<IEnumerable<PlayCardAction>>(trick =>
                {
                    foreach (var action in trick)
                    {
                        Assert.DoesNotContain(action.Card, playedCards[action.Player.Index()]);
                        playedCards[action.Player.Index()].Add(action.Card);
                    }
                });

            var roundManager = new RoundManager(players[0].Object, players[1].Object, players[2].Object, players[3].Object);
            roundManager.PlayRound(1, PlayerPosition.South, 0, 0, 0);

            var allPlayed = new CardCollection();
            for (var i = 0; i < 4; i++)
            {
                Assert.Equal(5, handsAtBidding[i].Count);
                Assert.Equal(8, playedCards[i].Count);
                Assert.All(handsAtBidding[i], card => Assert.Contains(card, playedCards[i]));
                foreach (var card in playedCards[i])
                {
                    Assert.DoesNotContain(card, allPlayed);
                    allPlayed.Add(card);
                }
            }

            Assert.Equal(32, allPlayed.Count);
        }

        private Mock<IPlayer> MockPlayer(BidType bidType)
        {
            var player = new Mock<IPlayer>();
            player.Setup(x => x.GetBid(It.IsAny<PlayerGetBidContext>()))
                .Returns<PlayerGetBidContext>(c => c.AvailableBids.HasFlag(bidType) ? bidType : BidType.Pass);
            player.Setup(x => x.GetAnnounces(It.IsAny<PlayerGetAnnouncesContext>()))
                .Returns(() => new List<Announce>());
            player.Setup(x => x.PlayCard(It.IsAny<PlayerPlayCardContext>()))
                .Returns<PlayerPlayCardContext>(x => new PlayCardAction(x.AvailableCardsToPlay.RandomElement()));
            return player;
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
