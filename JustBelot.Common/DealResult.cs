namespace JustBelot.Common
{
    internal struct DealResult
    {
        public DealResult(bool dealPlayed, int eastWestPoints = 0, int southNorthPoints = 0)
            : this()
        {
            this.DealPlayed = dealPlayed;
            this.EastWestPoints = eastWestPoints;
            this.SouthNorthPoints = southNorthPoints;
        }

        public bool DealPlayed { get; private set; }

        public int EastWestPoints { get; private set; }

        public int SouthNorthPoints { get; private set; }
    }
}
