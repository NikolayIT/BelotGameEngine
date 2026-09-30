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
        [InlineData("neural-master")]
        [InlineData("rollout-master")]
        public void PublicFactoriesPreserveTheCurrentConfigurationAndReturnIndependentPlayers(string name)
        {
            Func<ClaudePlayerNeural> factory = name switch
            {
                "fast" => ClaudePlayerProfiles.CreateFast,
                "expert" => ClaudePlayerProfiles.CreateExpert,
                "neural-master" => ClaudePlayerProfiles.CreateNeuralMaster,
                "rollout-master" => ClaudePlayerProfiles.CreateRolloutMaster,
                _ => ClaudePlayerProfiles.CreateMaster,
            };
            var first = factory();
            var second = factory();
            Assert.NotSame(first, second);
            var master = name == "master";
            var neural = name == "neural-master";
            var rollout = name == "rollout-master";
            var human = !neural && !rollout;
            Assert.Equal(!rollout, first.UseEndgameSearch);
            Assert.Equal(rollout ? 100 : master ? 32 : 0, first.SearchDeals);
            Assert.Equal(rollout ? 400 : master ? 40 : 0, first.SearchTimeLimitMilliseconds);
            Assert.Equal(master ? 5 : 0, first.SearchDoubleDummyTricks);
            Assert.Equal(master ? 3 : 0, first.SearchCandidateCards);
            Assert.Equal(master ? 3 : 0, first.SearchCandidateMargin);
            Assert.Equal(0, first.Temperature);
            Assert.Equal(double.PositiveInfinity, first.MaxRegret);
            Assert.Equal(master ? 256 : neural ? 128 : 0, first.EndgameSampledWorlds);
            Assert.Equal(master ? 1500000 : neural ? 250000 : 0, first.EndgameNodeLimit);
            Assert.Equal(master ? 24 : neural ? 8 : 0, first.EndgameTimeLimitMilliseconds);
            Assert.Equal(0, first.EndgamePolicyActions);
            Assert.Equal(master || neural, first.EndgameUseTranspositions);
            Assert.False(first.EndgamePruneEquivalentCards);
            Assert.Equal(master || neural, first.CardSuitEnsemble);
            Assert.Equal(human, first.HumanStyle);
            Assert.True(first.NaturalBidding);
            Assert.Equal(human, first.EndgameRawTieBreak);
            Assert.Equal(human ? ClaudePlayerProfiles.DoubleMargin : 0, first.DoubleMargin);
            Assert.Equal(name == "expert" ? ClaudePlayerProfiles.ExpertTolerance : 0.3, first.HumanNetworkTolerance);
            Assert.Equal(name == "expert" ? ClaudePlayerProfiles.ExpertTolerance : 0.02, first.HumanSearchTolerance);
            Assert.Equal(master ? 0.3 : 1, first.EndgameSignalWeight);
            Assert.False(first.PlayForMatch);
            if (!rollout)
            {
                Assert.True(first.EndgameUseDeclarations);
                Assert.Equal(master || neural ? 5 : 3, first.EndgameTricks);
                Assert.Equal(master || neural ? 1680 : 90, first.EndgameThreeTrickWorldLimit);
            }

            if (master || neural)
            {
                Assert.Equal(1, first.EndgameOwnershipPower);
                Assert.Equal(0.1, first.EndgameOwnershipUniformMix);
            }

            first.Temperature = 7;
            first.SearchDeals = 17;
            first.EndgameTricks = 5;
            first.CardSuitEnsemble = !(master || neural);
            first.HumanStyle = !human;
            Assert.Equal(0, second.Temperature);
            Assert.Equal(rollout ? 100 : master ? 32 : 0, second.SearchDeals);
            Assert.Equal(master || neural ? 5 : rollout ? 2 : 3, second.EndgameTricks);
            Assert.Equal(master || neural, second.CardSuitEnsemble);
            Assert.Equal(human, second.HumanStyle);
        }

        [Theory]
        [InlineData("fast")]
        [InlineData("expert")]
        [InlineData("master")]
        [InlineData("neural-master")]
        [InlineData("rollout-master")]
        public void TrainerFactoriesKeepTheSuppliedModelsAndSeed(string name)
        {
            var models = RandomModels.Create(7331);
            var factory = OpponentCatalog.Factory(name, models);
            var player = Assert.IsType<ClaudePlayerNeural>(factory(7332));
            var other = Assert.IsType<ClaudePlayerNeural>(factory(7332));
            Assert.NotSame(player, other);
            Assert.NotSame(player.Rng, other.Rng);
            Assert.Equal(name == "master" || name == "neural-master", player.CardSuitEnsemble);
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
