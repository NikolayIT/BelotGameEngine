namespace Belot.AI.ClaudePlayer.Search
{
    using Belot.Engine.Game;

    /// <summary>
    /// A combination as the search knows it: who declared it, what it is, and its rank (the top
    /// card's type of a sequence, the type of a carre), or -1 when the rank is hidden.
    /// </summary>
    internal struct DeclaredAnnounce
    {
        public int Seat;

        public AnnounceType Type;

        public int Rank;

        public DeclaredAnnounce(int seat, AnnounceType type, int rank)
        {
            this.Seat = seat;
            this.Type = type;
            this.Rank = rank;
        }
    }
}
