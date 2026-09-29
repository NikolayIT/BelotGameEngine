namespace Belot.UI.Tests
{
    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class LineupSelectionTests
    {
        public LineupSelectionTests() => AppState.Reset();

        [Fact]
        public void TheThreeSelectorsShouldPersistTheirOwnSeatsIndependently()
        {
            Assert.Equal("claude", LineupSelection.Select(PlayerPosition.North, 4)!.Id);
            Assert.Equal("random", LineupSelection.Select(PlayerPosition.West, 0)!.Id);
            Assert.Equal("expert", LineupSelection.Select(PlayerPosition.East, 3)!.Id);

            Assert.Equal("claude", AppSettings.PartnerLevel);
            Assert.Equal("random", AppSettings.WestLevel);
            Assert.Equal("expert", AppSettings.EastLevel);
            Assert.Equal(4, LineupSelection.SelectedIndex(PlayerPosition.North));
            Assert.Equal(0, LineupSelection.SelectedIndex(PlayerPosition.West));
            Assert.Equal(3, LineupSelection.SelectedIndex(PlayerPosition.East));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void RefreshingOrCancellingAPickerShouldNotOverwriteAnySavedChoice(int index)
        {
            AppSettings.PartnerLevel = "claude";
            AppSettings.WestLevel = "expert";
            AppSettings.EastLevel = "dummy";

            Assert.Null(LineupSelection.Select(PlayerPosition.North, index));
            Assert.Null(LineupSelection.Select(PlayerPosition.West, index));
            Assert.Null(LineupSelection.Select(PlayerPosition.East, index));
            Assert.Equal("claude", AppSettings.PartnerLevel);
            Assert.Equal("expert", AppSettings.WestLevel);
            Assert.Equal("dummy", AppSettings.EastLevel);
        }

        [Fact]
        public void ExistingSavedIdsShouldSelectTheSameLevelsAfterTheHomeRedesign()
        {
            AppSettings.PartnerLevel = "CLAUDE";
            AppSettings.WestLevel = "retired";
            AppSettings.EastLevel = string.Empty;

            Assert.Equal(4, LineupSelection.SelectedIndex(PlayerPosition.North));
            Assert.Equal(2, LineupSelection.SelectedIndex(PlayerPosition.West));
            Assert.Equal(2, LineupSelection.SelectedIndex(PlayerPosition.East));
            Assert.Equal("retired", AppSettings.WestLevel);
        }
    }
}
