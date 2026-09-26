namespace Belot.Engine
{
    using System;

    using Belot.Engine.Cards;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /*  N
     * W E
     *  S
     */

    /// <summary>
    /// Plays whole games between four <see cref="IPlayer"/>s: a thin loop over
    /// <see cref="BelotMatch"/> that asks the seat to move for its decision and applies it. The
    /// rules live in <see cref="BelotMatch"/>; use it directly when decisions arrive from outside
    /// (a person at a UI, the network) instead of from an <see cref="IPlayer"/>.
    /// </summary>
    public class BelotGame : IBelotGame
    {
        private readonly IPlayer[] players;

        private readonly Deck deck;

        // (roundNumber, firstToPlay, southNorthPoints, eastWestPoints, hangingPoints) => result:
        // rounds scripted by tests instead of played; null in real games.
        private readonly Func<int, PlayerPosition, int, int, int, RoundResult> scriptedRounds;

        public BelotGame(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
            : this(southPlayer, eastPlayer, northPlayer, westPlayer, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BelotGame"/> class.
        /// </summary>
        /// <param name="southPlayer">The South player.</param>
        /// <param name="eastPlayer">The East player.</param>
        /// <param name="northPlayer">The North player.</param>
        /// <param name="westPlayer">The West player.</param>
        /// <param name="random">The source of the deals' shuffles; null for a shared per-thread
        /// one. Every deal shuffles the whole deck once, so with a seeded source the n-th deal of
        /// a game depends only on the seed and n, whatever the players do: the same seed with
        /// the teams swapped replays the same deals (a mirror match).</param>
        public BelotGame(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer, Random random)
        {
            this.players = new[] { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.deck = new Deck(random);
        }

        // Lets tests script the round results directly.
        internal BelotGame(IPlayer[] players, Func<int, PlayerPosition, int, int, int, RoundResult> playRound)
        {
            this.players = players;
            this.scriptedRounds = playRound;
        }

        public GameResult PlayGame(PlayerPosition firstToPlay = PlayerPosition.South)
        {
            // The same deck for every game of this instance, as its deals always were.
            var match = new BelotMatch(this.players, this.deck, firstToPlay, this.scriptedRounds);
            match.Start();
            match.PlayWith(this.players);
            return match.Result;
        }
    }
}
