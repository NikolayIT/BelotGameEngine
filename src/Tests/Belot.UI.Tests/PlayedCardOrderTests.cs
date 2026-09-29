namespace Belot.UI.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class PlayedCardOrderTests
    {
        public PlayedCardOrderTests() => AppState.Reset();

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        public void CardsStackInPlayOrderBeforeAnimationThroughoutWholeGames(int seed) => UiThread.Run(async () =>
        {
            var (session, table, _) = GameTableTests.Table("dummy", "smart", "dummy", seed);
            using (table)
            {
                var tester = new TableTester(session, table, seed);
                var playedSeats = new List<PlayerPosition>();
                var leaders = new HashSet<PlayerPosition>();
                var renderedOrder = new int[4];
                var renderedLastOrder = new int[4];
                var cards = 0;
                var forcedCards = 0;
                var tricks = 0;
                var rounds = 0;

                foreach (var position in Seats.All)
                {
                    var seat = table.Seat(position);
                    seat.PropertyChanged += (_, change) =>
                    {
                        // Model the bindings: the page starts its animation on PlayedCard.
                        if (change.PropertyName == nameof(SeatViewModel.PlayedCardZIndex))
                        {
                            renderedOrder[position.Index()] = seat.PlayedCardZIndex;
                        }
                        else if (change.PropertyName == nameof(SeatViewModel.LastTrickZIndex))
                        {
                            renderedLastOrder[position.Index()] = seat.LastTrickZIndex;
                        }
                        else if (change.PropertyName == nameof(SeatViewModel.PlayedCard) && seat.HasPlayedCard)
                        {
                            Assert.Equal(playedSeats.Count, seat.PlayedCardZIndex);
                            Assert.Equal(playedSeats.Count, renderedOrder[position.Index()]);
                            Assert.DoesNotContain(position, playedSeats);
                            playedSeats.Add(position);
                            Assert.Equal(
                                playedSeats,
                                Seats.All.Where(s => table.Seat(s).HasPlayedCard)
                                    .OrderBy(s => table.Seat(s).PlayedCardZIndex));
                            cards++;
                        }
                    };
                }

                table.PropertyChanged += (_, change) =>
                {
                    var image = change.PropertyName switch
                    {
                        nameof(GameViewModel.LastTrickSouth) => table.LastTrickSouth,
                        nameof(GameViewModel.LastTrickEast) => table.LastTrickEast,
                        nameof(GameViewModel.LastTrickNorth) => table.LastTrickNorth,
                        nameof(GameViewModel.LastTrickWest) => table.LastTrickWest,
                        _ => null,
                    };
                    if (image == null)
                    {
                        return;
                    }

                    // All miniature layers must be ready before any image is replaced.
                    Assert.Equal(4, playedSeats.Count);
                    for (var index = 0; index < playedSeats.Count; index++)
                    {
                        Assert.Equal(index, table.Seat(playedSeats[index]).LastTrickZIndex);
                        Assert.Equal(index, renderedLastOrder[playedSeats[index].Index()]);
                    }
                };
                session.CardPlayed += played =>
                {
                    Assert.Equal(played.IndexInTrick, table.Seat(played.Seat).PlayedCardZIndex);
                    Assert.Same(played.Card, table.Seat(played.Seat).PlayedCard!.Card);
                    if (played.IsAuto)
                    {
                        forcedCards++;
                    }
                };
                session.TrickCollected += trick =>
                {
                    Assert.Equal(trick.Cards.Select(c => c.Seat), playedSeats);
                    leaders.Add(trick.Cards[0].Seat);
                    for (var index = 0; index < trick.Cards.Count; index++)
                    {
                        var played = trick.Cards[index];
                        Assert.Equal(index, table.Seat(played.Seat).LastTrickZIndex);
                        Assert.Same(played.Card, LastTrickCard(table, played.Seat)!.Card);
                    }

                    AssertCurrentTrickCleared(table);
                    playedSeats.Clear();
                    tricks++;
                };
                session.RoundStarted += _ =>
                {
                    AssertCurrentTrickCleared(table);
                    AssertLastTrickCleared(table);
                    Assert.Empty(playedSeats);
                    rounds++;
                };

                table.StartGame();
                await tester.PlayToTheEndAsync();
                table.PlayAgainCommand.Execute(null);
                await tester.PlayToTheEndAsync();

                Assert.Equal(4, leaders.Count);
                Assert.True(rounds > 2);
                Assert.True(tricks > 16);
                Assert.Equal(tricks * 4, cards);
                Assert.InRange(forcedCards, 1, cards - 1);
            }
        });

        [Fact]
        public void RestartClearsBothTrickOrdersBeforeTheNewGameStarts() => UiThread.Run(async () =>
        {
            var (session, table, _) = GameTableTests.Table("dummy", "dummy", "dummy", seed: 5);
            using (table)
            {
                var driver = new TableDriver(session, 5);
                var restarted = false;
                session.CardPlayed += played =>
                {
                    if (restarted || played.TrickNumber != 2 || played.IndexInTrick != 2)
                    {
                        return;
                    }

                    Assert.True(table.HasLastTrick);
                    Assert.Equal(3, Seats.All.Count(s => table.Seat(s).HasPlayedCard));
                    Assert.Equal(2, table.Seat(played.Seat).PlayedCardZIndex);
                    restarted = true;
                    session.Restart();
                    AssertCurrentTrickCleared(table);
                    AssertLastTrickCleared(table);
                };

                await driver.PlayToTheEndAsync();
                Assert.True(restarted);
            }
        });

        private static CardSlot? LastTrickCard(GameViewModel table, PlayerPosition seat) => seat switch
        {
            PlayerPosition.South => table.LastTrickSouth,
            PlayerPosition.East => table.LastTrickEast,
            PlayerPosition.North => table.LastTrickNorth,
            _ => table.LastTrickWest,
        };

        private static void AssertCurrentTrickCleared(GameViewModel table)
        {
            foreach (var position in Seats.All)
            {
                var seat = table.Seat(position);
                Assert.False(seat.HasPlayedCard);
                Assert.Null(seat.PlayedCard);
                Assert.Equal(0, seat.PlayedCardZIndex);
            }
        }

        private static void AssertLastTrickCleared(GameViewModel table)
        {
            Assert.False(table.HasLastTrick);
            foreach (var position in Seats.All)
            {
                Assert.Null(LastTrickCard(table, position));
                Assert.Equal(0, table.Seat(position).LastTrickZIndex);
            }
        }
    }
}
