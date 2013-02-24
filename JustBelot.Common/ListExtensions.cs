namespace JustBelot.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class ListExtensions
    {
        /// <summary>
        /// Shuffle algorithm as seen on page 32 in the book "Algorithms" (4th edition) by Robert Sedgewick
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var array = source.ToList();
            var n = array.Count;
            for (var i = 0; i < n; i++)
            {
                // Exchange a[i] with random element in a[i..n-1]
                var r = i + RandomProvider.Next(0, n - i);
                var temp = array[i];
                array[i] = array[r];
                array[r] = temp;
            }

            return array;
        }

        public static string ElementsAsString<T>(this List<T> source)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < source.Count; i++)
            {
                if (i == 0)
                {
                    sb.Append(source[i]);
                }
                else
                {
                    sb.AppendFormat(" {0}", source[i]);
                }
            }

            return sb.ToString();
        }
    }
}
