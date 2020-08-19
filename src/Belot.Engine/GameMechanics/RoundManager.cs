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
            this.playerCards[0].Clear();
            this.playerCards[1].Clear();
            this.playerCards[2].Clear();
            this.playerCards[3].Clear();

            // Deal 5 cards to each player
            for (var i = 0; i < 5; i++)
            {
                this.playerCards[0].Add(this.deck.GetNextCard());
                this.playerCards[1].Add(this.deck.GetNextCard());
                this.playerCards[2].Add(this.deck.GetNextCard());
                this.playerCards[3].Add(this.deck.GetNextCard());
            }

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
            for (var i = 0; i < 3; i++)
            {
                this.playerCards[0].Add(this.deck.GetNextCard());
                this.playerCards[1].Add(this.deck.GetNextCard());
                this.playerCards[2].Add(this.deck.GetNextCard());
                this.playerCards[3].Add(this.deck.GetNextCard());
            }

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

            this.players[0].EndOfRound(result);
            this.players[1].EndOfRound(result);
            this.players[2].EndOfRound(result);
            this.players[3].EndOfRound(result);

            return result;
        }
    }
}
