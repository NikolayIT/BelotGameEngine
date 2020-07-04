namespace Belot.Engine.Players
{
    using Belot.Engine.Cards;

    public class PlayCardAction
    {
        public PlayCardAction(Card card, bool announceBeloteIfAvailable = true)
        {
            this.Card = card;
            this.Belote = announceBeloteIfAvailable;
        }

        public Card Card { get; }

        public bool Belote { get; internal set; }

        public PlayerPosition Player { get; internal set; }

        public int TrickNumber { get; set; }
    }
}
