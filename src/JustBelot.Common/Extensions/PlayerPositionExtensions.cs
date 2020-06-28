namespace JustBelot.Common.Extensions
{
    using System;

    public static class PlayerPositionExtensions
    {
        public static PlayerPosition PreviousPosition(this PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.South:
                    return PlayerPosition.West;
                case PlayerPosition.East:
                    return PlayerPosition.South;
                case PlayerPosition.North:
                    return PlayerPosition.East;
                case PlayerPosition.West:
                    return PlayerPosition.North;
                default:
                    throw new ArgumentOutOfRangeException("position");
            }
        }

        public static PlayerPosition NextPosition(this PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.South:
                    return PlayerPosition.East;
                case PlayerPosition.East:
                    return PlayerPosition.North;
                case PlayerPosition.North:
                    return PlayerPosition.West;
                case PlayerPosition.West:
                    return PlayerPosition.South;
                default:
                    throw new ArgumentOutOfRangeException("position");
            }
        }
    }
}
