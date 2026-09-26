namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using Xunit;

    public class MonteCarloBiddingTests
    {
        private const BidType AllContracts =
            BidType.Clubs | BidType.Diamonds | BidType.Hearts | BidType.Spades | BidType.NoTrumps | BidType.AllTrumps;

        [Fact]
        public void BidsTheSuitOfAStrongTrumpHand()
        {
            var bid = Claude().ChooseBid(Context("JH 9H AH 10H KH", new Bid(PlayerPosition.South, BidType.Pass), AllContracts));

            Assert.True(bid == BidType.Hearts || bid == BidType.AllTrumps, bid.ToString());
        }

        [Fact]
        public void PassesAWorthlessHand()
        {
            var bid = Claude().ChooseBid(Context("7C 8D 7H 8S 8C", new Bid(PlayerPosition.South, BidType.Pass), AllContracts));

            Assert.Equal(BidType.Pass, bid);
        }

        [Fact]
        public void DoublesAnOpponentsContractItHoldsEveryTrumpAgainst()
        {
            // East bid clubs; South holds the jack, nine, ace and ten of clubs and an ace.
            var claude = Claude();
            claude.MayDouble = true;
            var bid = claude.ChooseBid(Context(
                "JC 9C AC 10C AH",
                new Bid(PlayerPosition.East, BidType.Clubs),
                BidType.Diamonds | BidType.Hearts | BidType.Spades | BidType.NoTrumps | BidType.AllTrumps | BidType.Double));

            Assert.True(bid == BidType.Double || bid == BidType.AllTrumps, bid.ToString());
        }

        [Fact]
        public void OnlyChoosesAmongTheAvailableBids()
        {
            var random = new Random(3);
            for (var i = 0; i < 50; i++)
            {
                var cards = string.Join(" ", Deal.RandomHands(random)[0].Take(5).Select(Deal.Code));
                var available = BidType.NoTrumps | BidType.AllTrumps;
                var bid = Claude().ChooseBid(Context(cards, new Bid(PlayerPosition.West, BidType.Spades), available));

                Assert.True(bid == BidType.Pass || available.HasFlag(bid), bid.ToString());
            }
        }

        private static ClaudePlayerIsmcts Claude() => new ClaudePlayerIsmcts { Rng = new Random(1), BiddingDeals = 400 };

        private static PlayerGetBidContext Context(string cards, Bid current, BidType available) =>
            new PlayerGetBidContext
            {
                RoundNumber = 1,
                FirstToPlayInTheRound = PlayerPosition.South,
                MyPosition = PlayerPosition.South,
                MyCards = Deal.Cards(cards),
                Bids = current.Type == BidType.Pass ? new List<Bid>() : new List<Bid> { current },
                CurrentContract = current,
                AvailableBids = available,
            };
    }
}
