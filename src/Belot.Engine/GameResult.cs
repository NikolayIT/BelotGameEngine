namespace Belot.Engine
{
    using Belot.Engine.Players;

    public class GameResult
    {
        public PlayerPosition Winner =>
            this.SouthNorthPoints > this.EastWestPoints
                ? PlayerPosition.SouthNorthTeam
                : PlayerPosition.EastWestTeam;

        public int RoundsPlayed { get; set; }

        public int SouthNorthPoints { get; set; }

        public int EastWestPoints { get; set; }
    }
}
