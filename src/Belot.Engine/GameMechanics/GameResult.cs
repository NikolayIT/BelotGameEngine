namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Players;

    public class GameResult
    {
        public PlayerPosition Winner =>
            this.SouthNorthTeamPoints > this.EastWestTeamPoints
                ? PlayerPosition.SouthNorthTeam
                : PlayerPosition.EastWestTeam;

        public int RoundsPlayed { get; set; }

        public int SouthNorthTeamPoints { get; set; }

        public int EastWestTeamPoints { get; set; }
    }
}
