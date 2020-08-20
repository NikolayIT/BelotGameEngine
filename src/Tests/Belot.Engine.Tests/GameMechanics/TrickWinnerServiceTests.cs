namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class TrickWinnerServiceTests
    {
        [Theory]
        [InlineData(CardType.Seven, CardSuit.Spade, CardType.Seven, CardSuit.Diamond, CardType.Eight, CardSuit.Diamond, CardType.Nine, CardSuit.Diamond, BidType.Spades, PlayerPosition.South)]
        [InlineData(CardType.King, CardSuit.Club, CardType.Jack, CardSuit.Diamond, CardType.Ace, CardSuit.Club, CardType.Queen, CardSuit.Club, BidType.Diamonds, PlayerPosition.East)]
        [InlineData(CardType.Ace, CardSuit.Heart, CardType.Ace, CardSuit.Diamond, CardType.Ace, CardSuit.Spade, CardType.Ace, CardSuit.Club, BidType.Spades, PlayerPosition.North)]
        [InlineData(CardType.Ten, CardSuit.Heart, CardType.King, CardSuit.Heart, CardType.Eight, CardSuit.Spade, CardType.Ace, CardSuit.Heart, BidType.Hearts, PlayerPosition.West)]
        [InlineData(CardType.Seven, CardSuit.Heart, CardType.Ten, CardSuit.Spade, CardType.Eight, CardSuit.Club, CardType.Ace, CardSuit.Diamond, BidType.Hearts, PlayerPosition.South)]
        [InlineData(CardType.Jack, CardSuit.Heart, CardType.Jack, CardSuit.Spade, CardType.Jack, CardSuit.Club, CardType.Jack, CardSuit.Diamond, BidType.Hearts, PlayerPosition.South)]
        [InlineData(CardType.Eight, CardSuit.Heart, CardType.Jack, CardSuit.Spade, CardType.King, CardSuit.Club, CardType.Eight, CardSuit.Diamond, BidType.Hearts, PlayerPosition.South)]
        [InlineData(CardType.Ace, CardSuit.Heart, CardType.Jack, CardSuit.Spade, CardType.King, CardSuit.Club, CardType.Eight, CardSuit.Diamond, BidType.Clubs, PlayerPosition.North)]
        [InlineData(CardType.Queen, CardSuit.Heart, CardType.Jack, CardSuit.Spade, CardType.King, CardSuit.Club, CardType.Ace, CardSuit.Club, BidType.Clubs, PlayerPosition.West)]
        [InlineData(CardType.Ace, CardSuit.Heart, CardType.Jack, CardSuit.Spade, CardType.King, CardSuit.Club, CardType.Ace, CardSuit.Spade, BidType.Clubs, PlayerPosition.North)]
        [InlineData(CardType.Seven, CardSuit.Spade, CardType.Eight, CardSuit.Spade, CardType.Queen, CardSuit.Spade, CardType.Nine, CardSuit.Spade, BidType.Spades, PlayerPosition.West)]
        [InlineData(CardType.Jack, CardSuit.Spade, CardType.Nine, CardSuit.Spade, CardType.Ace, CardSuit.Spade, CardType.Ten, CardSuit.Spade, BidType.Spades, PlayerPosition.South)]
        [InlineData(CardType.Jack, CardSuit.Spade, CardType.Ace, CardSuit.Heart, CardType.Ace, CardSuit.Diamond, CardType.Ace, CardSuit.Club, BidType.Spades, PlayerPosition.South)]
        [InlineData(CardType.Jack, CardSuit.Heart, CardType.Ace, CardSuit.Heart, CardType.Nine, CardSuit.Spade, CardType.Ace, CardSuit.Diamond, BidType.Spades, PlayerPosition.North)]
        [InlineData(CardType.Ace, CardSuit.Diamond, CardType.Jack, CardSuit.Diamond, CardType.Nine, CardSuit.Diamond, CardType.Ten, CardSuit.Diamond, BidType.Diamonds, PlayerPosition.East)]
        [InlineData(CardType.Ace, CardSuit.Club, CardType.Jack, CardSuit.Club, CardType.Nine, CardSuit.Diamond, CardType.Ten, CardSuit.Diamond, BidType.Diamonds, PlayerPosition.North)]
        [InlineData(CardType.Ace, CardSuit.Club, CardType.Ten, CardSuit.Club, CardType.King, CardSuit.Club, CardType.Queen, CardSuit.Club, BidType.NoTrumps, PlayerPosition.South)]
        [InlineData(CardType.Ace, CardSuit.Club, CardType.Ten, CardSuit.Diamond, CardType.King, CardSuit.Spade, CardType.Queen, CardSuit.Heart, BidType.NoTrumps, PlayerPosition.South)]
        [InlineData(CardType.Eight, CardSuit.Club, CardType.Ten, CardSuit.Diamond, CardType.King, CardSuit.Spade, CardType.Queen, CardSuit.Heart, BidType.NoTrumps, PlayerPosition.South)]
        [InlineData(CardType.Ten, CardSuit.Spade, CardType.King, CardSuit.Spade, CardType.Queen, CardSuit.Spade, CardType.Queen, CardSuit.Heart, BidType.NoTrumps, PlayerPosition.South)]
        [InlineData(CardType.Eight, CardSuit.Spade, CardType.King, CardSuit.Spade, CardType.Seven, CardSuit.Spade, CardType.Jack, CardSuit.Spade, BidType.NoTrumps, PlayerPosition.East)]
        [InlineData(CardType.King, CardSuit.Spade, CardType.King, CardSuit.Diamond, CardType.King, CardSuit.Heart, CardType.King, CardSuit.Club, BidType.NoTrumps, PlayerPosition.South)]
        [InlineData(CardType.Eight, CardSuit.Spade, CardType.King, CardSuit.Spade, CardType.Ace, CardSuit.Spade, CardType.Jack, CardSuit.Spade, BidType.NoTrumps, PlayerPosition.North)]
        [InlineData(CardType.Eight, CardSuit.Diamond, CardType.King, CardSuit.Diamond, CardType.Ace, CardSuit.Spade, CardType.Ace, CardSuit.Diamond, BidType.NoTrumps, PlayerPosition.West)]
        [InlineData(CardType.Jack, CardSuit.Club, CardType.Ten, CardSuit.Club, CardType.King, CardSuit.Club, CardType.Queen, CardSuit.Club, BidType.AllTrumps, PlayerPosition.South)]
        [InlineData(CardType.Jack, CardSuit.Club, CardType.Jack, CardSuit.Diamond, CardType.Jack, CardSuit.Heart, CardType.Jack, CardSuit.Spade, BidType.AllTrumps, PlayerPosition.South)]
        [InlineData(CardType.Eight, CardSuit.Club, CardType.Ace, CardSuit.Club, CardType.Nine, CardSuit.Club, CardType.Jack, CardSuit.Spade, BidType.AllTrumps, PlayerPosition.North)]
        [InlineData(CardType.Ten, CardSuit.Diamond, CardType.Ace, CardSuit.Diamond, CardType.Nine, CardSuit.Diamond, CardType.Jack, CardSuit.Diamond, BidType.AllTrumps, PlayerPosition.West)]
        [InlineData(CardType.Queen, CardSuit.Diamond, CardType.King, CardSuit.Diamond, CardType.Seven, CardSuit.Diamond, CardType.Eight, CardSuit.Diamond, BidType.AllTrumps, PlayerPosition.East)]
        [InlineData(CardType.Queen, CardSuit.Diamond, CardType.King, CardSuit.Spade, CardType.Seven, CardSuit.Heart, CardType.Eight, CardSuit.Club, BidType.AllTrumps, PlayerPosition.South)]
        public void GetWinnerShouldWorkCorrectly(
            CardType southCardType,
            CardSuit southCardSuit,
            CardType eastCardType,
            CardSuit eastCardSuit,
            CardType northCardType,
            CardSuit northCardSuit,
            CardType westCardType,
            CardSuit westCardSuit,
            BidType bidType,
            PlayerPosition winnerPosition)
        {
            var trickWinnerService = new TrickWinnerService();
            var bid = new Bid(PlayerPosition.East, bidType);

            var trickActions = new List<PlayCardAction>
            {
                new PlayCardAction(Card.GetCard(southCardSuit, southCardType))
                {
                    Player = PlayerPosition.South,
                },
                new PlayCardAction(Card.GetCard(eastCardSuit, eastCardType))
                {
                    Player = PlayerPosition.East,
                },
                new PlayCardAction(Card.GetCard(northCardSuit, northCardType))
                {
                    Player = PlayerPosition.North,
                },
                new PlayCardAction(Card.GetCard(westCardSuit, westCardType))
                {
                    Player = PlayerPosition.West,
                },
            };

            var winner = trickWinnerService.GetWinner(bid, trickActions);

            Assert.Equal(winnerPosition, winner);
        }
    }
}
