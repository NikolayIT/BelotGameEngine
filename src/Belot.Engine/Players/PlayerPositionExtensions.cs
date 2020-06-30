namespace Belot.Engine.Players
{
    using System;
    using System.Runtime.CompilerServices;

    public static class PlayerPositionExtensions
    {
        // TODO: Test [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlayerPosition Next(this PlayerPosition playerPosition)
        {
            return playerPosition switch
                {
                    PlayerPosition.South => PlayerPosition.East,
                    PlayerPosition.East => PlayerPosition.North,
                    PlayerPosition.North => PlayerPosition.West,
                    PlayerPosition.West => PlayerPosition.South,
                    _ => throw new ArgumentException("Invalid playerPosition.", nameof(playerPosition)),
                };
        }

        public static int Index(this PlayerPosition playerPosition)
        {
            return playerPosition switch
                {
                    PlayerPosition.South => 0,
                    PlayerPosition.East => 1,
                    PlayerPosition.North => 2,
                    PlayerPosition.West => 3,
                    _ => throw new ArgumentException("Invalid playerPosition.", nameof(playerPosition)),
                };
        }

        public static bool IsInSameTeamWith(this PlayerPosition position, PlayerPosition otherPlayerPosition)
        {
            switch (position)
            {
                case PlayerPosition.South when otherPlayerPosition == PlayerPosition.North:
                case PlayerPosition.North when otherPlayerPosition == PlayerPosition.South:
                case PlayerPosition.East when otherPlayerPosition == PlayerPosition.West:
                case PlayerPosition.West when otherPlayerPosition == PlayerPosition.East:
                    return true;
                default:
                    return false;
            }
        }
    }
}
