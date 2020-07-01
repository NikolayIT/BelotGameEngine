namespace Belot.Engine.Players
{
    using System.Runtime.CompilerServices;

    public static class PlayerPositionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlayerPosition Next(this PlayerPosition playerPosition)
        {
            return playerPosition == PlayerPosition.South ? PlayerPosition.East :
                   playerPosition == PlayerPosition.East ? PlayerPosition.North :
                   playerPosition == PlayerPosition.North ? PlayerPosition.West : PlayerPosition.South;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(this PlayerPosition playerPosition)
        {
            return playerPosition == PlayerPosition.East ? 1 :
                   playerPosition == PlayerPosition.North ? 2 :
                   playerPosition == PlayerPosition.West ? 3 : 0;
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
