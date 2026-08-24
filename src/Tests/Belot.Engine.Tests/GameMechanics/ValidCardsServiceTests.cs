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
                    // The teammate played trump so the player should be able to play any card.
                    // Trick (in bitmask order): 9♦ led by an opponent, the teammate ruffs with 8♥,
                    // the other opponent discards 7♠ - the teammate holds the trick.
                    BidType.Hearts,
                    new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Spade, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Eight), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                    new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Eight), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Seven) },
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
                 // --The rival team played trump, but the player has higher trump card
                 BidType.Hearts,
                 new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack) },
                 new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack) },
             },
             new object[]
             {
                 // --The rival team played trump, but the player doesn't have higher trump card
                 BidType.Hearts,
                 new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Queen), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.King) },
                 new CardCollection { Card.GetCard(CardSuit.Club, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.King) },
             },
        };

        // The full obligation matrix (etc/Rules.md §3; hit.bg §3.1-3.3). Tricks here are ordered
        // Card arrays, NOT CardCollections - a CardCollection enumerates in bitmask order and
        // silently reorders a fixture (that corrupted one of the older cases in this file).
        public static IEnumerable<object[]> ObligationMatrixTests = new List<object[]>
        {
            // --- All Trumps: following must beat the best card so far when able (rule 3.3),
            //     even when the partner holds the trick; else any card of the led suit; void => any.
            new object[]
            {
                "AT: single higher card is forced",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ten) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack) },
            },
            new object[]
            {
                "AT: all higher cards are offered",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.King) },
            },
            new object[]
            {
                "AT: only lower cards => any card of the led suit",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Queen), Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Diamond, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Queen), Card.GetCard(CardSuit.Spade, CardType.King) },
            },
            new object[]
            {
                "AT third seat: must beat the best card so far, not the led one",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.King) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Eight), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Nine) },
            },
            new object[]
            {
                "AT: raise duty is NOT waived when the partner is winning",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack) },
            },
            new object[]
            {
                "AT fourth seat: must beat the best of three",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Eight), Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Queen) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack), Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Jack) },
            },
            new object[]
            {
                "AT: void in the led suit => any card",
                BidType.AllTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Jack) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Jack) },
            },

            // --- No Trumps: following the suit is the only duty (rule 3.2).
            new object[]
            {
                "NT: any card of the led suit, even all lower",
                BidType.NoTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Eight), Card.GetCard(CardSuit.Heart, CardType.King) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Eight) },
            },
            new object[]
            {
                "NT: void in the led suit => any card",
                BidType.NoTrumps,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Eight), Card.GetCard(CardSuit.Club, CardType.Queen) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Eight), Card.GetCard(CardSuit.Club, CardType.Queen) },
            },

            // --- Suit contract, trump led: must overtrump when able, even over the partner (rule 3.1).
            new object[]
            {
                "Trump led: must overtrump",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Heart, CardType.Ten) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Queen), Card.GetCard(CardSuit.Spade, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack) },
            },
            new object[]
            {
                "Trump led: only lower trumps => any trump",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Heart, CardType.Nine) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Queen), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Queen), Card.GetCard(CardSuit.Heart, CardType.Eight) },
            },
            new object[]
            {
                "Trump led: overtrump duty is NOT waived when the partner is winning",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Heart, CardType.Ten), Card.GetCard(CardSuit.Heart, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Club, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Jack) },
            },
            new object[]
            {
                "Trump led: void in trumps => any card",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Heart, CardType.Jack) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ace), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Club, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ace), Card.GetCard(CardSuit.Diamond, CardType.King), Card.GetCard(CardSuit.Club, CardType.Seven) },
            },

            // --- Suit contract, plain suit led (rule 3.1): follow without a rank duty; when void,
            //     trump only if an opponent holds the trick; over-ruff when able; no under-ruff duty.
            new object[]
            {
                "Plain led: must follow, no duty to beat",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.King) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven) },
            },
            new object[]
            {
                "Plain led: void with no trumps => any card",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Club, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Queen) },
                new CardCollection { Card.GetCard(CardSuit.Diamond, CardType.Seven), Card.GetCard(CardSuit.Club, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Queen) },
            },
            new object[]
            {
                "Second seat void: the leader is an opponent => must trump",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ten) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven) },
            },
            new object[]
            {
                "Third seat void: partner led and is winning => free discard",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Seven) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
            },
            new object[]
            {
                "Third seat void: partner led but the opponent overtook => must trump",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Ten), Card.GetCard(CardSuit.Spade, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Diamond, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven) },
            },
            new object[]
            {
                "Fourth seat void, nobody trumped, opponent winning => every trump is legal",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Eight) },
            },
            new object[]
            {
                "Fourth seat void: partner's ruff holds the trick => free discard",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Spade, CardType.Nine) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Eight), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
            },
            new object[]
            {
                "Fourth seat void: partner's ruff was over-ruffed => must over-over-ruff",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.Ten) },
            },
            new object[]
            {
                "Fourth seat void: cannot over-ruff => any card, no under-ruff duty",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.King), Card.GetCard(CardSuit.Heart, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Nine), Card.GetCard(CardSuit.Heart, CardType.King), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
            },
            new object[]
            {
                "Fourth seat void: low ruff under a high led nine must still be over-ruffed",
                BidType.Hearts,
                new[] { Card.GetCard(CardSuit.Spade, CardType.Nine), Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Eight) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Ten), Card.GetCard(CardSuit.Diamond, CardType.Ace) },
                new CardCollection { Card.GetCard(CardSuit.Heart, CardType.Ten) },
            },
            new object[]
            {
                "Leader plays first: the whole hand is legal",
                BidType.Hearts,
                new Card[0],
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Ten) },
                new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Seven), Card.GetCard(CardSuit.Heart, CardType.Jack), Card.GetCard(CardSuit.Diamond, CardType.Ace), Card.GetCard(CardSuit.Club, CardType.Ten) },
            },
        };

        [Theory]
        [MemberData(nameof(ObligationMatrixTests))]
        public void GetValidCardsShouldEnforceTheObligationMatrix(
            string caseName,
            BidType bidType,
            Card[] playedCards,
            CardCollection playerCards,
            CardCollection expectedCards)
        {
            Assert.False(string.IsNullOrEmpty(caseName));
            var validCardsService = new ValidCardsService();
            var currentTrickActions = playedCards.Select(card => new PlayCardAction(card)).ToList();
            var validCards = validCardsService.GetValidCards(playerCards, bidType, currentTrickActions);
            Assert.Equal(expectedCards, validCards);
        }

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
