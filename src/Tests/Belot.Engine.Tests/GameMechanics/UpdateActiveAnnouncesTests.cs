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
        public void SequencesAndCarresResolveInIndependentPools()
        {
            // Rules.md compares "sequential bonuses" and "quad bonuses" separately: a tierce is
            // not killed by the opponents' four jacks, and vice versa.
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.King)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.FourJacks, Card.GetCard(CardSuit.Spade, CardType.Jack)) { Player = PlayerPosition.East },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
        }

        [Theory]
        [InlineData(CardType.Ace, CardType.Ten)]
        [InlineData(CardType.Ten, CardType.King)]
        [InlineData(CardType.Ten, CardType.Queen)]
        [InlineData(CardType.King, CardType.Queen)]
        public void EqualValueCarreContestGoesToTheHigherTrumpCard(CardType strongerType, CardType weakerType)
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, weakerType)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, strongerType)) { Player = PlayerPosition.West },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.False(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
        }

        [Fact]
        public void FourNinesBeatFourAces()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, CardType.Ace)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.FourNines, Card.GetCard(CardSuit.Spade, CardType.Nine)) { Player = PlayerPosition.East },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.False(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
        }

        [Fact]
        public void EqualBestSequencesKillOnlyTheSequences()
        {
            // hit.bg: equal best runs void ALL sequence premiums for both teams - but the carre
            // contest and the belote are untouched.
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Diamond, CardType.King)) { Player = PlayerPosition.West },
                               new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.North },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Club, CardType.Nine)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, CardType.Ten)) { Player = PlayerPosition.East },
                               new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Heart, CardType.Queen)) { Player = PlayerPosition.South },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.False(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
            Assert.False(announces[2].IsActive);
            Assert.True(announces[3].IsActive);
            Assert.True(announces[4].IsActive);
        }

        [Fact]
        public void EqualMaxSequencesInTheSameTeamBothCount()
        {
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.King)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.North },
                               new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Heart, CardType.Nine)) { Player = PlayerPosition.West },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.True(announces[1].IsActive);
            Assert.False(announces[2].IsActive);
        }

        [Fact]
        public void LongerSequenceBeatsShorterAtEqualPointValue()
        {
            // Rules.md: "the team that has the longest such sequence scores" - a 6-card run to
            // the queen outranks a quint to the ace even though both are worth 100.
            var validAnnouncesService = new ValidAnnouncesService();
            var announces = new List<Announce>
                           {
                               new Announce(AnnounceType.SequenceOf6, Card.GetCard(CardSuit.Diamond, CardType.Queen)) { Player = PlayerPosition.South },
                               new Announce(AnnounceType.SequenceOf5, Card.GetCard(CardSuit.Spade, CardType.Ace)) { Player = PlayerPosition.East },
                           };

            validAnnouncesService.UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
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
