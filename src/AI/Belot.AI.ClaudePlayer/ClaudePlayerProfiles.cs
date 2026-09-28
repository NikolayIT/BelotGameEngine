namespace Belot.AI.ClaudePlayer
{
    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Shared player configurations for the app, ratings and training opponents.</summary>
    public static class ClaudePlayerProfiles
    {
        /// <summary>The Expert's action temperature in game points.</summary>
        public const double ExpertTemperature = 1.5;

        /// <summary>The largest action-value loss the Expert may accept.</summary>
        public const double ExpertMaxRegret = 4;

        /// <summary>The Master uses bounded endings instead of full-deal rollouts.</summary>
        public const int MasterSearchDeals = 0;

        /// <summary>The Master's endgame search budget in milliseconds.</summary>
        public const int MasterMilliseconds = 8;

        /// <summary>The previous Master's number of sampled full-deal rollouts.</summary>
        public const int RolloutMasterSearchDeals = 100;

        /// <summary>The previous Master's rollout budget in milliseconds.</summary>
        public const int RolloutMasterMilliseconds = 400;

        /// <summary>Creates a fresh fast player using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateFast() => CreateFast(NeuralModels.Embedded);

        /// <summary>Creates a fresh Expert using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateExpert() => CreateExpert(NeuralModels.Embedded);

        /// <summary>Creates a fresh Master using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateMaster() => CreateMaster(NeuralModels.Embedded);

        /// <summary>Creates the previous Master with 100 full-deal neural rollouts, for comparison.</summary>
        public static ClaudePlayerNeural CreateRolloutMaster() => CreateRolloutMaster(NeuralModels.Embedded);

        internal static ClaudePlayerNeural CreateFast(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        internal static ClaudePlayerNeural CreateExpert(NeuralModels models)
        {
            var player = CreateFast(models);
            player.Temperature = ExpertTemperature;
            player.MaxRegret = ExpertMaxRegret;
            return player;
        }

        internal static ClaudePlayerNeural CreateMaster(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            SearchDeals = MasterSearchDeals,
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 5,
            EndgameThreeTrickWorldLimit = 1680,
            EndgameSampledWorlds = 128,
            EndgameNodeLimit = 250000,
            EndgameTimeLimitMilliseconds = MasterMilliseconds,
            EndgameUseTranspositions = true,
            EndgameOwnershipModel = CardOwnershipModel.Embedded,
            EndgameOwnershipPower = 1,
            EndgameOwnershipUniformMix = 0.1,
        };

        internal static ClaudePlayerNeural CreateRolloutMaster(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            SearchDeals = RolloutMasterSearchDeals,
            SearchTimeLimitMilliseconds = RolloutMasterMilliseconds,
        };
    }
}
