namespace Belot.Engine
{
    using System;
    using System.Threading;

    // TODO: Replace with .NET 6 Random.Shared.Next
    // https://stackoverflow.com/a/9310486/1862812
    public static class ThreadSafeRandom
    {
        private static readonly ThreadLocal<Random> LocalRandom = new ThreadLocal<Random>(() =>
            {
                var seed = Interlocked.Increment(ref staticSeed) & 0x7FFFFFFF;
                return new Random(seed);
            });

        private static int staticSeed = Environment.TickCount;

        public static int Next(int min, int max) => LocalRandom.Value.Next(min, max);
    }
}
