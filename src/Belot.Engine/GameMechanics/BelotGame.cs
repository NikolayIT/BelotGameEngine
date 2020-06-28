namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Players;

    /*  N
     * W E
     *  S
     */
    public class BelotGame : IBelotGame
    {
        private readonly IPlayer southPlayer;

        private readonly IPlayer eastPlayer;

        private readonly IPlayer northPlayer;

        private readonly IPlayer westPlayer;

        private readonly GameRoundManager gameRoundManager;

        public BelotGame(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.southPlayer = southPlayer;
            this.eastPlayer = eastPlayer;
            this.northPlayer = northPlayer;
            this.westPlayer = westPlayer;
            this.gameRoundManager = new GameRoundManager(southPlayer, eastPlayer, northPlayer, westPlayer);
        }

        public GameResult PlayGame(PlayerPosition firstToPlay)
        {
            var southNorthTeamPoints = 0;
            var eastWestTeamPoints = 0;
            while (true)
            {
                var roundResult = this.gameRoundManager.PlayRound(southNorthTeamPoints, eastWestTeamPoints);
                southNorthTeamPoints += roundResult.SouthNorthTeamPoints;
                eastWestTeamPoints += roundResult.EastWestTeamPoints;

                if ((southNorthTeamPoints >= 151 || eastWestTeamPoints >= 151)
                                                && southNorthTeamPoints != eastWestTeamPoints
                                                && !roundResult.NoTricksForOneOfTheTeams)
                {
                    // Game over
                    break;
                }
            }

            var winner = southNorthTeamPoints > eastWestTeamPoints
                             ? PlayerPosition.SouthNorthTeam
                             : PlayerPosition.EastWestTeam;
            return new GameResult
            {
                Winners = winner,
                SouthNorthTeamPoints = southNorthTeamPoints,
                EastWestTeamPoints = eastWestTeamPoints,
            };
        }
    }
}
