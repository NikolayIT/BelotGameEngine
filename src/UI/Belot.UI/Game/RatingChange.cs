namespace Belot.UI.Game
{
    public sealed class RatingChange
    {
        public RatingChange(int oldElo, int newElo, bool won)
        {
            this.OldElo = oldElo;
            this.NewElo = newElo;
            this.Won = won;
        }

        public int OldElo { get; }

        public int NewElo { get; }

        public bool Won { get; }

        public int Delta => this.NewElo - this.OldElo;
    }
}
