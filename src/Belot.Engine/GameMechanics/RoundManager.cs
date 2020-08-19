namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class RoundManager
    {
        private readonly IList<IPlayer> players;

        private readonly ContractManager contractManager;

        private readonly TricksManager tricksManager;

        private readonly ScoreManager scoreManager;

        private readonly Deck deck;

        private readonly List<CardCollection> playerCards;

        public RoundManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            this.tricksManager = new TricksManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            this.scoreManager = new ScoreManager();
            this.deck = new Deck();
            this.playerCards = new List<CardCollection>(this.players.Count);
            for (var playerIndex = 0; playerIndex < this.players.Count; playerIndex++)
            {
                this.playerCards.Add(new CardCollection());
            }
        }

        public RoundResult PlayRound(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            int hangingPoints)
        {
            // Initialize the cards
            this.deck.Shuffle();
            for (var playerIndex = 0; playerIndex < this.players.Count; playerIndex++)
            {
                this.playerCards[playerIndex].Clear();
            }

            // Deal 5 cards to each player
            this.DealCards(5);

            // Bidding phase
            var contract = this.contractManager.GetContract(
                roundNumber,
                firstToPlay,
                southNorthPoints,
                eastWestPoints,
                this.playerCards,
                out var bids);

            // All pass
            if (contract.Type == BidType.Pass)
            {
                return new RoundResult(contract);
            }

            // Deal 3 more cards to each player
            this.DealCards(3);

            // Play 8 tricks
            this.tricksManager.PlayTricks(
                roundNumber,
                firstToPlay,
                southNorthPoints,
                eastWestPoints,
                this.playerCards,
                bids,
                contract,
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            // Score points
            var result = this.scoreManager.GetScore(
                contract,
                southNorthTricks,
                eastWestTricks,
                announces,
                hangingPoints,
                lastTrickWinner);

            foreach (var player in this.players)
            {
                player.EndOfRound(result);
            }

            return result;
        }

        private void DealCards(int cardsPerPlayer)
        {
            for (var playerIndex = 0; playerIndex < this.players.Count; playerIndex++)
            {
                for (var i = 0; i < cardsPerPlayer; i++)
                {
                    this.playerCards[playerIndex].Add(this.deck.GetNextCard());
                }
            }
        }
    }
}
