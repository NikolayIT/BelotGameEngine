namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;
    using Xunit;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "xUnit member data.")]
    public class TricksManagerTests
    {
        public static IEnumerable<object[]> ValidPlayTricksData = new List<object[]>
        {
            // South North Are Nuts
            new object[]
            {
                new FakePlayer(BidType.AllTrumps),
                new FakePlayer(BidType.Pass),
                new FakePlayer(BidType.Pass),
                new FakePlayer(BidType.Pass),

                new List<CardCollection>
                {
                    // South
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                        Card.GetCard(CardSuit.Spade, CardType.Jack),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Jack),
                        Card.GetCard(CardSuit.Heart, CardType.Nine),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                        Card.GetCard(CardSuit.Spade, CardType.Ace),
                        Card.GetCard(CardSuit.Heart, CardType.Ace),
                    },

                    // East
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Heart, CardType.Seven),
                        Card.GetCard(CardSuit.Spade, CardType.Seven),
                        Card.GetCard(CardSuit.Spade, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Eight),
                        Card.GetCard(CardSuit.Spade, CardType.Eight),
                        Card.GetCard(CardSuit.Diamond, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.Ten),
                    },

                    // North
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                        Card.GetCard(CardSuit.Spade, CardType.Nine),
                        Card.GetCard(CardSuit.Club, CardType.Ace),
                        Card.GetCard(CardSuit.Club, CardType.Seven),
                        Card.GetCard(CardSuit.Club, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.Eight),
                    },

                    // West
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Seven),
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Heart, CardType.Queen),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Heart, CardType.Eight),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Queen),
                    },
                },
                32,
                0,
                PlayerPosition.South,
            },

            // East West Are Nuts
            new object[]
            {
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.NoTrumps),
                new FakePlayer(BidType.Pass),

                new List<CardCollection>
                {
                    // South
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Nine),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.Seven),
                        Card.GetCard(CardSuit.Diamond, CardType.Eight),
                        Card.GetCard(CardSuit.Spade, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.Eight),
                    },

                    // East
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.Ace),
                        Card.GetCard(CardSuit.Heart, CardType.Ten),
                        Card.GetCard(CardSuit.Spade, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.Ten),
                    },

                    // North
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Spade, CardType.Seven),
                        Card.GetCard(CardSuit.Spade, CardType.Eight),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                        Card.GetCard(CardSuit.Spade, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Jack),
                        Card.GetCard(CardSuit.Heart, CardType.Nine),
                        Card.GetCard(CardSuit.Heart, CardType.King),
                        Card.GetCard(CardSuit.Club, CardType.Seven),
                    },

                    // West
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Seven),
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Eight),
                        Card.GetCard(CardSuit.Heart, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.Ace),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                    },
                },
                0,
                32,
                PlayerPosition.East,
            },

            // South North Wins
            new object[]
            {
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Hearts),

                new List<CardCollection>
                {
                    // South
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Nine),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.Seven),
                        Card.GetCard(CardSuit.Diamond, CardType.Eight),
                        Card.GetCard(CardSuit.Spade, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.King),
                    },

                    // East
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                        Card.GetCard(CardSuit.Heart, CardType.Ten),
                        Card.GetCard(CardSuit.Spade, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.Ten),
                    },

                    // North
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Spade, CardType.Seven),
                        Card.GetCard(CardSuit.Spade, CardType.Eight),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                        Card.GetCard(CardSuit.Spade, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Jack),
                        Card.GetCard(CardSuit.Heart, CardType.Seven),
                        Card.GetCard(CardSuit.Heart, CardType.Eight),
                        Card.GetCard(CardSuit.Heart, CardType.Queen),
                    },

                    // West
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                        Card.GetCard(CardSuit.Heart, CardType.Nine),
                        Card.GetCard(CardSuit.Heart, CardType.Ace),
                        Card.GetCard(CardSuit.Club, CardType.Ace),
                        Card.GetCard(CardSuit.Club, CardType.Eight),
                        Card.GetCard(CardSuit.Diamond, CardType.Seven),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                    },
                },
                8,
                24,
                PlayerPosition.East,
            },

            // Wrong Available Announce
            new object[]
            {
                new FakePlayer(
                    new Announce(
                        AnnounceType.Belot,
                        Card.GetCard(CardSuit.Diamond, CardType.Jack)),
                    BidType.Pass,
                    BidType.Pass),
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Pass, BidType.Pass),
                new FakePlayer(BidType.Spades),

                new List<CardCollection>
                {
                    // South
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Seven),
                        Card.GetCard(CardSuit.Heart, CardType.Eight),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                        Card.GetCard(CardSuit.Heart, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Jack),
                        Card.GetCard(CardSuit.Heart, CardType.Queen),
                        Card.GetCard(CardSuit.Heart, CardType.King),
                        Card.GetCard(CardSuit.Heart, CardType.Ace),
                    },

                    // East
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.Seven),
                        Card.GetCard(CardSuit.Club, CardType.Eight),
                        Card.GetCard(CardSuit.Heart, CardType.Nine),
                        Card.GetCard(CardSuit.Spade, CardType.Ten),
                        Card.GetCard(CardSuit.Club, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Queen),
                        Card.GetCard(CardSuit.Diamond, CardType.King),
                        Card.GetCard(CardSuit.Club, CardType.Ace),
                    },

                    // North
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Spade, CardType.Seven),
                        Card.GetCard(CardSuit.Spade, CardType.Eight),
                        Card.GetCard(CardSuit.Diamond, CardType.Nine),
                        Card.GetCard(CardSuit.Club, CardType.Ten),
                        Card.GetCard(CardSuit.Spade, CardType.Jack),
                        Card.GetCard(CardSuit.Spade, CardType.Queen),
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                    },

                    // West
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Seven),
                        Card.GetCard(CardSuit.Diamond, CardType.Eight),
                        Card.GetCard(CardSuit.Spade, CardType.Nine),
                        Card.GetCard(CardSuit.Diamond, CardType.Ten),
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                        Card.GetCard(CardSuit.Diamond, CardType.Queen),
                        Card.GetCard(CardSuit.Spade, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Ace),
                    },
                },
                20,
                12,
                PlayerPosition.East,
            },
        };

        public static IEnumerable<object[]> InvalidPlayTricksData = new List<object[]>
        {
            // South North are nuts
            new object[]
            {
                new FakePlayer(BidType.AllTrumps),
                new FakePlayer(Card.GetCard(CardSuit.Diamond, CardType.Jack), BidType.Pass),
                new FakePlayer(BidType.Pass),
                new FakePlayer(BidType.Pass),

                new List<CardCollection>
                {
                    // South
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Jack),
                        Card.GetCard(CardSuit.Club, CardType.Nine),
                    },

                    // East
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Club, CardType.King),
                        Card.GetCard(CardSuit.Spade, CardType.Ten),
                    },

                    // North
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Diamond, CardType.Ace),
                        Card.GetCard(CardSuit.Heart, CardType.King),
                    },

                    // West
                    new CardCollection
                    {
                        Card.GetCard(CardSuit.Heart, CardType.Ten),
                        Card.GetCard(CardSuit.Diamond, CardType.Seven),
                    },
                },
            },
        };

        [Theory]
        [MemberData(nameof(ValidPlayTricksData))]
        public void PlayTricksShouldReturnValidSouthNorthAndEastWesPoints(
            FakePlayer southPlayer,
            FakePlayer eastPlayer,
            FakePlayer northPlayer,
            FakePlayer westPlayer,
            List<CardCollection> playerCards,
            int expectedSouthNorthTricksCount,
            int expectedEastWestTricksCount,
            PlayerPosition expectedLastTrickWinner)
        {
            var trickManager = new TricksManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            var contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);

            var currentContract = contractManager.GetContract(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                out var bids);

            trickManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                bids,
                currentContract,
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            Assert.Equal(expectedSouthNorthTricksCount, southNorthTricks.Count);
            Assert.Equal(expectedEastWestTricksCount, eastWestTricks.Count);
            Assert.Equal(expectedLastTrickWinner, lastTrickWinner);
        }

        [Theory]
        [MemberData(nameof(InvalidPlayTricksData))]
        public void PlayTricksShouldThrowExceptionWhenInvalidCardIsPassed(
            FakePlayer southPlayer,
            FakePlayer eastPlayer,
            FakePlayer northPlayer,
            FakePlayer westPlayer,
            List<CardCollection> playerCards)
        {
            var trickManager = new TricksManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            var contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);

            var currentContract = contractManager.GetContract(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                out var bids);

            Assert.Throws<BelotGameException>(() => trickManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                bids,
                currentContract,
                out _,
                out _,
                out _,
                out _));
        }
    }
}
