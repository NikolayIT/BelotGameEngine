namespace JustBelot.AI.DummyPlayer
{
    using System.Collections.Generic;

    using JustBelot.Common;
    using JustBelot.Common.Extensions;

    public class DummyPlayer : IPlayer
    {
        private readonly Hand hand = new Hand();

        public DummyPlayer()
        {
            this.Name = "Dummy player";
        }

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
            return allowedBids.RandomElement();

            // Dummy player always says pass
            // return BidType.Pass;
        }

        public IEnumerable<CardsCombination> AskForCardsCombinations(IEnumerable<CardsCombination> allowedCombinations)
        {
            // Dummy player announces all allowed combinations
            return allowedCombinations;
        }

        public PlayAction PlayCard(IList<Card> allowedCards, IList<Card> currentTrickCards)
        {
            // Since this is a dummy player he will randomly return one of the allowed cards
            var cardToPlay = new List<Card>(allowedCards).RandomElement();
            this.hand.Remove(cardToPlay);
            return new PlayAction { Card = cardToPlay, AnnounceBeloteIfAvailable = true };
        }

        public void EndOfDeal(DealResult dealResult)
        {
        }
    }
}
