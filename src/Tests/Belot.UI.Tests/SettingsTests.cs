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
    }
}
