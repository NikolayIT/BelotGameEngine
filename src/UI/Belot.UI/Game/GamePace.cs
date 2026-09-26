namespace Belot.UI.Game
{
    /// <summary>
    /// The pauses of the game: the computer's thinking time, how long a finished trick stays on
    /// the table, and the pause before a card or pass the rules made for somebody.
    /// </summary>
    public readonly record struct GamePace(int ThinkDelayMs, int TrickSettleMs, int AutoMoveDelayMs)
    {
        /// <summary>Gets no pauses at all (tests).</summary>
        public static GamePace Instant => new(0, 0, 0);
    }
}
