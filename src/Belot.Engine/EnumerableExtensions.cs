namespace Belot.Engine
{
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        /// <summary>
        /// Fisher-Yates algorithm AKA the Knuth Shuffle.
        /// It runs in O(n) time.
        /// https://stackoverflow.com/a/110570/1862812.
        /// </summary>
        /// <param name="array">An array to shuffle.</param>
        /// <typeparam name="T">The generic type parameter of the collection.</typeparam>
        public static void Shuffle<T>(this T[] array)
        {
            var n = array.Length;
            while (n > 1)
            {
                // TODO: Replace with .NET 6 Random.Shared.Next
                var k = ThreadSafeRandom.Next(0, n--);
                var temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        // This method is optimized for small collections.
        // More info: https://nickstips.wordpress.com/2010/08/28/c-optimized-extension-method-get-a-random-element-from-a-collection/
        public static T RandomElement<T>(this IEnumerable<T> source)
        {
            // Get a random index
            // TODO: Replace with .NET 6 Random.Shared.Next
            var index = ThreadSafeRandom.Next(0, source.Count());

            // Get the random element by traversing the collection one element at a time.
            var enumerator = source.GetEnumerator();

            // Move down the collection one element at a time.
            // When index is -1 we are at the random element location
            while (index >= 0 && enumerator.MoveNext())
            {
                index--;
            }

            // Return the current element
            return enumerator.Current;
        }
    }
}
