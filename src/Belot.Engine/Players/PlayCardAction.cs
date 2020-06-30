namespace Belot.Engine.Players
{
    using Belot.Engine.Cards;

    public class PlayCardAction
    {
        public PlayCardAction(Card card, bool belote = false)
        {
            this.Card = card;
            this.Belote = belote;
        }

        public Card Card { get; }

        public bool Belote { get; }

        public PlayerPosition Player { get; internal set; }

        public byte TrickNumber { get; internal set; }
    }
}
