namespace Belot.UI.Tests
{
    using System.Globalization;

    using Belot.UI.Game;

    using Xunit;

    public class StatsTextTests
    {
        // The win rate keeps a half: 1 win in 8 games is 12,5%. It was rounded half to even, so
        // 12,5% showed as 12% (and 0,5% as 0%) while 13,5% showed as 14%.
        [Theory]
        [InlineData(1, 8, "bg-BG", "12,5%")]
        [InlineData(1, 8, "en-US", "12.5%")]
        [InlineData(3, 8, "bg-BG", "37,5%")]
        [InlineData(1, 200, "bg-BG", "0,5%")]
        [InlineData(1, 3, "bg-BG", "33,3%")]
        [InlineData(2, 3, "en-US", "66.7%")]
        [InlineData(1, 2, "bg-BG", "50%")]
        [InlineData(0, 5, "en-US", "0%")]
        [InlineData(5, 5, "bg-BG", "100%")]
        [InlineData(0, 0, "en-US", "—")]
        public void TheWinRateShouldShowItsDecimal(int wins, int games, string culture, string expected)
        {
            Assert.Equal(expected, StatsText.WinRate(wins, games, CultureInfo.GetCultureInfo(culture)));
        }
    }
}
