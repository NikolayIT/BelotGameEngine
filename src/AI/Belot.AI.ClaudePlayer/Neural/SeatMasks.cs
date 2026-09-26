namespace Belot.AI.ClaudePlayer.Neural
{
    using System.Runtime.CompilerServices;

    /// <summary>A card mask per seat (South, East, North, West).</summary>
    [InlineArray(4)]
    internal struct SeatMasks
    {
        private uint mask;
    }
}
