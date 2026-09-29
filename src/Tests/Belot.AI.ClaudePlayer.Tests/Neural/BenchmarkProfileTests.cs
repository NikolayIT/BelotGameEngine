namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.NeuralTrainer;

    using Xunit;

    public class BenchmarkProfileTests
    {
        [Theory]
        [InlineData("candidate")]
        [InlineData("neural")]
        public void ConfiguredBenchmarksPreserveExplicitFlags(string name)
        {
            var settings = TrainingSettings.Parse(
                new[] { "--player", name, "--card-suit-ensemble", "true", "--search-deals", "2", "--endgame", "true", "--endgame-tricks", "4" },
                new TrainingSettings());
            var models = RandomModels.Create(7211);
            var player = Program.CreateBenchmarkPlayer(settings, models, 7212);
            Assert.Same(models, player.Models);
            Assert.True(player.CardSuitEnsemble);
            Assert.Equal(2, player.SearchDeals);
            Assert.True(player.UseEndgameSearch);
            Assert.Equal(4, player.EndgameTricks);
            Assert.Equal(new Random(7212).Next(), player.Rng.Next());
        }

        [Theory]
        [InlineData("master")]
        [InlineData("rollout-master")]
        [InlineData("fast")]
        [InlineData("expert")]
        public void NamedBenchmarksUseTheExactProfileInsteadOfConflictingFlags(string name)
        {
            var settings = new TrainingSettings { Player = name, SearchDeals = 17, Endgame = false, CardSuitEnsemble = name != "master", Temperature = 7 };
            var models = RandomModels.Create(7213);
            var expected = Assert.IsType<ClaudePlayerNeural>(OpponentCatalog.Factory(name, models)(7214));
            var actual = Program.CreateBenchmarkPlayer(settings, models, 7214);
            Assert.Same(models, actual.Models);
            Assert.Equal(expected.Rng.Next(), actual.Rng.Next());
            Assert.Equal(expected.SearchDeals, actual.SearchDeals);
            Assert.Equal(expected.SearchTimeLimitMilliseconds, actual.SearchTimeLimitMilliseconds);
            Assert.Equal(expected.UseEndgameSearch, actual.UseEndgameSearch);
            Assert.Equal(expected.EndgameTricks, actual.EndgameTricks);
            Assert.Equal(expected.EndgameTimeLimitMilliseconds, actual.EndgameTimeLimitMilliseconds);
            Assert.Equal(expected.Temperature, actual.Temperature);
            Assert.Equal(expected.CardSuitEnsemble, actual.CardSuitEnsemble);
            Assert.NotEqual(settings.CardSuitEnsemble, actual.CardSuitEnsemble);
        }

        [Fact]
        public void BenchmarksRejectNonNeuralProfiles()
        {
            var settings = new TrainingSettings { Player = "smart" };
            Assert.Throws<ArgumentException>(() => Program.CreateBenchmarkPlayer(settings, RandomModels.Create(7215), 7216));
        }
    }
}
