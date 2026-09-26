namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    /// <summary>
    /// The neural player is trained in self-play (<see cref="NeuralDeal"/> playing whole deals on
    /// card masks) and plays through the engine's contexts. Both must be the same game: whole
    /// matches of random bids (doubles too), declarations and cards, some belotes kept back, are
    /// replayed into a self-play deal, which must offer the same bids and cards at every
    /// decision, declare and score the same, and give the networks exactly the inputs the
    /// player builds from the engine's context (and from the seat's view).
    /// </summary>
    public class SelfPlayAgreesWithEngineTests
    {
        private static readonly PlayerPosition[] Seats =
        {
            PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West,
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void EveryDecisionLooksTheSameInSelfPlay(int group)
        {
            var deals = 0;
            for (var seed = group * 100; seed < (group + 1) * 100; seed++)
            {
                deals += CheckMatch(seed);
            }

            Assert.True(deals > 300, $"{deals} deals");
        }

        private static int CheckMatch(int seed)
        {
            var random = new Random(seed);
            var alwaysBelote = seed % 2 == 0;
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(seed), FirstToPlay = Seats[seed % 4] });
            var decisions = new List<Decision>();
            match.Start();
            while (!match.IsFinished)
            {
                var seat = match.ToMove;
                var view = match.GetView(seat);
                switch (match.Decision)
                {
                    case BelotDecision.Bid:
                    {
                        var context = match.CreateBidContext();
                        var decision = Decision.OfBid(context);
                        Assert.Equal(decision.Features, Decision.OfBid(view.CreateBidContext()).Features);
                        decisions.Add(decision);
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Bid(RandomBid(context.AvailableBids, random))));
                        break;
                    }

                    case BelotDecision.Announce:
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.Declare(match.CreateAnnouncesContext().AvailableAnnounces)));
                        break;

                    default:
                    {
                        var context = match.CreatePlayCardContext();
                        var decision = Decision.OfCard(context);
                        Assert.Equal(decision.Features, Decision.OfCard(view.CreatePlayCardContext()).Features);
                        decisions.Add(decision);
                        var cards = context.AvailableCardsToPlay.ToList();
                        var card = cards[random.Next(cards.Count)];
                        Assert.Equal(BelotActResult.Ok, match.Act(seat, BelotAction.PlayCard(card, alwaysBelote || random.Next(3) > 0)));
                        break;
                    }
                }
            }

            var record = match.GetRecord();
            var simulator = new BelotSimulator();
            var buffer = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
            var next = 0;
            var hanging = 0;
            foreach (var round in record.Rounds)
            {
                var deal = NeuralDeal.Deal(round.Deal.Select(x => x.GetHashCode()).ToArray(), round.FirstToPlay.Index(), hanging);
                while (!deal.AuctionFinished)
                {
                    var bid = round.Bids[deal.BidCount];
                    Assert.Equal(bid.Player.Index(), deal.ToBid);
                    var expected = decisions[next++];
                    Assert.True(expected.IsBid);
                    Assert.Equal(expected.Seat, deal.ToBid);
                    Assert.Equal(expected.Legal, (uint)deal.AvailableBids());
                    Assert.Equal(expected.Features, Decision.Encode(deal, 0, true));
                    deal.Bid(bid.Type);
                }

                Assert.Equal(round.Bids.Count, deal.BidCount);
                Assert.Equal(round.Contract.Type, deal.Contract);
                if (deal.Contract == BidType.Pass)
                {
                    deal.Score(simulator, out var passedNorthSouth, out var passedEastWest, out var passedHanging);
                    Assert.Equal((0, 0, round.Result.HangingPoints), (passedNorthSouth, passedEastWest, passedHanging));
                    hanging = round.Result.HangingPoints;
                    continue;
                }

                Assert.Equal(round.Contract.Player.Index(), deal.Declarer);
                deal.StartPlay(simulator, buffer);
                CheckDeclarations(round, deal);
                foreach (var played in round.Tricks.SelectMany(x => x.Cards))
                {
                    deal.DeclareIfFirstCard();
                    Assert.Equal(played.Player.Index(), deal.Play.Turn);
                    var legal = simulator.LegalMoves(in deal.Play);
                    var card = played.Card.GetHashCode();
                    Assert.NotEqual(0u, legal & (1u << card));
                    if ((legal & (legal - 1)) != 0)
                    {
                        var expected = decisions[next++];
                        Assert.False(expected.IsBid);
                        Assert.Equal(expected.Seat, deal.Play.Turn);
                        Assert.Equal(expected.Legal, legal);
                        Assert.Equal(expected.Features, Decision.Encode(deal, legal, false));
                    }

                    if (alwaysBelote)
                    {
                        Assert.Equal(played.Belote, simulator.IsBelote(in deal.Play, card, legal));
                    }

                    deal.PlayCard(simulator, card, played.Belote);
                }

                Assert.True(deal.IsFinished);
                deal.Score(simulator, out var southNorth, out var eastWest, out var newHanging);
                Assert.Equal(
                    (round.Result.SouthNorthPoints, round.Result.EastWestPoints, round.Result.HangingPoints),
                    (southNorth, eastWest, newHanging));
                hanging = round.Result.HangingPoints;
            }

            Assert.Equal(decisions.Count, next);
            return record.Rounds.Count;
        }

        // Each seat declares what the engine let it (everything it was offered), and the same
        // combinations score.
        private static void CheckDeclarations(BelotRoundRecord round, NeuralDeal deal)
        {
            var combinations = round.Announces.Where(x => x.Type != AnnounceType.Belot).ToList();
            for (var seat = 0; seat < 4; seat++)
            {
                var bits = combinations.Where(x => x.Player.Index() == seat)
                    .Aggregate(0, (all, x) => all | NeuralDeal.DeclarationBit(x.Type));
                Assert.Equal(bits, deal.ToDeclare[seat]);
            }

            var scored = combinations.Where(x => x.IsScored == true).ToList();
            Assert.Equal(scored.Where(x => (x.Player.Index() & 1) == 0).Sum(x => x.Value), deal.SouthNorthAnnounces);
            Assert.Equal(scored.Where(x => (x.Player.Index() & 1) == 1).Sum(x => x.Value), deal.EastWestAnnounces);
        }

        // Passes more often than not, else any bid open (doubles included).
        private static BidType RandomBid(BidType available, Random random)
        {
            var bids = Enumerable.Range(0, 8).Select(x => (BidType)(1 << x)).Where(x => available.HasFlag(x)).ToList();
            return bids.Count == 0 || random.Next(10) < 6 ? BidType.Pass : bids[random.Next(bids.Count)];
        }

        private sealed class Decision
        {
            public bool IsBid { get; private set; }

            public int Seat { get; private set; }

            public uint Legal { get; private set; }

            public List<(int Index, float Value)> Features { get; private set; }

            public static Decision OfBid(PlayerGetBidContext context)
            {
                Assert.True(NeuralDeal.FromBidContext(context, out var deal));
                return new Decision
                {
                    IsBid = true,
                    Seat = context.MyPosition.Index(),
                    Legal = (uint)context.AvailableBids,
                    Features = Encode(deal, 0, true),
                };
            }

            public static Decision OfCard(PlayerPlayCardContext context)
            {
                Assert.True(NeuralDeal.FromPlayContext(context, new BelotSimulator(), out var deal));
                var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
                return new Decision
                {
                    Seat = context.MyPosition.Index(),
                    Legal = legal,
                    Features = Encode(deal, legal, false),
                };
            }

            public static List<(int Index, float Value)> Encode(NeuralDeal deal, uint legal, bool bid)
            {
                var indices = new int[FeatureEncoder.MaxActive];
                var values = new float[FeatureEncoder.MaxActive];
                var count = bid
                    ? FeatureEncoder.EncodeBid(in deal, indices, values)
                    : FeatureEncoder.EncodeCard(in deal, legal, indices, values);
                var inputs = bid ? FeatureEncoder.BidInputs : FeatureEncoder.CardInputs;
                Assert.All(indices.Take(count), x => Assert.InRange(x, 0, inputs - 1));
                Assert.Equal(count, indices.Take(count).Distinct().Count());
                return indices.Take(count).Zip(values.Take(count)).OrderBy(x => x.First).ToList();
            }
        }
    }
}
