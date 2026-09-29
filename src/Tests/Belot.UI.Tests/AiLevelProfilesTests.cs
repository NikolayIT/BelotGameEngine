namespace Belot.UI.Tests
{
    using Belot.AI.ClaudePlayer;
    using Belot.UI.Game;

    using Xunit;

    [Collection(AppState.Name)]
    public class AiLevelProfilesTests
    {
        public AiLevelProfilesTests() => AppState.Reset();

        [Fact]
        public void AppLevelsUseTheSharedExpertAndMasterFactories()
        {
            var expertLevel = AiLevels.ById("expert");
            var masterLevel = AiLevels.ById("claude");
            Assert.Equal(typeof(ClaudePlayerProfiles), expertLevel.Factory.Method.DeclaringType);
            Assert.Equal(typeof(ClaudePlayerProfiles), masterLevel.Factory.Method.DeclaringType);
            var expert = Assert.IsType<ClaudePlayerNeural>(expertLevel.CreatePlayer());
            var master = Assert.IsType<ClaudePlayerNeural>(masterLevel.CreatePlayer());
            Assert.Equal(AiLevels.ExpertTemperature, expert.Temperature);
            Assert.Equal(AiLevels.ExpertMaxRegret, expert.MaxRegret);
            Assert.True(expert.UseEndgameSearch);
            Assert.Equal(3, expert.EndgameTricks);
            Assert.Equal(90, expert.EndgameThreeTrickWorldLimit);
            Assert.False(expert.CardSuitEnsemble);
            Assert.Equal(AiLevels.MasterSearchDeals, master.SearchDeals);
            Assert.Equal(0, master.SearchTimeLimitMilliseconds);
            Assert.Equal(AiLevels.MasterMilliseconds, master.EndgameTimeLimitMilliseconds);
            Assert.True(master.UseEndgameSearch);
            Assert.True(master.EndgameUseDeclarations);
            Assert.Equal(5, master.EndgameTricks);
            Assert.Equal(1680, master.EndgameThreeTrickWorldLimit);
            Assert.Equal(128, master.EndgameSampledWorlds);
            Assert.Equal(250000, master.EndgameNodeLimit);
            Assert.True(master.EndgameUseTranspositions);
            Assert.True(master.CardSuitEnsemble);
            Assert.NotSame(expert, expertLevel.CreatePlayer());
            Assert.NotSame(master, masterLevel.CreatePlayer());
        }

        [Fact]
        public void HintsKeepTheirCheapIndependentFastProfile()
        {
            var first = AiLevels.CreateFastPlayer();
            var second = AiLevels.CreateFastPlayer();
            Assert.NotSame(first, second);
            Assert.True(first.UseEndgameSearch);
            Assert.True(first.EndgameUseDeclarations);
            Assert.Equal(3, first.EndgameTricks);
            Assert.Equal(90, first.EndgameThreeTrickWorldLimit);
            Assert.Equal(0, first.SearchDeals);
            Assert.Equal(0, first.EndgameSampledWorlds);
            Assert.Equal(0, first.Temperature);
            Assert.False(first.CardSuitEnsemble);
            Assert.False(second.CardSuitEnsemble);
            first.EndgameTricks = 5;
            Assert.Equal(3, second.EndgameTricks);
        }
    }
}
