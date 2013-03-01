namespace JustBelot.Common
{
    internal struct DealResult
    {
        public DealResult(bool dealPlayed, Contract contract, int eastWestPoints = 0, int southNorthPoints = 0)
            : this()
        {
            this.DealPlayed = dealPlayed;
            this.Contract = contract;
            this.EastWestPoints = eastWestPoints;
            this.SouthNorthPoints = southNorthPoints;
        }

        public bool DealPlayed { get; private set; }

        public Contract Contract { get; set; }

        public int EastWestPoints { get; private set; }

        public int SouthNorthPoints { get; private set; }
    }
}
