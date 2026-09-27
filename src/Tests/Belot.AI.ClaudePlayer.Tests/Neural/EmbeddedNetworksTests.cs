namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine;
    using Belot.Engine.Players;

    using Xunit;

    /// <summary>The trained networks built into the assembly (see NEURAL_NETWORK.md).</summary>
    public class EmbeddedNetworksTests
    {
        [Fact]
        public void TheTrainedNetworksAreEmbedded()
        {
            var models = NeuralModels.Embedded;
            Assert.Equal(4, models.Networks.Length);
            for (var tag = 0; tag < 4; tag++)
            {
                Assert.Equal(tag, models.Networks[tag].Tag);
                Assert.Equal(FeatureEncoder.LayoutVersion, models.Networks[tag].Layout);
            }
        }

        // A sanity check, not the benchmark (the simulator's neural suite is): two of the
        // player win clearly against two SmartPlayers in mirrored games.
        [Fact]
        public void TheTrainedPlayerBeatsSmartPlayer()
        {
            var wins = 0;
            const int Pairs = 100;
            for (var i = 0; i < Pairs; i++)
            {
                var neural = new[] { new ClaudePlayerNeural(), new ClaudePlayerNeural() };
                var smart = new[] { new SmartPlayer.SmartPlayer(), new SmartPlayer.SmartPlayer() };
                var first = (PlayerPosition)(1 << (i % 4));
                var game = new BelotGame(neural[0], smart[0], neural[1], smart[1], new Random(i)).PlayGame(first);
                var mirror = new BelotGame(smart[0], neural[0], smart[1], neural[1], new Random(i)).PlayGame(first);
                wins += (game.Winner == PlayerPosition.SouthNorthTeam ? 1 : 0) + (mirror.Winner == PlayerPosition.EastWestTeam ? 1 : 0);
                Assert.Equal(0, neural[0].Fallbacks + neural[1].Fallbacks);
            }

            Assert.True(wins > Pairs * 2 * 0.65, $"{wins} of {Pairs * 2}");
        }
    }
}
