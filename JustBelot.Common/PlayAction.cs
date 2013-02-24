namespace JustBelot.Common
{
    public struct PlayAction
    {
        public PlayAction(Card card, bool belote = false)
            : this()
        {
            this.Card = card;
            this.Belote = belote;
        }

        public Card Card { get; set; }

        public bool Belote { get; set; }
    }
}
