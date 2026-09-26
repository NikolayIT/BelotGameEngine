namespace Belot.UI.Game
{
    using System;

    using Belot.UI.Localization;

    public sealed class MatchHistoryEntry
    {
        public MatchHistoryEntry(string partnerId, string westId, string eastId, int ourPoints, int themPoints, bool won, DateTime whenUtc)
        {
            this.PartnerId = partnerId;
            this.WestId = westId;
            this.EastId = eastId;
            this.UsPoints = ourPoints;
            this.ThemPoints = themPoints;
            this.Won = won;
            this.WhenUtc = whenUtc;
        }

        /// <summary>Gets the partner's level id (see <see cref="AiLevels"/>).</summary>
        public string PartnerId { get; }

        /// <summary>Gets the level id of the rival on the person's left.</summary>
        public string WestId { get; }

        /// <summary>Gets the level id of the rival on the person's right.</summary>
        public string EastId { get; }

        /// <summary>Gets the partner's level in the current language.</summary>
        public string PartnerName => NameOf(this.PartnerId);

        /// <summary>Gets the rivals' levels in the current language: one name when they are the same level.</summary>
        public string RivalsName => string.Equals(this.WestId, this.EastId, StringComparison.OrdinalIgnoreCase)
            ? NameOf(this.WestId)
            : LocalizationManager.Instance.Format("History_Pair", NameOf(this.WestId), NameOf(this.EastId));

        public int UsPoints { get; }

        public int ThemPoints { get; }

        public bool Won { get; }

        public DateTime WhenUtc { get; }

        public string ScoreText => $"{this.UsPoints} – {this.ThemPoints}";

        private static string NameOf(string id) => AiLevels.Find(id)?.DisplayName ?? id;
    }
}
