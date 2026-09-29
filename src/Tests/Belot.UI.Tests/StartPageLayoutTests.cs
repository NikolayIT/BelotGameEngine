namespace Belot.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Belot.UI.Game;

    using Xunit;

    public class StartPageLayoutTests
    {
        [Theory]
        [InlineData(324, 1, 2)]
        [InlineData(300, 1, 2)]
        [InlineData(324, 2, 1)]
        [InlineData(300, 2, 1)]
        [InlineData(300, 1.3, 1)]
        [InlineData(260, 1, 1)]
        [InlineData(560, 2, 2)]
        [InlineData(0, 1, 1)]
        [InlineData(double.NaN, 1, 1)]
        [InlineData(double.PositiveInfinity, 1, 1)]
        [InlineData(300, 0, 2)]
        [InlineData(300, double.NaN, 2)]
        public void HomeControlsShouldStackWhenTheirScaledTextNeedsMoreRoom(double width, double fontScale, int columns)
        {
            Assert.Equal(columns, UiScale.StartColumnsFor(width, fontScale));
        }

        [Fact]
        public void TheTitleShouldHaveAnAvailableFullWidthRowWithoutDisablingTextScaling()
        {
            var page = ReadPage();
            var header = Named(page, "HeaderGrid");
            Assert.Equal("*,Auto,Auto", (string?)header.Attribute("ColumnDefinitions"));
            Assert.Equal("Auto,Auto", (string?)header.Attribute("RowDefinitions"));
            Assert.Same(header, Named(page, "TitleLabel").Parent);
            Assert.Same(header, Named(page, "LangButton").Parent);
            Assert.Same(header, Named(page, "SettingsButton").Parent);
            Assert.Null(Named(page, "TitleLabel").Attribute("FontAutoScalingEnabled"));
            Assert.Null(Named(page, "TitleLabel").Attribute("WidthRequest"));

            var code = ReadCode();
            Assert.Contains("Grid.SetColumnSpan(this.TitleLabel, stacked ? 3 : 1)", code);
            Assert.Contains("Grid.SetRow(this.LangButton, stacked ? 1 : 0)", code);
            Assert.Contains("Grid.SetRow(this.SettingsButton, stacked ? 1 : 0)", code);
        }

        [Theory]
        [InlineData("Partner")]
        [InlineData("West")]
        [InlineData("East")]
        public void EverySeatShouldRetainItsNativeControlsAndFullTextScalingWhenRepositioned(string seat)
        {
            var page = ReadPage();
            var grid = Named(page, "LineupGrid");
            Assert.Equal("*,1.2*", (string?)grid.Attribute("ColumnDefinitions"));
            Assert.Same(grid, Named(page, seat + "NameGroup").Parent);
            Assert.Same(grid, Named(page, seat + "Picker").Parent);
            Assert.Equal("Picker", Named(page, seat + "Picker").Name.LocalName);
            Assert.Equal("Entry", Named(page, seat + "NameEntry").Name.LocalName);
            Assert.Null(Named(page, seat + "Picker").Attribute("FontAutoScalingEnabled"));
            Assert.Null(Named(page, seat + "NameEntry").Attribute("FontAutoScalingEnabled"));
            Assert.Null(Named(page, seat + "Picker").Attribute("WidthRequest"));
        }

        [Fact]
        public void ResponsiveHomeShouldFollowFontChangesWithoutReloadingNamesOrPickerSelections()
        {
            var code = ReadCode();
            var appearStart = code.IndexOf("protected override void OnAppearing()", StringComparison.Ordinal);
            var disappearStart = code.IndexOf("protected override void OnDisappearing()", StringComparison.Ordinal);
            var resizeStart = code.IndexOf("protected override void OnSizeAllocated", StringComparison.Ordinal);
            Assert.Contains("UiScale.Current.PropertyChanged += this.OnScaleChanged", code[appearStart..disappearStart]);
            Assert.Contains("this.UpdateResponsiveLayout()", code[appearStart..disappearStart]);
            Assert.Contains("UiScale.Current.PropertyChanged -= this.OnScaleChanged", code[disappearStart..resizeStart]);

            var start = code.IndexOf("private void OnScaleChanged", StringComparison.Ordinal);
            var end = code.IndexOf("private void OnToggleLanguage", start, StringComparison.Ordinal);
            var layout = code[start..end];
            Assert.Contains("nameof(UiScale.SystemFontScale)", layout);
            Assert.Contains("nameof(UiScale.Page)", layout);
            Assert.Contains("if (columns == this.lineupColumns)", layout);
            Assert.Contains("row < 6 / columns", layout);
            Assert.Contains("Grid.SetRow(names[index], columns == 1 ? index * 2 : index)", layout);
            Assert.Contains("Grid.SetColumn(pickers[index], columns == 1 ? 0 : 1)", layout);
            Assert.Contains("Grid.SetRow(pickers[index], columns == 1 ? (index * 2) + 1 : index)", layout);
            Assert.DoesNotContain("SelectedIndex", layout);
            Assert.DoesNotContain(".Text =", layout);
            Assert.DoesNotContain("Children.Clear", layout);
        }

        private static XElement Named(XDocument page, string name) => page.Descendants()
            .Single(element => element.Attributes().Any(attribute => attribute.Name.LocalName == "Name" && attribute.Value == name));

        private static XDocument ReadPage() => XDocument.Load(Path.Combine(PagesDirectory(), "StartPage.xaml"));

        private static string ReadCode() => File.ReadAllText(Path.Combine(PagesDirectory(), "StartPage.xaml.cs"));

        private static string PagesDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Belot.sln")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("The src folder was not found."), "UI", "Belot.UI", "Pages");
        }
    }
}
