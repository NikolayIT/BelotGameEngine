namespace Belot.AI.ClaudePlayer.Tests.Heuristic
{
    using System;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Heuristic;
    using Belot.AI.DummyPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class ClaudePlayerHeuristicTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void PlaysWholeMatchesAgainstOtherBotsWithoutLosingTrackOfTheCards(int endgameTricks)
        {
            for (var game = 0; game < 4; game++)
            {
                var south = Create(game, endgameTricks);
                var north = Create(game + 100, endgameTricks);
                IPlayer East() => game % 2 == 0 ? new SmartPlayer.SmartPlayer() : new DummyPlayer();
                var result = new BelotGame(south, East(), north, East(), new Random(game)).PlayGame();

                Assert.True(result.SouthNorthPoints >= 151 || result.EastWestPoints >= 151);
                Assert.Equal(0, south.Inconsistent);
                Assert.Equal(0, north.Inconsistent);
                if (endgameTricks > 0)
                {
                    Assert.True(south.EndgameDecisions + north.EndgameDecisions > 0);
                }
            }
        }

        [Fact]
        public void PlaysEveryContractAgainstRandomPlayers()
        {
            // Random bidders reach every contract, doubles included, and declare at random.
            var heuristic = Create(7, 3);
            for (var game = 0; game < 3; game++)
            {
                new BelotGame(new RandomPlayer(new Random(game)), heuristic, new RandomPlayer(new Random(game + 10)), new RandomPlayer(new Random(game + 20)), new Random(game))
                    .PlayGame((PlayerPosition)(1 << game));
            }
        }

        [Fact]
        public void DecidesTheSameFromAViewAsFromTheEngineContext()
        {
            var decisions = 0;
            for (var seed = 0; seed < 3; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                var fromContext = Enumerable.Range(0, 4).Select(i => Create((seed * 4) + i, 5)).ToArray();
                var fromView = Enumerable.Range(0, 4).Select(i => Create((seed * 4) + i, 5)).ToArray();
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var expected = DecideFromContext(fromContext[seat.Index()], match);
                    var actual = fromView[seat.Index()].Decide(match.GetView(seat));
                    Assert.Equal(expected.Type, actual.Type);
                    Assert.Equal(expected.BidType, actual.BidType);
                    Assert.Equal(expected.Card, actual.Card);
                    Assert.Equal(expected.Belote, actual.Belote);
                    Assert.Equal(BelotActResult.Ok, match.Act(seat, expected));
                    decisions++;
                }
            }

            Assert.True(decisions > 500);
        }

        [Fact]
        public void BidsOnlyWhatAPersonReadsTheBidAs()
        {
            // A suit with its jack or nine and another card of it, no trumps with an ace, all trumps with a jack.
            var bids = 0;
            for (var seed = 0; seed < 20; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed) });
                var players = Enumerable.Range(0, 4).Select(i => Create(i, 0)).ToArray();
                match.Start();
                while (!match.IsFinished)
                {
                    var seat = match.ToMove;
                    var action = players[seat.Index()].Decide(match.GetView(seat));
                    if (match.Decision == BelotDecision.Bid && action.BidType != BidType.Pass && action.BidType != BidType.Double && action.BidType != BidType.ReDouble)
                    {
                        var hand = CardMemory.ToMask(match.CreateBidContext().MyCards);
                        Assert.True(IsNatural(hand, action.BidType), $"{action.BidType} with {string.Join(" ", match.CreateBidContext().MyCards)}");
                        bids++;
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(seat, action));
                }
            }

            Assert.True(bids > 50);
        }

        [Fact]
        public void TheChancesOfEachUnseenCardAddUpToOne()
        {
            var checkedCards = 0;
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(5) });
            var players = Enumerable.Range(0, 4).Select(i => Create(i, 0)).ToArray();
            var memory = new CardMemory { BidHonourWeight = 2 };
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                if (match.Decision == BelotDecision.PlayCard)
                {
                    Assert.True(memory.Build(match.CreatePlayCardContext()));
                    for (var rest = memory.Unseen; rest != 0; rest &= rest - 1)
                    {
                        var card = BitOperations.TrailingZeroCount(rest);
                        var total = 0.0;
                        for (var other = 0; other < 4; other++)
                        {
                            total += other == memory.Me ? 0 : memory.Chance(other, card);
                        }

                        Assert.Equal(1, total, 9);
                        checkedCards++;
                    }
                }

                Assert.Equal(BelotActResult.Ok, match.Act(seat, players[seat.Index()].Decide(match.GetView(seat))));
            }

            Assert.True(checkedCards > 1000);
        }

        [Fact]
        public void SettingsChangeByNameAndRejectUnknownNames()
        {
            var settings = new HeuristicSettings();
            settings.Set("tricks", "4");
            settings.Set("SUIT", "8.5");

            Assert.Equal(4, settings.EndgameTricks);
            Assert.Equal(8.5, settings.SuitThreshold);
            Assert.Throws<ArgumentException>(() => settings.Set("no-such-rule", "1"));
        }

        private static ClaudePlayerHeuristic Create(int seed, int endgameTricks)
        {
            var player = new ClaudePlayerHeuristic { Rng = new Random(seed) };
            player.Settings.EndgameTricks = endgameTricks;

            // Fixed work only, so the same seed decides the same on any machine.
            player.Settings.EndgameMilliseconds = 0;
            return player;
        }

        private static BelotAction DecideFromContext(IPlayer player, BelotMatch match)
        {
            switch (match.Decision)
            {
                case BelotDecision.Bid:
                    return BelotAction.Bid(player.GetBid(match.CreateBidContext()));
                case BelotDecision.Announce:
                    return BelotAction.Declare(player.GetAnnounces(match.CreateAnnouncesContext()));
                default:
                    var action = player.PlayCard(match.CreatePlayCardContext());
                    return BelotAction.PlayCard(action.Card, action.Belote);
            }
        }

        private static bool IsNatural(uint hand, BidType bid)
        {
            if (bid == BidType.NoTrumps)
            {
                return (hand & 0x80808080u) != 0;
            }

            if (bid == BidType.AllTrumps)
            {
                return (hand & 0x10101010u) != 0;
            }

            var suit = (int)bid.ToCardSuit();
            var cards = hand & (0xFFu << (suit * 8));
            var honours = CardMemory.Bit(suit, (int)CardType.Jack) | CardMemory.Bit(suit, (int)CardType.Nine);
            return (cards & honours) != 0 && BitOperations.PopCount(cards) >= 2;
        }
    }
}
