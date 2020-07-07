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
        private readonly RoundManager roundManager;

        public BelotGame(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.roundManager = new RoundManager(southPlayer, eastPlayer, northPlayer, westPlayer);
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
                var roundResult = this.roundManager.PlayRound(
                    roundNumber,
                    firstInRound,
                    southNorthPoints,
                    eastWestPoints,
                    hangingPoints);

                southNorthPoints += roundResult.SouthNorthPoints;
                eastWestPoints += roundResult.EastWestPoints;
                hangingPoints = roundResult.HangingPoints;

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

            return new GameResult
            {
                RoundsPlayed = roundNumber,
                SouthNorthPoints = southNorthPoints,
                EastWestPoints = eastWestPoints,
            };
        }
    }
}
