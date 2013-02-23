namespace JustBelot.AI.DummyPlayer
{
    using System.Collections.Generic;

    using JustBelot.Common;

    public class DummyPlayer : IPlayer
    {
        private readonly List<Card> cards = new List<Card>();

        public string Name
        {
            get { return "Dummy bot"; }
        }

        public GameManager Game { private get; set; }

        public PlayerPosition Position { private get; set; }

        public void AddCard(Card card)
        {
            this.cards.Add(card);
        }

        public AnnouncementType AskForAnnouncement()
        {
            return AnnouncementType.Pass;
        }

        public Card PlayCard()
        {
            // Since this is a dummy player he will randomly return one of the possible cards
            // TODO: Ask for the list of allowed cards
            var cardToPlay = this.cards[RandomProvider.Next(0, this.cards.Count)];
            this.cards.Remove(cardToPlay);
            return cardToPlay;
        }
    }
}
