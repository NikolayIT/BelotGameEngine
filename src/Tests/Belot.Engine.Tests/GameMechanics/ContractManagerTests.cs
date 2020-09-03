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
    }
}
