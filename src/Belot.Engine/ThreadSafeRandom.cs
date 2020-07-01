namespace Belot.Engine
{
    using System;

    // https://stackoverflow.com/a/11109361/1862812
    public static class ThreadSafeRandom
    {
        private static readonly Random GlobalRandom = new Random();

        [ThreadStatic]
        private static Random localRandom;

        public static int Next(int min, int max)
        {
            if (localRandom == null)
            {
                lock (GlobalRandom)
                {
                    if (localRandom == null)
                    {
                        var seed = GlobalRandom.Next();
                        localRandom = new Random(seed);
                    }
                }
            }

            return localRandom.Next(min, max);
        }
    }
}
