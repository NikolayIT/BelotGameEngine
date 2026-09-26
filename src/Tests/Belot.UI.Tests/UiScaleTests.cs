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
        // with the screen (a tablet shows the table bigger, not a small table in a big window).
        [Fact]
        public void BiggerScreensShouldDrawEverythingBigger()
        {
            Assert.Equal(1, UiScale.TableScaleFor(UiScale.TableDesignWidth, UiScale.TableDesignHeight), 3);
            Assert.True(UiScale.TableScaleFor(600, 1000) > 1.5);
            Assert.True(UiScale.TableScaleFor(800, 1230) > 1.9);
            Assert.True(UiScale.PageScaleFor(360, 640) >= 1);
            Assert.True(UiScale.PageScaleFor(600, 1000) >= 1.5);

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
