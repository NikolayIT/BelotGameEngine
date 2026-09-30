namespace Belot.AI.ClaudePlayer.Tests.Human
{
    using System;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class NaturalBiddingTests
    {
        [Theory]
        [InlineData("Q♣ 10♦ A♦ 10♥ A♠", BidType.NoTrumps)]
        [InlineData("J♦ J♥ A♥ 8♠ 9♠", BidType.Hearts | BidType.Spades | BidType.NoTrumps | BidType.AllTrumps)]
        [InlineData("J♣ 7♦ 8♦ 9♥ 7♠", BidType.AllTrumps)]
        [InlineData("J♣ 7♣ 8♦ 9♥ 10♥", BidType.Clubs | BidType.Hearts | BidType.AllTrumps)]
        [InlineData("7♣ 8♦ Q♥ K♥ 10♠", BidType.Pass)]
        public void AllowsOnlyTheContractsAPersonWouldReadFromTheBid(string cards, BidType contracts)
        {
            var hand = 0u;
            foreach (var name in cards.Split(' '))
            {
                hand |= 1u << Parse(name).GetHashCode();
            }

            Assert.Equal(contracts | BidType.Double | BidType.ReDouble, NaturalBidding.Allowed(hand));
        }

        // Over whole auctions a natural bidder's contract bids all fit its five cards.
        [Fact]
        public void TheNaturalPlayerNeverMakesAThinBid()
        {
            var player = new ClaudePlayerNeural(RandomModels.Create(51231)) { NaturalBidding = true };
            var bids = 0;
            for (var seed = 0; seed < 300; seed++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(51331 + seed) });
                match.Start();
                while (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = player.GetBid(context);
                    var plain = bid & ~(BidType.Double | BidType.ReDouble);
                    if (plain != BidType.Pass)
                    {
                        Assert.True((NaturalBidding.Allowed(NeuralDeal.ToMask(context.MyCards)) & plain) == plain, $"{bid} on {context.MyCards}");
                        bids++;
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(match.ToMove, BelotAction.Bid(bid)));
                }
            }

            Assert.True(bids > 50, $"Only {bids} contract bids.");
        }

        private static Card Parse(string name)
        {
            var suit = name[^1] switch
            {
                '♣' => CardSuit.Club,
                '♦' => CardSuit.Diamond,
                '♥' => CardSuit.Heart,
                _ => CardSuit.Spade,
            };
            var type = name[..^1] switch
            {
                "7" => CardType.Seven,
                "8" => CardType.Eight,
                "9" => CardType.Nine,
                "10" => CardType.Ten,
                "J" => CardType.Jack,
                "Q" => CardType.Queen,
                "K" => CardType.King,
                _ => CardType.Ace,
            };
            return Card.GetCard(suit, type);
        }
    }
}
