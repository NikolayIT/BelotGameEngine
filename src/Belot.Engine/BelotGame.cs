namespace Belot.Engine
{
    using System;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /*  N
     * W E
     *  S
     */
    public class BelotGame : IBelotGame
    {
        // (roundNumber, firstToPlay, southNorthPoints, eastWestPoints, hangingPoints) => result
        private readonly Func<int, PlayerPosition, int, int, int, RoundResult> playRound;

        private readonly IPlayer[] players;

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
            : this(
                new[] { southPlayer, eastPlayer, northPlayer, westPlayer },
                new RoundManager(southPlayer, eastPlayer, northPlayer, westPlayer, random).PlayRound)
        {
        }

        // Lets tests script the round results directly.
        internal BelotGame(IPlayer[] players, Func<int, PlayerPosition, int, int, int, RoundResult> playRound)
        {
            this.players = players;
            this.playRound = playRound;
        }

        public GameResult PlayGame(PlayerPosition firstToPlay = PlayerPosition.South)
        {
            var southNorthPoints = 0;
            var eastWestPoints = 0;
            var firstInRound = firstToPlay;
            var roundNumber = 1;
            var hangingPoints = 0;

            while (true)
            {
                var roundResult = this.playRound(
                    roundNumber,
                    firstInRound,
                    southNorthPoints,
                    eastWestPoints,
                    hangingPoints);

                southNorthPoints += roundResult.SouthNorthPoints;
                eastWestPoints += roundResult.EastWestPoints;
                hangingPoints = roundResult.HangingPoints;

                // A team wins with 151+ and more points than the other team, on a deal in which it
                // scored. A capot deal never ends the game ("С капо не се излиза"), and neither does
                // a passed-out one, so after them the leader must score again in a later deal.
                if (southNorthPoints >= 151
                    && southNorthPoints > eastWestPoints
                    && roundResult.SouthNorthPoints > 0
                    && !roundResult.NoTricksForOneOfTheTeams
                    && roundResult.Contract.Type != BidType.Pass)
                {
                    // Game over - south-north team wins
                    break;
                }

                if (eastWestPoints >= 151
                    && eastWestPoints > southNorthPoints
                    && roundResult.EastWestPoints > 0
                    && !roundResult.NoTricksForOneOfTheTeams
                    && roundResult.Contract.Type != BidType.Pass)
                {
                    // Game over - east-west team wins
                    break;
                }

                roundNumber++;
                firstInRound = firstInRound.Next();
            }

            var gameResult = new GameResult
                                 {
                                     RoundsPlayed = roundNumber,
                                     SouthNorthPoints = southNorthPoints,
                                     EastWestPoints = eastWestPoints,
                                 };

            this.players[0].EndOfGame(gameResult);
            this.players[1].EndOfGame(gameResult);
            this.players[2].EndOfGame(gameResult);
            this.players[3].EndOfGame(gameResult);

            return gameResult;
        }
    }
}
