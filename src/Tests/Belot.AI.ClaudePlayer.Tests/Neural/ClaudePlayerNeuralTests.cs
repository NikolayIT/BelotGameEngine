namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.AI.DummyPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class ClaudePlayerNeuralTests
    {
        [Fact]
        public void PlaysWholeGamesAtEverySeatWithoutFallingBack()
        {
            var models = RandomModels.Create(1);
            for (var game = 0; game < 40; game++)
            {
                var neural = Enumerable.Range(0, 2).Select(_ => new ClaudePlayerNeural(models)).ToArray();
                IPlayer other = game % 2 == 0 ? new SmartPlayer.SmartPlayer() : new RandomPlayer(new Random(game));
                var players = game % 4 < 2
                    ? new IPlayer[] { neural[0], other, neural[1], other }
                    : new IPlayer[] { other, neural[0], other, neural[1] };
                new BelotGame(players[0], players[1], players[2], players[3], new Random(game)).PlayGame((PlayerPosition)(1 << (game % 4)));
                Assert.All(neural, x => Assert.Equal(0, x.Fallbacks));
            }
        }

        // The values cover every legal action once, best first, and the player takes the best.
        [Fact]
        public void ValuesEveryActionAndTakesTheBest()
        {
            var player = new ClaudePlayerNeural(RandomModels.Create(2));
            var cards = 0;
            var bids = 0;
            ForEveryDecision(
                seed: 3,
                matches: 6,
                onBid: context =>
                {
                    var values = player.EvaluateBids(context);
                    var expected = Enumerable.Range(0, 8).Select(x => (BidType)(1 << x)).Where(x => context.AvailableBids.HasFlag(x)).Append(BidType.Pass);
                    Assert.Equal(expected.OrderBy(x => x), values.Select(x => x.Bid).OrderBy(x => x));
                    Assert.Equal(values.OrderByDescending(x => x.Value).Select(x => x.Value), values.Select(x => x.Value));
                    Assert.Equal(values[0].Value, values.First(x => x.Bid == player.GetBid(context)).Value);
                    bids++;
                },
                onCard: context =>
                {
                    var values = player.EvaluateCards(context);
                    Assert.Equal(context.AvailableCardsToPlay.OrderBy(x => x.GetHashCode()), values.Select(x => x.Card).OrderBy(x => x.GetHashCode()));
                    Assert.Equal(values.OrderByDescending(x => x.Value).Select(x => x.Value), values.Select(x => x.Value));
                    Assert.Equal(values[0].Value, values.First(x => x.Card == player.PlayCard(context).Card).Value);
                    cards++;
                });
            Assert.True(cards > 500 && bids > 100);
        }

        // Weaker on purpose: a temperature spreads the choices over the actions close to the
        // best, never further from it than MaxRegret; a seed repeats them.
        [Fact]
        public void TheTemperaturePicksNearlyAsGoodActions()
        {
            var models = RandomModels.Create(4);
            var weak = new ClaudePlayerNeural(models) { Temperature = 1.5, MaxRegret = 2, Rng = new Random(5) };
            var twin = new ClaudePlayerNeural(models) { Temperature = 1.5, MaxRegret = 2, Rng = new Random(5) };
            var judge = new ClaudePlayerNeural(models);
            var notBest = 0;
            ForEveryDecision(
                seed: 6,
                matches: 6,
                onBid: context => Assert.Equal(weak.GetBid(context), twin.GetBid(context)),
                onCard: context =>
                {
                    var values = judge.EvaluateCards(context);
                    var card = weak.PlayCard(context).Card;
                    Assert.Equal(card, twin.PlayCard(context).Card);
                    var value = values.First(x => x.Card == card).Value;
                    Assert.True(value >= values[0].Value - 2 - 1e-9, $"{value} vs {values[0].Value}");
                    notBest += value < values[0].Value ? 1 : 0;
                });
            Assert.True(notBest > 20);
        }

        // Plays random legal matches, handing each bid and card decision to the checks.
        private static void ForEveryDecision(int seed, int matches, Action<PlayerGetBidContext> onBid, Action<PlayerPlayCardContext> onCard)
        {
            for (var m = 0; m < matches; m++)
            {
                var random = new Random(seed + m);
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed + m) });
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    switch (match.Decision)
                    {
                        case BelotDecision.Bid:
                            var bidContext = match.CreateBidContext();
                            onBid(bidContext);
                            var bids = Enumerable.Range(0, 6).Select(x => (BidType)(1 << x)).Where(x => bidContext.AvailableBids.HasFlag(x)).ToList();
                            var bid = bids.Count > 0 && random.Next(3) == 0 ? bids[random.Next(bids.Count)] : BidType.Pass;
                            match.Act(seat, BelotAction.Bid(bid));
                            break;
                        case BelotDecision.Announce:
                            match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces));
                            break;
                        default:
                            var context = match.CreatePlayCardContext();
                            onCard(context);
                            var cards = context.AvailableCardsToPlay.ToList();
                            match.Act(seat, BelotAction.PlayCard(cards[random.Next(cards.Count)]));
                            break;
                    }
                }
            }
        }
    }
}
