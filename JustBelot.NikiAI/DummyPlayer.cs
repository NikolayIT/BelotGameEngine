namespace JustBelot.NikiAI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;

    public class DummyPlayer : IPlayer
    {
        private readonly List<Card> cards = new List<Card>();

        public string Name
        {
            get { return "Niki dummy bot"; }
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
            throw new NotImplementedException();
        }
    }
}
