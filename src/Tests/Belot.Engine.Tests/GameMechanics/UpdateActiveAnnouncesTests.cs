namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class UpdateActiveAnnouncesTests
    {
        [Fact]
        public void AllAnnouncesShouldBeValidWhenAnnouncedByOnePlayer()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Jack)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Spade, CardType.Queen)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Heart, CardType.Queen)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.South },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
            Assert.True(announces[2].IsActive);
            Assert.True(announces[3].IsActive);
        }

        [Fact]
        public void TierceAndQuarteShouldBeValidIfAnnouncedByTheSameTeam()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Jack)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Club, CardType.King)) { Player = PlayerPosition.North },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Heart, CardType.Jack)) { Player = PlayerPosition.West },
                               new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.East },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
            Assert.False(announces[2].IsActive);
            Assert.True(announces[3].IsActive);
            Assert.True(announces[4].IsActive);
        }

        [Fact]
        public void QuarteShouldBeValidIfAnnouncedQuarteAndTierceByDifferentTeams()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Jack)) { Player = PlayerPosition.West },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Ace)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.North },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.False(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
            Assert.True(announces[2].IsActive);
        }

        [Fact]
        public void BiggerFourOfAKindShouldDisableOpponentTeamFourOfAKind()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.FourJacks, Card.GetCard(CardSuit.Spade, CardType.Jack)) { Player = PlayerPosition.West },
                               new Announce(AnnounceType.FourNines, Card.GetCard(CardSuit.Spade, CardType.Nine)) { Player = PlayerPosition.South },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
        }

        [Fact]
        public void BothFourOfAKindShouldBeActiveIfAnnouncedInTheSameTeam()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, CardType.Ace)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.FourNines, Card.GetCard(CardSuit.Spade, CardType.Nine)) { Player = PlayerPosition.West },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
        }

        [Fact]
        public void SameAnnouncesInDifferentTeamsShouldNotBeActive()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Diamond, CardType.King)) { Player = PlayerPosition.West },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Ace)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.North },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.False(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
            Assert.False(announces[2].IsActive);
        }
    }
}
