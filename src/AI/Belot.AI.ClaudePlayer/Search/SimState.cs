namespace Belot.AI.ClaudePlayer.Search
{
    /// <summary>
    /// A fully known position of the card play, as the simulator advances it. Seats are the
    /// engine's player indexes (South 0, East 1, North 2, West 3), so a seat's team is seat &amp; 1
    /// (0 = South-North, 1 = East-West).
    /// </summary>
    internal struct SimState
    {
        public SeatHands Hands;

        // The seat to play.
        public int Turn;

        // Cards already played to the current trick (0-3).
        public int TrickCards;

        public int LedSuit;

        // Who holds the trick so far, and with which card.
        public int WinnerSeat;

        public int WinnerCard;

        // Points of the cards played to the current trick.
        public int TrickPoints;

        public int TricksPlayed;

        // Points of the tricks won plus the belotes declared (no other announces, no last 10).
        public int SouthNorthPoints;

        public int EastWestPoints;

        public int SouthNorthTricks;

        public int EastWestTricks;

        public int LastTrickTeam;
    }
}
