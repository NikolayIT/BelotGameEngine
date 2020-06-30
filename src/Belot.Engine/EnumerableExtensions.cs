namespace Belot.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public static class EnumerableExtensions
    {
        // https://stackoverflow.com/questions/19270507/correct-way-to-use-random-in-multithread-application
        public static readonly ThreadLocal<Random> Random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        private static int seed = Environment.TickCount;

        /// <summary>
        /// Shuffle algorithm as seen on page 32 in the book "Algorithms" (4th edition) by Robert Sedgewick.
        /// </summary>
        /// <param name="source">Collection to shuffle.</param>
        /// <typeparam name="T">The generic type parameter of the collection.</typeparam>
        /// <returns>The shuffled collection as IEnumerable.</returns>
        public static IList<T> Shuffle<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var array = source.ToArray();
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                // Exchange a[i] with random element in a[i..n-1]
                var r = i + Random.Value.Next(0, n - i);
                var temp = array[i];
                array[i] = array[r];
                array[r] = temp;
            }

            return array;
        }

        public static T RandomElement<T>(this IList<T> source)
        {
            return source[Random.Value.Next(0, source.Count)];
        }
    }
}
