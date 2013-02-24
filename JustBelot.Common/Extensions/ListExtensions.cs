namespace JustBelot.Common.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class ListExtensions
    {
        /// <summary>
        /// Shuffle algorithm as seen on page 32 in the book "Algorithms" (4th edition) by Robert Sedgewick
        /// </summary>
        public static void Shuffle<T>(this List<T> source)
        {
            var n = source.Count;
            for (var i = 0; i < n; i++)
            {
                // Exchange a[i] with random element in a[i..n-1]
                var r = i + RandomProvider.Next(0, n - i);
                var temp = source[i];
                source[i] = source[r];
                source[r] = temp;
            }
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
