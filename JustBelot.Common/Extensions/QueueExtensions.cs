namespace JustBelot.Common.Extensions
{
    using System.Collections.Generic;

    public static class QueueExtensions
    {
        /// <summary>
        /// Shuffle algorithm as seen on page 32 in the book "Algorithms" (4th edition) by Robert Sedgewick
        /// </summary>
        public static void Shuffle<T>(this Queue<T> source)
        {
            var tempArr = source.ToArray();

            var n = tempArr.Length;
            for (var i = 0; i < n; i++)
            {
                // Exchange a[i] with random element in a[i..n-1]
                var r = i + RandomProvider.Instance.Next(0, n - i);
                var temp = tempArr[i];
                tempArr[i] = tempArr[r];
                tempArr[r] = temp;
            }

            source.Clear();
            foreach (var element in tempArr)
            {
                source.Enqueue(element);
            }
        }
    }
}
