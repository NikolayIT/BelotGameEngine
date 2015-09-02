namespace JustBelot.Common
{
    using System;

    public class InvalidPlayerActionException : Exception
    {
        public InvalidPlayerActionException(IPlayer player, string message)
            : base(message)
        {
            this.Player = player;
        }

        public InvalidPlayerActionException(IPlayer player, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Player = player;
        }

        public IPlayer Player { get; private set; }
    }
}
