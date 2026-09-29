namespace Belot.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Belot.UI.Game;

    using Xunit;

    // Everything is drawn at a design size times a scale that follows the window (UiScale), so the
    // app looks the same on a phone, a tablet, an emulator at any density or a desktop window.
    [Collection(AppState.Name)]
    public class UiScaleTests
    {
        // Phones, a tablet held upright (and an emulator like BlueStacks), desktop windows, landscape.
        public static readonly TheoryData<double, double> Screens = new()
        {
            { 320, 560 },
            { 360, 640 },
            { 393, 780 },
            { 412, 860 },
            { 600, 1000 },
            { 800, 1230 },
            { 640, 930 },
            { 1100, 780 },
            { 1400, 800 },
            { 2200, 1180 },
        };

        public UiScaleTests()
        {
            UiScale.Current.UpdateSystemFontScale(1);
            UiScale.Current.Update(UiScale.TableDesignWidth, UiScale.TableDesignHeight);
        }

        [Theory]
        [InlineData(312, 1, 2)]
        [InlineData(312, 1.3, 1)]
        [InlineData(312, 2, 1)]
        [InlineData(600, 2, 2)]
        [InlineData(279, 1, 1)]
        [InlineData(280, 1, 2)]
        [InlineData(240, 0.85, 2)]
        [InlineData(0, 1, 1)]
        [InlineData(double.NaN, 1, 1)]
        [InlineData(double.PositiveInfinity, 1, 1)]
        [InlineData(312, double.NaN, 2)]
        [InlineData(312, 0, 2)]
        public void StatisticTilesShouldStackWhenTheirScaledNumbersNeedMoreRoom(double width, double fontScale, int columns)
        {
            Assert.Equal(columns, UiScale.StatisticColumnsFor(width, fontScale));
        }

        [Theory]
        [InlineData(280, 1, 2, 2)]
        [InlineData(280, 1, 3, 3)]
        [InlineData(280, 2, 2, 1)]
        [InlineData(280, 2, 3, 1)]
        [InlineData(560, 2, 2, 2)]
        [InlineData(540, 2, 3, 3)]
        [InlineData(240, 1, 3, 1)]
        [InlineData(double.NaN, 1, 2, 1)]
        [InlineData(280, double.NaN, 3, 3)]
        [InlineData(280, 0, 2, 2)]
        public void SettingsChoicesShouldKeepCompactRowsAtNormalTextAndStackAtLargeText(double width, double fontScale, int choices, int columns)
        {
            Assert.Equal(columns, UiScale.SettingsChoiceColumnsFor(width, fontScale, choices));
        }

        [Theory]
        [InlineData(0.85, 0.85)]
        [InlineData(1, 1)]
        [InlineData(1.3, 1.3)]
        [InlineData(2, 1.3)]
        [InlineData(double.MaxValue, 1.3)]
        public void OnlyTableTextShouldApplyTheBoundedSystemFontScale(double systemScale, double textScale)
        {
            var scale = UiScale.Current;
            scale.Update(600, 1000);
            var page = scale.Page;
            var table = scale.Table;
            var spacing = scale.HandSpacing;
            try
            {
                scale.UpdateSystemFontScale(systemScale);

                Assert.Equal(systemScale, scale.SystemFontScale);
                Assert.Equal(table * textScale, scale.TableText, 10);
                Assert.Equal(page, scale.Page);
                Assert.Equal(table, scale.Table);
                Assert.Equal(spacing, scale.HandSpacing);
            }
            finally
            {
                scale.UpdateSystemFontScale(1);
                scale.Update(UiScale.TableDesignWidth, UiScale.TableDesignHeight);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void InvalidSystemFontScalesShouldRestoreTheDefaultFactor(double invalid)
        {
            var scale = UiScale.Current;
            scale.UpdateSystemFontScale(1.3);
            scale.UpdateSystemFontScale(invalid);

            Assert.Equal(1, scale.SystemFontScale);
            Assert.Equal(scale.Table, scale.TableText);
        }

        [Fact]
        public void TableTextBindingsShouldRefreshForFontAndWindowChanges()
        {
            var scale = UiScale.Current;
            var changed = new System.Collections.Generic.List<string>();
            scale.PropertyChanged += Changed;
            try
            {
                scale.UpdateSystemFontScale(2);
                Assert.Equal(new[] { nameof(UiScale.SystemFontScale), nameof(UiScale.TableText) }, changed);

                changed.Clear();
                scale.Update(600, 1000);
                Assert.Contains(nameof(UiScale.Table), changed);
                Assert.Contains(nameof(UiScale.TableText), changed);
                Assert.Equal(UiScale.MaximumTableScale * 1.3, scale.TableText, 10);

                changed.Clear();
                scale.UpdateSystemFontScale(3);
                Assert.Equal(new[] { nameof(UiScale.SystemFontScale), nameof(UiScale.TableText) }, changed);

                changed.Clear();
                scale.UpdateSystemFontScale(3);
                scale.Update(600, 1000);
                Assert.Empty(changed);
            }
            finally
            {
                scale.PropertyChanged -= Changed;
                scale.UpdateSystemFontScale(1);
                scale.Update(UiScale.TableDesignWidth, UiScale.TableDesignHeight);
            }

            void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => changed.Add(e.PropertyName!);
        }

        // The table fits: its fixed rows (header, North, the side seats with eight cards, the South
        // bar and the hand) leave room for the trick in the middle, and eight cards fit the width.
        [Theory]
        [MemberData(nameof(Screens))]
        public void TheTableShouldFitTheWindow(double width, double height)
        {
            var scale = UiScale.TableScaleFor(width, height);
            var tableWidth = Math.Min(width, UiScale.TableMaxDesignWidth * scale);
            var spacing = UiScale.HandSpacingFor(tableWidth, scale);

            var card = UiScale.HandCardDesignWidth * scale;
            var hand = card + (7 * (card + spacing));
            Assert.True(hand <= tableWidth - (2 * UiScale.TablePaddingDesign * scale) + 0.5, $"{width}x{height}: a hand of {hand:0} in {tableWidth:0}");
            Assert.True(spacing < 0, "the hand's cards overlap");
            Assert.True(card + spacing >= (card * 0.3) - 0.1, "every card shows its corner");

            // The worst case in design units (trick one, declarations at every seat, eight cards
            // each): padding 16, header 62, North 122, the side seats 246 (the middle row's
            // minimum), the South bar 42, the hand 102, the gaps between the rows 16.
            var needed = (16 + 62 + 122 + 246 + 42 + 102 + 16) * scale;
            Assert.True(needed <= height || scale == 0.8, $"{width}x{height}: needs {needed:0}");
        }

        // Text is never smaller than on the small phone the pages are designed for, and it grows
        // modestly with the screen, without magnifying the whole interface on a tablet.
        [Fact]
        public void BiggerScreensShouldDrawEverythingBigger()
        {
            Assert.Equal(1, UiScale.TableScaleFor(UiScale.TableDesignWidth, UiScale.TableDesignHeight), 3);
            Assert.Equal(UiScale.MaximumTableScale, UiScale.TableScaleFor(600, 1000));
            Assert.Equal(UiScale.MaximumTableScale, UiScale.TableScaleFor(800, 1230));
            Assert.True(UiScale.PageScaleFor(360, 640) >= 1);
            Assert.Equal(UiScale.MaximumPageScale, UiScale.PageScaleFor(600, 1000));

            // A landscape window is limited by its height.
            Assert.Equal(UiScale.TableScaleFor(2000, 800), UiScale.TableScaleFor(1000, 800));
        }

        [Fact]
        public void TheScaleShouldFollowTheWindowAndTellWhenItChanges()
        {
            var scale = UiScale.Current;
            var changed = new System.Collections.Generic.List<string>();
            scale.PropertyChanged += Changed;
            try
            {
                scale.Update(600, 1000);
                Assert.Equal(Math.Round(UiScale.TableScaleFor(600, 1000), 3), scale.Table);
                Assert.Equal(Math.Round(UiScale.PageScaleFor(600, 1000), 3), scale.Page);
                Assert.Contains(nameof(UiScale.Table), changed);

                changed.Clear();
                scale.Update(600, 1000);
                Assert.Empty(changed);

                scale.Update(0, 0);
                scale.Update(double.NaN, 500);
                scale.Update(double.PositiveInfinity, 500);
                scale.Update(500, double.PositiveInfinity);
                Assert.Equal(Math.Round(UiScale.TableScaleFor(600, 1000), 3), scale.Table);
            }
            finally
            {
                scale.PropertyChanged -= Changed;
                scale.Update(UiScale.TableDesignWidth, UiScale.TableDesignHeight);
            }

            void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => changed.Add(e.PropertyName!);
        }

        // No fixed size in the pages: every font, card, gap and button goes through {ui:Size} or
        // {ui:TableSize} (a fixed one is what looked tiny on a tablet).
        [Fact]
        public void ThePagesShouldHaveNoFixedSizes()
        {
            var pages = Path.Combine(FindSource(), "UI", "Belot.UI", "Pages");
            var fixedSize = new Regex(@"(?<![A-Za-z.])(FontSize|HeightRequest|WidthRequest|MaximumWidthRequest|MinimumWidthRequest|MinimumHeightRequest|Spacing|ColumnSpacing|RowSpacing|Padding|Margin|TranslationX|TranslationY)=""-?\d");
            var found = Directory.EnumerateFiles(pages, "*.xaml")
                .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (File: Path.GetFileName(f), Line: i + 1, Text: line)))
                .Where(x => fixedSize.IsMatch(x.Text))
                .Select(x => $"{x.File}:{x.Line}: {x.Text.Trim()}")
                .ToList();
            Assert.Empty(found);
            Assert.DoesNotContain(
                Directory.EnumerateFiles(pages, "*.xaml").Select(File.ReadAllText),
                text => text.Contains("OnIdiom", StringComparison.Ordinal));
        }

        private static string FindSource()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Belot.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("The src folder (Belot.sln) was not found.");
        }
    }
}
