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
        /// The Expert uses the networks with bounded endgames and plays like a person a little below the
        /// Master: among the cards valued within this many game points of the best it plays the most
        /// natural one (see ClaudePlayerNeural.HumanStyle), so it is weaker without ever being absurd.
        /// </summary>
        public const double ExpertTolerance = ClaudePlayerProfiles.ExpertTolerance;

        /// <summary>
        /// The Master is the heuristic player (see HEURISTIC_PLAYER.md): it bids by point counts learned from
        /// how every bid did and plays its first tricks by the rules strong players teach (it counts the
        /// cards, draws trumps from the top, signals to its partner and reads its partner's discards), then
        /// plays this many last tricks out exactly over the deals the play allows. It needs no networks.
        /// </summary>
        public const int MasterEndgameTricks = ClaudePlayerProfiles.HeuristicEndgameTricks;

        // Pair ratings (two of a level against two of another) from the `elo` simulator round
        // robin, anchored at Dummy = 1200 (October 8, 2026, elo 20000 60 20000, human-style
        // Expert and the heuristic Master). Re-run that suite and update these if the players
        // change; uncertainty is in HEURISTIC_PLAYER.md.
        public static IReadOnlyList<AiLevel> All { get; } = new[]
        {
            new AiLevel("random", "🎲", "Level_Random_Name", "Level_Random_Tag", 1, 659, () => new RandomPlayer()),
            new AiLevel("dummy", "🙂", "Level_Dummy_Name", "Level_Dummy_Tag", 2, 1200, () => new DummyPlayer()),
            new AiLevel("smart", "🃏", "Level_Smart_Name", "Level_Smart_Tag", 3, 1483, () => new SmartPlayer()),
            new AiLevel(
                "expert",
                "🎓",
                "Level_Expert_Name",
                "Level_Expert_Tag",
                4,
                1664,
                ClaudePlayerProfiles.CreateExpert),
            new AiLevel(
                "claude",
                "👑",
                "Level_Claude_Name",
                "Level_Claude_Tag",
                5,
                1796,
                ClaudePlayerProfiles.CreateHeuristic),
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
