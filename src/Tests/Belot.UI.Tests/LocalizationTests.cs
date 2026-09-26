namespace Belot.UI.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    // The app's two string tables: the same keys and arguments in both, every key the app uses
    // present, and the rules page saying what the engine does.
    [Collection(AppState.Name)]
    public class LocalizationTests
    {
        private static readonly string[] Languages = { LocalizationManager.English, LocalizationManager.Bulgarian };

        public LocalizationTests() => AppState.Reset();

        [Fact]
        public void BothLanguagesShouldHaveTheSameStrings()
        {
            var english = AppStrings.Table(LocalizationManager.English).Keys.OrderBy(k => k, StringComparer.Ordinal);
            var bulgarian = AppStrings.Table(LocalizationManager.Bulgarian).Keys.OrderBy(k => k, StringComparer.Ordinal);
            Assert.Equal(english, bulgarian);
        }

        // A translation takes the same arguments as the English text, so Format never throws or
        // drops a value in either language.
        [Fact]
        public void TranslationsShouldTakeTheSameArguments()
        {
            foreach (var (key, english) in AppStrings.Table(LocalizationManager.English))
            {
                var bulgarian = AppStrings.Table(LocalizationManager.Bulgarian)[key];
                Assert.True(Placeholders(english) == Placeholders(bulgarian), $"{key}: \"{english}\" vs \"{bulgarian}\"");
                _ = string.Format(bulgarian, "a", "b", "c", "d");
            }
        }

        // Every key the pages and the game layer ask for is in the tables (a missing one shows
        // as the key itself).
        [Fact]
        public void EveryKeyTheAppUsesShouldBeTranslated()
        {
            var app = Path.Combine(FindSource(), "UI", "Belot.UI");
            var files = Directory.EnumerateFiles(app, "*.xaml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(f => !f.EndsWith("AppStrings.cs", StringComparison.Ordinal))
                .ToList();
            Assert.NotEmpty(files);

            var keyPattern = new Regex(@"(?:loc:Tr |\[""|Format\("")([A-Z][A-Za-z]+_[A-Za-z0-9_]+)");
            var used = files.SelectMany(f => keyPattern.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value)).Distinct().ToList();
            Assert.True(used.Count > 100, $"only {used.Count} keys found");
            foreach (var language in Languages)
            {
                var table = AppStrings.Table(language);
                Assert.All(used, key => Assert.True(table.ContainsKey(key), $"{language}: {key} is missing"));
            }
        }

        // The texts built from keys (levels, ranks, bids, combinations) are all translated.
        [Fact]
        public void TheGameTextsShouldBeTranslatedInBothLanguages()
        {
            foreach (var language in Languages)
            {
                LocalizationManager.Instance.SetLanguage(language);
                foreach (var level in AiLevels.All)
                {
                    Assert.DoesNotContain("_", level.DisplayName + level.Tagline + level.DifficultyLabel);
                }

                var texts = Card.AllCards.Select(BelotTexts.CardText)
                    .Concat(Enum.GetValues<BidType>().Select(BelotTexts.BidText))
                    .Concat(Enum.GetValues<BidType>().Select(BelotTexts.ShortBidText))
                    .Concat(Enum.GetValues<AnnounceType>().SelectMany(type => Card.AllCards.Select(card => BelotTexts.CombinationText(type, card))))
                    .ToList();
                Assert.All(texts, text => Assert.DoesNotContain("_", text));
            }

            LocalizationManager.Instance.SetLanguage(LocalizationManager.Bulgarian);
            Assert.Equal("Р♠", BelotTexts.CardText(Card.GetCard(CardSuit.Spade, CardType.King)));
            Assert.Equal("Каре дами", BelotTexts.CombinationText(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Heart, CardType.Queen)));
            Assert.Equal("♥ Купа ×2", BelotTexts.ContractText(BidType.Hearts | BidType.Double));
            Assert.Equal("Без коз", BelotTexts.BidText(BidType.NoTrumps));
        }

        // The rules page states the numbers the engine plays by.
        [Fact]
        public void TheRulesPageShouldStateTheEnginesNumbers()
        {
            foreach (var language in Languages)
            {
                var text = string.Join(" ", AppStrings.Table(language).Where(x => x.Key.StartsWith("Rules_", StringComparison.Ordinal)).Select(x => x.Value));
                Assert.Contains(BelotMatch.PointsToWin.ToString(), text);
                foreach (var number in new[] { "90", "20", "50", "100", "150", "200", "10" })
                {
                    Assert.Matches($@"\b{number}\b", text);
                }
            }
        }

        private static string Placeholders(string text) =>
            string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(v => v, StringComparer.Ordinal));

        // The repository's src folder, found from where the tests run.
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
