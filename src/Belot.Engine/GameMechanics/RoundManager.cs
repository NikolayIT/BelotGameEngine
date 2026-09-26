namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays deals with <see cref="IPlayer"/>s: a loop over the step-by-step <see cref="Round"/>,
    /// which holds the rules. Keeps one deck and one set of hands for all its deals.
    /// </summary>
    public class RoundManager
    {
        private readonly IPlayer[] players;

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
            var round = new Round(
                this.players,
                this.deck,
                this.playerCards,
                roundNumber,
                firstToPlay,
                southNorthPoints,
                eastWestPoints,
                hangingPoints);
            PlayerDriver.Play(this.players, round);
            return round.Result;
        }
    }
}
