namespace Belot.AI.ClaudePlayer.Neural
{
    using System.Runtime.CompilerServices;

    /// <summary>A byte per seat (South, East, North, West).</summary>
    [InlineArray(4)]
    internal struct SeatBytes
    {
        private byte value;
    }
}
