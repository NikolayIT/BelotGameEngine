namespace JustBelot.AI.DummyPlayer
{
    using System.Collections.Generic;

    using JustBelot.Common;
    using JustBelot.Common.Extensions;

    public class DummyPlayer : IPlayer
    {
        private readonly CardsCollection hand = new CardsCollection();

        public DummyPlayer(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        private GameInfo Game { get; set; }

        private PlayerPosition Position { get; set; }

        public void StartNewGame(GameInfo gameInfo, PlayerPosition position)
        {
            this.Position = position;
            this.Game = gameInfo;
        }

        public void StartNewDeal(DealInfo dealInfo)
        {
            this.hand.Clear();
        }

        public void AddCards(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                this.hand.Add(card);
            }
        }

        public BidType AskForBid(Contract currentContract, IList<BidType> allowedBids, IList<BidType> previousBids)
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
            var cardToPlay = this.hand[RandomProvider.Next(0, this.hand.Count)];
            this.hand.Remove(cardToPlay);
            var action = new PlayAction { Card = cardToPlay };
            return action;
        }
    }
}
