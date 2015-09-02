namespace JustBelot.Common
{
    public struct PlayAction
    {
        public PlayAction(Card card, bool announceBeloteIfAvailable = true)
            : this()
        {
            this.Card = card;
            this.AnnounceBeloteIfAvailable = announceBeloteIfAvailable;
            this.Belote = false;
        }

        public Card Card { get; set; }

        public bool AnnounceBeloteIfAvailable { get; set; }

        public bool Belote { get; internal set; }
    }
}
