namespace Belot.Engine.Tests
{
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    /// <summary>
    /// The match-level rules of BelotGame (winning at 151, level scores, "С капо не се излиза",
    /// passed-out deals, hanging points and the dealer rotation), driven by scripted round
    /// results instead of real deals so every scenario is exact.
    /// </summary>
    public class BelotGameRulesTests
    {
        [Fact]
        public void TheGameEndsAsSoonAsATeamReaches151AndLeads()
        {
            var game = new ScriptedGame(Round(100, 60), Round(51, 10));

            var result = game.Play();

            Assert.Equal(2, result.RoundsPlayed);
            Assert.Equal(151, result.SouthNorthPoints);
            Assert.Equal(70, result.EastWestPoints);
            Assert.Equal(PlayerPosition.SouthNorthTeam, result.Winner);
        }

        [Fact]
        public void OneHundredAndFiftyIsNotEnough()
        {
            var game = new ScriptedGame(Round(150, 0), Round(0, 16), Round(6, 10));

            var result = game.Play();

            Assert.Equal(3, result.RoundsPlayed);
            Assert.Equal(156, result.SouthNorthPoints);
            Assert.Equal(26, result.EastWestPoints);
        }

        [Fact]
        public void EastWestWinTheSameWay()
        {
            var game = new ScriptedGame(Round(60, 140), Round(0, 26));

            var result = game.Play();

            Assert.Equal(2, result.RoundsPlayed);
            Assert.Equal(166, result.EastWestPoints);
            Assert.Equal(PlayerPosition.EastWestTeam, result.Winner);
        }

        [Fact]
        public void WhenBothTeamsPass151TogetherTheHigherScoreWins()
        {
            var game = new ScriptedGame(Round(140, 145), Round(16, 10));

            var result = game.Play();

            Assert.Equal(2, result.RoundsPlayed);
            Assert.Equal(156, result.SouthNorthPoints);
            Assert.Equal(155, result.EastWestPoints);
            Assert.Equal(PlayerPosition.SouthNorthTeam, result.Winner);
        }

        [Fact]
        public void LevelScoresOver151KeepThePlayGoingUntilSomeoneLeads()
        {
            var game = new ScriptedGame(Round(140, 144), Round(16, 12), Round(10, 16));

            var result = game.Play();

            Assert.Equal(3, result.RoundsPlayed);
            Assert.Equal(166, result.SouthNorthPoints);
            Assert.Equal(172, result.EastWestPoints);
            Assert.Equal(PlayerPosition.EastWestTeam, result.Winner);
        }

        [Fact]
        public void TheGameCannotBeWonOnACapotDeal()
        {
            // "С капо не се излиза": South-North pass 151 with a capot, so one more deal is played.
            var game = new ScriptedGame(Round(130, 60), Capot(25, 0), Round(10, 6));

            var result = game.Play();

            Assert.Equal(3, result.RoundsPlayed);
            Assert.Equal(165, result.SouthNorthPoints);
            Assert.Equal(66, result.EastWestPoints);
            Assert.Equal(PlayerPosition.SouthNorthTeam, result.Winner);
        }

        [Fact]
        public void ACapotDealDoesNotEndTheGameForTheOtherTeamEither()
        {
            // East-West take every trick of South-North's contract and pass 151 on it.
            var game = new ScriptedGame(Round(60, 130), Capot(0, 25), Round(6, 10));

            var result = game.Play();

            Assert.Equal(3, result.RoundsPlayed);
            Assert.Equal(165, result.EastWestPoints);
            Assert.Equal(PlayerPosition.EastWestTeam, result.Winner);
        }

        [Fact]
        public void TheExtraDealAfterACapotCanHandTheGameToTheOtherTeam()
        {
            var game = new ScriptedGame(Round(130, 140), Capot(25, 0), Round(0, 26));

            var result = game.Play();

            Assert.Equal(3, result.RoundsPlayed);
            Assert.Equal(155, result.SouthNorthPoints);
            Assert.Equal(166, result.EastWestPoints);
            Assert.Equal(PlayerPosition.EastWestTeam, result.Winner);
        }

        [Fact]
        public void APassedOutDealIsNotTheExtraDealAfterACapot()
        {
            var game = new ScriptedGame(Round(130, 60), Capot(25, 0), PassedOut(), Round(10, 6));

            var result = game.Play();

            Assert.Equal(4, result.RoundsPlayed);
            Assert.Equal(165, result.SouthNorthPoints);
        }

        [Fact]
        public void ASecondCapotIsNotTheExtraDealEither()
        {
            var game = new ScriptedGame(Round(130, 60), Capot(25, 0), Capot(25, 0), Round(10, 6));

            var result = game.Play();

            Assert.Equal(4, result.RoundsPlayed);
            Assert.Equal(190, result.SouthNorthPoints);
        }

        [Fact]
        public void AfterACapotTheLeaderMustScoreInTheDealThatEndsTheGame()
        {
            // South-North reach 160 with a capot (East-West 100). In the next deal only East-West
            // score (160-126): South-North lead with 151+ but wrote nothing, so the play goes on
            // until a deal without a capot in which the leader scores.
            var game = new ScriptedGame(Round(135, 100), Capot(25, 0), Round(0, 26), Round(10, 6));

            var result = game.Play();

            Assert.Equal(4, result.RoundsPlayed);
            Assert.Equal(170, result.SouthNorthPoints);
            Assert.Equal(132, result.EastWestPoints);
            Assert.Equal(PlayerPosition.SouthNorthTeam, result.Winner);
        }

