namespace JustBelot.Common
{
    public struct PlayAction
    {
        public PlayAction(Card card, bool announceBeloteIfAvailable = true)
            : this()
        {
            this.Card = card;
            this.AnnounceBeloteIfAvailable = announceBeloteIfAvailable;
        }

        public Card Card { get; set; }

        public bool AnnounceBeloteIfAvailable { get; set; }
    }
}
