namespace BelotV2
{
    /// <summary>
    /// Seat/team geometry recovered from the main form's DFM:
    ///   Player1 Tag=1 (South, bottom, human)   player2 Tag=2 (East, right)
    ///   player3 Tag=3 (North, top)             player4 Tag=4 (West, left)
    /// Teams pair opposite seats: {South,North} vs {East,West}, i.e. Tags {1,3} vs {2,4}.
    /// We use 0-based seat = Tag-1, so seats 0,1,2,3 = S,E,N,W; teammates are 0&amp;2 and 1&amp;3;
    /// team = seat &amp; 1. Play proceeds seat -&gt; (seat+1)%4 (AutoPosDirection = counter-clockwise
    /// through the visual Bottom-&gt;Right-&gt;Top-&gt;Left order = Tag 1-&gt;2-&gt;3-&gt;4).
    /// </summary>
    public static class Seats
    {
        public const int Count = 4;

        public static int Next(int seat) => (seat + 1) & 3;

        public static int Partner(int seat) => (seat + 2) & 3;

        public static int TeamOf(int seat) => seat & 1;

        public static bool SameTeam(int a, int b) => ((a ^ b) & 1) == 0;

        public static readonly string[] NamesEn = { "South", "East", "North", "West" };

        // Design-time placeholder names from the main form's NameLabel1..4
        // (South, East, North, West). Cosmetic; the original lets the user rename them.
        public static readonly string[] DefaultNames = { "Sauron", "Lutien", "Aragorn", "Galadriel" };
    }
}
