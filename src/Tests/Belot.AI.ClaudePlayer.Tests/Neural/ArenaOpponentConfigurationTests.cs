namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.GameMechanics;
    using Belot.NeuralTrainer;

    using Xunit;

    public class ArenaOpponentConfigurationTests
    {
        [Fact]
        public void ConfiguredOpponentUsesIndependentSettingsAndReportsResolvedValues()
        {
            WithConfig(
                "{\"Endgame\":true,\"EndgameTricks\":5,\"EndgameSampledWorlds\":64,\"EndgameNodes\":3210,"
                + "\"EndgameTranspositions\":true,\"EndgameOwnershipPower\":0.8,\"Temperature\":0.25,\"MayDouble\":false}",
                settings =>
                {
                    settings.Opponent = "configured";
                    settings.SearchDeals = 99;
                    settings.Temperature = 4;
                    settings.EndgameTricks = 2;
                    var resolved = Arena.ResolveOpponent(settings, RandomModels.Create(179));
                    var player = Assert.IsType<ClaudePlayerNeural>(resolved.Create(19));
                    Assert.Equal("configured", resolved.Name);
                    Assert.NotSame(settings, resolved.Settings);
                    Assert.Equal("candidate", resolved.Settings.Player);
                    Assert.True(player.UseEndgameSearch);
                    Assert.Equal(5, player.EndgameTricks);
                    Assert.Equal(64, player.EndgameSampledWorlds);
                    Assert.Equal(3210, player.EndgameNodeLimit);
                    Assert.True(player.EndgameUseTranspositions);
                    Assert.Equal(.8, player.EndgameOwnershipPower);
                    Assert.Equal(.25, player.Temperature);
                    Assert.False(player.MayDouble);
                    Assert.Equal(0, player.SearchDeals);
                    Assert.Equal(99, settings.SearchDeals);
                    Assert.Equal(2, settings.EndgameTricks);

                    var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
                    using var report = JsonDocument.Parse(JsonSerializer.Serialize(resolved, options));
                    Assert.Equal("configured", report.RootElement.GetProperty("Name").GetString());
                    Assert.Equal(string.Empty, report.RootElement.GetProperty("In").GetString());
                    Assert.Equal(5, report.RootElement.GetProperty("Settings").GetProperty("EndgameTricks").GetInt32());
                    Assert.False(report.RootElement.TryGetProperty("Create", out _));
                });
        }

        [Theory]
        [InlineData("config")]
        [InlineData("opponent")]
        [InlineData("candidate")]
        public void OpponentWeightPriorityIsConfigThenOpponentFolderThenCandidate(string source)
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-arena-weights-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var candidatePath = Path.Combine(directory, "candidate");
                var opponentPath = Path.Combine(directory, "opponent");
                var configPath = Path.Combine(directory, "config");
                RandomModels.Create(180).Save(candidatePath);
                RandomModels.Create(181).Save(opponentPath);
                RandomModels.Create(182).Save(configPath);
                var settings = new TrainingSettings
                {
                    In = candidatePath,
                    OpponentIn = source == "candidate" ? string.Empty : source == "config" ? Path.Combine(directory, "missing") : opponentPath,
                    OpponentConfig = Path.Combine(directory, "opponent.json"),
                };
                File.WriteAllText(settings.OpponentConfig, JsonSerializer.Serialize(new { In = source == "config" ? configPath : string.Empty }));
                var resolved = Arena.ResolveOpponent(settings, NeuralModels.Load(candidatePath));
                var expectedPath = source == "config" ? configPath : source == "opponent" ? opponentPath : candidatePath;
                Assert.Equal(Path.GetFullPath(expectedPath), resolved.In);
                Assert.Equal(resolved.In, resolved.Settings.In);
                var expected = new ClaudePlayerNeural(NeuralModels.Load(expectedPath));
                var actual = Assert.IsType<ClaudePlayerNeural>(resolved.Create(23));
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(184) });
                match.Start();
                var context = match.CreateBidContext();
                var expectedValues = expected.EvaluateBids(context).OrderBy(value => value.Bid).ToArray();
                var actualValues = actual.EvaluateBids(context).OrderBy(value => value.Bid).ToArray();
                Assert.Equal(expectedValues.Select(value => value.Bid), actualValues.Select(value => value.Bid));
                Assert.Equal(expectedValues.Select(value => value.Value), actualValues.Select(value => value.Value));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void NamedOpponentsKeepTheirCatalogSettingsWhenNoConfigIsPresent()
        {
            var settings = new TrainingSettings { Opponent = "fast", SearchDeals = 99, EndgameTricks = 5, Temperature = 4 };
            var resolved = Arena.ResolveOpponent(settings, RandomModels.Create(185));
            var player = Assert.IsType<ClaudePlayerNeural>(resolved.Create(24));
            Assert.Equal("fast", resolved.Name);
            Assert.Null(resolved.Settings);
            Assert.True(player.UseEndgameSearch);
            Assert.Equal(3, player.EndgameTricks);
            Assert.Equal(90, player.EndgameThreeTrickWorldLimit);
            Assert.Equal(0, player.SearchDeals);
            Assert.Equal(0, player.Temperature);
        }

        [Theory]
        [InlineData("{\"EndgameTrikcs\":5}")]
        [InlineData("{\"Endgame\":true,\"endgame\":false}")]
        [InlineData("{\"Endgame\":1}")]
        [InlineData("{\"Endgame\":true")]
        [InlineData("[]")]
        [InlineData("null")]
        public void MalformedUnknownDuplicateOrNonObjectConfigurationsAreRejected(string json)
        {
            WithConfig(json, settings => Assert.ThrowsAny<JsonException>(() => Arena.ResolveOpponent(settings, RandomModels.Create(186))));
        }

        [Fact]
        public void NamedOpponentAndNestedConfigurationAreRejected()
        {
            WithConfig("{}", settings =>
            {
                settings.Opponent = "smart";
                Assert.Throws<ArgumentException>(() => Arena.ResolveOpponent(settings, RandomModels.Create(187)));
            });
            WithConfig("{\"OpponentConfig\":\"other.json\"}", settings =>
                Assert.Throws<ArgumentException>(() => Arena.ResolveOpponent(settings, RandomModels.Create(188))));
        }

        [Fact]
        public void CommandLineAcceptsOpponentConfigAndJsonCanReadReportedInfinity()
        {
            var parsed = TrainingSettings.Parse(new[] { "--opponent-config", "opponent.json" }, new TrainingSettings());
            Assert.Equal("opponent.json", parsed.OpponentConfig);
            WithConfig("{\"MaxRegret\":\"Infinity\",\"temperature\":0.5}", settings =>
            {
                var resolved = Arena.ResolveOpponent(settings, RandomModels.Create(189));
                var player = Assert.IsType<ClaudePlayerNeural>(resolved.Create(25));
                Assert.True(double.IsPositiveInfinity(player.MaxRegret));
                Assert.Equal(.5, player.Temperature);
            });
        }

        private static void WithConfig(string json, Action<TrainingSettings> check)
        {
            var directory = Path.Combine(Path.GetTempPath(), "belot-arena-config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "opponent.json");
                File.WriteAllText(path, json);
                check(new TrainingSettings { OpponentConfig = path });
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
