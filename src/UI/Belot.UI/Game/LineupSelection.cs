namespace Belot.UI.Game
{
    using System;

    using Belot.Engine.Players;

    /// <summary>The home page's three independent selectors, using the existing persisted level ids.</summary>
    public static class LineupSelection
    {
        public static int SelectedIndex(PlayerPosition seat)
        {
            var id = seat switch
            {
                PlayerPosition.North => AppSettings.PartnerLevel,
                PlayerPosition.West => AppSettings.WestLevel,
                PlayerPosition.East => AppSettings.EastLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(seat)),
            };
            var level = AiLevels.ById(id);
            for (var i = 0; i < AiLevels.All.Count; i++)
            {
                if (AiLevels.All[i] == level)
                {
                    return i;
                }
            }

            return 2;
        }

        /// <summary>Ignores a picker's temporary empty selection while its localized items are refreshed.</summary>
        public static AiLevel? Select(PlayerPosition seat, int index)
        {
            if (index < 0 || index >= AiLevels.All.Count)
            {
                return null;
            }

            var level = AiLevels.All[index];
            switch (seat)
            {
                case PlayerPosition.North:
                    AppSettings.PartnerLevel = level.Id;
                    break;
                case PlayerPosition.West:
                    AppSettings.WestLevel = level.Id;
                    break;
                case PlayerPosition.East:
                    AppSettings.EastLevel = level.Id;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(seat));
            }

            return level;
        }
    }
}
