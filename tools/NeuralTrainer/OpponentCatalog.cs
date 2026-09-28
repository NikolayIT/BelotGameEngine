namespace Belot.NeuralTrainer
{
    using System;
    using System.Globalization;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine.Players;
    using BelotLegacy;

    /// <summary>One named catalog for reproducible evaluation and training opponents.</summary>
    internal static class OpponentCatalog
    {
        public static Func<int, IPlayer> Factory(string name, NeuralModels models = null)
        {
            if (name.StartsWith("ismcts:", StringComparison.OrdinalIgnoreCase))
            {
                var milliseconds = int.Parse(name[7..], CultureInfo.InvariantCulture);
                if (milliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(name), "ISMCTS milliseconds must be positive.");
                }

                return seed => new ClaudePlayerIsmcts { Rng = new Random(seed), TimeLimitMilliseconds = milliseconds };
            }

            return name.ToLowerInvariant() switch
            {
                "random" => seed => new RandomPlayer(new Random(seed)),
                "dummy" => _ => new DummyPlayer(),
                "smart" => _ => new SmartPlayer(),
                "sharpbelot" => seed => new SharpBelotPlayer(seed),
                "belot206" => seed => new Belot206Player(seed),
                "neural" => seed => Neural(models, seed, false),
                "fast" => seed => Neural(models, seed, true),
                "expert" => seed => Expert(models, seed),
                "master" => seed => Master(models, seed),
                _ => throw new ArgumentException($"Unknown player '{name}'. Use random, dummy, smart, sharpbelot, belot206, neural, fast, expert, master or ismcts:100.", nameof(name)),
            };
        }

        private static ClaudePlayerNeural Neural(NeuralModels models, int seed, bool endgame) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            Rng = new Random(seed),
            UseEndgameSearch = endgame,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        private static ClaudePlayerNeural Expert(NeuralModels models, int seed)
        {
            var player = Neural(models, seed, true);
            player.Temperature = 1.5;
            player.MaxRegret = 4;
            return player;
        }

        private static ClaudePlayerNeural Master(NeuralModels models, int seed)
        {
            var player = Neural(models, seed, false);
            player.SearchDeals = 100;
            player.SearchTimeLimitMilliseconds = 400;
            return player;
        }
    }
}
