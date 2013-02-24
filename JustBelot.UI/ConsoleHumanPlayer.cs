namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly List<Card> cards = new List<Card>();

        public ConsoleHumanPlayer(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

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
            // TODO: Improve user interaction

            Console.Write("It's your turn! Please enter your contract (A, N, S, H, D, C, P, D1, D2): ");
            var contract = Console.ReadLine();
            switch (contract.ToUpper())
            {
                case "A": return BidType.AllTrumps;
                case "N": return BidType.NoTrumps;
                case "S": return BidType.Spades;
                case "H": return BidType.Hearts;
                case "D": return BidType.Diamonds;
                case "C": return BidType.Clubs;
                case "P": return BidType.Pass;
                default: return BidType.Pass;
            }
        }

        public IEnumerable<Declaration> AskForDeclarations()
        {
            Console.WriteLine("Which of your declarations will you announce (100, 50, T1, T2... bla bla TODO...) ?");
            throw new NotImplementedException();
        }

        public Card PlayCard()
        {
            Console.Write("It's your turn! Please select the card you want to play: ");
            throw new NotImplementedException();
        }
    }
}