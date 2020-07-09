namespace Belot.AI.SmartPlayer
{
    public static class GlobalCounters
    {
        public const int CountersCount = 8;

        public static int[] Counters { get; set; } = new int[CountersCount];
    }
}
