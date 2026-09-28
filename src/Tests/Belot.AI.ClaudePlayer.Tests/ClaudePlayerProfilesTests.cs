namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.GameMechanics;
    using Belot.NeuralTrainer;

    using Xunit;

    public class ClaudePlayerProfilesTests
    {
        [Theory]
        [InlineData("fast")]
        [InlineData("expert")]
        [InlineData("master")]
        public void PublicFactoriesPreserveTheCurrentConfigurationAndReturnIndependentPlayers(string name)
        {
            Func<ClaudePlayerNeural> factory = name switch
            {
                "fast" => ClaudePlayerProfiles.CreateFast,
                "expert" => ClaudePlayerProfiles.CreateExpert,
                _ => ClaudePlayerProfiles.CreateMaster,
            };
            var first = factory();
            var second = factory();
            Assert.NotSame(first, second);
            Assert.Equal(name != "master", first.UseEndgameSearch);
            Assert.Equal(name == "master" ? 100 : 0, first.SearchDeals);
            Assert.Equal(name == "master" ? 400 : 0, first.SearchTimeLimitMilliseconds);
            Assert.Equal(name == "expert" ? 1.5 : 0, first.Temperature);
            Assert.Equal(name == "expert" ? 4 : double.PositiveInfinity, first.MaxRegret);
            Assert.Equal(0, first.EndgameSampledWorlds);
            Assert.Equal(0, first.EndgameNodeLimit);
            Assert.Equal(0, first.EndgameTimeLimitMilliseconds);
            Assert.Equal(0, first.EndgamePolicyActions);
            Assert.False(first.EndgameUseTranspositions);
            if (name != "master")
            {
                Assert.True(first.EndgameUseDeclarations);
                Assert.Equal(3, first.EndgameTricks);
                Assert.Equal(90, first.EndgameThreeTrickWorldLimit);
            }

            first.Temperature = 7;
            first.SearchDeals = 17;
            first.EndgameTricks = 5;
            Assert.Equal(name == "expert" ? 1.5 : 0, second.Temperature);
            Assert.Equal(name == "master" ? 100 : 0, second.SearchDeals);
            Assert.Equal(name == "master" ? 2 : 3, second.EndgameTricks);
        }

        [Theory]
        [InlineData("fast")]
        [InlineData("expert")]
        [InlineData("master")]
        public void TrainerFactoriesKeepTheSuppliedModelsAndSeed(string name)
        {
            var models = RandomModels.Create(7331);
            var factory = OpponentCatalog.Factory(name, models);
            var player = Assert.IsType<ClaudePlayerNeural>(factory(7332));
            var other = Assert.IsType<ClaudePlayerNeural>(factory(7332));
            Assert.NotSame(player, other);
            Assert.NotSame(player.Rng, other.Rng);
            Assert.Equal(new Random(7332).Next(), player.Rng.Next());
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(7333) });
            match.Start();
            var context = match.CreateBidContext();
            Assert.Equal(new ClaudePlayerNeural(models).EvaluateBids(context).Select(value => value.Value), player.EvaluateBids(context).Select(value => value.Value));
        }

        [Fact]
        public void HistoricalSampledFourControlKeepsItsFixedConfiguration()
        {
            var player = Assert.IsType<ClaudePlayerNeural>(OpponentCatalog.Factory("sampled4", RandomModels.Create(7334))(7335));
            Assert.Equal(new Random(7335).Next(), player.Rng.Next());
            Assert.True(player.UseEndgameSearch);
            Assert.True(player.EndgameUseDeclarations);
            Assert.Equal(4, player.EndgameTricks);
            Assert.Equal(1680, player.EndgameThreeTrickWorldLimit);
            Assert.Equal(128, player.EndgameSampledWorlds);
            Assert.Equal(250000, player.EndgameNodeLimit);
            Assert.Equal(0, player.EndgameTimeLimitMilliseconds);
            Assert.Equal(0, player.SearchDeals);
            Assert.Equal(0, player.Temperature);
            Assert.False(player.EndgameUseTranspositions);
        }
    }
}
