namespace Belot.Engine.Tests.GameMechanics
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;

    using Xunit;

    public class GetAvailableAnnouncesTests
    {
        [Fact]
        public void FourJacks()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Heart, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourJacks && x.Card == Card.GetCard(CardSuit.Spade, CardType.Jack));
        }

        [Fact]
        public void FourNines()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Diamond, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourNines && x.Card == Card.GetCard(CardSuit.Spade, CardType.Nine));
        }

        [Fact]
        public void FourOfAKindAces()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Heart, CardType.Ace),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void FourOfAKindTens()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ten));
        }

        [Fact]
        public void FourOfAKindQueens()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Heart, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.Queen));
        }

        [Fact]
        public void FourOfAKindKings()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.King));
        }

        [Fact]
        public void FourOfAKindQueensAndKings()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Heart, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.King));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.Queen));
        }

        [Fact]
        public void NoFourOfAKindSevens()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(0, combinations.Count);
            Assert.DoesNotContain(combinations, x => x.Type == AnnounceType.FourOfAKind);
        }

        [Fact]
        public void NoFourOfAKingEights()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(0, combinations.Count);
            Assert.DoesNotContain(combinations, x => x.Type == AnnounceType.FourOfAKind);
        }

        [Fact]
        public void TierceFromSevenToNine()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Club, CardType.Seven),

                               Card.GetCard(CardSuit.Club, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Club, CardType.Nine));
        }

        [Fact]
        public void QuartFromJackToAce()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Diamond, CardType.King),

                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Diamond, CardType.Ace));
        }

        [Fact]
        public void QuartFromSevenToTenWithAnotherCardOfTheSameSuit()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),

                               Card.GetCard(CardSuit.Club, CardType.Ace),
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ten));
        }

        [Fact]
        public void QuintFromNineToKing()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Heart, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                               Card.GetCard(CardSuit.Heart, CardType.Jack),
                               Card.GetCard(CardSuit.Heart, CardType.Queen),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf5 && x.Card == Card.GetCard(CardSuit.Heart, CardType.King));
        }

        [Fact]
        public void QuintFromSevenToTenWithAnotherCardOfTheSameSuit()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                               Card.GetCard(CardSuit.Heart, CardType.Jack),
                               Card.GetCard(CardSuit.Heart, CardType.Ace),

                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf5 && x.Card == Card.GetCard(CardSuit.Heart, CardType.Jack));
        }

        [Fact]
        public void SequenceOf6FromEightToKing()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Diamond, CardType.Nine),
                               Card.GetCard(CardSuit.Diamond, CardType.Ten),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf6 && x.Card == Card.GetCard(CardSuit.Diamond, CardType.King));
        }

        [Fact]
        public void SequenceOf6FromSevenToQueenWithAceOfTheSameSuit()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Club, CardType.Jack),
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.Ace),

                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf6 && x.Card == Card.GetCard(CardSuit.Club, CardType.Queen));
        }

        [Fact]
        public void SequenceOf7FromSevenToKing()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Club, CardType.Jack),
                               Card.GetCard(CardSuit.Club, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(1, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf7 && x.Card == Card.GetCard(CardSuit.Club, CardType.King));
        }

        [Fact]
        public void SequenceOf8FromSevenToAce()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf8 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void TwoTiercesFromTheSameSuit()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ten));
        }

        [Fact]
        public void TwoQuartsFromDifferentSuits()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Club, CardType.Ten));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void NoCombinationsAvailable()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Club, CardType.Ten),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Club, CardType.Ace),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(0, combinations.Count);
        }

        [Fact]
        public void FourOfAKindAndQuart()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.King),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Ten),
                               Card.GetCard(CardSuit.Diamond, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.FourOfAKind && x.Card == Card.GetCard(CardSuit.Spade, CardType.King));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Diamond, CardType.Queen));
        }
    }
}
