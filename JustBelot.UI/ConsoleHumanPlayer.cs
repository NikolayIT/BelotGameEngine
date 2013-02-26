namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;
    using System.Text;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly Hand cards;

        public ConsoleHumanPlayer(string name)
        {
            this.Name = name;
            this.cards = new Hand();
        }

        public string Name { get; private set; }

        private GameInfo Game { get; set; }

        private PlayerPosition Position { get; set; }

        public void StartNewGame(GameInfo game, PlayerPosition position)
        {
            Console.Clear();
            this.Position = position;
            this.Game = game;
            this.Game.PlayerBid += this.Game_PlayerBid;
        }

        private void Game_PlayerBid(BidEventArgs e)
        {
            this.Draw();
            if (e.Position != this.Position)
            {
                if (e.Position == PlayerPosition.East)
                {
                    ConsoleHelper.DrawTextBoxTopRight(e.Bid.ToString(), 80 - 2 - this.Game[PlayerPosition.East].Name.Length - 2, 7);
                }

                if (e.Position == PlayerPosition.North)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(e.Bid.ToString(), 40 - 2 - e.Bid.ToString().Length / 2, 2);
                }

                if (e.Position == PlayerPosition.West)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(e.Bid.ToString(), this.Game[PlayerPosition.West].Name.Length + 3, 7);
                }

                ConsoleHelper.WriteOnPosition(string.Format("{0} from {1} player", e.Bid, e.Position), 0, 18);
                ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, 19);
                Console.ReadLine();
            }
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
            this.Draw();
            ConsoleHelper.WriteOnPosition("P(ass), A(ll), N(o), S(♠), H(♥), D(♦), C(♣), 2(double), 4(re double)", 0, 19);
            ConsoleHelper.WriteOnPosition("It's your turn! Please enter your bid: ", 0, 18);
            var contract = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(contract))
            {
                return BidType.Pass;
            }

            switch (char.ToUpper(contract[0]))
            {
                case 'A': return BidType.AllTrumps;
                case 'N': return BidType.NoTrumps;
                case 'S': return BidType.Spades;
                case 'H': return BidType.Hearts;
                case 'D': return BidType.Diamonds;
                case 'C': return BidType.Clubs;
                case 'P': return BidType.Pass;
                case '2': return BidType.Double;
                case '4': return BidType.ReDouble;
                default: return BidType.Pass;
            }
        }

        public IEnumerable<Declaration> AskForDeclarations()
        {
            // TODO: Find declarations and ask user for them
            ConsoleHelper.WriteOnPosition("No declarations available.", 0, 18);
            return new List<Declaration>();
        }

        public PlayAction PlayCard()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cards.Count; i++)
            {
                sb.AppendFormat("{0}({1}); ", i + 1, this.cards[i]);
            }

            while (true)
            {
                this.Draw();
                ConsoleHelper.WriteOnPosition(sb.ToString().Trim(), 0, 19);
                ConsoleHelper.WriteOnPosition("It's your turn! Please select card to play: ", 0, 18);
                var cardIndexAsString = Console.ReadLine();
                int cardIndex;
                if (int.TryParse(cardIndexAsString, out cardIndex))
                {
                    if (cardIndex > 0 && cardIndex <= this.cards.Count)
                    {
                        // TODO: Ask player if he wants to announce belote
                        var cardToPlay = this.cards[cardIndex - 1];
                        this.cards.RemoveAt(cardIndex - 1);
                        return new PlayAction(cardToPlay);
                    }
                }
            }
        }

        private void Draw()
        {
            ConsoleHelper.ClearAndResetConsole();
            ConsoleHelper.DrawTextBoxTopLeft(Settings.ProgramName, 0, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            ConsoleHelper.DrawTextBoxTopRight(string.Format("{0} - {1}", this.Game.SouthNorthScore, this.Game.EastWestScore), Console.WindowWidth - 1, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.West].Name, 2, 8, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.East].Name, 80 - 2 - this.Game[PlayerPosition.East].Name.Length, 8, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.North].Name, 40 - 1 - (this.Game[PlayerPosition.North].Name.Length / 2), 1, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.South].Name, 40 - 1 - (this.Game[PlayerPosition.South].Name.Length / 2), 17, ConsoleColor.Black, ConsoleColor.Gray);

            int left = 40 - 1 - (this.cards.ToString().Replace(" ", string.Empty).Length / 2);
            this.cards.Sort(ContractType.AllTrumps);
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

                ConsoleHelper.WriteOnPosition(cardAsString, left, 16, color, ConsoleColor.White);
                left += cardAsString.Length;
            }
        }
    }
}