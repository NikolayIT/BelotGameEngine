namespace Belot.Engine
{
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /*  N
     * W E
     *  S
     */
    public class BelotGame : IBelotGame
    {
        private readonly GameRoundManager gameRoundManager;

        public BelotGame(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.gameRoundManager = new GameRoundManager(southPlayer, eastPlayer, northPlayer, westPlayer);
        }

        public GameResult PlayGame(PlayerPosition firstToPlay)
        {
            var southNorthTeamPoints = 0;
            var eastWestTeamPoints = 0;
            var firstInRound = firstToPlay;
            var roundNumber = 1;
            var hangingPoints = 0;

            while (true)
            {
                var roundResult = this.gameRoundManager.PlayRound(
                    roundNumber,
                    firstInRound,
                    southNorthTeamPoints,
                    eastWestTeamPoints,
                    hangingPoints);

                southNorthTeamPoints += roundResult.SouthNorthPoints;
                eastWestTeamPoints += roundResult.EastWestPoints;
                hangingPoints = roundResult.HangingPoints;

                // TODO: Inside?
                if ((southNorthTeamPoints >= 151 || eastWestTeamPoints >= 151)
                    && southNorthTeamPoints != eastWestTeamPoints
                    && !roundResult.NoTricksForOneOfTheTeams
                    && roundResult.Contract.Type != BidType.Pass)
                {
                    // Game over
                    break;
                }

                roundNumber++;
                firstInRound = firstInRound.Next();
            }

            return new GameResult
            {
                RoundsPlayed = roundNumber,
                SouthNorthTeamPoints = southNorthTeamPoints,
                EastWestTeamPoints = eastWestTeamPoints,
            };
        }
    }
}
