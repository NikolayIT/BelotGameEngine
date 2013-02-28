namespace JustBelot.AI.DummyPlayer
{
    using System.Collections.Generic;

    using JustBelot.Common;
    using JustBelot.Common.Extensions;

    public class DummyPlayer : IPlayer
    {
        private readonly List<Card> cards = new List<Card>();

        public DummyPlayer(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        private GameInfo Game { get; set; }

        private PlayerPosition Position { get; set; }

        public void StartNewGame(GameInfo game, PlayerPosition position)
        {
            this.Position = position;
            this.Game = game;
        }

        public void StartNewDeal()
        {
            this.cards.Clear();
        }

        public void AddCard(Card card)
        {
            this.cards.Add(card);
        }

        public BidType AskForBid(Contract currentContract, IList<BidType> availableBids, IList<BidType> previousBids)
        {
            //return availableBids.RandomElement();

            // Dummy player always says pass
            return BidType.Pass;
        }

        public IEnumerable<Declaration> AskForDeclarations()
        {
            // Dummy player never announce his declarations
            return new List<Declaration>();
        }

        public PlayAction PlayCard()
        {
            // Since this is a dummy player he will randomly return one of the possible cards
            // TODO: Ask for the list of allowed cards
            var cardToPlay = this.cards[RandomProvider.Next(0, this.cards.Count)];
            this.cards.Remove(cardToPlay);
            var action = new PlayAction { Card = cardToPlay };
            return action;
        }
    }
}
