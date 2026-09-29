namespace Belot.UI.Tests
{
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.Engine.GameMechanics;
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class ManualHumanCardTests
    {
        public ManualHumanCardTests() => AppState.Reset();

        [Theory]
        [InlineData(5)]
        [InlineData(13)]
        public void EveryHumanCardWaitsForATapIncludingForcedAndLastCards(int seed) => UiThread.Run(async () =>
        {
            var (session, table, _) = GameTableTests.Table("dummy", "smart", "dummy", seed);
            using (table)
            {
                var tester = new TableTester(session, table, seed);
                var decisions = 0;
                var played = 0;
                var forced = 0;
                var lastCards = 0;
                var autoComputerCards = 0;
                session.CardPlayed += card =>
                {
                    if (card.Seat == Seats.Person)
                    {
                        Assert.False(card.IsAuto);
                        Assert.Equal(++played, decisions);
                    }
                    else if (card.IsAuto)
                    {
                        autoComputerCards++;
                    }
                };
                tester.OnDecision = async decision =>
                {
                    if (decision != BelotDecision.PlayCard)
                    {
                        return;
                    }

                    decisions++;
                    var before = session.GetView(Seats.Person)!;
                    if (before.PlayableCards.Count != 1)
                    {
                        return;
                    }

                    forced++;
                    lastCards += before.Hand.Count == 1 ? 1 : 0;
                    var countBefore = played;
                    var onlyCard = Assert.Single(table.MyHand, slot => slot.IsPlayable);
                    Assert.Same(before.PlayableCards[0], onlyCard.Card);

                    // Give queued continuations time to run without tapping the only legal card.
                    await Task.Delay(5);
                    Assert.Equal(countBefore, played);
                    Assert.Equal(BelotDecision.PlayCard, session.AwaitedDecision);
                    Assert.Equal(before.Hand, session.GetView(Seats.Person)!.Hand);
                    Assert.True(table.IsMyTurn);
                    Assert.Contains(onlyCard, table.MyHand);
                };

                // Restart uses the same manual-seat option as the first game.
                for (var game = 0; game < 2; game++)
                {
                    var beforeDecisions = decisions;
                    var beforeLastCards = lastCards;
                    if (game == 0)
                    {
                        table.StartGame();
                    }
                    else
                    {
                        table.PlayAgainCommand.Execute(null);
                    }

                    await tester.PlayToTheEndAsync();
                    var rounds = session.GetRecord()!.Rounds.Count(r => r.Tricks.Count == 8);
                    Assert.Equal(rounds * 8, decisions - beforeDecisions);
                    Assert.Equal(rounds, lastCards - beforeLastCards);
                    Assert.Equal(decisions, played);
                }

                Assert.True(forced > lastCards, "A single legal card must also wait before the last trick.");
                Assert.True(autoComputerCards > 0, "Computer forced moves must remain automatic.");
            }
        });
    }
}
