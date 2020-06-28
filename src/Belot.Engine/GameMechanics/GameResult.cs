namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Players;

    public class GameResult
    {
        public PlayerPosition Winners { get; set; }

        public int SouthNorthTeamPoints { get; set; }

        public int EastWestTeamPoints { get; set; }
    }
}
