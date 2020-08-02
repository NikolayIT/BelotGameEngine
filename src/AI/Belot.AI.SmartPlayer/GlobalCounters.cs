namespace Belot.AI.SmartPlayer
{
    public static class GlobalCounters
    {
        public const int CountersCount = 8;

        public static long[] Counters { get; set; } = new long[CountersCount];
    }
}
