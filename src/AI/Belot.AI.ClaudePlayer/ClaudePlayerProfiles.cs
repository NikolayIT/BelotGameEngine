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

        /// <summary>The Master's number of sampled full-deal rollouts.</summary>
        public const int MasterSearchDeals = 100;

        /// <summary>The Master's search budget in milliseconds.</summary>
        public const int MasterMilliseconds = 400;

        /// <summary>Creates a fresh fast player using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateFast() => CreateFast(NeuralModels.Embedded);

        /// <summary>Creates a fresh Expert using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateExpert() => CreateExpert(NeuralModels.Embedded);

        /// <summary>Creates a fresh Master using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateMaster() => CreateMaster(NeuralModels.Embedded);

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
            SearchTimeLimitMilliseconds = MasterMilliseconds,
        };
    }
}
