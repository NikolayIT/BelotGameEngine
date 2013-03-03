namespace JustBelot.Common
{
    using System.Collections.Generic;

    public class CardPlayedEventArgs
    {
        public CardPlayedEventArgs(PlayerPosition position, PlayAction playAction, IEnumerable<Card> currentTrickCards)
        {
            this.Position = position;
            this.PlayAction = playAction;
            this.CurrentTrickCards = currentTrickCards;
        }

        public PlayerPosition Position { get; set; }

        public PlayAction PlayAction { get; set; }

        public IEnumerable<Card> CurrentTrickCards { get; set; }
    }
}
