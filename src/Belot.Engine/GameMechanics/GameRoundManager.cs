namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class GameRoundManager
    {
        private readonly IList<IPlayer> players;

        private readonly ContractManager contractManager;

        private readonly TricksManager tricksManager;

        public GameRoundManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            this.tricksManager = new TricksManager(southPlayer, eastPlayer, northPlayer, westPlayer);
        }

        public RoundResult PlayRound(int roundNumber, PlayerPosition firstToPlay, int southNorthTeamPoints, int eastWestTeamPoints)
        {
            // Initialize deck
            var deck = new Deck();
            var playerCards = new List<CardCollection>(this.players.Count);
            for (var playerIndex = 0; playerIndex < this.players.Count; playerIndex++)
            {
                playerCards.Add(new CardCollection());
            }

            // Deal 5 cards to each player
            this.DealCards(playerCards, deck, 5);

            // Bidding phase
            var contract = this.contractManager.GetContract(
                roundNumber,
                firstToPlay,
                southNorthTeamPoints,
                eastWestTeamPoints,
                playerCards,
                out var bids);

            // All pass
            if (contract == BidType.Pass)
            {
                return new RoundResult
                {
                    NoTricksForOneOfTheTeams = false,
                    EastWestTeamPoints = 0,
                    SouthNorthTeamPoints = 0,
                };
            }

            // Deal 3 more cards to each player
            this.DealCards(playerCards, deck, 3);

            // Play 8 tricks
            this.tricksManager.PlayTricks(
                roundNumber,
                firstToPlay,
                southNorthTeamPoints,
                eastWestTeamPoints,
                playerCards,
                bids,
                contract);

            //// TODO: Score points

            return new RoundResult
            {
                NoTricksForOneOfTheTeams = false,
                EastWestTeamPoints = EnumerableExtensions.Random.Value.Next(0, 13),
                SouthNorthTeamPoints = EnumerableExtensions.Random.Value.Next(0, 13),
            };
        }

        private void DealCards(IList<CardCollection> playerCards, Deck deck, int cardsPerPlayer)
        {
            for (var playerIndex = 0; playerIndex < this.players.Count; playerIndex++)
            {
                for (var i = 0; i < cardsPerPlayer; i++)
                {
                    playerCards[playerIndex].Add(deck.GetNextCard());
                }
            }
        }
    }
}
