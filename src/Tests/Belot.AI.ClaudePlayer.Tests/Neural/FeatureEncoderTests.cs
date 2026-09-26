namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;

    using Xunit;

    public class FeatureEncoderTests
    {
        private static readonly BidType[] Suits = { BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades };

        // One network plays all four suit contracts: a deal in hearts and the same deal with
        // every suit moved on (so it is in spades) look exactly the same to it.
        [Fact]
        public void TheSameDealInAnotherTrumpSuitLooksTheSame()
        {
            var decisions = 0;
            for (var seed = 0; seed < 300; seed++)
            {
                var random = new Random(seed);
                var trump = random.Next(4);
                var shift = 1 + random.Next(3);
                var deck = Enumerable.Range(0, 32).OrderBy(_ => random.Next()).ToArray();
                var first = random.Next(4);
                var doubling = random.Next(3);
                var original = Bid(NeuralDeal.Deal(deck, first, 0), Suits[trump], doubling);
                var moved = Bid(NeuralDeal.Deal(deck.Select(x => Move(x, shift)).ToArray(), first, 0), Suits[(trump + shift) & 3], doubling);
                var simulator = new BelotSimulator();
                var movedSimulator = new BelotSimulator();
                var buffer = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
                original.StartPlay(simulator, buffer);
                moved.StartPlay(movedSimulator, buffer);
                while (!original.IsFinished)
                {
                    original.DeclareIfFirstCard();
                    moved.DeclareIfFirstCard();
                    var legal = simulator.LegalMoves(in original.Play);
                    Assert.Equal(Move(legal, shift), movedSimulator.LegalMoves(in moved.Play));
                    Assert.Equal(Encode(original, legal), Encode(moved, Move(legal, shift)));
                    decisions++;
                    var card = Enumerable.Range(0, 32).Where(x => (legal & (1u << x)) != 0).OrderBy(_ => random.Next()).First();
                    original.PlayCard(simulator, card, legal);
                    moved.PlayCard(movedSimulator, Move(card, shift), Move(legal, shift));
                }

                original.Score(simulator, out var southNorth, out var eastWest, out _);
                moved.Score(movedSimulator, out var movedSouthNorth, out var movedEastWest, out _);
                Assert.Equal((southNorth, eastWest), (movedSouthNorth, movedEastWest));
            }

            Assert.True(decisions > 9000);
        }

        [Fact]
        public void TheOutputsMapBackToTheCards()
        {
            for (var kind = 0; kind < 6; kind++)
            {
                var rotation = FeatureEncoder.Rotation(kind);
                var outputs = Enumerable.Range(0, 32).Select(x => FeatureEncoder.ToNetwork(x, rotation)).ToArray();
                Assert.Equal(Enumerable.Range(0, 32), outputs.OrderBy(x => x));
                Assert.All(Enumerable.Range(0, 32), x => Assert.Equal(x, FeatureEncoder.FromNetwork(FeatureEncoder.ToNetwork(x, rotation), rotation)));
                if (kind < 4)
                {
                    // The trump suit comes first.
                    Assert.All(Enumerable.Range(kind * 8, 8), x => Assert.Equal(0, FeatureEncoder.ToNetwork(x, rotation) >> 3));
                }
            }

            foreach (var bid in new[] { BidType.Pass, BidType.Clubs, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble })
            {
                Assert.Equal(bid, FeatureEncoder.BidOfIndex(FeatureEncoder.BidIndex(bid)));
            }

            Assert.Equal(FeatureEncoder.BidOutputs - 1, FeatureEncoder.BidIndex(BidType.ReDouble));
        }

        // The first bidder bids the contract; the others pass, or double it now and then.
        private static NeuralDeal Bid(NeuralDeal deal, BidType contract, int doubling)
        {
            deal.Bid(contract);
            if (doubling > 0)
            {
                deal.Bid(BidType.Double);
                if (doubling > 1)
                {
                    deal.Bid(BidType.ReDouble);
                }
            }

            while (!deal.AuctionFinished)
            {
                deal.Bid(BidType.Pass);
            }

            return deal;
        }

        private static int Move(int card, int shift) => ((((card >> 3) + shift) & 3) << 3) | (card & 7);

        private static uint Move(uint cards, int shift) => BitOperations.RotateLeft(cards, shift * 8);

        private static (int, float)[] Encode(NeuralDeal deal, uint legal)
        {
            var indices = new int[FeatureEncoder.MaxActive];
            var values = new float[FeatureEncoder.MaxActive];
            var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
            return indices.Take(count).Zip(values.Take(count)).OrderBy(x => x.First).ToArray();
        }
    }
}
