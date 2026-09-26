namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Moq;
    using Xunit;

    /// <summary>
    /// "Ако една карта участва едновременно в каре и поредица, играчът избира кое от двете да
    /// обяви" (hit.bg §Премии): a card may take part in only one combination, and when a carre
    /// and a sequence share a card the player chooses. The engine offers both and registers the
    /// declarations in the player's order, dropping one that reuses an already declared card, so
    /// a player that declares everything it is offered keeps the carre. Also: five or more cards
    /// in a row are one quint, so a whole suit is a single quint.
    /// </summary>
    public class CombinationChoiceTests
    {
        private static readonly ValidAnnouncesService Service = new ValidAnnouncesService();

        [Fact]
        public void ACarreInsideAQuintOffersTheQuintAsTheAlternative()
        {
            var combinations = Service.GetAvailableAnnounces(FourQueensAndTenJackKingAceOfSpades());

            Assert.Equal(2, combinations.Count);
            AssertAnnounce(combinations[0], AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.Queen));
            AssertAnnounce(combinations[1], AnnounceType.SequenceOf5, C(CardSuit.Spade, CardType.Ace));
        }

        [Fact]
        public void ACarreAtTheEndOfARunOffersTheLeftoverRunAndTheWholeRun()
        {
            // Four kings + 9-Q of spades: carre + quarte to the queen, or the quint 9-K instead.
            var hand = new CardCollection
            {
                C(CardSuit.Club, CardType.King), C(CardSuit.Diamond, CardType.King),
                C(CardSuit.Heart, CardType.King), C(CardSuit.Spade, CardType.King),
                C(CardSuit.Spade, CardType.Nine), C(CardSuit.Spade, CardType.Ten),
                C(CardSuit.Spade, CardType.Jack), C(CardSuit.Spade, CardType.Queen),
            };

            var combinations = Service.GetAvailableAnnounces(hand);

            Assert.Equal(3, combinations.Count);
            AssertAnnounce(combinations[0], AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.King));
            AssertAnnounce(combinations[1], AnnounceType.SequenceOf4, C(CardSuit.Spade, CardType.Queen));
            AssertAnnounce(combinations[2], AnnounceType.SequenceOf5, C(CardSuit.Spade, CardType.King));
        }

        [Fact]
        public void ARunThatDoesNotTouchTheCarreIsOfferedOnce()
        {
            var hand = new CardCollection
            {
                C(CardSuit.Club, CardType.Ace), C(CardSuit.Diamond, CardType.Ace),
                C(CardSuit.Heart, CardType.Ace), C(CardSuit.Spade, CardType.Ace),
                C(CardSuit.Heart, CardType.Seven), C(CardSuit.Heart, CardType.Eight),
                C(CardSuit.Heart, CardType.Nine), C(CardSuit.Club, CardType.Jack),
            };

            var combinations = Service.GetAvailableAnnounces(hand);

            Assert.Equal(2, combinations.Count);
            AssertAnnounce(combinations[0], AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.Ace));
            AssertAnnounce(combinations[1], AnnounceType.SequenceOf3, C(CardSuit.Heart, CardType.Nine));
        }

        [Fact]
        public void AWholeSuitIsASingleQuint()
        {
            var hand = new CardCollection();
            foreach (var type in Card.AllTypes)
            {
                hand.Add(C(CardSuit.Heart, type));
            }

            var combination = Assert.Single(Service.GetAvailableAnnounces(hand));

            AssertAnnounce(combination, AnnounceType.SequenceOf8, C(CardSuit.Heart, CardType.Ace));
            Assert.Equal(100, combination.Value);
        }

        [Theory]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Queen, AnnounceType.SequenceOf5, CardSuit.Spade, CardType.Ace, true)]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Queen, AnnounceType.SequenceOf5, CardSuit.Heart, CardType.Ace, true)]
        [InlineData(AnnounceType.FourOfAKind, CardSuit.Spade, CardType.Queen, AnnounceType.SequenceOf3, CardSuit.Heart, CardType.Nine, false)]
        [InlineData(AnnounceType.FourJacks, CardSuit.Spade, CardType.Jack, AnnounceType.FourNines, CardSuit.Spade, CardType.Nine, false)]
        [InlineData(AnnounceType.SequenceOf4, CardSuit.Spade, CardType.Queen, AnnounceType.SequenceOf5, CardSuit.Spade, CardType.King, true)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Nine, AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Queen, false)]
        [InlineData(AnnounceType.SequenceOf3, CardSuit.Spade, CardType.Nine, AnnounceType.SequenceOf3, CardSuit.Heart, CardType.Nine, false)]
        [InlineData(AnnounceType.SequenceOf8, CardSuit.Club, CardType.Ace, AnnounceType.SequenceOf3, CardSuit.Club, CardType.Nine, true)]
        [InlineData(AnnounceType.Belot, CardSuit.Spade, CardType.King, AnnounceType.SequenceOf3, CardSuit.Spade, CardType.King, false)]
        public void HaveCommonCardsTellsWhichCombinationsExcludeEachOther(
            AnnounceType firstType,
            CardSuit firstSuit,
            CardType firstCard,
            AnnounceType secondType,
            CardSuit secondSuit,
            CardType secondCard,
            bool expected)
        {
            var first = new Announce(firstType, C(firstSuit, firstCard));
            var second = new Announce(secondType, C(secondSuit, secondCard));

            Assert.Equal(expected, Service.HaveCommonCards(first, second));
            Assert.Equal(expected, Service.HaveCommonCards(second, first));
        }

        [Fact]
        public void HaveCommonCardsRejectsASequenceThatCannotExist()
        {
            // Six cards in a row cannot end at the nine: only 7-8-9 lie below it.
            var impossible = new Announce(AnnounceType.SequenceOf6, C(CardSuit.Spade, CardType.Nine));
            var tierce = new Announce(AnnounceType.SequenceOf3, C(CardSuit.Spade, CardType.Nine));

            Assert.Throws<BelotGameException>(() => Service.HaveCommonCards(impossible, tierce));
        }

        [Fact]
        public void DeclaringEverythingOfferedKeepsTheCarre()
        {
            // What every shipped player does: return context.AvailableAnnounces as it is.
            var announces = PlayRoundDeclaring(available => available);

            AssertAnnounce(Assert.Single(announces), AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.Queen));
        }

        [Fact]
        public void DeclaringOnlyTheQuintRegistersTheQuintInsteadOfTheCarre()
        {
            var announces = PlayRoundDeclaring(available => available.Where(x => x.Type == AnnounceType.SequenceOf5).ToList());

            var registered = Assert.Single(announces);
            AssertAnnounce(registered, AnnounceType.SequenceOf5, C(CardSuit.Spade, CardType.Ace));
            Assert.True(registered.IsActive);
        }

        [Fact]
        public void TheFirstOfTwoCombinationsSharingACardIsTheOneRegistered()
        {
            var announces = PlayRoundDeclaring(available => available.Reverse().ToList());

            AssertAnnounce(Assert.Single(announces), AnnounceType.SequenceOf5, C(CardSuit.Spade, CardType.Ace));
        }

        // South holds four queens and 10-J-K-A of spades; the others' cards are split so that
        // none of them holds a combination. Everybody plays random legal cards.
        private static List<Announce> PlayRoundDeclaring(Func<IList<Announce>, IList<Announce>> southDeclares)
        {
            var south = FourQueensAndTenJackKingAceOfSpades();
            var rest = Card.AllCards.Where(x => !south.Contains(x)).ToList();
            var hands = new List<CardCollection> { south };
            for (var player = 0; player < 3; player++)
            {
                // Dealing the rest round-robin leaves every other hand without a combination.
                hands.Add(new CardCollection(new CardCollection(), x => false));
                for (var i = player; i < rest.Count; i += 3)
                {
                    hands[player + 1].Add(rest[i]);
                }

                Assert.Empty(Service.GetAvailableAnnounces(hands[player + 1]));
            }

            var random = new Random(7);
            var players = Enumerable.Range(0, 4).Select(index =>
            {
                var player = new Mock<IPlayer>();
                player.Setup(x => x.GetAnnounces(It.IsAny<PlayerGetAnnouncesContext>()))
                    .Returns<PlayerGetAnnouncesContext>(c => index == 0 ? southDeclares(c.AvailableAnnounces) : new List<Announce>());
                player.Setup(x => x.PlayCard(It.IsAny<PlayerPlayCardContext>()))
                    .Returns<PlayerPlayCardContext>(c =>
                        new PlayCardAction(c.AvailableCardsToPlay.Skip(random.Next(c.AvailableCardsToPlay.Count)).First(), false));
                return player.Object;
            }).ToArray();

            var tricksManager = new TricksManager(players[0], players[1], players[2], players[3]);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                hands,
                new List<Bid>(),
                new Bid(PlayerPosition.South, BidType.AllTrumps),
                out var announces,
                out _,
                out _,
                out _);
            return announces;
        }

        private static CardCollection FourQueensAndTenJackKingAceOfSpades() => new CardCollection
        {
            C(CardSuit.Club, CardType.Queen), C(CardSuit.Diamond, CardType.Queen),
            C(CardSuit.Heart, CardType.Queen), C(CardSuit.Spade, CardType.Queen),
            C(CardSuit.Spade, CardType.Ten), C(CardSuit.Spade, CardType.Jack),
            C(CardSuit.Spade, CardType.King), C(CardSuit.Spade, CardType.Ace),
        };

        private static void AssertAnnounce(Announce announce, AnnounceType type, Card card)
        {
            Assert.Equal(type, announce.Type);
            Assert.Equal(card, announce.Card);
        }

        private static Card C(CardSuit suit, CardType type) => Card.GetCard(suit, type);
    }
}
