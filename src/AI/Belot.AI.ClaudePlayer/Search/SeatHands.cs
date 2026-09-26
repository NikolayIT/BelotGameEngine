namespace Belot.AI.ClaudePlayer.Search
{
    using System.Runtime.CompilerServices;

    /// <summary>The four hands as card masks, indexed by seat (South, East, North, West).</summary>
    [InlineArray(4)]
    internal struct SeatHands
    {
        private uint hand;
    }
}
