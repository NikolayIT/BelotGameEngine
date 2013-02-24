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

        public ContractType AskForContract()
        {
            // TODO: Improve user interaction

            Console.Write("It's your turn! Please enter your contract (A, N, S, H, D, C, P, D1, D2): ");
            var contract = Console.ReadLine();
            switch (contract.ToUpper())
            {
                case "A": return ContractType.AllTrumps;
                case "N": return ContractType.NoTrump;
                case "S": return ContractType.Spades;
                case "H": return ContractType.Hearts;
                case "D": return ContractType.Diamonds;
                case "C": return ContractType.Clubs;
                case "P": return ContractType.Pass;
                default: return ContractType.Pass;
            }
        }

        public Card PlayCard()
        {
            Console.Write("It's your turn! Please select the card you want to play: ");
            throw new NotImplementedException();
        }
    }
}