namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Runs the bidding of a deal with <see cref="IPlayer"/>s: a loop over the step-by-step
    /// <see cref="Auction"/>, which holds the rules.
    /// </summary>
    public class ContractManager
    {
        private readonly IPlayer[] players;

        public ContractManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new[] { southPlayer, eastPlayer, northPlayer, westPlayer };
        }

        public Bid GetContract(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            IReadOnlyList<CardCollection> playerCards,
            out IList<Bid> bids)
        {
            var auction = new Auction(roundNumber, firstToPlay, southNorthPoints, eastWestPoints, playerCards);
            while (!auction.IsFinished)
            {
                PlayerDriver.Bid(this.players, auction);
            }

            bids = auction.Bids;
            return auction.Contract;
        }
    }
}
