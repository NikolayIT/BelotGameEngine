namespace JustBelot.Common
{
    public struct PlayerInfo
    {
        public PlayerInfo(IPlayer player)
            : this()
        {
            this.Name = player.Name;
        }

        public string Name { get; set; }
    }
}
