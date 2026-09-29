namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class ManualCardPlayTests
    {
        [Theory]
        [InlineData(PlayerPosition.South)]
        [InlineData(PlayerPosition.East)]
        [InlineData(PlayerPosition.North)]
        [InlineData(PlayerPosition.West)]
        public void ManualSeatMustPlayAllEightCardsWithoutChangingTheGame(PlayerPosition seat)
        {
            var belotes = 0;
            foreach (var contract in new[] { BidType.Clubs, BidType.NoTrumps, BidType.AllTrumps })
            {
                for (var seed = 0; seed < 6; seed++)
                {
                    var baseline = Play(seed, contract, seat, PlayerPosition.Unknown);
                    var manual = Play(seed, contract, seat, seat);
                    Assert.Equal(JsonSerializer.Serialize(baseline.Record), JsonSerializer.Serialize(manual.Record));
                    Assert.Equal(manual.Record.Rounds.Count * 8, manual.CardDecisions);
                    Assert.Equal(manual.Record.Rounds.Count, manual.LastCardDecisions);
                    Assert.True(manual.ForcedCards.Count > 0);
                    Assert.True(baseline.CardDecisions < manual.CardDecisions);

                    foreach (var (round, trick, card) in manual.ForcedCards)
                    {
                        var played = manual.Record.Rounds[round - 1].Tricks[trick - 1].Cards.Single(c => c.Player == seat);
                        Assert.Same(card, played.Card);
                        Assert.False(played.Belote);
                    }

                    belotes += manual.Record.Rounds.SelectMany(r => r.Announces).Count(a => a.Type == AnnounceType.Belot);
                }
            }

            // Explicit card play still claims valid belotes; waiting does not change scoring.
            Assert.True(belotes > 0);
        }

        [Fact]
        public void ManualCardPlayIsInternalAndDisabledByDefault()
        {
            Assert.Equal(PlayerPosition.Unknown, new BelotMatchOptions().ManualCardPlaySeats);
            Assert.Null(typeof(BelotMatchOptions).GetProperty("ManualCardPlaySeats"));
        }

        private static PlayedMatch Play(int seed, BidType contract, PlayerPosition seat, PlayerPosition manualSeats)
        {
            var match = new BelotMatch(new BelotMatchOptions
            {
                FirstToPlay = seat,
                Random = new Random(seed),
                ManualCardPlaySeats = manualSeats,
            });
            var cardDecisions = 0;
            var lastCardDecisions = 0;
            var forced = new List<(int Round, int Trick, Card Card)>();
            match.Start();
            while (!match.IsFinished)
            {
                var view = match.GetView(match.ToMove);
                BelotAction action;
                switch (match.Decision)
                {
                    case BelotDecision.Bid:
                        action = BelotAction.Bid(view.Contract.Type == BidType.Pass ? contract : BidType.Pass);
                        break;
                    case BelotDecision.Announce:
                        action = BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces);
                        break;
                    default:
                        var card = view.PlayableCards[0];
                        if (match.ToMove == seat)
                        {
                            cardDecisions++;
                            if (view.Hand.Count == 1)
                            {
                                lastCardDecisions++;
                            }
                        }

                        if (view.PlayableCards.Count == 1)
                        {
                            Assert.NotEqual(PlayerPosition.Unknown, manualSeats & match.ToMove);
                            forced.Add((view.RoundNumber, view.Tricks.Count + 1, card));
                            Assert.Single(match.CreatePlayCardContext().AvailableCardsToPlay);

                            // A refused tap must leave even a forced decision awaiting the player.
                            var invalid = Card.AllCards.First(c => !view.Hand.Contains(c));
                            Assert.Equal(BelotActResult.InvalidAction, match.Act(match.ToMove, BelotAction.PlayCard(invalid)));
                            Assert.Equal(BelotDecision.PlayCard, match.Decision);
                            Assert.Equal(view.Hand, match.GetView(match.ToMove).Hand);
                        }

                        action = BelotAction.PlayCard(card, true);
                        break;
                }

                Assert.Equal(BelotActResult.Ok, match.Act(match.ToMove, action));
            }

            return new PlayedMatch(match.GetRecord(), cardDecisions, lastCardDecisions, forced);
        }

        private sealed record PlayedMatch(
            BelotMatchRecord Record,
            int CardDecisions,
            int LastCardDecisions,
            IReadOnlyList<(int Round, int Trick, Card Card)> ForcedCards);
    }
}
