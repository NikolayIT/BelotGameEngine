namespace Belot.UI.Tests
{
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    [Collection(AppState.Name)]
    public class SeatNamesTests
    {
        public SeatNamesTests() => AppState.Reset();

        [Fact]
        public void ChosenNamesShouldBeTrimmedAndReturnedInEngineSeatOrder()
        {
            var names = SeatNames.Create("  Me  ", "  Partner name  ", "  Left name  ", "  Right name  ");

            Assert.Equal(new[] { "Me", "Right name", "Partner name", "Left name" }, names);
        }

        [Theory]
        [InlineData("en", "You", "Right rival", "Partner", "Left rival")]
        [InlineData("bg", "Ти", "Десен съперник", "Партньор", "Ляв съперник")]
        public void BlankNamesShouldUseLocalizedRolesWithoutDifficultyLabels(string language, string south, string east, string north, string west)
        {
            LocalizationManager.Instance.SetLanguage(language);
            var names = SeatNames.Create(null, string.Empty, " \t ", "\n");

            Assert.Equal(new[] { south, east, north, west }, names);
            foreach (var level in AiLevels.All)
            {
                Assert.DoesNotContain(names, name => name.Contains(level.DisplayName, System.StringComparison.Ordinal));
            }
        }

        [Fact]
        public void LanguageChangesAndAiChoicesShouldNotReplaceCustomNames()
        {
            AppSettings.PartnerLevel = "claude";
            AppSettings.WestLevel = "expert";
            AppSettings.EastLevel = "dummy";
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Bulgarian);

            Assert.Equal(new[] { "Ники", "Мария", "Иван", "Петър" }, SeatNames.Create("Ники", "Иван", "Петър", "Мария"));
        }
    }
}
