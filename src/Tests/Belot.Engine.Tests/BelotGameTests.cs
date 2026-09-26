namespace Belot.Engine.Tests
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    public class BelotGameTests
    {
        // Invariant tests: whatever the shuffles produce, a finished game must have a strict
        // winner with at least 151 points, a consistent Winner property, and exactly one
        // EndOfGame callback per player carrying the same result object.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(42)]
        public void PlayGameEndsWithAStrictWinnerOver151(int seed)
        {
            var south = new SeededRandomPlayer((seed * 4) + 0, bidRandomly: true);
            var east = new SeededRandomPlayer((seed * 4) + 1, bidRandomly: true);
            var north = new SeededRandomPlayer((seed * 4) + 2, bidRandomly: true);
            var west = new SeededRandomPlayer((seed * 4) + 3, bidRandomly: true);

            var game = new BelotGame(south, east, north, west);
            var result = game.PlayGame(PlayerPosition.South);

            var winnerPoints = Math.Max(result.SouthNorthPoints, result.EastWestPoints);
            var loserPoints = Math.Min(result.SouthNorthPoints, result.EastWestPoints);
            Assert.True(winnerPoints >= 151, $"The winner finished with only {winnerPoints} points.");
            Assert.True(winnerPoints > loserPoints, "A finished game cannot end level.");
            Assert.True(result.RoundsPlayed >= 1);
            Assert.Equal(
                result.SouthNorthPoints > result.EastWestPoints
                    ? PlayerPosition.SouthNorthTeam
                    : PlayerPosition.EastWestTeam,
                result.Winner);

            foreach (var player in new[] { south, east, north, west })
            {
                Assert.Equal(1, player.EndOfGameCalls);
                Assert.Same(result, player.LastGameResult);
            }
        }

        [Fact]
        public void ASeededGameIsReproducible()
        {
            var first = PlaySeeded(3, playerSeed: 10, out _);
            var second = PlaySeeded(3, playerSeed: 10, out _);

            Assert.Equal(first.SouthNorthPoints, second.SouthNorthPoints);
            Assert.Equal(first.EastWestPoints, second.EastWestPoints);
            Assert.Equal(first.RoundsPlayed, second.RoundsPlayed);
        }

        [Fact]
        public void TheSameSeedDealsTheSameCardsWhateverThePlayersDo()
        {
            // Differently playing players make the games differ, not the deals: a mirror match
            // (the same seed with the teams swapped) replays the same deals.
            PlaySeeded(5, playerSeed: 1, out var firstDeals);
            PlaySeeded(5, playerSeed: 2, out var secondDeals);

            // A seat whose only possible bid is Pass is not asked, so compare what both saw.
            var compared = 0;
            foreach (var pair in firstDeals)
            {
                if (secondDeals.TryGetValue(pair.Key, out var hand))
                {
                    Assert.Equal(pair.Value, hand);
                    compared++;
                }
            }

            Assert.True(compared >= 8);
        }

        private static GameResult PlaySeeded(int deckSeed, int playerSeed, out Dictionary<(int Round, int Seat), string> deals)
        {
            var recorded = new Dictionary<(int Round, int Seat), string>();
            var players = new IPlayer[4];
            for (var i = 0; i < 4; i++)
            {
                players[i] = new DealRecorder(new SeededRandomPlayer((playerSeed * 4) + i, bidRandomly: true), recorded, i);
            }

            var result = new BelotGame(players[0], players[1], players[2], players[3], new Random(deckSeed)).PlayGame();
            deals = recorded;
            return result;
        }

        // Records each seat's five cards at its first bid of every deal.
        private class DealRecorder : IPlayer
        {
            private readonly IPlayer inner;
            private readonly Dictionary<(int Round, int Seat), string> deals;
            private readonly int seat;

            public DealRecorder(IPlayer inner, Dictionary<(int Round, int Seat), string> deals, int seat)
            {
                this.inner = inner;
                this.deals = deals;
                this.seat = seat;
            }

            public BidType GetBid(PlayerGetBidContext context)
            {
                this.deals.TryAdd((context.RoundNumber, this.seat), string.Join(",", context.MyCards));

                return this.inner.GetBid(context);
            }

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.inner.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.inner.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.inner.EndOfTrick(trickActions);

            public void EndOfRound(RoundResult roundResult) => this.inner.EndOfRound(roundResult);

            public void EndOfGame(GameResult gameResult) => this.inner.EndOfGame(gameResult);
        }
    }
}
