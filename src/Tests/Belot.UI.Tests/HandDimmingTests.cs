namespace Belot.UI.Tests
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class HandDimmingTests
    {
        public HandDimmingTests() => AppState.Reset();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OnlyIllegalCardsDimOnOurCardTurnAndAcceptedMovesClearThemImmediately(bool assists) => UiThread.Run(async () =>
        {
            AppSettings.AssistsEnabled = assists;
            var (session, table, _) = GameTableTests.Table("dummy", "dummy", "dummy", seed: 5);
            using (table)
            {
                var driver = new TableDriver(session, 5) { AutoPlay = false, AutoContinue = false };
                var decisions = new HashSet<BelotDecision>();
                var restrictedTurns = 0;
                var opponentTurnsWithCards = 0;
                driver.Turn += turn =>
                {
                    if (!turn.IsHuman)
                    {
                        AssertHandUndimmed(table);
                        if (table.MyHand.Count > 0)
                        {
                            opponentTurnsWithCards++;
                        }

                        return;
                    }

                    decisions.Add(turn.Decision);
                    switch (turn.Decision)
                    {
                        case BelotDecision.Bid:
                            AssertHandUndimmed(table);
                            table.BidCommand.Execute(table.BidOptions.First(o => o.IsEnabled && o.Bid != BidType.Pass));
                            break;
                        case BelotDecision.Announce:
                            AssertHandUndimmed(table);
                            table.DeclareCommand.Execute(null);
                            break;
                        default:
                            if (CheckCardTurnAndPlay(session, table))
                            {
                                restrictedTurns++;
                            }

                            break;
                    }

                    // Still in the synchronous command call: no engine continuation has run.
                    Assert.False(table.IsMyTurn);
                    AssertHandUndimmed(table);
                };
                session.RoundStarted += _ => AssertHandUndimmed(table);
                session.ContractSettled += _ => AssertHandUndimmed(table);
                session.CardPlayed += _ => AssertHandUndimmed(table);
                session.RoundFinished += _ =>
                {
                    AssertHandUndimmed(table);
                    table.RoundOverlayContinueCommand.Execute(null);
                };

                await driver.PlayToTheEndAsync();

                Assert.True(restrictedTurns > 0);
                Assert.True(opponentTurnsWithCards > 0);
                Assert.Contains(BelotDecision.Bid, decisions);
                Assert.Contains(BelotDecision.Announce, decisions);
                Assert.Contains(BelotDecision.PlayCard, decisions);
            }
        });

        private static bool CheckCardTurnAndPlay(GameSession session, GameViewModel table)
        {
            var view = session.GetView(Seats.Person)!;
            Assert.NotEmpty(view.PlayableCards);
            Assert.All(table.MyHand, slot =>
            {
                var legal = view.PlayableCards.Contains(slot.Card!);
                Assert.Equal(legal, slot.IsPlayable);
                Assert.Equal(!legal, slot.IsDimmed);
                Assert.False(slot.IsFaceDown);
            });

            var illegal = table.MyHand.FirstOrDefault(slot => slot.IsDimmed);
            if (illegal != null)
            {
                var before = table.MyHand.Select(slot => (slot.Card, slot.IsDimmed)).ToArray();
                table.TapCardCommand.Execute(illegal);
                Assert.True(table.IsMyTurn);
                Assert.True(session.IsAwaiting(BelotDecision.PlayCard));
                Assert.Equal(before, table.MyHand.Select(slot => (slot.Card, slot.IsDimmed)));
            }

            var hand = table.MyHand.ToArray();
            var renderedDimming = hand.ToDictionary(slot => slot, slot => slot.IsDimmed);
            void TrackDimming(object? sender, PropertyChangedEventArgs change)
            {
                if (change.PropertyName == nameof(CardSlot.IsDimmed) && sender is CardSlot slot)
                {
                    renderedDimming[slot] = slot.IsDimmed;
                }
            }

            foreach (var slot in hand)
            {
                slot.PropertyChanged += TrackDimming;
            }

            try
            {
                table.TapCardCommand.Execute(hand.First(slot => slot.IsPlayable));
                Assert.False(table.IsMyTurn);
                Assert.False(session.IsAwaiting(BelotDecision.PlayCard));
                AssertHandUndimmed(table);
                Assert.All(renderedDimming.Values, dimmed => Assert.False(dimmed));
            }
            finally
            {
                foreach (var slot in hand)
                {
                    slot.PropertyChanged -= TrackDimming;
                }
            }

            return illegal != null;
        }

        private static void AssertHandUndimmed(GameViewModel table) => Assert.All(table.MyHand, slot =>
        {
            Assert.True(slot.IsPlayable);
            Assert.False(slot.IsDimmed);
            Assert.False(slot.IsFaceDown);
        });
    }
}
