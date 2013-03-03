namespace JustBelot.Common
{
    public class CardPlayedEventArgs
    {
        public CardPlayedEventArgs(PlayerPosition position, PlayAction playAction)
        {
            this.Position = position;
            this.PlayAction = playAction;
        }

        public PlayerPosition Position { get; set; }

        public PlayAction PlayAction { get; set; }
    }
}
