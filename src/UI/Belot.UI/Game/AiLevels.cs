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
        // Pair ratings (two of a level against two of another) from the `elo` simulator round
        // robin, anchored at Dummy = 1200. Re-run that suite and update these if the players change.
        public static IReadOnlyList<AiLevel> All { get; } = new[]
        {
            new AiLevel("random", "🎲", "Level_Random_Name", "Level_Random_Tag", 1, 685, () => new RandomPlayer()),
            new AiLevel("dummy", "🙂", "Level_Dummy_Name", "Level_Dummy_Tag", 2, 1200, () => new DummyPlayer()),
            new AiLevel("smart", "🃏", "Level_Smart_Name", "Level_Smart_Tag", 3, 1555, () => new SmartPlayer()),
            new AiLevel("claude", "👑", "Level_Claude_Name", "Level_Claude_Tag", 4, 1930, () => new ClaudePlayerIsmcts()),
        };

        /// <summary>The level with this id, or the Smart one for an unknown id.</summary>
        public static AiLevel ById(string? id) => Find(id) ?? All[2];

        /// <summary>The level with this id, or null for an unknown one.</summary>
        public static AiLevel? Find(string? id) =>
            All.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
