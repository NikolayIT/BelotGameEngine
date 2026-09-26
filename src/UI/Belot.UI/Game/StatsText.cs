namespace Belot.UI.Game
{
    using System;
    using System.Globalization;

    /// <summary>
    /// How the statistics show their numbers. No MAUI types here: the UI tests compile this file.
    /// </summary>
    public static class StatsText
    {
        /// <summary>
        /// The share of games won, with one decimal when there is one, in the culture's format:
        /// 1 of 8 is "12,5%" in Bulgarian ("12.5%" in English), 1 of 2 "50%", 1 of 3 "33,3%".
        /// "—" before the first game.
        /// </summary>
        public static string WinRate(int wins, int games, IFormatProvider? culture = null) =>
            games > 0
                ? (100.0 * wins / games).ToString("0.#", culture ?? CultureInfo.CurrentCulture) + "%"
                : "—";
    }
}
