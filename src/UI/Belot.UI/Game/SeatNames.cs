namespace Belot.UI.Game
{
    using Belot.UI.Localization;

    /// <summary>Names at the table, without AI difficulty labels. Blank names use localized seat roles.</summary>
    public static class SeatNames
    {
        /// <summary>Returns the four names in engine seat order: South, East, North, West.</summary>
        public static string[] Create(string? playerName, string? partnerName, string? westName, string? eastName) =>
            new[]
            {
                Resolve(playerName, "Start_DefaultName"),
                Resolve(eastName, "Seat_DefaultEast"),
                Resolve(partnerName, "Seat_DefaultPartner"),
                Resolve(westName, "Seat_DefaultWest"),
            };

        private static string Resolve(string? name, string fallbackKey) =>
            string.IsNullOrWhiteSpace(name) ? LocalizationManager.Instance[fallbackKey] : name.Trim();
    }
}
