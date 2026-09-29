namespace Belot.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Xunit;

    public class SupportingPageTests
    {
        [Theory]
        [InlineData("SettingsPage")]
        [InlineData("StatisticsPage")]
        [InlineData("RulesPage")]
        public void IconOnlyBackButtonsShouldHaveLocalizedAccessibleNames(string page)
        {
            var back = ReadPage(page).Descendants().Single(e => (string?)e.Attribute("Clicked") == "OnBack");

            Assert.Equal("{loc:Tr Common_Back}", (string?)back.Attribute("SemanticProperties.Description"));
            Assert.Equal("{ui:Size '48'}", (string?)back.Attribute("WidthRequest"));
        }

        [Fact]
        public void TheSettingsIconShouldAnnounceItsPurpose()
        {
            var settings = ReadPage("StartPage").Descendants().Single(e => (string?)e.Attribute("Clicked") == "OnOpenSettings");

            Assert.Equal("{loc:Tr Settings_Title}", (string?)settings.Attribute("SemanticProperties.Description"));
        }

        [Fact]
        public void TheCompactHomeShouldKeepTextScalingAndAvoidDecorativeTileGrids()
        {
            var page = ReadPage("StartPage");
            var title = page.Descendants().Single(e => (string?)e.Attribute("Text") == "{loc:Tr Start_Title}");
            Assert.Null(title.Attribute("FontAutoScalingEnabled"));
            Assert.Equal("{ui:Size '24'}", (string?)title.Attribute("FontSize"));
            Assert.DoesNotContain(page.Descendants(), e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value.EndsWith("Host", StringComparison.Ordinal)));
        }

        [Theory]
        [InlineData("HapticsSwitch", "Settings_Haptics", "Settings_HapticsHint")]
        [InlineData("AssistsSwitch", "Settings_Assists", "Settings_AssistsHint")]
        public void SettingsSwitchesShouldIdentifyWhatTheyToggle(string name, string description, string hint)
        {
            var control = ReadPage("SettingsPage").Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == name));

            Assert.Equal($"{{loc:Tr {description}}}", (string?)control.Attribute("SemanticProperties.Description"));
            Assert.Equal($"{{loc:Tr {hint}}}", (string?)control.Attribute("SemanticProperties.Hint"));
        }

        [Fact]
        public void AllLanguageAndSpeedChoicesShouldExposeTheirSelectedState()
        {
            var code = File.ReadAllText(Path.Combine(PagesDirectory(), "SettingsPage.xaml.cs"));
            var start = code.IndexOf("private static void StyleSegment", StringComparison.Ordinal);
            var end = code.IndexOf("private async void OnBack", start, StringComparison.Ordinal);
            var style = code[start..end];

            Assert.Contains("selected ? \"Common_Selected\" : \"Common_NotSelected\"", style);
            Assert.Contains("SemanticProperties.SetDescription(button", style);
            foreach (var name in new[] { "LangEnButton", "LangBgButton", "SpeedRelaxedButton", "SpeedNormalButton", "SpeedFastButton" })
            {
                Assert.Contains($"StyleSegment(this.{name},", code);
            }
        }

        [Fact]
        public void ChangingLanguageShouldRefreshBothGroupsAccessibleDescriptions()
        {
            var code = File.ReadAllText(Path.Combine(PagesDirectory(), "SettingsPage.xaml.cs"));
            var start = code.IndexOf("private void SetLanguage", StringComparison.Ordinal);
            var end = code.IndexOf("private void OnSpeedRelaxed", start, StringComparison.Ordinal);
            var change = code[start..end];
            var languageChanged = change.IndexOf("LocalizationManager.Instance.SetLanguage(language)", StringComparison.Ordinal);

            Assert.True(languageChanged >= 0);
            Assert.True(change.IndexOf("this.RefreshLanguageButtons()", StringComparison.Ordinal) > languageChanged);
            Assert.True(change.IndexOf("this.RefreshSpeedButtons()", StringComparison.Ordinal) > languageChanged);
            foreach (var key in new[] { "Speed_Relaxed", "Speed_Normal", "Speed_Fast" })
            {
                Assert.Contains($"text[\"{key}\"]", code);
            }
        }

        [Theory]
        [InlineData("LanguageChoicesGrid", 2)]
        [InlineData("SpeedChoicesGrid", 3)]
        public void SettingsChoiceGroupsShouldKeepNativeButtonsInAdaptiveGrids(string name, int count)
        {
            var page = ReadPage("SettingsPage");
            var group = page.Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == name));

            Assert.Equal("Grid", group.Name.LocalName);
            Assert.Equal(count, group.Elements().Count());
            Assert.DoesNotContain(page.Descendants(), e => e.Name.LocalName == "FlexLayout");
            foreach (var button in group.Elements())
            {
                Assert.Equal("Button", button.Name.LocalName);
                Assert.Equal("NoWrap", (string?)button.Attribute("LineBreakMode"));
                Assert.Null(button.Attribute("WidthRequest"));
                Assert.Null(button.Attribute("HeightRequest"));
                Assert.Null(button.Attribute("FontAutoScalingEnabled"));
            }
        }

        [Theory]
        [InlineData("SettingsPage", "UpdateChoiceLayout")]
        [InlineData("StatisticsPage", "UpdateMetricLayout")]
        public void AdaptiveLayoutsShouldFollowFontChangesOnlyWhileTheirPageIsActive(string page, string update)
        {
            var code = File.ReadAllText(Path.Combine(PagesDirectory(), page + ".xaml.cs"));
            var appearStart = code.IndexOf("protected override void OnAppearing()", StringComparison.Ordinal);
            var disappearStart = code.IndexOf("protected override void OnDisappearing()", StringComparison.Ordinal);
            var resizeStart = code.IndexOf("protected override void OnSizeAllocated", StringComparison.Ordinal);
            var appearing = code[appearStart..disappearStart];
            var disappearing = code[disappearStart..resizeStart];

            Assert.Contains("UiScale.Current.PropertyChanged += this.OnScaleChanged", appearing);
            Assert.Contains($"this.{update}()", appearing);
            Assert.Contains("UiScale.Current.PropertyChanged -= this.OnScaleChanged", disappearing);
            Assert.Contains("nameof(UiScale.SystemFontScale)", code);
            Assert.Contains("nameof(UiScale.Page)", code);
        }

        [Theory]
        [InlineData("CurrentEloLabel")]
        [InlineData("PeakEloLabel")]
        [InlineData("GamesLabel")]
        [InlineData("WinRateLabel")]
        public void StatisticValuesShouldStayUnbrokenAndKeepFullSystemTextScaling(string name)
        {
            var page = ReadPage("StatisticsPage");
            var value = page.Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == name));

            Assert.Equal("NoWrap", (string?)value.Attribute("LineBreakMode"));
            Assert.Null(value.Attribute("FontAutoScalingEnabled"));
            Assert.Null(value.Attribute("WidthRequest"));
            Assert.Null(value.Attribute("HeightRequest"));
            Assert.Contains(value.Ancestors(), e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "MetricsGrid"));
        }

        [Fact]
        public void AndroidStartupShouldLoadTheSystemFontScaleBeforeCreatingPages()
        {
            var code = File.ReadAllText(Path.Combine(PagesDirectory(), "..", "MauiProgram.cs"));
            var fontScale = code.IndexOf("Game.UiScale.Current.UpdateSystemFontScale", StringComparison.Ordinal);

            Assert.True(fontScale >= 0);
            Assert.True(fontScale < code.IndexOf("var builder = MauiApp.CreateBuilder()", StringComparison.Ordinal));
            Assert.Contains("Resources?.Configuration?.FontScale", code);
        }

        [Theory]
        [InlineData("StartPage")]
        [InlineData("SettingsPage")]
        [InlineData("StatisticsPage")]
        [InlineData("RulesPage")]
        public void ScrollPageButtonsShouldGrowForLargeTextInsteadOfClippingIt(string page)
        {
            var buttons = ReadPage(page).Descendants().Where(e => e.Name.LocalName == "Button").ToArray();
            Assert.NotEmpty(buttons);
            foreach (var button in buttons)
            {
                Assert.Null(button.Attribute("HeightRequest"));
                if (button.Parent!.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value is "LanguageChoicesGrid" or "SpeedChoicesGrid"))
                {
                    Assert.Equal("NoWrap", (string?)button.Attribute("LineBreakMode"));
                }
                else
                {
                    Assert.Equal("WordWrap", (string?)button.Attribute("LineBreakMode"));
                }

                Assert.Contains((string?)button.Attribute("MinimumHeightRequest"), new[] { "{ui:Size '48'}", "{ui:Size '54'}" });
                Assert.Null(button.Attribute("FontAutoScalingEnabled"));
            }
        }

        [Theory]
        [InlineData("PartnerPicker", "OnPartnerChanged", "Start_Partner")]
        [InlineData("WestPicker", "OnWestChanged", "Start_West")]
        [InlineData("EastPicker", "OnEastChanged", "Start_East")]
        public void EverySeatShouldHaveAnIndependentNativePicker(string name, string handler, string title)
        {
            var page = ReadPage("StartPage");
            var control = page.Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == name));
            Assert.Equal("Picker", control.Name.LocalName);
            Assert.Equal(handler, (string?)control.Attribute("SelectedIndexChanged"));
            Assert.Equal($"{{loc:Tr {title}}}", (string?)control.Attribute("Title"));
            Assert.Equal("{ui:Size '48'}", (string?)control.Attribute("MinimumHeightRequest"));
            Assert.Null(control.Attribute("HeightRequest"));
            Assert.Null(control.Attribute("FontAutoScalingEnabled"));
            Assert.Equal(3, page.Descendants().Count(e => e.Name.LocalName == "Picker"));
        }

        [Theory]
        [InlineData("PartnerNameEntry", "Start_PartnerName")]
        [InlineData("WestNameEntry", "Start_WestName")]
        [InlineData("EastNameEntry", "Start_EastName")]
        public void BotNamesShouldHaveAccessibleSingleLineInputsThatExplainTheyAreOptional(string name, string description)
        {
            var page = ReadPage("StartPage");
            var input = page.Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == name));

            Assert.Equal("Entry", input.Name.LocalName);
            Assert.Equal("OnBotNameChanged", (string?)input.Attribute("Unfocused"));
            Assert.Equal($"{{loc:Tr {description}}}", (string?)input.Attribute("SemanticProperties.Description"));
            Assert.Equal("{loc:Tr Start_OptionalName}", (string?)input.Attribute("Placeholder"));
            Assert.Null(input.Attribute("WidthRequest"));
            Assert.Null(input.Attribute("MaxLength"));
        }

        [Fact]
        public void StartingTheGameShouldComeBeforeOptionalDescriptionsAndHistory()
        {
            var page = ReadPage("StartPage");
            var named = page.Descendants().SelectMany(e => e.Attributes().Where(a => a.Name.LocalName == "Name").Select(a => a.Value)).ToArray();
            Assert.True(Array.IndexOf(named, "EastPicker") < Array.IndexOf(named, "PlayButton"));
            Assert.True(Array.IndexOf(named, "PlayButton") < Array.IndexOf(named, "TaglineLabel"));
            Assert.True(Array.IndexOf(named, "PlayButton") < Array.IndexOf(named, "HistoryList"));
        }

        [Theory]
        [InlineData("StartPage", "OnOpenStatistics", 2)]
        [InlineData("SettingsPage", "OnOpenGitHub", 1)]
        public void PageLinksShouldBeNativeButtonsInsteadOfGestureOnlyText(string page, string handler, int count)
        {
            var document = ReadPage(page);
            var controls = document.Descendants().Where(e => (string?)e.Attribute("Clicked") == handler).ToArray();

            Assert.Equal(count, controls.Length);
            Assert.All(controls, control => Assert.Equal("Button", control.Name.LocalName));
            Assert.DoesNotContain(document.Descendants(), e => e.Name.LocalName == "TapGestureRecognizer");
        }

        [Fact]
        public void MultiplayerShouldBeASecondaryNativeButtonBetweenPlayAndOptionalDetails()
        {
            var page = ReadPage("StartPage");
            var button = page.Descendants().Single(e => (string?)e.Attribute("Clicked") == "OnPlayOnline");
            var named = page.Descendants().SelectMany(e => e.Attributes().Where(a => a.Name.LocalName == "Name").Select(a => a.Value)).ToArray();
            Assert.Equal("Button", button.Name.LocalName);
            Assert.Equal("{loc:Tr Start_PlayOnline}", (string?)button.Attribute("Text"));
            Assert.Equal("{loc:Tr Start_OnlineHint}", (string?)button.Attribute("SemanticProperties.Hint"));
            Assert.Equal("Transparent", (string?)button.Attribute("BackgroundColor"));
            Assert.Equal("1", (string?)button.Attribute("BorderWidth"));
            Assert.True(Array.IndexOf(named, "PlayButton") < Array.IndexOf(named, "OnlinePlayButton"));
            Assert.True(Array.IndexOf(named, "OnlinePlayButton") < Array.IndexOf(named, "TaglineLabel"));
            Assert.Contains(button.Parent!.Elements(), e => (string?)e.Attribute("Text") == "ednaigra.com");

            var code = File.ReadAllText(Path.Combine(PagesDirectory(), "StartPage.xaml.cs"));
            var start = code.IndexOf("private async void OnPlayOnline", StringComparison.Ordinal);
            var end = code.IndexOf("private Task PlayAsync", start, StringComparison.Ordinal);
            var handler = code[start..end];
            Assert.Contains("OnlinePlay.OpenAsync(", handler);
            Assert.Contains("this.actions,", handler);
            Assert.Contains("Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred)", handler);
            Assert.Contains("text[\"Start_OnlineUnavailable\"]", handler);
            Assert.Contains("this.DisplayAlertAsync", handler);
        }

        [Fact]
        public void TheRatingCardShouldHaveOneAccessibleHitTarget()
        {
            var rating = ReadPage("StartPage").Descendants().Single(e => e.Attributes().Any(a => a.Name.LocalName == "Name" && a.Value == "RatingButton"));
            var decoration = rating.Parent!.Elements().Single(e => e.Name.LocalName == "Border");

            Assert.Equal("Button", rating.Name.LocalName);
            Assert.Equal("OnOpenStatistics", (string?)rating.Attribute("Clicked"));
            Assert.Equal("True", (string?)decoration.Attribute("AutomationProperties.ExcludedWithChildren"));
            Assert.Equal("{loc:Tr Start_Statistics}", (string?)rating.Attribute("SemanticProperties.Hint"));
        }

        private static XDocument ReadPage(string page) => XDocument.Load(Path.Combine(PagesDirectory(), page + ".xaml"));

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
