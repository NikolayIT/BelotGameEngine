namespace JustBelot.Common
{
    public struct DealResult
    {
        public DealResult(bool dealPlayed, Contract contract, int southNorthPoints = 0, int eastWestPoints = 0, bool noTricksForOneOfTheTeams = false)
            : this()
        {
            this.DealPlayed = dealPlayed;
            this.Contract = contract;
            this.EastWestPoints = eastWestPoints;
            this.SouthNorthPoints = southNorthPoints;
            this.NoTricksForOneOfTheTeams = noTricksForOneOfTheTeams;
        }

        public bool DealPlayed { get; private set; }

        public Contract Contract { get; private set; }

        public int SouthNorthPoints { get; private set; }

        public int EastWestPoints { get; private set; }

        public bool NoTricksForOneOfTheTeams { get; private set; }
    }
}
