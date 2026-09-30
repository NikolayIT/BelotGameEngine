namespace Belot.AI.ClaudePlayer.Tests.Human
{
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    using Xunit;

    public class HumanPreferenceTests
    {
        private const int South = 0;
        private const int East = 1;
        private const int North = 2;
        private const int West = 3;

        [Fact]
        public void GivesTheLowestCardToATrickTheOpponentsWin()
        {
            // Hearts; East leads the ace of spades, South (last) holds the ten and the seven.
            var deal = Position(BidType.Hearts, East, East, Mask(C(CardSuit.Spade, CardType.Ten), C(CardSuit.Spade, CardType.Seven), C(CardSuit.Club, CardType.King)), C(CardSuit.Spade, CardType.Ace), C(CardSuit.Spade, CardType.Eight), C(CardSuit.Spade, CardType.Nine));
            var legal = Mask(C(CardSuit.Spade, CardType.Ten), C(CardSuit.Spade, CardType.Seven));
            Assert.Equal(C(CardSuit.Spade, CardType.Seven), Preferred(in deal, legal));
        }

        [Fact]
        public void KeepsTheTrumpTenUnderTheOpponentsJack()
        {
            // Diamonds are trumps; East leads the jack, South holds the ten and the king of trumps.
            var deal = Position(BidType.Diamonds, East, East, Mask(C(CardSuit.Diamond, CardType.Ten), C(CardSuit.Diamond, CardType.King), C(CardSuit.Heart, CardType.Nine)), C(CardSuit.Diamond, CardType.Jack), C(CardSuit.Diamond, CardType.Seven), C(CardSuit.Diamond, CardType.Eight));
            var legal = Mask(C(CardSuit.Diamond, CardType.Ten), C(CardSuit.Diamond, CardType.King));
            Assert.Equal(C(CardSuit.Diamond, CardType.King), Preferred(in deal, legal));
        }

        [Fact]
        public void GivesPointsToThePartnersSureTrick()
        {
            // No trumps; North (the partner) holds the trick with the ace of clubs, South is last and void.
            var hand = Mask(C(CardSuit.Diamond, CardType.Seven), C(CardSuit.Heart, CardType.King), C(CardSuit.Heart, CardType.Seven), C(CardSuit.Spade, CardType.Eight));
            var deal = Position(BidType.NoTrumps, East, East, hand, C(CardSuit.Club, CardType.Seven), C(CardSuit.Club, CardType.Ace), C(CardSuit.Club, CardType.Eight));
            Assert.Equal(C(CardSuit.Heart, CardType.King), Preferred(in deal, hand));
        }

        [Fact]
        public void ThrowsFromTheSuitWithNothingInIt()
        {
            // Hearts; South has no spades and no trumps: the seven of clubs sits with the ace, the eight of diamonds alone.
            var hand = Mask(C(CardSuit.Club, CardType.Ace), C(CardSuit.Club, CardType.Seven), C(CardSuit.Diamond, CardType.Eight));
            var deal = Position(BidType.Hearts, North, East, hand, C(CardSuit.Spade, CardType.Ace), C(CardSuit.Spade, CardType.Eight), C(CardSuit.Spade, CardType.Nine));
            Assert.Equal(C(CardSuit.Diamond, CardType.Eight), Preferred(in deal, Mask(C(CardSuit.Club, CardType.Seven), C(CardSuit.Diamond, CardType.Eight))));
        }

        [Fact]
        public void DeclaresTheBeloteWhenFollowingRatherThanGiveTheTen()
        {
            // All trumps; East leads the jack of hearts, South (last) holds the ten, queen and king.
            var hand = Mask(C(CardSuit.Heart, CardType.Ten), C(CardSuit.Heart, CardType.Queen), C(CardSuit.Heart, CardType.King), C(CardSuit.Club, CardType.Seven));
            var deal = Position(BidType.AllTrumps, East, East, hand, C(CardSuit.Heart, CardType.Jack), C(CardSuit.Heart, CardType.Seven), C(CardSuit.Heart, CardType.Eight));
            var legal = Mask(C(CardSuit.Heart, CardType.Ten), C(CardSuit.Heart, CardType.Queen), C(CardSuit.Heart, CardType.King));
            Assert.Contains(Preferred(in deal, legal), new[] { C(CardSuit.Heart, CardType.Queen), C(CardSuit.Heart, CardType.King) });
        }

        [Fact]
        public void APersonNeverGivesTheHigherOfTwoCardsThatLoseTheTrick()
        {
            // Hearts; East leads the ace of spades, South (last) holds the ten, king and seven.
            var spades = Mask(C(CardSuit.Spade, CardType.Ten), C(CardSuit.Spade, CardType.King), C(CardSuit.Spade, CardType.Seven));
            var lost = Position(BidType.Hearts, East, East, spades, C(CardSuit.Spade, CardType.Ace), C(CardSuit.Spade, CardType.Eight), C(CardSuit.Spade, CardType.Nine));
            Assert.Equal(Mask(C(CardSuit.Spade, CardType.Ten), C(CardSuit.Spade, CardType.King)), HumanPreference.Dominated(in lost, spades));

            // Diamonds are trumps and East's jack holds: the nine loses as well as the ace, with more points.
            var trumps = Mask(C(CardSuit.Diamond, CardType.Nine), C(CardSuit.Diamond, CardType.Ace));
            var trumped = Position(BidType.Diamonds, East, East, trumps, C(CardSuit.Diamond, CardType.Jack), C(CardSuit.Diamond, CardType.Seven), C(CardSuit.Diamond, CardType.Eight));
            Assert.Equal(Mask(C(CardSuit.Diamond, CardType.Nine)), HumanPreference.Dominated(in trumped, trumps));

            // The partner holds the trick: nothing is dominated.
            var partner = Position(BidType.Hearts, North, North, spades, C(CardSuit.Spade, CardType.Ace), C(CardSuit.Spade, CardType.Eight));
            Assert.Equal(0u, HumanPreference.Dominated(in partner, spades));
        }

        [Fact]
        public void DefendersDoNotLeadTrumps()
        {
            // Spades by East; South leads with a low trump and a low plain card.
            var hand = Mask(C(CardSuit.Spade, CardType.Seven), C(CardSuit.Heart, CardType.Eight), C(CardSuit.Heart, CardType.King));
            var deal = Position(BidType.Spades, East, South, hand);
            Assert.Equal(C(CardSuit.Heart, CardType.Eight), Preferred(in deal, Mask(C(CardSuit.Spade, CardType.Seven), C(CardSuit.Heart, CardType.Eight))));
        }

        [Fact]
        public void CashesAMasterWhenNobodyCanTrumpIt()
        {
            // No trumps; South leads, holding the ace of hearts and a low club.
            var hand = Mask(C(CardSuit.Heart, CardType.Ace), C(CardSuit.Club, CardType.Seven));
            var deal = Position(BidType.NoTrumps, South, South, hand);
            Assert.Equal(C(CardSuit.Heart, CardType.Ace), Preferred(in deal, hand));
        }

        private static int C(CardSuit suit, CardType type) => Card.GetCard(suit, type).GetHashCode();

        private static uint Mask(params int[] cards)
        {
            var mask = 0u;
            foreach (var card in cards)
            {
                mask |= 1u << card;
            }

            return mask;
        }

        // A contract played by the declarer; the leader has played the given cards to the trick
        // and South is to play with the hand.
        private static NeuralDeal Position(BidType contract, int declarer, int leader, uint hand, params int[] trick)
        {
            var simulator = new BelotSimulator();
            var deal = NeuralDeal.Start(leader, 0);
            deal.Contract = contract;
            deal.Declarer = declarer;
            deal.AuctionFinished = true;
            deal.StartPlay(simulator);
            foreach (var card in trick)
            {
                deal.PlayCard(simulator, card, false);
            }

            Assert.Equal(South, deal.Play.Turn);
            deal.Play.Hands[South] = hand;
            _ = West;
            return deal;
        }

        private static int Preferred(in NeuralDeal deal, uint legal)
        {
            var scores = new float[32];
            HumanPreference.Score(in deal, default, legal, scores);
            var best = -1;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (best < 0 || scores[card] > scores[best])
                {
                    best = card;
                }
            }

            return best;
        }
    }
}
