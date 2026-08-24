namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Compression;
    using System.Runtime.Serialization.Json;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Moq;
    using Xunit;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "xUnit member data.")]
    public class ContractManagerTests
    {
        public static IEnumerable<object[]> BidTypesTestData = new List<object[]>
        {
            new object[]
            {
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.Pass,
            },
            new object[]
            {
                new[] { BidType.Diamonds },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.Diamonds,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.NoTrumps },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.NoTrumps,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.AllTrumps },
                BidType.AllTrumps,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Hearts },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.Hearts,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.Pass },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Spades },
                new[] { BidType.Pass },
                BidType.Spades,
            },
            new object[]
            {
                new[] { BidType.Diamonds, BidType.Pass },
                new[] { BidType.NoTrumps },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.NoTrumps,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.NoTrumps },
                new[] { BidType.AllTrumps },
                BidType.AllTrumps,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.Pass },
                new[] { BidType.Diamonds },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.Diamonds,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.Pass },
                new[] { BidType.Diamonds, BidType.Pass },
                new[] { BidType.Hearts },
                new[] { BidType.Pass },
                BidType.Hearts,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.Pass },
                new[] { BidType.Diamonds, BidType.Pass },
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Spades },
                BidType.Spades,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.NoTrumps },
                new[] { BidType.Diamonds, BidType.Pass },
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Spades, BidType.Pass },
                BidType.NoTrumps,
            },
            new object[]
            {
                new[] { BidType.Clubs, BidType.NoTrumps, BidType.Pass },
                new[] { BidType.Diamonds, BidType.AllTrumps },
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Spades, BidType.Pass },
                BidType.AllTrumps,
            },

            // Double/Re-Double Bid Types
            new object[]
            {
                new[] { BidType.Clubs, BidType.NoTrumps, BidType.Pass },
                new[] { BidType.Diamonds, BidType.AllTrumps, BidType.Pass },
                new[] { BidType.Hearts, BidType.Double },
                new[] { BidType.Spades, BidType.Pass },
                BidType.AllTrumps | BidType.Double,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Double },
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Spades, BidType.Pass },
                new[] { BidType.NoTrumps, BidType.Pass },
                BidType.NoTrumps | BidType.Double,
            },
            new object[]
            {
                new[] { BidType.Hearts, BidType.ReDouble },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Pass, BidType.Pass },
                new[] { BidType.Double, BidType.Pass },
                BidType.Hearts | BidType.ReDouble,
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.Double, BidType.Pass },
                new[] { BidType.Hearts, BidType.Pass, BidType.Pass },
                new[] { BidType.Spades, BidType.Pass, BidType.Pass },
                new[] { BidType.NoTrumps, BidType.ReDouble },
                BidType.NoTrumps | BidType.ReDouble,
            },
            new object[]
            {
                // Jump bid straight to the top of the ladder.
                new[] { BidType.Clubs, BidType.Pass },
                new[] { BidType.AllTrumps },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                BidType.AllTrumps,
            },
            new object[]
            {
                // A higher bid over a doubled contract clears the Double flag.
                new[] { BidType.Hearts, BidType.Pass, BidType.Pass },
                new[] { BidType.Double, BidType.Pass },
                new[] { BidType.Spades },
                new[] { BidType.Pass },
                BidType.Spades,
            },
            new object[]
            {
                // A higher bid after a redouble clears both flags.
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Double, BidType.Pass },
                new[] { BidType.ReDouble, BidType.Pass },
                new[] { BidType.Spades },
                BidType.Spades,
            },
            new object[]
            {
                // The re-raised contract can be doubled again by the other team.
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Double, BidType.Pass },
                new[] { BidType.Spades, BidType.Pass },
                new[] { BidType.Double },
                BidType.Spades | BidType.Double,
            },
        };

        public static IEnumerable<object[]> InvalidBidTypesData = new List<object[]>
        {
            new object[]
            {
                new[] { BidType.Hearts },
                new[] { BidType.Pass },
                new[] { BidType.Clubs },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Double },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.ReDouble },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Spades },
                new[] { BidType.Pass },
                new[] { BidType.Diamonds },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Pass },
                new[] { BidType.NoTrumps },
                new[] { BidType.Pass },
                new[] { BidType.Diamonds },
            },
            new object[]
            {
                new[] { BidType.Pass },
                new[] { BidType.AllTrumps },
                new[] { BidType.NoTrumps },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Pass, BidType.NoTrumps },
                new[] { BidType.Spades },
                new[] { BidType.Pass },
                new[] { BidType.NoTrumps },
            },
            new object[]
            {
                new[] { BidType.Spades },
                new[] { BidType.Pass },
                new[] { BidType.Double },
                new[] { BidType.Pass },
            },
            new object[]
            {
                new[] { BidType.Spades, BidType.Diamonds },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
                new[] { BidType.AllTrumps },
            },
            new object[]
            {
                new[] { BidType.Pass },
                new[] { BidType.Hearts | BidType.Double },
                new[] { BidType.Pass },
                new[] { BidType.Hearts },
            },
            new object[]
            {
                // Doubling the own team's contract.
                new[] { BidType.Hearts },
                new[] { BidType.Pass },
                new[] { BidType.Double },
                new[] { BidType.Pass },
            },
            new object[]
            {
                // Redouble without a double on the table.
                new[] { BidType.Hearts },
                new[] { BidType.ReDouble },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
            },
            new object[]
            {
                // Double after a redouble.
                new[] { BidType.Hearts, BidType.Pass },
                new[] { BidType.Double, BidType.Pass },
                new[] { BidType.ReDouble },
                new[] { BidType.Double },
            },
            new object[]
            {
                // A second double by the doubler's teammate.
                new[] { BidType.Hearts },
                new[] { BidType.Double },
                new[] { BidType.Pass },
                new[] { BidType.Double },
            },
            new object[]
            {
                // Repeating the current contract level.
                new[] { BidType.Hearts },
                new[] { BidType.Hearts },
                new[] { BidType.Pass },
                new[] { BidType.Pass },
            },
        };

        [Theory]
        [MemberData(nameof(BidTypesTestData))]
        public void GetContractShouldReturnTheValidBid(
            BidType[] southBidTypes,
            BidType[] eastBidTypes,
            BidType[] northBidTypes,
            BidType[] westBidTypes,
            BidType winnerBidType)
        {
            var southPlayer = new FakePlayer(southBidTypes);
            var eastPlayer = new FakePlayer(eastBidTypes);
            var northPlayer = new FakePlayer(northBidTypes);
            var westPlayer = new FakePlayer(westBidTypes);

            var contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);

            var playerCards = new List<CardCollection>
            {
                new CardCollection(),
                new CardCollection(),
                new CardCollection(),
                new CardCollection(),
            };

            var contract = contractManager.GetContract(1, PlayerPosition.South, 0, 0, playerCards, out _);

            Assert.Equal(winnerBidType, contract.Type);
        }

        [Fact]
        public void PlayersWithOnlyPassAvailableAreNotConsulted()
        {
            // After the partner's All Trumps there is nothing the partner's side can say, so the
            // engine auto-passes for them; the opponents can still double, so they ARE asked.
            var south = BidMock(BidType.AllTrumps);
            var east = BidMock(BidType.Pass);
            var north = BidMock(BidType.Pass);
            var west = BidMock(BidType.Pass);

            var contractManager = new ContractManager(south.Object, east.Object, north.Object, west.Object);
            var contract = contractManager.GetContract(1, PlayerPosition.South, 0, 0, EmptyHands(), out _);

            Assert.Equal(BidType.AllTrumps, contract.Type);
            south.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
            east.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
            north.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Never);
            west.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
        }

        [Fact]
        public void AllPassAsksEachPlayerExactlyOnce()
        {
            var south = BidMock(BidType.Pass);
            var east = BidMock(BidType.Pass);
            var north = BidMock(BidType.Pass);
            var west = BidMock(BidType.Pass);

            var contractManager = new ContractManager(south.Object, east.Object, north.Object, west.Object);
            var contract = contractManager.GetContract(1, PlayerPosition.East, 0, 0, EmptyHands(), out var bids);

            Assert.Equal(BidType.Pass, contract.Type);
            Assert.Equal(4, bids.Count);
            south.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
            east.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
            north.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
            west.Verify(x => x.GetBid(It.IsAny<PlayerGetBidContext>()), Times.Once);
        }

        [Fact]
        public void BidsOutListRecordsTheWholeDialogue()
        {
            var contractManager = new ContractManager(
                new FakePlayer(BidType.Hearts, BidType.Pass),
                new FakePlayer(BidType.Double, BidType.Pass),
                new FakePlayer(BidType.Spades),
                new FakePlayer(BidType.Pass));

            var contract = contractManager.GetContract(1, PlayerPosition.South, 0, 0, EmptyHands(), out var bids);

            Assert.Equal(BidType.Spades, contract.Type);
            Assert.Equal(PlayerPosition.North, contract.Player);

            var expected = new[]
            {
                (PlayerPosition.South, BidType.Hearts),
                (PlayerPosition.East, BidType.Double),
                (PlayerPosition.North, BidType.Spades),
                (PlayerPosition.West, BidType.Pass),
                (PlayerPosition.South, BidType.Pass),
                (PlayerPosition.East, BidType.Pass),
            };
            Assert.Equal(expected.Length, bids.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Item1, bids[i].Player);
                Assert.Equal(expected[i].Item2, bids[i].Type);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidBidTypesData))]
        public void GetContractShouldThrowExceptionWhenInvalidBidTypeIsRaised(
            BidType[] southBidTypes,
            BidType[] eastBidTypes,
            BidType[] northBidTypes,
            BidType[] westBidTypes)
        {
            var southPlayer = new FakePlayer(southBidTypes);
            var eastPlayer = new FakePlayer(eastBidTypes);
            var northPlayer = new FakePlayer(northBidTypes);
            var westPlayer = new FakePlayer(westBidTypes);

            var contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);

            var playerCards = new List<CardCollection>
            {
                new CardCollection(),
                new CardCollection(),
                new CardCollection(),
                new CardCollection(),
            };

            Assert.Throws<BelotGameException>(
                () => contractManager.GetContract(1, PlayerPosition.South, 0, 0, playerCards, out _));
        }

        private static Mock<IPlayer> BidMock(BidType bidType)
        {
            var player = new Mock<IPlayer>();
            player.Setup(x => x.GetBid(It.IsAny<PlayerGetBidContext>())).Returns(bidType);
            return player;
        }

        private static List<CardCollection> EmptyHands() => new List<CardCollection>
        {
            new CardCollection(),
            new CardCollection(),
            new CardCollection(),
            new CardCollection(),
        };
    }
}
