namespace Belot.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Xunit;

    /// <summary>Contracts for the real XAML: the headless game tests cannot measure native controls.</summary>
    public class TableLayoutTests
    {
        [Fact]
        public void TableSizingShouldFollowTheContentHostInsideTheSystemBars()
        {
            var page = LoadPage();
            Assert.Equal("Container", (string?)page.Root!.Attribute("SafeAreaEdges"));
            var host = Named(page, "TableHost");
            Assert.Equal("Grid", host.Name.LocalName);
            Assert.Equal("OnTableHostSizeChanged", (string?)host.Attribute("SizeChanged"));
            Assert.Same(host, Named(page, "TableGrid").Parent);
            Assert.Null(host.Attribute("HeightRequest"));
            Assert.Null(host.Attribute("WidthRequest"));
        }

        [Fact]
        public void LongNamesShouldHaveBoundedSpaceSeparateFromTheHintButton()
        {
            var page = LoadPage();
            var bar = Named(page, "SouthBar");
            Assert.Equal("*,Auto", (string?)bar.Attribute("ColumnDefinitions"));
            var name = Named(page, "SouthName");
            Assert.Equal("Auto,*,Auto", (string?)name.Parent!.Attribute("ColumnDefinitions"));
            Assert.Equal("1", (string?)name.Attribute("Grid.Column"));
            Assert.Equal("TailTruncation", (string?)name.Attribute("LineBreakMode"));
            Assert.Equal("1", (string?)name.Attribute("MaxLines"));
            var hint = bar.Elements().Single(e => (string?)e.Attribute("Command") == "{Binding HintCommand}");
            Assert.Equal("1", (string?)hint.Attribute("Grid.Column"));
        }

        [Theory]
        [InlineData("RoundOverlayRoot")]
        [InlineData("GameOverlayRoot")]
        public void ResultActionsShouldRemainReachableWithLargeText(string overlay)
        {
            var result = Named(LoadPage(), overlay);
            var scroll = Assert.Single(result.Elements().Where(e => e.Name.LocalName == "ScrollView"));
            Assert.Equal("Fill", (string?)scroll.Attribute("VerticalOptions"));
            Assert.NotEmpty(scroll.Descendants().Where(e => e.Name.LocalName == "Button"));
            Assert.All(scroll.Descendants().Where(e => e.Name.LocalName == "Button"), button => Assert.Null(button.Attribute("HeightRequest")));
        }

        [Fact]
        public void InteractiveCardsAndSuitBidsShouldExposeSpokenLabels()
        {
            var page = LoadPage();
            var hand = Named(page, "HandStack");
            var card = Assert.Single(hand.Descendants().Where(e => e.Name.LocalName == "ImageButton"));
            Assert.Equal("{Binding Description}", (string?)card.Attribute("SemanticProperties.Description"));
            Assert.Contains("TapCardCommand", (string?)card.Attribute("Command"));
            var suitButtons = page.Descendants().Where(e => e.Name.LocalName == "Button"
                && new[] { "♣", "♦", "♥", "♠" }.Contains((string?)e.Attribute("Text"))).ToList();
            Assert.Equal(4, suitButtons.Count);
            Assert.All(suitButtons, button => Assert.Equal("{Binding Text}", (string?)button.Attribute("SemanticProperties.Description")));
        }

        [Fact]
        public void ScoresShouldShareBoundedColumnsBesideTheMenu()
        {
            var scores = Named(LoadPage(), "ScoreGrid");
            Assert.Equal("*,*", (string?)scores.Attribute("ColumnDefinitions"));
            Assert.Equal("0", (string?)scores.Attribute("Grid.Column"));
            Assert.Contains(scores.Descendants(), e => (string?)e.Attribute("Text") == "{Binding UsPoints}");
            Assert.Contains(scores.Descendants(), e => (string?)e.Attribute("Text") == "{Binding ThemPoints}");
        }

        [Fact]
        public void CombinationsShouldUseNaturalHeightAboveTheHand()
        {
            var page = LoadPage();
            var panel = Named(page, "AnnouncePanel");
            Assert.Equal("3", (string?)panel.Attribute("Grid.Row"));
            Assert.Equal("Fill", (string?)panel.Attribute("HorizontalOptions"));
            Assert.Null(panel.Attribute("HeightRequest"));
            Assert.Null(panel.Attribute("MaximumHeightRequest"));
            var choices = Assert.Single(panel.Descendants().Where(e => e.Name.LocalName == "ScrollView"));
            Assert.Equal("{ui:TableSize 90}", (string?)choices.Attribute("MaximumHeightRequest"));
            Assert.Contains(choices.Descendants(), e => (string?)e.Attribute("BindableLayout.ItemsSource") == "{Binding AnnounceOptions}");
            Assert.DoesNotContain(choices.Descendants(), e => (string?)e.Attribute("Command") == "{Binding DeclareCommand}");
            Assert.Contains(Named(page, "TableGrid"), panel.Ancestors());
            Assert.NotEmpty(panel.Descendants().Where(e => e.Name.LocalName == "Button"));
        }

        [Fact]
        public void OpponentCardsShouldNotOverflowOntoThePlayersControls()
        {
            Assert.Equal("True", (string?)Named(LoadPage(), "MiddleRow").Attribute("IsClippedToBounds"));
        }

        [Fact]
        public void BiddingShouldUseItsOwnCompactRowAboveTheHand()
        {
            var page = LoadPage();
            Assert.Equal("Auto,Auto,*,Auto,Auto,Auto", (string?)Named(page, "TableGrid").Attribute("RowDefinitions"));
            Assert.Equal("2", (string?)Named(page, "MiddleRow").Attribute("Grid.Row"));
            Assert.Equal("3", (string?)Named(page, "BidPanel").Attribute("Grid.Row"));
            Assert.Equal("5", (string?)Named(page, "HandStack").Attribute("Grid.Row"));
            Assert.DoesNotContain(Named(page, "BidPanel").Descendants(), e => (string?)e.Attribute("Text") == "{loc:Tr Game_YourBid}");
            Assert.Null(Named(page, "BidPanel").Attribute("HeightRequest"));
            Assert.Null(Named(page, "BidPanel").Attribute("MaximumHeightRequest"));
            Assert.DoesNotContain(Named(page, "BidPanel").Descendants(), e => e.Name.LocalName == "ScrollView");
            Assert.Equal(9, Named(page, "BidPanel").Descendants().Count(e => e.Name.LocalName == "Button"));
        }

        [Theory]
        [InlineData("North")]
        [InlineData("East")]
        [InlineData("South")]
        [InlineData("West")]
        public void PlayedCardsShouldUseChronologicalStackingForEverySeat(string seat)
        {
            var page = LoadPage();
            Assert.Equal($"{{Binding {seat}.PlayedCardZIndex}}", (string?)Named(page, seat + "Played").Attribute("ZIndex"));
            var last = page.Descendants().Single(e => ((string?)e.Attribute("Source"))?.StartsWith("{Binding LastTrick" + seat + ".", StringComparison.Ordinal) == true);
            Assert.Equal($"{{Binding {seat}.LastTrickZIndex}}", (string?)last.Attribute("ZIndex"));
        }

        [Fact]
        public void EverySeatNameShouldTruncateInsideItsOwnSpace()
        {
            var page = LoadPage();
            var names = page.Descendants().Where(e => (string?)e.Attribute("Text") == "{Binding Name}").ToArray();
            Assert.Equal(4, names.Length);
            Assert.All(names, label => Assert.Equal("TailTruncation", (string?)label.Attribute("LineBreakMode")));
        }

        [Fact]
        public void TheTableShouldKeepTheHandInItsBottomRowInsteadOfScrollingWithTheContent()
        {
            var page = LoadPage();
            var table = Named(page, "TableGrid");
            Assert.DoesNotContain(table.Ancestors(), e => e.Name.LocalName == "ScrollView");
            Assert.Equal("Auto,Auto,*,Auto,Auto,Auto", (string?)table.Attribute("RowDefinitions"));
            Assert.Equal("5", (string?)Named(page, "HandStack").Attribute("Grid.Row"));
            Assert.Null(Named(page, "MiddleRow").Attribute("MinimumHeightRequest"));
        }

        [Theory]
        [InlineData("StartPage", "All")]
        [InlineData("SettingsPage", "Container")]
        [InlineData("StatisticsPage", "Container")]
        [InlineData("RulesPage", "Container")]
        public void PagesShouldRespectSystemBarsAndTheSetupKeyboard(string page, string safeArea)
        {
            Assert.Equal(safeArea, (string?)LoadPage(page).Root!.Attribute("SafeAreaEdges"));
        }

        [Fact]
        public void IllegalCardsShouldKeepReadableFacesWithoutMovingTheCards()
        {
            var hand = Named(LoadPage(), "HandStack");
            Assert.DoesNotContain(hand.Descendants(), e => e.Name.LocalName == "BoxView");
            var image = Assert.Single(hand.Descendants().Where(e => e.Name.LocalName == "ImageButton"));
            Assert.Equal("{Binding FaceOpacity}", (string?)image.Attribute("Opacity"));
            Assert.Equal("{Binding ImageSource}", (string?)image.Attribute("Source"));
            var border = image.Parent!.Parent!;
            Assert.Equal("Black", (string?)border.Attribute("BackgroundColor"));
            Assert.Null(border.Attribute("Opacity"));
            Assert.Null(border.Attribute("Margin"));
            Assert.DoesNotContain(border.Descendants(), e => (string?)e.Attribute("Binding") == "{Binding IsPlayable}"
                || (string?)e.Attribute("Property") is "Margin" or "TranslationY" or "Opacity");
        }

        [Fact]
        public void OpponentsShouldKeepTheirExpandedCardBacksInsteadOfNumericCounters()
        {
            var page = LoadPage();
            Assert.Equal(3, page.Descendants().Count(e => (string?)e.Attribute("BindableLayout.ItemsSource") == "{Binding Backs}"));
            Assert.DoesNotContain(page.Descendants(), e => e.Name.LocalName == "Label"
                && (string?)e.Attribute("Text") is "{Binding CardCount}" or "{Binding CardsDescription}");
        }

        [Theory]
        [InlineData("West")]
        [InlineData("East")]
        public void SideCardBacksShouldFitTheSpaceLeftAfterTheSeatLabels(string seat)
        {
            var page = LoadPage();
            var side = Named(page, "MiddleRow").Elements().Single(e => (string?)e.Attribute("BindingContext") == "{Binding " + seat + "}");
            Assert.Equal("Grid", side.Name.LocalName);
            Assert.Equal("Auto,Auto,Auto,*", (string?)side.Attribute("RowDefinitions"));
            Assert.Equal("Fill", (string?)side.Attribute("VerticalOptions"));
            var backs = Assert.Single(side.Elements().Where(e => (string?)e.Attribute("BindableLayout.ItemsSource") == "{Binding Backs}"));
            Assert.Equal("VerticalCardBackLayout", backs.Name.LocalName);
            Assert.Equal("clr-namespace:Belot.UI.Controls", backs.Name.NamespaceName);
            Assert.Equal("3", (string?)backs.Attribute("Grid.Row"));
            Assert.Equal("Fill", (string?)backs.Attribute("VerticalOptions"));
            Assert.Equal("{ui:TableSize 1}", (string?)backs.Attribute("CardScale"));
            var image = Assert.Single(backs.Descendants().Where(e => e.Name.LocalName == "Image"));
            Assert.Null(image.Attribute("WidthRequest"));
            Assert.Null(image.Attribute("HeightRequest"));
            Assert.Equal("AspectFit", (string?)image.Attribute("Aspect"));
            Assert.Single(page.Descendants().Where(e => e.Name.LocalName == "HorizontalStackLayout"
                && (string?)e.Attribute("BindableLayout.ItemsSource") == "{Binding Backs}"));
        }

        [Fact]
        public void PlayedTricksShouldNameTheSeatAndCardForScreenReaders()
        {
            var page = LoadPage();
            foreach (var seat in new[] { "North", "East", "South", "West" })
            {
                AssertDescriptionBindings(Named(page, seat + "Played"), seat + ".Name", seat + ".PlayedCard.Description");
                var last = page.Descendants().Single(e => ((string?)e.Attribute("Source"))?.StartsWith("{Binding LastTrick" + seat + ".", StringComparison.Ordinal) == true);
                AssertDescriptionBindings(last, seat + ".Name", "LastTrick" + seat + ".Description");
            }
        }

        [Fact]
        public void ExpandedHandsShouldExposeOneSeatAndCountDescription()
        {
            var stacks = LoadPage().Descendants().Where(e => (string?)e.Attribute("BindableLayout.ItemsSource") == "{Binding Backs}").ToArray();
            Assert.Equal(3, stacks.Length);
            foreach (var stack in stacks)
            {
                AssertDescriptionBindings(stack, "Name", "CardsDescription");
                var back = Assert.Single(stack.Descendants().Where(e => e.Name.LocalName == "Image"));
                Assert.Equal("True", (string?)back.Attribute("AutomationProperties.ExcludedWithChildren"));
            }
        }

        [Fact]
        public void HintOutlinesShouldOverlayTheFixedCardWithoutChangingItsLayout()
        {
            var hand = Named(LoadPage(), "HandStack");
            var image = Assert.Single(hand.Descendants().Where(e => e.Name.LocalName == "ImageButton"));
            var card = image.Parent!;
            Assert.Equal("Grid", card.Name.LocalName);
            Assert.Equal(image.Attribute("WidthRequest")!.Value, (string?)card.Attribute("WidthRequest"));
            Assert.Equal(image.Attribute("HeightRequest")!.Value, (string?)card.Attribute("HeightRequest"));
            var hint = Assert.Single(card.Elements().Where(e => (string?)e.Attribute("IsVisible") == "{Binding IsHinted}"));
            Assert.Equal("Border", hint.Name.LocalName);
            Assert.Equal("True", (string?)hint.Attribute("InputTransparent"));
            Assert.Equal("Transparent", (string?)hint.Attribute("BackgroundColor"));
            Assert.Equal("3", (string?)hint.Attribute("StrokeThickness"));
            Assert.Equal("{StaticResource GoldHighlight}", (string?)hint.Attribute("Stroke"));
            Assert.Equal("0", (string?)card.Parent!.Attribute("StrokeThickness"));
            Assert.DoesNotContain(hand.Descendants(), e => (string?)e.Attribute("Binding") == "{Binding IsHinted}");
        }

        [Fact]
        public void TableTextShouldGrowWithinTheBoardWhileResultsKeepSystemScaling()
        {
            var page = LoadPage();
            var text = Named(page, "TableGrid").Descendants().Where(e => e.Attribute("FontSize") != null).ToArray();
            Assert.NotEmpty(text);
            Assert.All(text, element =>
            {
                Assert.Equal("False", (string?)element.Attribute("FontAutoScalingEnabled"));
                Assert.StartsWith("{ui:Table", (string?)element.Attribute("FontSize"));
            });
            foreach (var overlay in new[] { "RoundOverlayRoot", "GameOverlayRoot" })
            {
                Assert.All(Named(page, overlay).Descendants(), element => Assert.Null(element.Attribute("FontAutoScalingEnabled")));
            }
        }

        [Fact]
        public void DeclarationHintsShouldNotChangeTheSizeOfTheChoices()
        {
            var panel = Named(LoadPage(), "AnnouncePanel");
            Assert.DoesNotContain(panel.Descendants(), e => (string?)e.Attribute("Property") == "StrokeThickness");
            var highlight = Assert.Single(panel.Descendants().Where(e => (string?)e.Attribute("IsVisible") == "{Binding IsHinted}"));
            Assert.Equal("True", (string?)highlight.Attribute("InputTransparent"));
            Assert.Equal("2", (string?)highlight.Attribute("StrokeThickness"));
        }

        [Fact]
        public void DoubleAndRedoubleShouldShareTheBidActionRowWithoutHidingOpponentCards()
        {
            var buttons = Named(LoadPage(), "BidPanel").Descendants().Where(e => e.Name.LocalName == "Button").ToArray();
            var pass = buttons.Single(e => (string?)e.Attribute("BindingContext") == "{Binding BidOptions[8]}");
            foreach (var option in new[] { 4, 5, 6, 7 })
            {
                var button = buttons.Single(e => (string?)e.Attribute("BindingContext") == $"{{Binding BidOptions[{option}]}}");
                Assert.Same(pass.Parent, button.Parent);
            }

            Assert.Equal("*,*,Auto,*", (string?)pass.Parent!.Attribute("ColumnDefinitions"));
        }

        [Fact]
        public void StatisticsHeadingShouldHaveAFullWidthRowAvailableForLargeText()
        {
            var page = LoadPage("StatisticsPage");
            var heading = Named(page, "StatisticsHeading");
            Assert.Same(Named(page, "HeadingGrid"), heading.Parent);
            Assert.Equal("Auto,Auto", (string?)heading.Parent!.Attribute("RowDefinitions"));
        }

        private static void AssertDescriptionBindings(XElement control, string first, string second)
        {
            var description = Assert.Single(control.Elements().Where(e => e.Name.LocalName == "SemanticProperties.Description"));
            var binding = Assert.Single(description.Elements());
            Assert.Equal("MultiBinding", binding.Name.LocalName);
            Assert.Equal("{}{0}: {1}", (string?)binding.Attribute("StringFormat"));
            Assert.Equal(new[] { first, second }, binding.Elements().Select(e => (string?)e.Attribute("Path")));
        }

        private static XElement Named(XDocument page, string name) => page.Descendants().Single(e => (string?)e.Attribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2009/xaml")) == name);

        private static XDocument LoadPage(string page = "GamePage")
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Belot.sln")))
            {
                directory = directory.Parent;
            }

            return XDocument.Load(Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("src not found"), "UI", "Belot.UI", "Pages", page + ".xaml"));
        }
    }
}
