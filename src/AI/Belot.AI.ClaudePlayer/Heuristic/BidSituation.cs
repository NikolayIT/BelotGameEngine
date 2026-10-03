namespace Belot.AI.ClaudePlayer.Heuristic
{
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// What one bid decision knows, as the learned bidding (<see cref="LearnedBidding"/>) counts it:
    /// the five cards and the auction so far, each bid by its seat relative to the bidder (0 the
    /// bidder, 1 the opponent who bids next, 2 the partner, 3 the opponent who bid just before).
    /// </summary>
    internal struct BidSituation
    {
        /// <summary>The bidder's five cards, one bit per card (suit * 8 + type).</summary>
        public uint Hand;

        /// <summary>The bidder's place in the bidding: 0 for the first to bid, who also leads the first trick.</summary>
        public int Position;

        /// <summary>The relative seat that holds the contract, or -1 while nobody has bid.</summary>
        public int Holder;

        /// <summary>The contract so far without the doubling (Pass while nobody has bid).</summary>
        public BidType Contract;

        /// <summary>1 when the contract is doubled, 2 when redoubled.</summary>
        public int Doubled;

        public BidType Available;

        /// <summary>The suits the partner bid, one bit per suit.</summary>
        public int PartnerSuits;

        public bool PartnerNoTrumps;

        public bool PartnerAllTrumps;

        /// <summary>How many times the partner has spoken (passes included).</summary>
        public int PartnerTurns;

        public int OpponentSuits;

        public bool OpponentNoTrumps;

        public bool OpponentAllTrumps;

        /// <summary>How many times the opponents have spoken (passes included).</summary>
        public int OpponentTurns;

        /// <summary>How many contracts the opponents have bid.</summary>
        public int OpponentBids;

        public int MyTurns;

        public int MyBids;

        public static BidSituation From(PlayerGetBidContext context)
        {
            var me = context.MyPosition.Index();
            var situation = new BidSituation
            {
                Hand = CardMemory.ToMask(context.MyCards),
                Position = (me - context.FirstToPlayInTheRound.Index() + 4) & 3,
                Holder = -1,
                Available = context.AvailableBids,
            };
            foreach (var bid in context.Bids)
            {
                situation.Add((bid.Player.Index() - me + 4) & 3, bid.Type);
            }

            return situation;
        }

        /// <summary>Adds the next bid of the auction, made by the given relative seat.</summary>
        public void Add(int relative, BidType bid)
        {
            var partner = relative == 2;
            var opponent = (relative & 1) != 0;
            if (relative == 0)
            {
                this.MyTurns++;
            }
            else if (partner)
            {
                this.PartnerTurns++;
            }
            else
            {
                this.OpponentTurns++;
            }

            if (bid == BidType.Pass)
            {
                return;
            }

            if (bid == BidType.Double || bid == BidType.ReDouble)
            {
                this.Doubled = bid == BidType.Double ? 1 : 2;
                return;
            }

            this.Holder = relative;
            this.Contract = bid;
            this.Doubled = 0;
            if (relative == 0)
            {
                this.MyBids++;
            }
            else if (opponent)
            {
                this.OpponentBids++;
            }

            if (bid == BidType.NoTrumps)
            {
                this.PartnerNoTrumps |= partner;
                this.OpponentNoTrumps |= opponent;
            }
            else if (bid == BidType.AllTrumps)
            {
                this.PartnerAllTrumps |= partner;
                this.OpponentAllTrumps |= opponent;
            }
            else if (partner)
            {
                this.PartnerSuits |= 1 << (int)bid.ToCardSuit();
            }
            else if (opponent)
            {
                this.OpponentSuits |= 1 << (int)bid.ToCardSuit();
            }
        }
    }
}
