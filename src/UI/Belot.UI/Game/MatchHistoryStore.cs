namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Finished games, persisted on the device (see <see cref="SettingsStore"/>). Stored as one
    /// delimited line per game (pipe-separated fields, newline-separated records) — no JSON, so
    /// it's trimming/AOT-safe on every platform. Newest first, capped at <see cref="MaxEntries"/>.
    /// </summary>
    public static class MatchHistoryStore
    {
        private const string Key = "match.history";
        private const int MaxEntries = 100;
        private const char FieldSeparator = '|';
        private const char RecordSeparator = '\n';

        public static IReadOnlyList<MatchHistoryEntry> All()
        {
            var raw = SettingsStore.Current.Get(Key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return Array.Empty<MatchHistoryEntry>();
            }

            var list = new List<MatchHistoryEntry>();
            foreach (var record in raw.Split(RecordSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                // won|us|them|partner|west|east|when
                var fields = record.Split(FieldSeparator);
                if (fields.Length != 7 || (fields[0] != "0" && fields[0] != "1")
                    || !TryPoints(fields[1], out var us) || !TryPoints(fields[2], out var them)
                    || string.IsNullOrWhiteSpace(fields[3]) || string.IsNullOrWhiteSpace(fields[4]) || string.IsNullOrWhiteSpace(fields[5])
                    || !DateTime.TryParse(fields[6], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when))
                {
                    continue;
                }

                list.Add(new MatchHistoryEntry(
                    fields[3],
                    fields[4],
                    fields[5],
                    us,
                    them,
                    fields[0] == "1",
                    when));
                if (list.Count == MaxEntries)
                {
                    break;
                }
            }

            return list;
        }

        public static void Add(MatchHistoryEntry entry)
        {
            var list = All().ToList();
            list.Insert(0, entry);
            if (list.Count > MaxEntries)
            {
                list = list.GetRange(0, MaxEntries);
            }

            SettingsStore.Current.Set(Key, string.Join(RecordSeparator.ToString(), list.Select(Encode)));
        }

        public static void Clear() => SettingsStore.Current.Remove(Key);

        private static string Encode(MatchHistoryEntry e) => string.Join(
            FieldSeparator.ToString(),
            e.Won ? "1" : "0",
            e.UsPoints.ToString(CultureInfo.InvariantCulture),
            e.ThemPoints.ToString(CultureInfo.InvariantCulture),
            Sanitize(e.PartnerId),
            Sanitize(e.WestId),
            Sanitize(e.EastId),
            e.WhenUtc.ToString("o", CultureInfo.InvariantCulture));

        private static string Sanitize(string value) =>
            value.Replace(FieldSeparator, ' ').Replace(RecordSeparator, ' ').Replace('\r', ' ');

        private static bool TryPoints(string s, out int points) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out points) && points >= 0;
    }
}
