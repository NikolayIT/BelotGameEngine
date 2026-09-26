namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class RoundManager
    {
        private readonly IPlayer[] players;

        private readonly ContractManager contractManager;

        private readonly TricksManager tricksManager;

        private readonly ScoreManager scoreManager;

        private readonly Deck deck;

        private readonly List<CardCollection> playerCards;

        public RoundManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
            : this(southPlayer, eastPlayer, northPlayer, westPlayer, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundManager"/> class.
        /// </summary>
        /// <param name="southPlayer">The South player.</param>
        /// <param name="eastPlayer">The East player.</param>
        /// <param name="northPlayer">The North player.</param>
        /// <param name="westPlayer">The West player.</param>
        /// <param name="random">The source of the deals' shuffles (see <see cref="Deck(Random)"/>);
        /// null for a shared per-thread one.</param>
        public RoundManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer, Random random)
        {
            this.players = new[] { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.contractManager = new ContractManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            this.tricksManager = new TricksManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            this.scoreManager = new ScoreManager();
            this.deck = new Deck(random);
            this.playerCards = new List<CardCollection>(this.players.Length);
            for (var playerIndex = 0; playerIndex < this.players.Length; playerIndex++)
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

            // All pass. Hanging points stay on the table for the winner of the next played deal.
            if (contract.Type == BidType.Pass)
            {
                var passResult = new RoundResult(contract) { HangingPoints = hangingPoints };
                this.NotifyEndOfRound(passResult);
                return passResult;
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

            this.NotifyEndOfRound(result);
            return result;
        }

        private void NotifyEndOfRound(RoundResult result)
        {
            this.players[0].EndOfRound(result);
            this.players[1].EndOfRound(result);
            this.players[2].EndOfRound(result);
            this.players[3].EndOfRound(result);
        }
    }
}
