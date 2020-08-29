namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "xUnit member data.")]
    public class ValidCardsServiceTests
    {
        public static IEnumerable<object[]> NoCardsPlayedTests = new List<object[]>
        {
             new object[]
                 {
                     BidType.NoTrumps,
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                 },
             new object[]
                 {
                     BidType.AllTrumps,
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                 },
        };

        public static IEnumerable<object[]> OneOrMoreCardsPlayedTests = new List<object[]>
        {
             new object[]
                 {
                     // The player should play higher card in all trumps
                     BidType.AllTrumps,
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack) },
                 },
             new object[]
                {
                    // The player should play highest card in all trumps
                    BidType.AllTrumps,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Eight),  Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Queen) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.King) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack),  Card.GetCard(CardSuit.Diamond, CardType.King) },
                },
             new object[]
                {
                    // The teammate played trump but the competition team have higher trump and the player is obligated to play lowest card suit
                    BidType.AllTrumps,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Diamond, CardType.Queen) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven) },
                },
             new object[]
                {
                    // The teammate played trump but the rival team have higher card and the player is obligated to play lowest card
                    BidType.NoTrumps,
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Spade, CardType.King) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten) },
                },
             new object[]
                {
                    // The player should play higher card in no trumps
                    BidType.NoTrumps,
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.King) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten) },
                },
             new object[]
                {
                    // The teammate played trump so the player should be able to play any card
                    BidType.Hearts,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                },
             new object[]
                {
                    // The teammate played trump but the player is obligated to play the first card suit
                    BidType.Hearts,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Jack) },
                },
             new object[]
                {
                    // The teammate played trump but the player is obligated to play the highest card suit
                    BidType.Diamonds,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Spade, CardType.Nine) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack) },
                },
             new object[]
                {
                    // The teammate didn't played trump but the player is obligated to play the first card suit
                    BidType.Clubs,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Spade, CardType.Ace) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven) },
                },
             new object[]
                {
                    // The teammate didn't played trump but the player is obligated to play any card
                    BidType.Spades,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Diamond, CardType.King) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                },
             new object[]
                {
                    // The teammate played lower trump but the player is obligated to play highest trump card
                    BidType.Diamonds,
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                },
             new object[]
                {
                    // The teammate played trump but the rival team have higher trump and the player is obligated to play any card
                    BidType.Hearts,
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Queen), Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Club, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                    new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Club, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                },
             new object[]
             {
                 // ---
                 BidType.Hearts,
                 new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack) },
                 new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack) },
             },
             new object[]
             {
                 // ---
                 BidType.Hearts,
                 new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.King) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.King) },
             },
        };

        [Theory]
        [MemberData(nameof(NoCardsPlayedTests))]
        public void GetValidCardsShouldWorkCorrectlyWhenNoCardsArePlayed(
            BidType bidType,
            CardCollection playerCards,
            CardCollection expectedCards)
        {
            var validCardsService = new ValidCardsService();
            var validCards = validCardsService.GetValidCards(playerCards, bidType, new List<PlayCardAction>());
            Assert.Equal(expectedCards, validCards);
        }

        [Theory]
        [MemberData(nameof(OneOrMoreCardsPlayedTests))]
        public void GetValidCardsShouldWorkCorrectlyWhenOneOrMoreCardsArePlayed(
            BidType bidType,
            CardCollection playedCards,
            CardCollection playerCards,
            CardCollection expectedCards)
        {
            var validCardsService = new ValidCardsService();
            var currentTrickActions = playedCards.Select(card => new PlayCardAction(card)).ToList();
            var validCards = validCardsService.GetValidCards(playerCards, bidType, currentTrickActions);
            Assert.Equal(expectedCards, validCards);
        }
    }
}
