namespace JustBelot.Common
{
    using System;

    public static class RandomProvider
    {
        private static object syncRoot = new object();
        private static volatile Random instance;

        public static Random Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Random();
                        }
                    }
                }

                return instance;
            }
        }
    }
}
