namespace Belot.UI.Tests
{
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class SettingsTests
    {
        public SettingsTests() => AppState.Reset();

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        public void UnknownSavedSpeedShouldSelectTheNormalPreset(int value)
        {
            SettingsStore.Current.Set("settings.speed", value);

            Assert.Equal(GameSpeed.Normal, AppSettings.Speed);
            Assert.Equal(new GamePace(500, 1100, 300), AppSettings.Pace);
        }

        [Fact]
        public void SettingAnUnknownSpeedShouldPersistTheNormalPreset()
        {
            AppSettings.Speed = (GameSpeed)99;

            Assert.Equal((int)GameSpeed.Normal, SettingsStore.Current.Get("settings.speed", -1));
        }

        [Fact]
        public void BotNamesShouldPersistIndependentlyFromEachOtherAndTheirLevels()
        {
            AppSettings.PartnerLevel = "claude";
            AppSettings.WestLevel = "expert";
            AppSettings.EastLevel = "dummy";
            AppSettings.PartnerName = "  Иван  ";
            AppSettings.WestName = "  Петър  ";
            AppSettings.EastName = "  Мария  ";

            Assert.Equal("Иван", AppSettings.PartnerName);
            Assert.Equal("Петър", AppSettings.WestName);
            Assert.Equal("Мария", AppSettings.EastName);
            Assert.Equal("Иван", SettingsStore.Current.Get("settings.partnerName", string.Empty));
            Assert.Equal("Петър", SettingsStore.Current.Get("settings.westName", string.Empty));
            Assert.Equal("Мария", SettingsStore.Current.Get("settings.eastName", string.Empty));
            Assert.Equal("claude", AppSettings.PartnerLevel);
            Assert.Equal("expert", AppSettings.WestLevel);
            Assert.Equal("dummy", AppSettings.EastLevel);
        }

        [Fact]
        public void ClearingABotNameShouldRestoreItsDefaultWithoutChangingTheOthers()
        {
            AppSettings.PartnerName = "Иван";
            AppSettings.WestName = "Петър";
            AppSettings.EastName = "Мария";
            AppSettings.PartnerName = " \t ";

            Assert.Equal(string.Empty, AppSettings.PartnerName);
            Assert.Equal("Петър", AppSettings.WestName);
            Assert.Equal("Мария", AppSettings.EastName);
        }
    }
}
