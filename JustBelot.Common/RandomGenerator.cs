namespace JustBelot.Common
{
    using System;

    public static class RandomProvider
    {
        private static readonly Random RandomGenerator = new Random();
        private static readonly object LockObject = new object();

        public static int Next()
        {
            lock (LockObject)
            {
                return RandomGenerator.Next();
            }
        }

        public static int Next(int max)
        {
            lock (LockObject)
            {
                return RandomGenerator.Next(max);
            }
        }

        public static int Next(int min, int max)
        {
            lock (LockObject)
            {
                return RandomGenerator.Next(min, max);
            }
        }
    }
}
