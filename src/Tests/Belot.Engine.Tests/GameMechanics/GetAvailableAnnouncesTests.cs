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

            // The 7-8-9 of spades is offered as the alternative to the carre (they share the 9♠).
            Assert.Equal(2, combinations.Count);
            Assert.True(combinations[0].Type == AnnounceType.FourNines && combinations[0].Card == Card.GetCard(CardSuit.Spade, CardType.Nine));
            Assert.True(combinations[1].Type == AnnounceType.SequenceOf3 && combinations[1].Card == Card.GetCard(CardSuit.Spade, CardType.Nine));
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

            // J-Q-K of spades is offered as the alternative to the carre (they share the K♠).
            Assert.Equal(2, combinations.Count);
            Assert.True(combinations[0].Type == AnnounceType.FourOfAKind && combinations[0].Card == Card.GetCard(CardSuit.Spade, CardType.King));
            Assert.True(combinations[1].Type == AnnounceType.SequenceOf3 && combinations[1].Card == Card.GetCard(CardSuit.Spade, CardType.King));
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

            // Five or more cards in a row are one quint: the whole suit is worth 100, no more.
            var combination = Assert.Single(combinations);
            Assert.Equal(AnnounceType.SequenceOf8, combination.Type);
            Assert.Equal(Card.GetCard(CardSuit.Spade, CardType.Ace), combination.Card);
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
        public void TierceAndQuarteFromDifferentSuits()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.Eight),
                               Card.GetCard(CardSuit.Heart, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Club, CardType.Seven),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Heart, CardType.Nine));
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void FourNinesConsumeTheNineOfTheSequence()
        {
            // A card takes part in only one combination: with the nines in the carre, the
            // leftover 7-8 and 10-J of spades are no sequences. The quint 7-J through the nine of
            // spades is offered only as the alternative to the carre.
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Diamond, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Spade, CardType.Ten),
                               Card.GetCard(CardSuit.Spade, CardType.Jack),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            Assert.Equal(AnnounceType.FourNines, combinations[0].Type);
            Assert.True(combinations[1].Type == AnnounceType.SequenceOf5 && combinations[1].Card == Card.GetCard(CardSuit.Spade, CardType.Jack));
            Assert.True(validAnnouncesService.HaveCommonCards(combinations[0], combinations[1]));
        }

        [Fact]
        public void TierceAtTheTopOfTheSuit()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Spade, CardType.Queen),
                               Card.GetCard(CardSuit.Spade, CardType.King),
                               Card.GetCard(CardSuit.Spade, CardType.Ace),
                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Diamond, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                               Card.GetCard(CardSuit.Heart, CardType.Ten),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Single(combinations);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == Card.GetCard(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void MiddleQuarteHasItsOwnTopCard()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Diamond, CardType.Nine),
                               Card.GetCard(CardSuit.Diamond, CardType.Ten),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Queen),
                               Card.GetCard(CardSuit.Diamond, CardType.Ace),
                               Card.GetCard(CardSuit.Heart, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Eight),
                               Card.GetCard(CardSuit.Club, CardType.Nine),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Single(combinations);
            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf4 && x.Card == Card.GetCard(CardSuit.Diamond, CardType.Queen));
        }

        [Fact]
        public void ThreeOfAKindIsNotACombination()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var hand = new CardCollection
                           {
                               Card.GetCard(CardSuit.Club, CardType.Jack),
                               Card.GetCard(CardSuit.Diamond, CardType.Jack),
                               Card.GetCard(CardSuit.Heart, CardType.Jack),
                               Card.GetCard(CardSuit.Spade, CardType.Seven),
                               Card.GetCard(CardSuit.Spade, CardType.Nine),
                               Card.GetCard(CardSuit.Diamond, CardType.Seven),
                               Card.GetCard(CardSuit.Heart, CardType.King),
                               Card.GetCard(CardSuit.Club, CardType.Eight),
                           };

            var combinations = validAnnouncesService.GetAvailableAnnounces(hand);

            Assert.Empty(combinations);
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

            // The carre plus the quarte of the free diamonds, or instead the quint 9-K♦ through
            // the king of diamonds.
            Assert.Equal(3, combinations.Count);
            Assert.True(combinations[0].Type == AnnounceType.FourOfAKind && combinations[0].Card == Card.GetCard(CardSuit.Spade, CardType.King));
            Assert.True(combinations[1].Type == AnnounceType.SequenceOf4 && combinations[1].Card == Card.GetCard(CardSuit.Diamond, CardType.Queen));
            Assert.True(combinations[2].Type == AnnounceType.SequenceOf5 && combinations[2].Card == Card.GetCard(CardSuit.Diamond, CardType.King));
        }
    }
}
