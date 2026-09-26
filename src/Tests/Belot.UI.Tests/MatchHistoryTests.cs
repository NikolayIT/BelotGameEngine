namespace Belot.UI.Tests
{
    using System;
    using System.Linq;

    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    [Collection(AppState.Name)]
    public class MatchHistoryTests
    {
        public MatchHistoryTests() => AppState.Reset();

        // A game is kept with its levels' ids: the lists name them in the current language, the
        // rivals as a pair ("Master & Beginner") or once when both are the same level.
        [Fact]
        public void HistoryShouldNameTheLevelsInTheCurrentLanguage()
        {
            MatchHistoryStore.Add(new MatchHistoryEntry("smart", "claude", "dummy", 151, 90, true, DateTime.UtcNow));
            MatchHistoryStore.Add(new MatchHistoryEntry("dummy", "random", "random", 40, 160, false, DateTime.UtcNow));

            var entries = MatchHistoryStore.All();
            Assert.Equal(2, entries.Count);
            Assert.Equal("Beginner", entries[0].PartnerName);
            Assert.Equal("Random", entries[0].RivalsName);
            Assert.Equal("Skilled", entries[1].PartnerName);
            Assert.Equal("Master & Beginner", entries[1].RivalsName);

            LocalizationManager.Instance.SetLanguage(LocalizationManager.Bulgarian);
            Assert.Equal("Майстор и Начинаещ", MatchHistoryStore.All()[1].RivalsName);
            Assert.Equal("Случаен", MatchHistoryStore.All()[0].RivalsName);
        }

        [Fact]
        public void HistoryShouldKeepEveryFieldNewestFirstAndCapped()
        {
            var when = new DateTime(2026, 9, 26, 10, 30, 0, DateTimeKind.Utc);
            for (var i = 0; i < 105; i++)
            {
                MatchHistoryStore.Add(new MatchHistoryEntry("smart", "dummy", "claude", 151 + i, i, i % 2 == 0, when.AddMinutes(i)));
            }

            var entries = MatchHistoryStore.All();
            Assert.Equal(100, entries.Count);
            var newest = entries[0];
            Assert.Equal(("smart", "dummy", "claude"), (newest.PartnerId, newest.WestId, newest.EastId));
            Assert.Equal((255, 104, true), (newest.UsPoints, newest.ThemPoints, newest.Won));
            Assert.Equal(when.AddMinutes(104), newest.WhenUtc);
            Assert.Equal("255 – 104", newest.ScoreText);
            Assert.Equal(when.AddMinutes(5), entries[^1].WhenUtc);
        }

        // An id that is no longer a level keeps its id as its name (never another level's).
        [Fact]
        public void AnUnknownLevelShouldKeepItsId()
        {
            MatchHistoryStore.Add(new MatchHistoryEntry("retired", "smart", "old|bot", 151, 0, true, DateTime.UtcNow));
            var entry = Assert.Single(MatchHistoryStore.All());
            Assert.Equal("retired", entry.PartnerName);
            Assert.Equal("old bot", entry.EastId);
            Assert.Null(AiLevels.Find("retired"));
            Assert.Same(AiLevels.All[2], AiLevels.ById("retired"));
        }

        [Fact]
        public void ResetShouldForgetTheHistory()
        {
            MatchHistoryStore.Add(new MatchHistoryEntry("smart", "smart", "smart", 151, 0, true, DateTime.UtcNow));
            OpponentStatsStore.Record("smart", true);
            MatchHistoryStore.Clear();
            OpponentStatsStore.Clear(AiLevels.All.Select(l => l.Id));
            Assert.Empty(MatchHistoryStore.All());
            Assert.Equal((0, 0), OpponentStatsStore.For("smart"));
        }
    }
}
