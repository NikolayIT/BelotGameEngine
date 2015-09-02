namespace JustBelot.Common
{
    public struct DealResult
    {
        public DealResult(bool dealPlayed, Contract contract, int southNorthPoints = 0, int eastWestPoints = 0, int hangingPoints = 0, bool contractNotKept = false, bool noTricksForOneOfTheTeams = false)
            : this()
        {
            this.DealPlayed = dealPlayed;
            this.Contract = contract;
            this.EastWestPoints = eastWestPoints;
            this.SouthNorthPoints = southNorthPoints;
            this.HangingPoints = hangingPoints;
            this.ContractNotKept = contractNotKept;
            this.NoTricksForOneOfTheTeams = noTricksForOneOfTheTeams;
        }

        public bool DealPlayed { get; private set; }

        public Contract Contract { get; private set; }

        public int SouthNorthPoints { get; private set; }

        public int EastWestPoints { get; private set; }

        public int HangingPoints { get; private set; }

        public bool ContractNotKept { get; private set; }

        public bool NoTricksForOneOfTheTeams { get; private set; }
    }
}
