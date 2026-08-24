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
        [InlineData(BidType.AllTrumps | BidType.Double, 0, 0, 30, 145)]
        [InlineData(BidType.NoTrumps | BidType.Double, 0, 0, 28, 136)]
        [InlineData(BidType.Hearts | BidType.Double, 0, 0, 26, 127)]
        [InlineData(BidType.Diamonds | BidType.Double, 0, 0, 26, 131)]
        [InlineData(BidType.AllTrumps | BidType.ReDouble, 0, 0, 60, 145)]
        [InlineData(BidType.NoTrumps | BidType.ReDouble, 0, 0, 56, 136)]
        [InlineData(BidType.Hearts | BidType.ReDouble, 0, 0, 52, 127)]
        [InlineData(BidType.Diamonds | BidType.ReDouble, 0, 0, 52, 131)]
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

        // The complete rounding tables (etc/Rules.md par.Rounding; hit.bg par.Закръглявяне):
        // all trumps rounds up from 5, remainder 4 rounds down for the team with more points and
        // up for the other; a suit contract rounds up from 7, remainder 6 splitting the same way;
        // no trumps rounds to the nearest ten. Includes the documentation examples verbatim
        // (34-224, 164-194, 56-106, 6-156) and the hanging specials (154-154, 106-106).
        [Theory]
        [InlineData(BidType.AllTrumps, 120, 12, 12)]
        [InlineData(BidType.AllTrumps, 121, 12, 12)]
        [InlineData(BidType.AllTrumps, 122, 12, 12)]
        [InlineData(BidType.AllTrumps, 123, 12, 12)]
        [InlineData(BidType.AllTrumps, 124, 12, 13)]
        [InlineData(BidType.AllTrumps, 125, 13, 13)]
        [InlineData(BidType.AllTrumps, 126, 13, 13)]
        [InlineData(BidType.AllTrumps, 127, 13, 13)]
        [InlineData(BidType.AllTrumps, 128, 13, 13)]
        [InlineData(BidType.AllTrumps, 129, 13, 13)]
        [InlineData(BidType.AllTrumps, 34, 3, 4)]
        [InlineData(BidType.AllTrumps, 224, 22, 23)]
        [InlineData(BidType.AllTrumps, 164, 16, 17)]
        [InlineData(BidType.AllTrumps, 194, 19, 20)]
        [InlineData(BidType.AllTrumps, 154, 15, 16)]
        [InlineData(BidType.Hearts, 80, 8, 8)]
        [InlineData(BidType.Hearts, 81, 8, 8)]
        [InlineData(BidType.Hearts, 82, 8, 8)]
        [InlineData(BidType.Hearts, 83, 8, 8)]
        [InlineData(BidType.Hearts, 84, 8, 8)]
        [InlineData(BidType.Hearts, 85, 8, 8)]
        [InlineData(BidType.Hearts, 86, 8, 9)]
        [InlineData(BidType.Hearts, 87, 9, 9)]
        [InlineData(BidType.Hearts, 88, 9, 9)]
        [InlineData(BidType.Hearts, 89, 9, 9)]
        [InlineData(BidType.Clubs, 56, 5, 6)]
        [InlineData(BidType.Clubs, 106, 10, 11)]
        [InlineData(BidType.Clubs, 6, 0, 1)]
        [InlineData(BidType.Clubs, 156, 15, 16)]
        [InlineData(BidType.NoTrumps, 130, 13, 13)]
        [InlineData(BidType.NoTrumps, 132, 13, 13)]
        [InlineData(BidType.NoTrumps, 134, 13, 13)]
        [InlineData(BidType.NoTrumps, 136, 14, 14)]
        [InlineData(BidType.NoTrumps, 138, 14, 14)]
        public void RoundPointsShouldFollowTheContractRoundingTables(
            BidType bidType,
            int points,
            int expectedWhenWinner,
            int expectedWhenLoser)
        {
            Assert.Equal(expectedWhenWinner, ScoreManager.RoundPoints(bidType, points, true));
            Assert.Equal(expectedWhenLoser, ScoreManager.RoundPoints(bidType, points, false));
        }

        // hit.bg hanging special: 106-106 in a suit game (possible with a 50 announce) banks 10
        // for the non-bidding team and hangs 11.
        [Fact]
        public void EqualSuitGameAt106ShouldBank10AndHang11()
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart); // 62 trump points
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.Ace)); // +11
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.Ten)); // +10
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.Queen)); // +3
            southNorthTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Ten)); // +10 => 96

            var eastWestTricks = new CardCollection
            {
                Card.GetCard(CardSuit.Club, CardType.Ace), // 11
                Card.GetCard(CardSuit.Club, CardType.Ten), // 10
                Card.GetCard(CardSuit.Club, CardType.King), // 4
                Card.GetCard(CardSuit.Club, CardType.Queen), // 3
                Card.GetCard(CardSuit.Club, CardType.Jack), // 2
                Card.GetCard(CardSuit.Diamond, CardType.Ace), // 11
                Card.GetCard(CardSuit.Diamond, CardType.King), // 4
                Card.GetCard(CardSuit.Diamond, CardType.Queen), // 3
                Card.GetCard(CardSuit.Diamond, CardType.Jack), // 2
                Card.GetCard(CardSuit.Spade, CardType.King), // 4
                Card.GetCard(CardSuit.Spade, CardType.Jack), // 2 => 56
            };

            var announces = new List<Announce>
            {
                new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Diamond, CardType.Ace)) { IsActive = true, Player = PlayerPosition.East },
            };

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.Hearts),
                southNorthTricks,
                eastWestTricks,
                announces,
                0,
                PlayerPosition.South);

            Assert.Equal(106, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(106, score.EastWestTotalInRoundPoints);
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(10, score.EastWestPoints);
            Assert.Equal(11, score.HangingPoints);
        }

        // The all-trumps twin from the docs: 154-154 (with a 50 announce) banks 15 and hangs 16.
        [Fact]
        public void EqualAllTrumpsAt154ShouldBank15AndHang16()
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart); // 62
            foreach (var card in AllOfSuit(CardSuit.Spade))
            {
                southNorthTricks.Add(card); // +62
            }

            southNorthTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Jack)); // +20 => 144

            var eastWestTricks = AllOfSuit(CardSuit.Club); // 62
            foreach (var card in AllOfSuit(CardSuit.Diamond))
            {
                if (card.Type != CardType.Jack)
                {
                    eastWestTricks.Add(card); // +42 => 104
                }
            }

            var announces = new List<Announce>
            {
                new Announce(AnnounceType.SequenceOf4, Card.GetCard(CardSuit.Club, CardType.Ace)) { IsActive = true, Player = PlayerPosition.West },
            };

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.AllTrumps),
                southNorthTricks,
                eastWestTricks,
                announces,
                0,
                PlayerPosition.South);

            Assert.Equal(154, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(154, score.EastWestTotalInRoundPoints);
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(15, score.EastWestPoints);
            Assert.Equal(16, score.HangingPoints);
        }

        // Inside: the defenders record everything, including the failed declarer's own announces
        // (etc/Rules.md: "the team that didn't bid records all points including those scored by
        // the bidding team"), plus whatever was hanging.
        [Theory]
        [InlineData(0, 18)]
        [InlineData(7, 25)]
        public void InsideDeclarersLoseEverythingIncludingTheirAnnounces(int hanging, int expectedEastWestPoints)
        {
            var southNorthTricks = AllOfSuit(CardSuit.Spade); // 30 plain points
            var eastWestTricks = AllOfSuit(CardSuit.Heart); // 62 (trump)
            foreach (var card in AllOfSuit(CardSuit.Diamond))
            {
                eastWestTricks.Add(card); // +30
            }

            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                eastWestTricks.Add(card); // +30 => 122
            }

            var announces = new List<Announce>
            {
                new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Heart, CardType.Queen)) { IsActive = true, Player = PlayerPosition.South },
            };

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.Hearts),
                southNorthTricks,
                eastWestTricks,
                announces,
                hanging,
                PlayerPosition.West);

            Assert.Equal(50, score.SouthNorthTotalInRoundPoints); // 30 + belote 20
            Assert.Equal(132, score.EastWestTotalInRoundPoints); // 122 + last 10
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(expectedEastWestPoints, score.EastWestPoints);
            Assert.Equal(0, score.HangingPoints);
        }

        // A made suit contract at the x6/y6 boundary: the winner rounds down, the loser rounds
        // up (86-76 => 8-8), and hanging points go to the round's winner.
        [Theory]
        [InlineData(0, 8, 8)]
        [InlineData(9, 17, 8)]
        public void MadeSuitContractAtTheX6BoundaryRoundsWinnerDownLoserUp(
            int hanging,
            int expectedSouthNorthPoints,
            int expectedEastWestPoints)
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart); // 62
            southNorthTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Ace)); // +11
            southNorthTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Queen)); // +3 => 76

            var eastWestTricks = AllOfSuit(CardSuit.Spade); // 30
            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                eastWestTricks.Add(card); // +30
            }

            foreach (var card in AllOfSuit(CardSuit.Diamond))
            {
                if (card.Type != CardType.Ace && card.Type != CardType.Queen)
                {
                    eastWestTricks.Add(card); // +16 => 76
                }
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.Hearts),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                hanging,
                PlayerPosition.South);

            Assert.Equal(86, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(76, score.EastWestTotalInRoundPoints);
            Assert.Equal(expectedSouthNorthPoints, score.SouthNorthPoints);
            Assert.Equal(expectedEastWestPoints, score.EastWestPoints);
            Assert.Equal(0, score.HangingPoints);
        }

        // The all-trumps x4/y4 boundary: 134-124 must score 13-13 so the board total stays 26.
        [Fact]
        public void MadeAllTrumpsAtTheX4BoundaryScores13To13()
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart);
            foreach (var card in AllOfSuit(CardSuit.Spade))
            {
                southNorthTricks.Add(card); // 124
            }

            var eastWestTricks = AllOfSuit(CardSuit.Diamond);
            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                eastWestTricks.Add(card); // 124
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.AllTrumps),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                0,
                PlayerPosition.South);

            Assert.Equal(134, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(124, score.EastWestTotalInRoundPoints);
            Assert.Equal(13, score.SouthNorthPoints);
            Assert.Equal(13, score.EastWestPoints);
        }

        // A doubled contract won by the DEFENDERS: they record the whole pot doubled.
        [Fact]
        public void DoubledContractWonByTheDefendersPaysTheWholePotDoubled()
        {
            var eastWestTricks = AllOfSuit(CardSuit.Heart); // 62
            eastWestTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Ace)); // +11
            eastWestTricks.Add(Card.GetCard(CardSuit.Diamond, CardType.Queen)); // +3 => 76

            var southNorthTricks = AllOfSuit(CardSuit.Spade); // 30
            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                southNorthTricks.Add(card); // +30
            }

            foreach (var card in AllOfSuit(CardSuit.Diamond))
            {
                if (card.Type != CardType.Ace && card.Type != CardType.Queen)
                {
                    southNorthTricks.Add(card); // +16 => 76
                }
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.Hearts | BidType.Double),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                0,
                PlayerPosition.East);

            Assert.Equal(76, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(86, score.EastWestTotalInRoundPoints);
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(32, score.EastWestPoints); // 162 -> 16, doubled
        }

        // A redoubled contract that ends level: the whole quadrupled pot hangs, nobody banks.
        [Theory]
        [InlineData(0, 64)]
        [InlineData(10, 74)]
        public void RedoubledEqualRoundHangsTheQuadrupledPot(int hanging, int expectedHangingPoints)
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart); // 62
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.King)); // +4
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.Queen)); // +3
            southNorthTricks.Add(Card.GetCard(CardSuit.Spade, CardType.Jack)); // +2 => 71

            var eastWestTricks = new CardCollection();
            foreach (var card in AllOfSuit(CardSuit.Spade))
            {
                if (card.Type != CardType.King && card.Type != CardType.Queen && card.Type != CardType.Jack)
                {
                    eastWestTricks.Add(card); // 21
                }
            }

            foreach (var card in AllOfSuit(CardSuit.Diamond))
            {
                eastWestTricks.Add(card); // +30
            }

            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                eastWestTricks.Add(card); // +30 => 81
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.East, BidType.Hearts | BidType.ReDouble),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                hanging,
                PlayerPosition.South);

            Assert.Equal(81, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(81, score.EastWestTotalInRoundPoints);
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(0, score.EastWestPoints);
            Assert.Equal(expectedHangingPoints, score.HangingPoints);
        }

        // No trumps doubles the card points and the last ten inside the doubling.
        [Fact]
        public void NoTrumpsDoublesCardsAndLastTen()
        {
            var southNorthTricks = AllOfSuit(CardSuit.Heart); // 30 plain
            foreach (var card in AllOfSuit(CardSuit.Spade))
            {
                southNorthTricks.Add(card); // +30
            }

            var eastWestTricks = AllOfSuit(CardSuit.Diamond); // 30
            foreach (var card in AllOfSuit(CardSuit.Club))
            {
                eastWestTricks.Add(card); // +30
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.NoTrumps),
                southNorthTricks,
                eastWestTricks,
                new List<Announce>(),
                0,
                PlayerPosition.South);

            Assert.Equal(140, score.SouthNorthTotalInRoundPoints); // (60 + 10) x2
            Assert.Equal(120, score.EastWestTotalInRoundPoints); // 60 x2
            Assert.Equal(14, score.SouthNorthPoints);
            Assert.Equal(12, score.EastWestPoints);
        }

        // The belote participates in the made/inside/level decision: here it lifts the declarers
        // from inside (71 vs 91) to exactly level, so their half hangs instead of being lost.
        [Fact]
        public void BeloteCountsTowardTheEqualityDecision()
        {
            var southNorthTricks = new CardCollection
            {
                Card.GetCard(CardSuit.Heart, CardType.Jack), // 20
                Card.GetCard(CardSuit.Heart, CardType.Nine), // 14
                Card.GetCard(CardSuit.Heart, CardType.Ace), // 11
                Card.GetCard(CardSuit.Spade, CardType.Ten), // 10
                Card.GetCard(CardSuit.Spade, CardType.King), // 4
                Card.GetCard(CardSuit.Spade, CardType.Jack), // 2 => 61
            };

            var eastWestTricks = new CardCollection();
            foreach (var card in Card.AllCards)
            {
                if (!southNorthTricks.Contains(card))
                {
                    eastWestTricks.Add(card); // 91
                }
            }

            var announces = new List<Announce>
            {
                new Announce(AnnounceType.Belot, Card.GetCard(CardSuit.Heart, CardType.Queen)) { IsActive = true, Player = PlayerPosition.South },
            };

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.South, BidType.Hearts),
                southNorthTricks,
                eastWestTricks,
                announces,
                0,
                PlayerPosition.South);

            Assert.Equal(91, score.SouthNorthTotalInRoundPoints); // 61 + last 10 + belote 20
            Assert.Equal(91, score.EastWestTotalInRoundPoints);
            Assert.Equal(0, score.SouthNorthPoints);
            Assert.Equal(9, score.EastWestPoints);
            Assert.Equal(9, score.HangingPoints);
        }

        private static CardCollection AllOfSuit(CardSuit suit)
        {
            var cards = new CardCollection();
            foreach (var type in Card.AllTypes)
            {
                cards.Add(Card.GetCard(suit, type));
            }

            return cards;
        }
    }
}
