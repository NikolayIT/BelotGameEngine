namespace Belot.Engine
{
    using Belot.Engine.Players;

    /*  N
     * W E
     *  S
     */
    public class BelotGame : IBelotGame
    {
        private readonly IPlayer southPlayer;

        private readonly IPlayer northPlayer;

        private readonly IPlayer eastPlayer;

        private readonly IPlayer westPlayer;

        public BelotGame(IPlayer southPlayer, IPlayer northPlayer, IPlayer eastPlayer, IPlayer westPlayer)
        {
            this.southPlayer = southPlayer;
            this.northPlayer = northPlayer;
            this.eastPlayer = eastPlayer;
            this.westPlayer = westPlayer;
        }

        public void StartNew()
        {
        }
    }
}