        [Fact]
        public void ALeaderWhoScoresNothingInADoubledLevelDealHasNotWonYet()
        {
            // A level doubled deal hangs the whole pot: nobody scores, so it cannot end the game.
            var doubledLevel = new RoundResult(new Bid(PlayerPosition.East, BidType.Hearts | BidType.Double))
            {
                HangingPoints = 32,
            };
            var game = new ScriptedGame(Round(130, 60), Capot(25, 0), doubledLevel, Round(42, 6));

            var result = game.Play();

            Assert.Equal(4, result.RoundsPlayed);
            Assert.Equal(197, result.SouthNorthPoints);
            Assert.Equal(new[] { 0, 0, 0, 32 }, game.HangingPointsIn);
        }

        [Fact]
        public void HangingPointsAreHandedToTheNextDealsAndThroughPassedOutOnes()
        {
            // RoundManager returns the carried hanging points on a passed-out deal, and the game
            // must hand whatever the last deal left hanging to the next one.
            var game = new ScriptedGame(
                Round(0, 8, hanging: 8),
                PassedOut(hanging: 8),
                Round(24, 10),
                Round(150, 0));

            game.Play();

            Assert.Equal(new[] { 0, 8, 8, 0 }, game.HangingPointsIn);
        }

        [Fact]
        public void EveryDealGetsTheNextFirstPlayerAndTheRunningScore()
        {
            var game = new ScriptedGame(Round(10, 6), PassedOut(), Round(0, 16), Round(6, 10), Round(151, 0));

            var result = game.Play(PlayerPosition.East);

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, game.RoundNumbersIn);
            Assert.Equal(
                new[] { PlayerPosition.East, PlayerPosition.North, PlayerPosition.West, PlayerPosition.South, PlayerPosition.East },
                game.FirstToPlayIn);
            Assert.Equal(new[] { 0, 10, 10, 10, 16 }, game.SouthNorthPointsIn);
            Assert.Equal(new[] { 0, 6, 6, 22, 32 }, game.EastWestPointsIn);
            Assert.Equal(5, result.RoundsPlayed);
            Assert.Equal(167, result.SouthNorthPoints);
            Assert.Equal(32, result.EastWestPoints);
        }

        [Fact]
        public void EveryPlayerIsToldTheFinalResultOnce()
        {
            var players = new[]
            {
                new SeededRandomPlayer(1), new SeededRandomPlayer(2), new SeededRandomPlayer(3), new SeededRandomPlayer(4),
            };
            var game = new ScriptedGame(players, Round(80, 80), Round(80, 0));

            var result = game.Play();

            foreach (var player in players)
            {
                Assert.Equal(1, player.EndOfGameCalls);
                Assert.Same(result, player.LastGameResult);
            }
        }

        private static RoundResult Round(int southNorthPoints, int eastWestPoints, int hanging = 0) =>
            new RoundResult(new Bid(PlayerPosition.South, BidType.Hearts))
            {
                SouthNorthPoints = southNorthPoints,
                EastWestPoints = eastWestPoints,
                HangingPoints = hanging,
            };

        private static RoundResult Capot(int southNorthPoints, int eastWestPoints)
        {
            var round = Round(southNorthPoints, eastWestPoints);
            round.NoTricksForOneOfTheTeams = true;
            return round;
        }

        private static RoundResult PassedOut(int hanging = 0) =>
            new RoundResult(new Bid(PlayerPosition.South, BidType.Pass)) { HangingPoints = hanging };

        private sealed class ScriptedGame
        {
            private readonly IPlayer[] players;
            private readonly Queue<RoundResult> rounds;

            public ScriptedGame(params RoundResult[] rounds)
                : this(
                    new IPlayer[]
                    {
                        new SeededRandomPlayer(1), new SeededRandomPlayer(2), new SeededRandomPlayer(3), new SeededRandomPlayer(4),
                    },
                    rounds)
            {
            }

            public ScriptedGame(IPlayer[] players, params RoundResult[] rounds)
            {
                this.players = players;
                this.rounds = new Queue<RoundResult>(rounds);
            }

            public List<int> RoundNumbersIn { get; } = new List<int>();

            public List<PlayerPosition> FirstToPlayIn { get; } = new List<PlayerPosition>();

            public List<int> SouthNorthPointsIn { get; } = new List<int>();

            public List<int> EastWestPointsIn { get; } = new List<int>();

            public List<int> HangingPointsIn { get; } = new List<int>();

            public GameResult Play(PlayerPosition firstToPlay = PlayerPosition.South)
            {
                var game = new BelotGame(this.players, this.PlayRound);
                var result = game.PlayGame(firstToPlay);
                Assert.Empty(this.rounds);
                return result;
            }

            private RoundResult PlayRound(
                int roundNumber,
                PlayerPosition firstToPlay,
                int southNorthPoints,
                int eastWestPoints,
                int hangingPoints)
            {
                // Running out of script means the game went on after it should have ended.
                Assert.NotEmpty(this.rounds);
                this.RoundNumbersIn.Add(roundNumber);
                this.FirstToPlayIn.Add(firstToPlay);
                this.SouthNorthPointsIn.Add(southNorthPoints);
                this.EastWestPointsIn.Add(eastWestPoints);
                this.HangingPointsIn.Add(hangingPoints);
                return this.rounds.Dequeue();
            }
        }
    }
}
