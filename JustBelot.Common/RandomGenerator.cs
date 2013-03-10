namespace JustBelot.Common
{
    using System;

    public static class RandomProvider
    {
        [ThreadStatic]
        public static readonly Random Instance;

        static RandomProvider()
        {
            Instance = new Random();
        }
    }
}
