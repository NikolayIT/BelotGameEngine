namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class BidModelTests
    {
        private static readonly BidType[] Bids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        [Fact]
        public void SmartBidIsSmartPlayersBid()
        {
            var random = new Random(1);
            var smart = new SmartPlayer.SmartPlayer();
            var model = new BidModel();
            for (var i = 0; i < 50_000; i++)
            {
                var cards = RandomFive(random);
                var available = BidType.Pass;
                foreach (var bid in Bids)
                {
                    if (random.Next(3) > 0)
                    {
                        available |= bid;
                    }
                }

                var partnerBidSuit = random.Next(2) == 0;
                var context = new PlayerGetBidContext
                {
                    MyPosition = PlayerPosition.South,
                    MyCards = cards,
                    AvailableBids = available,
                    Bids = partnerBidSuit ? new List<Bid> { new Bid(PlayerPosition.North, BidType.Hearts) } : new List<Bid>(),
                    CurrentContract = new Bid(PlayerPosition.South, BidType.Pass),
                };

                Assert.Equal(smart.GetBid(context), model.SmartBid(Deal.Mask(cards), available, partnerBidSuit));
            }
        }

        [Fact]
        public void EverySmartPlayersAuctionFitsItsFirstFiveCards()
        {
            // Four SmartPlayers bid in real deals; the model must read every choice as made by
            // the cards each had, and offer the bids the engine offered.
            for (var seed = 0; seed < 300; seed++)
            {
                var recorder = new AuctionRecorder();
                var players = Enumerable.Range(0, 4).Select(_ => (IPlayer)recorder).ToArray();
                new RoundManager(players[0], players[1], players[2], players[3], new Random(seed))
                    .PlayRound(1, (PlayerPosition)(1 << (seed % 4)), 0, 0, 0);

                var model = new BidModel();
                model.Read(recorder.Bids, (PlayerPosition)(1 << (seed % 4)));
                for (var seat = 0; seat < 4; seat++)
                {
                    if (recorder.FirstFive.TryGetValue(seat, out var five))
                    {
                        Assert.True(model.Fits(seat, five));
                    }
                }

                foreach (var (seat, available, contract, declarer) in recorder.Offers)
                {
                    Assert.Equal(available, BidModel.AvailableBids(contract, declarer, seat));
                }
            }
        }

        [Fact]
        public void APassWithAStrongHandDoesNotFit()
        {
            var model = new BidModel();
            model.Read(
                new List<Bid> { new Bid(PlayerPosition.South, BidType.Pass), new Bid(PlayerPosition.East, BidType.Hearts) },
                PlayerPosition.South);

            Assert.False(model.Fits(0, Deal.Mask(Deal.Cards("JC 9C AC 10C KC"))));
            Assert.True(model.Fits(0, Deal.Mask(Deal.Cards("7C 8D 9H 7S 8S"))));
            Assert.True(model.Fits(1, Deal.Mask(Deal.Cards("JH 9H AH 7C 8C"))));
            Assert.False(model.Fits(1, Deal.Mask(Deal.Cards("7H 8D 9C 7S 8S"))));
            Assert.Equal((1 << 0) | (1 << 1), model.InformativeSeats);
        }

        private static CardCollection RandomFive(Random random)
        {
            var cards = new CardCollection();
            while (cards.Count < 5)
            {
                cards.Add(Card.AllCards[random.Next(32)]);
            }

            return cards;
        }

        // Bids like SmartPlayer and remembers each seat's first five cards and every offer.
        private sealed class AuctionRecorder : IPlayer
        {
            private readonly SmartPlayer.SmartPlayer smart = new SmartPlayer.SmartPlayer();

            public Dictionary<int, uint> FirstFive { get; } = new Dictionary<int, uint>();

            public List<(int Seat, BidType Available, BidType Contract, int Declarer)> Offers { get; } =
                new List<(int Seat, BidType Available, BidType Contract, int Declarer)>();

            public List<Bid> Bids { get; private set; } = new List<Bid>();

            public BidType GetBid(PlayerGetBidContext context)
            {
                var seat = context.MyPosition.Index();
                this.FirstFive.TryAdd(seat, Deal.Mask(context.MyCards));
                this.Offers.Add((seat, context.AvailableBids, context.CurrentContract.Type, context.CurrentContract.Player.Index()));
                this.Bids = (List<Bid>)context.Bids;
                return this.smart.GetBid(context);
            }

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.smart.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
            }

            public void EndOfRound(RoundResult roundResult)
            {
            }

            public void EndOfGame(GameResult gameResult)
            {
            }
        }
    }
}
