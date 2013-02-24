namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly Hand cards;

        public ConsoleHumanPlayer(string name)
        {
            this.Name = name;
            this.cards = new Hand();
        }

        public string Name { get; private set; }

        public GameInfo Game { private get; set; }

        private PlayerPosition Position { get; set; }

        public void StartNewGame(PlayerPosition position)
        {
            Console.Clear();
            this.Position = position;
        }

        public void StartNewDeal()
        {
            this.cards.Clear();
            this.Draw();
        }

        public void AddCard(Card card)
        {
            this.cards.Add(card);
            this.Draw();
        }

        public BidType AskForBid()
        {
            // TODO: Improve user interaction

            //Console.Write("It's your turn! Please enter your contract (A, N, S, H, D, C, P, D1, D2): ");
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
            // Console.WriteLine("Which of your declarations will you announce (100, 50, T1, T2... bla bla TODO...) ?");
            // throw new NotImplementedException();
            return new List<Declaration>();
        }

        public PlayAction PlayCard()
        {
            // Console.Write("It's your turn! Please select the card you want to play: ");
            // throw new NotImplementedException();
            return new PlayAction();
        }

        private void Draw()
        {
            ConsoleHelper.ClearAndResetConsole();
            ConsoleHelper.DrawTextBoxTopLeft(Settings.ProgramName, 0, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            ConsoleHelper.DrawTextBoxTopRight(string.Format("{0} - {1}", this.Game.SouthNorthScore, this.Game.EastWestScore), Console.WindowWidth - 1, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.West].Name, 2, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.East].Name, 80 - 2 - this.Game[PlayerPosition.East].Name.Length, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.North].Name, 40 - 1 - (this.Game[PlayerPosition.North].Name.Length / 2), 1, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.South].Name, 40 - 1 - (this.Game[PlayerPosition.South].Name.Length / 2), 18, ConsoleColor.Black, ConsoleColor.Gray);

            int left = 40 - 1 - (this.cards.ToString().Replace(" ", string.Empty).Length / 2);
            foreach (var card in this.cards)
            {
                var cardAsString = card.ToString();
                ConsoleColor color;
                if (card.Suit == CardSuit.Diamonds || card.Suit == CardSuit.Hearts)
                {
                    color = ConsoleColor.Red;
                }
                else
                {
                    color = ConsoleColor.Black;
                }

                ConsoleHelper.WriteOnPosition(cardAsString, left, 17, color, ConsoleColor.White);
                left += cardAsString.Length;
            }
        }
    }
}