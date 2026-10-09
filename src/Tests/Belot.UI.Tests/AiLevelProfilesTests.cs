namespace Belot.UI.Tests
{
    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Heuristic;
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
            var master = Assert.IsType<ClaudePlayerHeuristic>(masterLevel.CreatePlayer());
            Assert.Equal(0, expert.Temperature);
            Assert.True(expert.HumanStyle);
            Assert.Equal(AiLevels.ExpertTolerance, expert.HumanNetworkTolerance);
            Assert.Equal(AiLevels.ExpertTolerance, expert.HumanSearchTolerance);
            Assert.True(expert.UseEndgameSearch);
            Assert.Equal(3, expert.EndgameTricks);
            Assert.Equal(90, expert.EndgameThreeTrickWorldLimit);
            Assert.False(expert.CardSuitEnsemble);

            // The Master is the heuristic player: learned bids, the taught rules, exact last five tricks.
            Assert.Equal(5, AiLevels.MasterEndgameTricks);
            Assert.Equal(AiLevels.MasterEndgameTricks, master.Settings.EndgameTricks);
            Assert.Equal(48, master.Settings.EndgameWorlds);
            Assert.Equal(150000, master.Settings.EndgameNodeLimit);
            Assert.Same(BidModel.Default, master.Settings.Bids);

            // Unlike a deterministic host, the app keeps the endgame's clock: slower phones search less, not longer.
            Assert.True(master.Settings.EndgameMilliseconds > 0);
            Assert.NotSame(expert, expertLevel.CreatePlayer());
            var another = Assert.IsType<ClaudePlayerHeuristic>(masterLevel.CreatePlayer());
            Assert.NotSame(master, another);
            Assert.NotSame(master.Settings, another.Settings);
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
            Assert.True(first.HumanStyle);
            Assert.True(first.EndgameRawTieBreak);
            Assert.False(first.CardSuitEnsemble);
            Assert.False(second.CardSuitEnsemble);
            first.EndgameTricks = 5;
            Assert.Equal(3, second.EndgameTricks);
        }
    }
}
