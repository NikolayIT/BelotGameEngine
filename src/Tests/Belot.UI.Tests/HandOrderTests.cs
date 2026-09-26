namespace Belot.UI.Tests
{
    using System;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.UI.Game;

    using Xunit;

    // The person's hand is laid out by suit, colours alternating, the trump suit first; within a
    // suit from the strongest card down in the contract's order.
    public class HandOrderTests
    {
        [Fact]
        public void DuringTheBiddingSuitsShouldAlternateInPlainOrder()
        {
            var hand = Cards("7♦ J♠ 10♠ A♥ K♣ Q♠ 9♥");
            Assert.Equal(Cards("10♠ Q♠ J♠ A♥ 9♥ K♣ 7♦"), HandOrder.Sort(hand, BidType.Pass));
        }

        [Fact]
        public void InASuitContractTheTrumpsShouldComeFirstInTrumpOrder()
        {
            var hand = Cards("A♥ 9♥ J♥ 10♥ A♠ 10♣ J♣ 8♦");
            Assert.Equal(Cards("J♥ 9♥ A♥ 10♥ 10♣ J♣ 8♦ A♠"), HandOrder.Sort(hand, BidType.Hearts));
            Assert.Equal(HandOrder.Sort(hand, BidType.Hearts), HandOrder.Sort(hand, BidType.Hearts | BidType.ReDouble));

            // Diamonds: the cycle starts at diamonds and goes on with spades.
            Assert.Equal(Cards("8♦ A♠ A♥ 10♥ J♥ 9♥ 10♣ J♣"), HandOrder.Sort(hand, BidType.Diamonds | BidType.Double));
        }

        [Fact]
        public void InAllTrumpsEverySuitShouldBeInTrumpOrder()
        {
            var hand = Cards("A♠ 9♠ J♠ K♥ 9♥ 10♣ J♣ 7♦");
            Assert.Equal(Cards("J♠ 9♠ A♠ 9♥ K♥ J♣ 10♣ 7♦"), HandOrder.Sort(hand, BidType.AllTrumps));
        }

        [Fact]
        public void InNoTrumpsEverySuitShouldBeInPlainOrder()
        {
            var hand = Cards("9♠ J♠ A♠ 10♠ K♥ 9♥ J♣ 10♣");
            Assert.Equal(Cards("A♠ 10♠ J♠ 9♠ K♥ 9♥ 10♣ J♣"), HandOrder.Sort(hand, BidType.NoTrumps));
        }

        // Whatever the hand and the contract, the suits stay together and a suit's neighbours are
        // of the other colour whenever the hand allows.
        [Fact]
        public void SuitsShouldStayTogetherWithColoursAlternating()
        {
            var random = new Random(1);
            var contracts = new[] { BidType.Pass, BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
            for (var i = 0; i < 2000; i++)
            {
                var hand = Card.AllCards.OrderBy(_ => random.Next()).Take(8).ToList();
                var contract = contracts[random.Next(contracts.Length)];
                var sorted = HandOrder.Sort(hand, contract);
                Assert.Equal(hand.OrderBy(c => c.GetHashCode()), sorted.OrderBy(c => c.GetHashCode()));

                var suits = sorted.Select(c => c.Suit).Distinct().ToList();
                Assert.Equal(suits.Count, sorted.Select((c, k) => k == 0 || sorted[k - 1].Suit != c.Suit).Count(x => x));
                if (suits.Count == 4)
                {
                    for (var k = 1; k < 4; k++)
                    {
                        Assert.NotEqual(BelotTexts.IsRed(suits[k - 1]), BelotTexts.IsRed(suits[k]));
                    }
                }

                if (contract is BidType.Clubs or BidType.Diamonds or BidType.Hearts or BidType.Spades && hand.Any(c => c.Suit == contract.ToCardSuit()))
                {
                    Assert.Equal(contract.ToCardSuit(), sorted[0].Suit);
                }
            }
        }

        // "10♠ Q♠ 7♦" to cards.
        private static Card[] Cards(string text) => text.Split(' ').Select(Parse).ToArray();

        private static Card Parse(string card)
        {
            var suit = card[^1] switch
            {
                '♣' => CardSuit.Club,
                '♦' => CardSuit.Diamond,
                '♥' => CardSuit.Heart,
                _ => CardSuit.Spade,
            };
            var type = card[..^1] switch
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
