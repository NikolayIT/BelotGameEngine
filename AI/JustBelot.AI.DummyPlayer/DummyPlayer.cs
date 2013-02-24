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

        private PlayerPosition Position { get; set; }

        public void StartNewGame(PlayerPosition position)
        {
            this.Position = position;
        }

        public void StartNewDeal()
        {
            this.cards.Clear();
        }

        public void AddCard(Card card)
        {
            this.cards.Add(card);
        }

        public BidType AskForBid()
        {
            // Dummy player always says pass
            return BidType.Pass;
        }

        public IEnumerable<Declaration> AskForDeclarations()
        {
            // Dummy player never announce his declarations
            return new List<Declaration>();
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
