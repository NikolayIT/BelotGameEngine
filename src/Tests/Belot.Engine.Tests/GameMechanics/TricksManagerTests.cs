namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
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

        [Fact]
        public void NoTrumpsRoundNeverAsksForAnnouncesAndDeniesBelote()
        {
            // Suit-partitioned hands: South holds all spades and wins every trick; West plays
            // the heart king and queen with the default announce-belote flag, which must be
            // rejected in No Trumps. Nobody may even be asked for combinations.
            var south = new ScriptedPlayer(
                Card.GetCard(CardSuit.Spade, CardType.Seven),
                Card.GetCard(CardSuit.Spade, CardType.Eight),
                Card.GetCard(CardSuit.Spade, CardType.Nine),
                Card.GetCard(CardSuit.Spade, CardType.Ten),
                Card.GetCard(CardSuit.Spade, CardType.Jack),
                Card.GetCard(CardSuit.Spade, CardType.Queen),
                Card.GetCard(CardSuit.Spade, CardType.King));
            var east = SuitedPlayer(CardSuit.Club);
            var north = SuitedPlayer(CardSuit.Diamond);
            var west = SuitedPlayer(CardSuit.Heart);

            var playerCards = new List<CardCollection>
            {
                AllOfSuit(CardSuit.Spade),
                AllOfSuit(CardSuit.Club),
                AllOfSuit(CardSuit.Diamond),
                AllOfSuit(CardSuit.Heart),
            };

            var tricksManager = new TricksManager(south, east, north, west);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                new List<Bid>(),
                new Bid(PlayerPosition.South, BidType.NoTrumps),
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            Assert.Empty(announces);
            Assert.Equal(0, south.AnnounceAsksCount);
            Assert.Equal(0, east.AnnounceAsksCount);
            Assert.Equal(0, north.AnnounceAsksCount);
            Assert.Equal(0, west.AnnounceAsksCount);
            Assert.Equal(32, southNorthTricks.Count);
            Assert.Equal(0, eastWestTricks.Count);
            Assert.Equal(PlayerPosition.South, lastTrickWinner);
        }

        [Fact]
        public void ForcedSingleCardsAreAutoPlayedWithoutAskingThePlayer()
        {
            // The belote-round script from the rules audit: South's 8-of-hearts follow in trick 2
            // and West's queen-of-spades follow in trick 4 are the only legal cards, so the
            // engine must play them without consuming the players' scripts, and the whole eighth
            // trick is auto-played. Each player is told about all 8 tricks.
            var south = new ScriptedPlayer(
                Card.GetCard(CardSuit.Heart, CardType.Seven),
                Card.GetCard(CardSuit.Spade, CardType.Ace),
                Card.GetCard(CardSuit.Spade, CardType.Jack),
                Card.GetCard(CardSuit.Spade, CardType.Ten),
                Card.GetCard(CardSuit.Spade, CardType.Nine),
                Card.GetCard(CardSuit.Spade, CardType.Eight));
            var east = SuitedPlayer(CardSuit.Club);
            var north = SuitedPlayer(CardSuit.Diamond);
            var west = new ScriptedPlayer(
                Card.GetCard(CardSuit.Heart, CardType.Ace),
                Card.GetCard(CardSuit.Heart, CardType.Nine),
                Card.GetCard(CardSuit.Spade, CardType.King),
                Card.GetCard(CardSuit.Heart, CardType.Ten),
                Card.GetCard(CardSuit.Heart, CardType.Jack),
                Card.GetCard(CardSuit.Heart, CardType.Queen));

            var playerCards = new List<CardCollection>
            {
                new CardCollection
                {
                    Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Eight),
                    Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Ten),
                    Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.Ace),
                    Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Eight),
                },
                AllOfSuit(CardSuit.Club),
                AllOfSuit(CardSuit.Diamond),
                new CardCollection
                {
                    Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Spade, CardType.Queen),
                    Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Ten),
                    Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Queen),
                    Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace),
                },
            };

            var tricksManager = new TricksManager(south, east, north, west);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                new List<Bid>(),
                new Bid(PlayerPosition.South, BidType.Spades),
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            // The forced queen of spades no longer holds the king, so exactly one belote exists.
            Assert.Equal(1, announces.Count(x => x.Type == AnnounceType.Belot));

            Assert.Equal(6, south.CardAsksCount);
            Assert.Equal(7, east.CardAsksCount);
            Assert.Equal(7, north.CardAsksCount);
            Assert.Equal(6, west.CardAsksCount);

            Assert.Equal(8, south.EndOfTrickCalls);
            Assert.Equal(8, east.EndOfTrickCalls);
            Assert.Equal(8, north.EndOfTrickCalls);
            Assert.Equal(8, west.EndOfTrickCalls);

            Assert.Equal(24, southNorthTricks.Count);
            Assert.Equal(8, eastWestTricks.Count);
            Assert.Equal(PlayerPosition.South, lastTrickWinner);
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

        private static ScriptedPlayer SuitedPlayer(CardSuit suit) =>
            new ScriptedPlayer(
                Card.GetCard(suit, CardType.Seven),
                Card.GetCard(suit, CardType.Eight),
                Card.GetCard(suit, CardType.Nine),
                Card.GetCard(suit, CardType.Ten),
                Card.GetCard(suit, CardType.Jack),
                Card.GetCard(suit, CardType.Queen),
                Card.GetCard(suit, CardType.King));

        private static CardCollection AllOfSuit(CardSuit suit)
        {
            var cards = new CardCollection();
            foreach (var type in Card.AllTypes)
            {
                cards.Add(Card.GetCard(suit, type));
            }

            return cards;
        }
    }
}
