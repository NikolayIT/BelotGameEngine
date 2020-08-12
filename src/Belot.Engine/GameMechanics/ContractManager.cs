namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ContractManager
    {
        private readonly IList<IPlayer> players;

        public ContractManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
        }

        public Bid GetContract(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            IReadOnlyList<CardCollection> playerCards,
            out IList<Bid> bids)
        {
            bids = new List<Bid>(8);
            var consecutivePasses = 0;
            var currentPlayerPosition = firstToPlay;
            var contract = new Bid(currentPlayerPosition, BidType.Pass);
            var bidContext = new PlayerGetBidContext
            {
                RoundNumber = roundNumber,
                FirstToPlayInTheRound = firstToPlay,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                Bids = bids,
            };
            while (true)
            {
                var availableBids = this.GetAvailableBids(contract, currentPlayerPosition);

                BidType bid;
                if (availableBids == BidType.Pass)
                {
                    // Only pass is available so we don't ask the player
                    bid = BidType.Pass;
                }
                else
                {
                    // Prepare context
                    bidContext.AvailableBids = availableBids;
                    bidContext.MyCards = playerCards[currentPlayerPosition.Index()];
                    bidContext.MyPosition = currentPlayerPosition;
                    bidContext.CurrentContract = contract;

                    // Execute GetBid()
                    bid = this.players[currentPlayerPosition.Index()].GetBid(bidContext);

                    // Validate
                    if (bid != BidType.Pass && (bid & (bid - 1)) != 0)
                    {
                        throw new BelotGameException($"Invalid bid from {currentPlayerPosition} player. More than 1 flags returned.");
                    }

                    if (!availableBids.HasFlag(bid))
                    {
                        throw new BelotGameException($"Invalid bid from {currentPlayerPosition} player. This bid is not permitted.");
                    }

                    if (bid == BidType.Double || bid == BidType.ReDouble)
                    {
                        contract.Type &= ~BidType.Double;
                        contract.Type &= ~BidType.ReDouble;
                        contract.Type |= bid;
                        contract.Player = currentPlayerPosition;
                    }
                    else if (bid != BidType.Pass)
                    {
                        contract.Type = bid;
                        contract.Player = currentPlayerPosition;
                    }
                }

                bids.Add(new Bid(currentPlayerPosition, bid));

                consecutivePasses = (bid == BidType.Pass) ? consecutivePasses + 1 : 0;
                if (contract.Type == BidType.Pass && consecutivePasses == 4)
                {
                    break;
                }

                if (contract.Type != BidType.Pass && consecutivePasses == 3)
                {
                    break;
                }

                currentPlayerPosition = currentPlayerPosition.Next();
            }

            return contract;
        }

        private BidType GetAvailableBids(
            Bid currentContract,
            PlayerPosition currentPlayer)
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

            if (!currentPlayer.IsInSameTeamWith(currentContract.Player) && currentContract.Type != BidType.Pass)
            {
                if (currentContract.Type.HasFlag(BidType.Double))
                {
                    availableBids |= BidType.ReDouble;
                }
                else if (currentContract.Type.HasFlag(BidType.ReDouble))
                {
                }
                else
                {
                    availableBids |= BidType.Double;
                }
            }

            return availableBids;
        }
    }
}
