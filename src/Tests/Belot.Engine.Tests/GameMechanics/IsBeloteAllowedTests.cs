namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class IsBeloteAllowedTests
    {
        [Fact]
        public void AllTrumpsPlayingFirst()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Heart, CardType.Queen),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Heart, CardType.Ace),
                           };

            var trick = new List<PlayCardAction>();

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Heart, CardType.Queen)));

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Heart, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Heart, CardType.Ace)));
        }

        [Fact]
        public void AllTrumpsPlayingSecond()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                           };

            var trick = new List<PlayCardAction> { new PlayCardAction(Card.GetCard(CardSuit.Club, CardType.Jack)) };

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Spade, CardType.Queen)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Spade, CardType.King)));
        }

        [Fact]
        public void TrumpsPlayingFirst()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                           };

            var trick = new List<PlayCardAction>();

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));
        }

        [Fact]
        public void TrumpsPlayingSecond()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                           };

            var trick = new List<PlayCardAction> { new PlayCardAction(Card.GetCard(CardSuit.Club, CardType.Jack)) };

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.True(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Clubs,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Diamonds,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));
        }

        [Fact]
        public void NoTrumpsShouldNotAllowBelote()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                           };

            var trick = new List<PlayCardAction> { new PlayCardAction(Card.GetCard(CardSuit.Club, CardType.Jack)) };

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.NoTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Queen)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.NoTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.King)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.NoTrumps,
                    trick,
                    Card.GetCard(CardSuit.Club, CardType.Ace)));
        }

        [Fact]
        public void BeloteIsNotAllowedWhenNotHavingAKing()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Heart, CardType.Queen),
                               Card.GetCard(CardSuit.Heart, CardType.Ace),
                           };

            var trick = new List<PlayCardAction>();

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.AllTrumps,
                    trick,
                    Card.GetCard(CardSuit.Heart, CardType.Queen)));

            Assert.False(
                validAnnouncesService.IsBeloteAllowed(
                    hand,
                    BidType.Hearts,
                    trick,
                    Card.GetCard(CardSuit.Heart, CardType.Queen)));
        }
    }
}
