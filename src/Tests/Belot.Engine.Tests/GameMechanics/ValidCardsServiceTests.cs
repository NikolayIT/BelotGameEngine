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
                     BidType.AllTrumps,
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                     new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Jack) },
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
