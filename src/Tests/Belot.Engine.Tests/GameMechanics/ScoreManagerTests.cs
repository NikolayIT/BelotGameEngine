namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class ScoreManagerTests
    {
        [Theory]
        [InlineData(BidType.AllTrumps, 0, false, 0)]
        [InlineData(BidType.AllTrumps, 258, false, 26)]
        [InlineData(BidType.AllTrumps, 155, true, 16)]
        [InlineData(BidType.AllTrumps, 155, false, 16)]
        [InlineData(BidType.AllTrumps, 153, false, 15)]
        [InlineData(BidType.AllTrumps, 154, true, 15)]
        [InlineData(BidType.AllTrumps, 154, false, 16)]
        [InlineData(BidType.AllTrumps, 3, false, 0)]
        [InlineData(BidType.NoTrumps, 130, false, 13)]
        [InlineData(BidType.NoTrumps, 134, false, 13)]
        [InlineData(BidType.NoTrumps, 136, false, 14)]
        [InlineData(BidType.NoTrumps, 260, true, 26)]
        [InlineData(BidType.NoTrumps, 0, true, 0)]
        [InlineData(BidType.Spades, 0, false, 0)]
        [InlineData(BidType.Diamonds, 162, false, 16)]
        [InlineData(BidType.Hearts, 87, true, 9)]
        [InlineData(BidType.Clubs, 85, false, 8)]
        [InlineData(BidType.Clubs, 86, false, 9)]
        [InlineData(BidType.Hearts, 86, true, 8)]
        [InlineData(BidType.Spades, 76, false, 8)]
        [InlineData(BidType.Diamonds, 76, true, 7)]
        public void RoundPointsShouldWorkCorrectly(BidType bidType, int points, bool isWinner, int expectedResult)
        {
            var actualResult = ScoreManager.RoundPoints(bidType, points, isWinner);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(BidType.AllTrumps, PlayerPosition.South, 9, 85, 7, 65, 0)]
        [InlineData(BidType.NoTrumps, PlayerPosition.South, 7, 66, 3, 26, 0)]
        [InlineData(BidType.Hearts, PlayerPosition.South, 7, 71, 5, 47, 0)]
        [InlineData(BidType.Diamonds, PlayerPosition.South, 7, 67, 5, 51, 0)]
        [InlineData(BidType.AllTrumps, PlayerPosition.West, 15, 85, 0, 65, 0)]
        [InlineData(BidType.NoTrumps, PlayerPosition.West, 9, 66, 0, 26, 0)]
        [InlineData(BidType.Hearts, PlayerPosition.West, 12, 71, 0, 47, 0)]
        [InlineData(BidType.Diamonds, PlayerPosition.West, 12, 67, 0, 51, 0)]
        [InlineData(BidType.AllTrumps | BidType.Double, PlayerPosition.South, 30, 85, 0, 65, 0)]
        [InlineData(BidType.NoTrumps | BidType.Double, PlayerPosition.South, 18, 66, 0, 26, 0)]
        [InlineData(BidType.Hearts | BidType.Double, PlayerPosition.South, 24, 71, 0, 47, 0)]
        [InlineData(BidType.Diamonds | BidType.Double, PlayerPosition.South, 24, 67, 0, 51, 0)]
        [InlineData(BidType.AllTrumps | BidType.Double, PlayerPosition.West, 30, 85, 0, 65, 0)]
        [InlineData(BidType.NoTrumps | BidType.Double, PlayerPosition.West, 18, 66, 0, 26, 0)]
        [InlineData(BidType.Hearts | BidType.Double, PlayerPosition.West, 24, 71, 0, 47, 0)]
        [InlineData(BidType.Diamonds | BidType.Double, PlayerPosition.West, 24, 67, 0, 51, 0)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, PlayerPosition.North, 60, 85, 0, 65, 0)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, PlayerPosition.North, 36, 66, 0, 26, 0)]
        [InlineData(BidType.Hearts | BidType.ReDouble, PlayerPosition.North, 48, 71, 0, 47, 0)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, PlayerPosition.North, 48, 67, 0, 51, 0)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, PlayerPosition.East, 60, 85, 0, 65, 0)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, PlayerPosition.East, 36, 66, 0, 26, 0)]
        [InlineData(BidType.Hearts | BidType.ReDouble, PlayerPosition.East, 48, 71, 0, 47, 0)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, PlayerPosition.East, 48, 67, 0, 51, 0)]
        [InlineData(BidType.AllTrumps, PlayerPosition.North, 19, 85, 7, 65, 10)]
        [InlineData(BidType.NoTrumps, PlayerPosition.North, 17, 66, 3, 26, 10)]
        [InlineData(BidType.Hearts, PlayerPosition.North, 17, 71, 5, 47, 10)]
        [InlineData(BidType.Diamonds, PlayerPosition.North, 17, 67, 5, 51, 10)]
        [InlineData(BidType.AllTrumps, PlayerPosition.East, 25, 85, 0, 65, 10)]
        [InlineData(BidType.NoTrumps, PlayerPosition.East, 19, 66, 0, 26, 10)]
        [InlineData(BidType.Hearts, PlayerPosition.East, 22, 71, 0, 47, 10)]
        [InlineData(BidType.Diamonds, PlayerPosition.East, 22, 67, 0, 51, 10)]
        public void GetScoreShouldWorkCorrectly(
            BidType bidType,
            PlayerPosition bidBy,
            int expectedSouthNorthPoints,
            int expectedSouthNorthTotalInRoundPoints,
            int expectedEastWestPoints,
            int expectedEastWestTotalInRoundPoints,
            int hanging)
        {
            var southNorthTricks = new CardCollection
                                       {
                                           Card.GetCard(CardSuit.Heart, CardType.Jack),
                                           Card.GetCard(CardSuit.Heart, CardType.Ace),
                                           Card.GetCard(CardSuit.Heart, CardType.Ten),
                                           Card.GetCard(CardSuit.Diamond, CardType.Nine),
                                       };

            var eastWestTricks = new CardCollection
                                     {
                                         Card.GetCard(CardSuit.Heart, CardType.Nine),
                                         Card.GetCard(CardSuit.Diamond, CardType.Jack),
                                         Card.GetCard(CardSuit.Diamond, CardType.Ace),
                                     };

            var announces = new List<Announce>
                            {
                                new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Queen)) { IsActive = true, Player = PlayerPosition.South },
                                new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Heart, CardType.Queen)) { IsActive = true, Player = PlayerPosition.East },
                                new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Spade, CardType.Queen)) { IsActive = false, Player = PlayerPosition.North },
                                new Announce(AnnounceType.SequenceOf3, Card.GetCard(CardSuit.Diamond, CardType.Nine)) { IsActive = false, Player = PlayerPosition.West },
                            };
            if (bidType.HasFlag(BidType.NoTrumps))
            {
                foreach (var announce in announces)
                {
                    announce.IsActive = false;
                }
            }

            var scoreManager = new ScoreManager();

            var score = scoreManager.GetScore(
                new Bid(bidBy, bidType),
                southNorthTricks,
                eastWestTricks,
                announces,
                hanging,
                PlayerPosition.South);
            Assert.Equal(bidBy, score.Contract.Player);
            Assert.Equal(bidType, score.Contract.Type);
            Assert.Equal(expectedSouthNorthPoints, score.SouthNorthPoints);
            Assert.Equal(expectedSouthNorthTotalInRoundPoints, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(expectedEastWestPoints, score.EastWestPoints);
            Assert.Equal(expectedEastWestTotalInRoundPoints, score.EastWestTotalInRoundPoints);
            Assert.False(score.NoTricksForOneOfTheTeams);
            Assert.Equal(0, score.HangingPoints);

            var reverseScore = scoreManager.GetScore(
                new Bid(bidBy.Next(), bidType),
                eastWestTricks,
                southNorthTricks,
                announces,
                hanging,
                PlayerPosition.East);
            Assert.Equal(expectedEastWestPoints, reverseScore.SouthNorthPoints);
            Assert.Equal(expectedEastWestTotalInRoundPoints, reverseScore.SouthNorthTotalInRoundPoints);
            Assert.Equal(expectedSouthNorthPoints, reverseScore.EastWestPoints);
            Assert.Equal(expectedSouthNorthTotalInRoundPoints, reverseScore.EastWestTotalInRoundPoints);
            Assert.False(reverseScore.NoTricksForOneOfTheTeams);
            Assert.Equal(0, reverseScore.HangingPoints);
        }

        [Theory]
        [InlineData(BidType.AllTrumps, 0, 0, 15, 145)]
        [InlineData(BidType.NoTrumps, 0, 0, 14, 136)]
        [InlineData(BidType.Hearts, 0, 0, 13, 127)]
        [InlineData(BidType.Diamonds, 0, 0, 13, 131)]
        [InlineData(BidType.AllTrumps | BidType.Double, 0, 0, 14, 145)]
        [InlineData(BidType.NoTrumps | BidType.Double, 0, 0, 14, 136)]
        [InlineData(BidType.Hearts | BidType.Double, 0, 0, 13, 127)]
        [InlineData(BidType.Diamonds | BidType.Double, 0, 0, 13, 131)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, 0, 0, 14, 145)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, 0, 0, 14, 136)]
        [InlineData(BidType.Hearts | BidType.ReDouble, 0, 0, 13, 127)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, 0, 0, 13, 131)]
        public void GetScoreShouldWorkCorrectlyWhenOneOfTheTeamsHasNoTricks(
            BidType bidType,
            int expectedSouthNorthPoints,
            int expectedSouthNorthTotalInRoundPoints,
            int expectedEastWestPoints,
            int expectedEastWestTotalInRoundPoints)
        {
            var tricks = new CardCollection
                             {
                                 Card.GetCard(CardSuit.Heart, CardType.Nine),
                                 Card.GetCard(CardSuit.Diamond, CardType.Jack),
                                 Card.GetCard(CardSuit.Diamond, CardType.Ace),
                             };

            var scoreManager = new ScoreManager();

            var score = scoreManager.GetScore(
                new Bid(PlayerPosition.East, bidType),
                new CardCollection(),
                tricks,
                new List<Announce>(),
                0,
                PlayerPosition.West);
            Assert.Equal(expectedSouthNorthPoints, score.SouthNorthPoints);
            Assert.Equal(expectedSouthNorthTotalInRoundPoints, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(expectedEastWestPoints, score.EastWestPoints);
            Assert.Equal(expectedEastWestTotalInRoundPoints, score.EastWestTotalInRoundPoints);
            Assert.True(score.NoTricksForOneOfTheTeams);
            Assert.Equal(0, score.HangingPoints);

            var reverseScore = scoreManager.GetScore(
                new Bid(PlayerPosition.North, bidType),
                tricks,
                new CardCollection(),
                new List<Announce>(),
                0,
                PlayerPosition.South);
            Assert.Equal(expectedEastWestPoints, reverseScore.SouthNorthPoints);
            Assert.Equal(expectedEastWestTotalInRoundPoints, reverseScore.SouthNorthTotalInRoundPoints);
            Assert.Equal(expectedSouthNorthPoints, reverseScore.EastWestPoints);
            Assert.Equal(expectedSouthNorthTotalInRoundPoints, reverseScore.EastWestTotalInRoundPoints);
            Assert.True(reverseScore.NoTricksForOneOfTheTeams);
            Assert.Equal(0, reverseScore.HangingPoints);
        }

        [Theory]
        [InlineData(BidType.AllTrumps, PlayerPosition.South, 0, 2, 2, 0)]
        [InlineData(BidType.NoTrumps, PlayerPosition.South, 0, 4, 4, 0)]
        [InlineData(BidType.Hearts, PlayerPosition.South, 0, 2, 2, 0)]
        [InlineData(BidType.Diamonds, PlayerPosition.South, 0, 2, 2, 0)]
        [InlineData(BidType.AllTrumps | BidType.Double, PlayerPosition.South, 0, 0, 8, 0)]
        [InlineData(BidType.NoTrumps | BidType.Double, PlayerPosition.South, 0, 0, 16, 0)]
        [InlineData(BidType.Hearts | BidType.Double, PlayerPosition.South, 0, 0, 8, 0)]
        [InlineData(BidType.Diamonds | BidType.Double, PlayerPosition.South, 0, 0, 8, 0)]
        [InlineData(BidType.AllTrumps, PlayerPosition.South, 0, 2, 12, 10)]
        [InlineData(BidType.NoTrumps, PlayerPosition.South, 0, 4, 14, 10)]
        [InlineData(BidType.Hearts, PlayerPosition.South, 0, 2, 12, 10)]
        [InlineData(BidType.Diamonds, PlayerPosition.South, 0, 2, 12, 10)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, PlayerPosition.South, 0, 0, 26, 10)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, PlayerPosition.South, 0, 0, 42, 10)]
        [InlineData(BidType.Hearts | BidType.ReDouble, PlayerPosition.South, 0, 0, 26, 10)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, PlayerPosition.South, 0, 0, 26, 10)]
        public void GetScoreShouldWorkCorrectlyWhenTheScoreIsEqual(
            BidType bidType,
            PlayerPosition bidBy,
            int expectedSouthNorthPoints,
            int expectedEastWestPoints,
            int expectedHanging,
            int hanging)
        {
            var southNorthTricks = new CardCollection
                                       {
                                           Card.GetCard(CardSuit.Spade, CardType.Ace),
                                           Card.GetCard(CardSuit.Heart, CardType.Ten),
                                       };
            var eastWestTricks = new CardCollection
                                     {
                                         Card.GetCard(CardSuit.Club, CardType.Ace),
                                     };
            var expectedTotalInRoundPoints = bidType.HasFlag(BidType.NoTrumps) ? 42 : 21;

            var scoreManager = new ScoreManager();

            var score = scoreManager.GetScore(
                new Bid(bidBy, bidType),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                hanging,
                PlayerPosition.West);
            Assert.Equal(expectedSouthNorthPoints, score.SouthNorthPoints);
            Assert.Equal(expectedTotalInRoundPoints, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(expectedEastWestPoints, score.EastWestPoints);
            Assert.Equal(expectedTotalInRoundPoints, score.EastWestTotalInRoundPoints);
            Assert.False(score.NoTricksForOneOfTheTeams);
            Assert.Equal(expectedHanging, score.HangingPoints);

            var reverseScore = scoreManager.GetScore(
                new Bid(bidBy.Next(), bidType),
                eastWestTricks,
                southNorthTricks,
                new List<Announce>(),
                hanging,
                PlayerPosition.North);
            Assert.Equal(expectedEastWestPoints, reverseScore.SouthNorthPoints);
            Assert.Equal(expectedSouthNorthPoints, reverseScore.EastWestPoints);
            Assert.Equal(expectedHanging, score.HangingPoints);
        }
    }
}
