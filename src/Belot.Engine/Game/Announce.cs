namespace Belot.Engine.Game
{
    using Belot.Engine.Players;

    public class Announce
    {
        public Announce(PlayerPosition playerPosition, AnnounceType announceType)
        {
            this.PlayerPosition = playerPosition;
            this.AnnounceType = announceType;
        }

        public PlayerPosition PlayerPosition { get; set; }

        public AnnounceType AnnounceType { get; set; }
    }
}
