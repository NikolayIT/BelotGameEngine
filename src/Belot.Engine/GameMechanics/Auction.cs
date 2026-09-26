namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// The bidding of one deal as a step-by-step state machine: it never asks anybody for a bid.
    /// Whoever drives it reads <see cref="ToMove"/> (and <see cref="Context"/>, what GetBid
    /// receives), checks the bid with <see cref="Check"/> and passes it to <see cref="Apply"/>. A
    /// seat whose only possible bid is Pass passes without being asked. It ends after three
    /// passes in a row once somebody has bid, or four when nobody has.
    /// </summary>
    internal sealed class Auction
    {
        private readonly IReadOnlyList<CardCollection> playerCards;

        private readonly List<Bid> bids;

        private readonly Bid contract;

        private readonly PlayerGetBidContext context;

        private int consecutivePasses;

        private PlayerPosition current;

        public Auction(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            IReadOnlyList<CardCollection> playerCards,
            int hangingPoints = 0)
        {
            this.playerCards = playerCards;
            this.bids = new List<Bid>(8);
            this.current = firstToPlay;
            this.contract = new Bid(firstToPlay, BidType.Pass);
            this.context = new PlayerGetBidContext
            {
                RoundNumber = roundNumber,
                FirstToPlayInTheRound = firstToPlay,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                HangingPoints = hangingPoints,
                Bids = this.bids,
            };

            this.MoveToTheNextBidder();
        }

        public bool IsFinished { get; private set; }

        /// <summary>Gets the seat to bid, or <see cref="PlayerPosition.Unknown"/> once the bidding is over.</summary>
        public PlayerPosition ToMove => this.IsFinished ? PlayerPosition.Unknown : this.current;

        /// <summary>Gets the contract so far (the final one once finished). The object is live.</summary>
        public Bid Contract => this.contract;

        /// <summary>Gets every bid so far, the passes of seats that could only pass included. Live.</summary>
        public List<Bid> Bids => this.bids;

        /// <summary>Gets what the seat to bid receives. Live: it changes as the bidding goes on.</summary>
        public PlayerGetBidContext Context => this.context;

        /// <summary>The bids open to <paramref name="player"/> after <paramref name="currentContract"/>.</summary>
        public static BidType GetAvailableBids(Bid currentContract, PlayerPosition player)
        {
            var cleanContract = currentContract.Type;
            cleanContract &= ~BidType.Double;
            cleanContract &= ~BidType.ReDouble;
            var availableBids = BidType.Pass;
            if (cleanContract < BidType.Clubs)
            {
                availableBids |= BidType.Clubs;
            }

            if (cleanContract < BidType.Diamonds)
            {
                availableBids |= BidType.Diamonds;
            }

            if (cleanContract < BidType.Hearts)
            {
                availableBids |= BidType.Hearts;
            }

            if (cleanContract < BidType.Spades)
            {
                availableBids |= BidType.Spades;
            }

            if (cleanContract < BidType.NoTrumps)
            {
                availableBids |= BidType.NoTrumps;
            }

            if (cleanContract < BidType.AllTrumps)
            {
                availableBids |= BidType.AllTrumps;
            }

            // The opponents of the declarer may double; then the declaring team may redouble.
            if (currentContract.Type != BidType.Pass && !currentContract.Type.HasFlag(BidType.ReDouble))
            {
                var isDeclaringTeam = player.IsInSameTeamWith(currentContract.Player);
                if (currentContract.Type.HasFlag(BidType.Double))
                {
                    if (isDeclaringTeam)
                    {
                        availableBids |= BidType.ReDouble;
                    }
                }
                else if (!isDeclaringTeam)
                {
                    availableBids |= BidType.Double;
                }
            }

            return availableBids;
        }

        /// <summary>Whether the seat to bid may make this bid.</summary>
        public BidCheck Check(BidType bid)
        {
            if (bid != BidType.Pass && (bid & (bid - 1)) != 0)
            {
                return BidCheck.MoreThanOneFlag;
            }

            return this.context.AvailableBids.HasFlag(bid) ? BidCheck.Ok : BidCheck.NotPermitted;
        }

        /// <summary>Makes the bid of the seat to bid (see <see cref="Check"/>) and goes on.</summary>
        public void Apply(BidType bid)
        {
            if (bid == BidType.Double || bid == BidType.ReDouble)
            {
                // Doubling only multiplies the contract: it stays with the declarer.
                this.contract.Type &= ~BidType.Double;
                this.contract.Type &= ~BidType.ReDouble;
                this.contract.Type |= bid;
            }
            else if (bid != BidType.Pass)
            {
                this.contract.Type = bid;
                this.contract.Player = this.current;
            }

            if (!this.Record(bid))
            {
                this.current = this.current.Next();
                this.MoveToTheNextBidder();
            }
        }

        // Passes for the seats that can only pass, up to one that has a choice (or the end).
        private void MoveToTheNextBidder()
        {
            while (true)
            {
                var availableBids = GetAvailableBids(this.contract, this.current);
                if (availableBids != BidType.Pass)
                {
                    this.context.AvailableBids = availableBids;
                    this.context.MyCards = this.playerCards[this.current.Index()];
                    this.context.MyPosition = this.current;
                    this.context.CurrentContract = this.contract;
                    return;
                }

                if (this.Record(BidType.Pass))
                {
                    return;
                }

                this.current = this.current.Next();
            }
        }

        // Adds the bid to the auction; true when that ends it.
        private bool Record(BidType bid)
        {
            this.bids.Add(new Bid(this.current, bid));
            this.consecutivePasses = bid == BidType.Pass ? this.consecutivePasses + 1 : 0;
            if ((this.contract.Type == BidType.Pass && this.consecutivePasses == 4)
                || (this.contract.Type != BidType.Pass && this.consecutivePasses == 3))
            {
                this.IsFinished = true;
            }

            return this.IsFinished;
        }
    }
}
