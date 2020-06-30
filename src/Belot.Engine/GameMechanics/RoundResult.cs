namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Game;

    public class RoundResult
    {
        public RoundResult(Bid contract)
        {
            this.Contract = contract;
        }

        public Bid Contract { get; set; }

        public int SouthNorthPoints { get; set; }

        public int SouthNorthTotalInRoundPoints { get; set; }

        public int EastWestPoints { get; set; }

        public int EastWestTotalInRoundPoints { get; set; }

        public bool NoTricksForOneOfTheTeams { get; set; }

        public int HangingPoints { get; set; }
    }
}
