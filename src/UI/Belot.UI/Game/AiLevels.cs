namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;

    public static class AiLevels
    {
        /// <summary>
        /// The Expert uses the networks with bounded endgames and plays a little loose: a card or bid worth d
        /// game points less than the best is chosen e^(-d / T) times as often, never one worse than the best
        /// by more than MaxRegret points (see ClaudePlayerNeural).
        /// </summary>
        public const double ExpertTemperature = 1.5;

        public const double ExpertMaxRegret = 4;

        /// <summary>
        /// The Master plays each legal card out in this many deals of the unseen cards, with the
        /// networks for every seat (ClaudePlayerNeural.SearchDeals): about 60 ms a card on a
        /// desktop. It checks <see cref="MasterMilliseconds"/> between complete sampled deals,
        /// after at least eight, so the budget is not a strict deadline.
        /// </summary>
        public const int MasterSearchDeals = 100;

        public const int MasterMilliseconds = 400;

        // Pair ratings (two of a level against two of another) from the `elo` simulator round
        // robin, anchored at Dummy = 1200 (September 27, 2026, elo 20000 60, PPO all-trump weights).
        // Re-run that suite and update these if the players change; uncertainty is in NEURAL_NETWORK.md.
        public static IReadOnlyList<AiLevel> All { get; } = new[]
        {
            new AiLevel("random", "🎲", "Level_Random_Name", "Level_Random_Tag", 1, 656, () => new RandomPlayer()),
            new AiLevel("dummy", "🙂", "Level_Dummy_Name", "Level_Dummy_Tag", 2, 1200, () => new DummyPlayer()),
            new AiLevel("smart", "🃏", "Level_Smart_Name", "Level_Smart_Tag", 3, 1466, () => new SmartPlayer()),
            new AiLevel(
                "expert",
                "🎓",
                "Level_Expert_Name",
                "Level_Expert_Tag",
                4,
                1600,
                CreateExpertPlayer),
            new AiLevel(
                "claude",
                "👑",
                "Level_Claude_Name",
                "Level_Claude_Tag",
                5,
                1760,
                () => new ClaudePlayerNeural { SearchDeals = MasterSearchDeals, SearchTimeLimitMilliseconds = MasterMilliseconds }),
        };

        /// <summary>The level with this id, or the Smart one for an unknown id.</summary>
        public static AiLevel ById(string? id) => Find(id) ?? All[2];

        /// <summary>The level with this id, or null for an unknown one.</summary>
        public static AiLevel? Find(string? id) =>
            All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>The validated fast profile used for hints and as the basis of Expert.</summary>
        public static ClaudePlayerNeural CreateFastPlayer() => new ClaudePlayerNeural
        {
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        private static ClaudePlayerNeural CreateExpertPlayer()
        {
            var player = CreateFastPlayer();
            player.Temperature = ExpertTemperature;
            player.MaxRegret = ExpertMaxRegret;
            return player;
        }
    }
}
