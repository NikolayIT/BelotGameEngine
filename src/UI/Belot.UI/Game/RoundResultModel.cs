namespace Belot.UI.Game
{
    using System;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.Players;
    using Belot.UI.Localization;

    /// <summary>What the end-of-deal screen shows, from the person's side (Us = South-North).</summary>
    public sealed class RoundResultModel
    {
        public RoundResultModel(RoundEndInfo round, Func<PlayerPosition, string> names)
        {
            var text = LocalizationManager.Instance;
            this.Round = round;
            var declaredByUs = Seats.IsUs(round.Declarer);
            (this.Icon, this.Title) = round.Outcome switch
            {
                RoundOutcome.PassedOut => ("🔁", text["Round_PassedOut"]),
                RoundOutcome.Hanging => ("⚖️", text["Round_Hanging"]),
                RoundOutcome.Made => declaredByUs ? ("✅", text["Round_WeMade"]) : ("😐", text["Round_TheyMade"]),
                _ => declaredByUs ? ("❌", text["Round_WeInside"]) : ("🎉", text["Round_TheyInside"]),
            };

            this.ContractLine = round.Contract == BidType.Pass
                ? text["Round_NoContract"]
                : $"{BelotTexts.ContractText(round.Contract)} · {names(round.Declarer)}";
            this.IsCapot = round.IsCapot;

            var ourCapot = round.IsCapot && round.EastWestTricks == 0 ? 90 : 0;
            var themCapot = round.IsCapot && round.SouthNorthTricks == 0 ? 90 : 0;
            this.UsCards = round.SouthNorthInDeal - round.SouthNorthCombinations - ourCapot;
            this.ThemCards = round.EastWestInDeal - round.EastWestCombinations - themCapot;
            this.UsCombinations = round.SouthNorthCombinations;
            this.ThemCombinations = round.EastWestCombinations;
            this.UsCapot = ourCapot;
            this.ThemCapot = themCapot;
            this.UsInDeal = round.SouthNorthInDeal;
            this.ThemInDeal = round.EastWestInDeal;
            this.UsAwarded = $"+{round.SouthNorthAwarded}";
            this.ThemAwarded = $"+{round.EastWestAwarded}";
            this.UsTotal = round.SouthNorthGamePoints;
            this.ThemTotal = round.EastWestGamePoints;
            this.UsCombinationsText = Describe(round, true);
            this.ThemCombinationsText = Describe(round, false);
            this.HangingText = round.HangingAfter > 0 ? text.Format("Round_HangingFormat", round.HangingAfter) : string.Empty;
        }

        public RoundEndInfo Round { get; }

        public string Icon { get; }

        public string Title { get; }

        public string ContractLine { get; }

        public bool IsCapot { get; }

        public bool IsPassedOut => this.Round.Outcome == RoundOutcome.PassedOut;

        public bool IsPlayed => !this.IsPassedOut;

        public int UsCards { get; }

        public int ThemCards { get; }

        public int UsCombinations { get; }

        public int ThemCombinations { get; }

        public int UsCapot { get; }

        public int ThemCapot { get; }

        public int UsInDeal { get; }

        public int ThemInDeal { get; }

        public string UsAwarded { get; }

        public string ThemAwarded { get; }

        public int UsTotal { get; }

        public int ThemTotal { get; }

        public string UsCombinationsText { get; }

        public string ThemCombinationsText { get; }

        public bool HasUsCombinations => this.UsCombinationsText.Length > 0;

        public bool HasThemCombinations => this.ThemCombinationsText.Length > 0;

        public bool HasCombinations => this.HasUsCombinations || this.HasThemCombinations;

        public string HangingText { get; }

        public bool HasHanging => this.HangingText.Length > 0;

        // The team's combinations, the ones that did not score in brackets.
        private static string Describe(RoundEndInfo round, bool us) => string.Join(
            ", ",
            round.Combinations
                .Where(x => Seats.IsUs(x.Seat) == us)
                .Select(x => x.IsScored == false
                    ? $"({BelotTexts.CombinationText(x.Type, x.Card)})"
                    : BelotTexts.CombinationText(x.Type, x.Card)));
    }
}
