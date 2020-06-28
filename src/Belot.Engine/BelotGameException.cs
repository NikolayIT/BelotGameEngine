namespace Belot.Engine
{
    using System;

    public class BelotGameException : Exception
    {
        public BelotGameException(string message)
            : base(message)
        {
        }
    }
}
