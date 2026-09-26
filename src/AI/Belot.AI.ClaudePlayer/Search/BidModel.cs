namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// What the auction says about the hands. Every bot here bids like SmartPlayer (points for
    /// the trumps, the aces and tens and the combinations of the first five cards, a bid from
    /// 100 up), so that is the model: <see cref="SmartBid"/> is SmartPlayer.GetBid on a card mask
    /// (the tests check it against SmartPlayer), and <see cref="Read"/> replays the auction to
    /// list, for each seat, the choices it made (a pass is a choice too when it could bid). A
    /// seat's first five cards are consistent with the auction when the model makes all its
    /// choices. Doubles and redoubles, which SmartPlayer never makes, say nothing.
    /// </summary>
    internal sealed class BidModel
    {
        private const int MaxChoices = 16;

        private static readonly BidType[] SuitBids = { BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades };

        private static readonly BidType[] ContractBids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];

        // The choices, in auction order: who chose, among which bids, with the partner's suit
        // bid behind them or not, and what they bid.
        private readonly int[] seats = new int[MaxChoices];
        private readonly BidType[] availableBids = new BidType[MaxChoices];
        private readonly bool[] partnerBidSuit = new bool[MaxChoices];
        private readonly BidType[] choices = new BidType[MaxChoices];
        private int count;

        /// <summary>Gets the seats (as bits) whose choices say something.</summary>
        public int InformativeSeats { get; private set; }

        /// <summary>The bids open to a seat, as ContractManager computes them.</summary>
        public static BidType AvailableBids(BidType contract, int declarer, int seat)
        {
            var plain = contract & ~(BidType.Double | BidType.ReDouble);
            var available = BidType.Pass;
            foreach (var bid in ContractBids)
            {
                if (plain < bid)
                {
                    available |= bid;
                }
            }

            if (contract != BidType.Pass && !contract.HasFlag(BidType.ReDouble))
            {
                var declaringTeam = ((seat ^ declarer) & 1) == 0;
                if (contract.HasFlag(BidType.Double))
                {
                    if (declaringTeam)
                    {
                        available |= BidType.ReDouble;
                    }
                }
                else if (!declaringTeam)
                {
                    available |= BidType.Double;
                }
            }

            return available;
        }

        /// <summary>SmartPlayer's bid with these five cards.</summary>
        public BidType SmartBid(uint cards, BidType available, bool partnerBidSuit)
        {
            var announceCount = AnnounceScorer.AddDeclaredCombinations(cards, 0, this.announces, 0);
            var announcePoints = 0;
            for (var i = 0; i < announceCount; i++)
            {
                announcePoints += AnnounceScorer.Value(this.announces[i].Type);
            }

            var best = BidType.Pass;
            var bestPoints = 99;
            for (var suit = 0; suit < 4; suit++)
            {
                if (available.HasFlag(SuitBids[suit]))
                {
                    Consider(SuitBids[suit], TrumpPoints(cards, suit, announcePoints), ref best, ref bestPoints);
                }
            }

            if (available.HasFlag(BidType.AllTrumps))
            {
                Consider(BidType.AllTrumps, AllTrumpsPoints(cards, partnerBidSuit, announcePoints), ref best, ref bestPoints);
            }

            if (available.HasFlag(BidType.NoTrumps))
            {
                Consider(BidType.NoTrumps, NoTrumpsPoints(cards), ref best, ref bestPoints);
            }

            return best;
        }

        /// <summary>Replays the auction into the choices each seat made.</summary>
        public void Read(IEnumerable<Bid> bids, PlayerPosition firstToBid)
        {
            this.count = 0;
            this.InformativeSeats = 0;
            var contract = BidType.Pass;
            var declarer = firstToBid.Index();
            var suitBids = 0;
            foreach (var bid in bids)
            {
                var seat = bid.Player.Index();
                var available = AvailableBids(contract, declarer, seat);
                if (available != BidType.Pass && bid.Type != BidType.Double && bid.Type != BidType.ReDouble
                                              && this.count < MaxChoices)
                {
                    this.seats[this.count] = seat;
                    this.availableBids[this.count] = available;
                    this.partnerBidSuit[this.count] = (suitBids & (1 << ((seat + 2) & 3))) != 0;
                    this.choices[this.count] = bid.Type;
                    this.count++;
                    this.InformativeSeats |= 1 << seat;
                }

                if (bid.Type == BidType.Double || bid.Type == BidType.ReDouble)
                {
                    contract = (contract & ~(BidType.Double | BidType.ReDouble)) | bid.Type;
                }
                else if (bid.Type != BidType.Pass)
                {
                    contract = bid.Type;
                    declarer = seat;
                    if (Array.IndexOf(SuitBids, bid.Type) >= 0)
                    {
                        suitBids |= 1 << seat;
                    }
                }
            }
        }

        /// <summary>Whether these first five cards of the seat make all its choices.</summary>
        public bool Fits(int seat, uint firstFive)
        {
            for (var i = 0; i < this.count; i++)
            {
                if (this.seats[i] == seat && this.SmartBid(firstFive, this.availableBids[i], this.partnerBidSuit[i]) != this.choices[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether a random five of the seat's eight cards make its choices: accepting a deal
        /// with this probability samples the deals in proportion to how well they explain the
        /// auction (the first five of eight dealt cards are any five of them).
        /// </summary>
        public bool FitsRandomFive(int seat, uint eightCards, Random random)
        {
            var five = eightCards;
            for (var drop = 0; drop < 3; drop++)
            {
                var index = random.Next(BitOperations.PopCount(five));
                var rest = five;
                for (var i = 0; i < index; i++)
                {
                    rest &= rest - 1;
                }

                five &= ~(rest & (~rest + 1));
            }

            return this.Fits(seat, five);
        }

        private static void Consider(BidType bid, int points, ref BidType best, ref int bestPoints)
        {
            if (points > bestPoints)
            {
                bestPoints = points;
                best = bid;
            }
        }

        private static bool Has(uint cards, int suit, int type) => (cards & (1u << ((suit * 8) + type))) != 0;

        private static int TrumpPoints(uint cards, int trump, int announcePoints)
        {
            var points = announcePoints / 2;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                var type = card & 7;
                if (suit == trump)
                {
                    points += type switch
                        {
                            4 => 55, // jack
                            2 => 35, // nine
                            7 => 25, // ace
                            3 => 20, // ten
                            5 => Has(cards, trump, 6) ? 25 : 16, // queen, with the king or not
                            6 => 16, // king
                            _ => 15, // seven, eight
                        };
                }
                else if (type == 7)
                {
                    points += 20;
                }
                else if (type == 3)
                {
                    points += Has(cards, suit, 7) ? 15 : 10;
                }
            }

            return points;
        }

        private static int AllTrumpsPoints(uint cards, bool partnerBidSuit, int announcePoints)
        {
            var points = announcePoints / 3;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                switch (card & 7)
                {
                    case 4:
                        points += 45;
                        break;
                    case 2:
                        points += Has(cards, suit, 4) ? 25 : 15;
                        break;
                    case 7:
                        points += Has(cards, suit, 4) && Has(cards, suit, 2) ? 10 : 5;
                        break;
                }
            }

            return points + (partnerBidSuit ? 5 : 0);
        }

        private static int NoTrumpsPoints(uint cards)
        {
            var points = 0;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                switch (card & 7)
                {
                    case 7:
                        points += 45;
                        break;
                    case 3:
                        points += Has(cards, suit, 7) ? 25 : 15;
                        break;
                    case 6:
                        points += Has(cards, suit, 7) && Has(cards, suit, 3) ? 10 : 5;
                        break;
                }
            }

            return points;
        }
    }
}
