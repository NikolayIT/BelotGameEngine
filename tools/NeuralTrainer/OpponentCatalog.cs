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
                "fast" => seed => Seed(ClaudePlayerProfiles.CreateFast(models), seed),
                "sampled4" => seed => SampledFour(models, seed),
                "expert" => seed => Seed(ClaudePlayerProfiles.CreateExpert(models), seed),
                "master" => seed => Seed(ClaudePlayerProfiles.CreateMaster(models), seed),
                _ => throw new ArgumentException($"Unknown player '{name}'. Use random, dummy, smart, sharpbelot, belot206, neural, fast, sampled4, expert, master or ismcts:100.", nameof(name)),
            };
        }

        public static ClaudePlayerNeural Configured(TrainingSettings settings, NeuralModels models, int seed) => new ClaudePlayerNeural(models)
        {
            Rng = new Random(seed),
            MayDouble = settings.MayDouble,
            Temperature = settings.Temperature,
            MaxRegret = settings.MaxRegret,
            SearchDeals = settings.SearchDeals,
            SearchPriorDeals = settings.SearchPriorDeals,
            SearchPruneMargin = settings.SearchPruneMargin,
            SearchTimeLimitMilliseconds = settings.SearchMilliseconds,
            SearchControlVariateDeals = settings.SearchControlVariateDeals,
            SearchRolloutTricks = settings.SearchRolloutTricks,
            SearchRolloutRootLeaf = settings.SearchRolloutRootLeaf,
            SearchDoubleDummyTricks = settings.SearchDoubleDummyTricks,
            SearchUseDeclarations = settings.SearchDeclarations,
            SearchMinimumDeals = settings.SearchMinimumDeals,
            UseEndgameSearch = settings.Endgame,
            EndgameUseDeclarations = settings.EndgameDeclarations,
            EndgameTricks = settings.EndgameTricks,
            EndgameThreeTrickWorldLimit = settings.EndgameWorlds,
            EndgameSampledWorlds = settings.EndgameSampledWorlds,
            EndgameNodeLimit = settings.EndgameNodes,
            EndgameTimeLimitMilliseconds = settings.EndgameMilliseconds,
            EndgamePruneEquivalentCards = settings.EndgamePruning,
            EndgameUseTranspositions = settings.EndgameTranspositions,
            EndgamePolicyActions = settings.EndgamePolicyActions,
            EndgamePolicyTemperature = settings.EndgamePolicyTemperature,
            EndgamePolicyUniformMix = settings.EndgamePolicyUniformMix,
            EndgamePolicyPower = settings.EndgamePolicyPower,
            EndgameOwnershipModel = string.IsNullOrEmpty(settings.EndgameOwnership) ? null : CardOwnershipModel.LoadCached(settings.EndgameOwnership),
            EndgameOwnershipPower = settings.EndgameOwnershipPower,
            EndgameOwnershipUniformMix = settings.EndgameOwnershipUniformMix,
            CardCorrectionModel = string.IsNullOrEmpty(settings.CardCorrection) ? null : LateCardCorrectionModel.LoadCached(settings.CardCorrection),
        };

        private static ClaudePlayerNeural Neural(NeuralModels models, int seed, bool endgame) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            Rng = new Random(seed),
            UseEndgameSearch = endgame,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        private static ClaudePlayerNeural Seed(ClaudePlayerNeural player, int seed)
        {
            player.Rng = new Random(seed);
            return player;
        }

        private static ClaudePlayerNeural SampledFour(NeuralModels models, int seed)
        {
            return new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
            {
                Rng = new Random(seed),
                UseEndgameSearch = true,
                EndgameUseDeclarations = true,
                EndgameTricks = 4,
                EndgameThreeTrickWorldLimit = 1680,
                EndgameSampledWorlds = 128,
                EndgameNodeLimit = 250000,
            };
        }
    }
}
