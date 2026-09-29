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
        public const double ExpertTemperature = ClaudePlayerProfiles.ExpertTemperature;

        public const double ExpertMaxRegret = ClaudePlayerProfiles.ExpertMaxRegret;

        /// <summary>
        /// The Master uses neural action values followed by bounded five-trick endings,
        /// with a learned model of unseen card ownership. Full-deal rollouts are disabled.
        /// </summary>
        public const int MasterSearchDeals = ClaudePlayerProfiles.MasterSearchDeals;

        /// <summary>The Master's endgame budget; managed execution is not a hard real-time deadline.</summary>
        public const int MasterMilliseconds = ClaudePlayerProfiles.MasterMilliseconds;

        // Pair ratings (two of a level against two of another) from the `elo` simulator round
        // robin, anchored at Dummy = 1200 (September 29, 2026, elo 20000 60, belief5-v2 Master).
        // Re-run that suite and update these if the players change; uncertainty is in NEURAL_NETWORK.md.
        public static IReadOnlyList<AiLevel> All { get; } = new[]
        {
            new AiLevel("random", "🎲", "Level_Random_Name", "Level_Random_Tag", 1, 669, () => new RandomPlayer()),
            new AiLevel("dummy", "🙂", "Level_Dummy_Name", "Level_Dummy_Tag", 2, 1200, () => new DummyPlayer()),
            new AiLevel("smart", "🃏", "Level_Smart_Name", "Level_Smart_Tag", 3, 1462, () => new SmartPlayer()),
            new AiLevel(
                "expert",
                "🎓",
                "Level_Expert_Name",
                "Level_Expert_Tag",
                4,
                1611,
                ClaudePlayerProfiles.CreateExpert),
            new AiLevel(
                "claude",
                "👑",
                "Level_Claude_Name",
                "Level_Claude_Tag",
                5,
                1838,
                ClaudePlayerProfiles.CreateMaster),
        };

        /// <summary>The level with this id, or the Smart one for an unknown id.</summary>
        public static AiLevel ById(string? id) => Find(id) ?? All[2];

        /// <summary>The level with this id, or null for an unknown one.</summary>
        public static AiLevel? Find(string? id) =>
            All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>The validated fast profile used for hints and as the basis of Expert.</summary>
        public static ClaudePlayerNeural CreateFastPlayer() => ClaudePlayerProfiles.CreateFast();
    }
}
