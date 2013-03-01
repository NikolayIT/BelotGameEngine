namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using JustBelot.Common;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly CardsCollection hand;

        public ConsoleHumanPlayer(string name)
        {
            this.Name = name;
            this.hand = new CardsCollection();
        }

        public string Name { get; private set; }

        private GameInfo Game { get; set; }

        private DealInfo Deal { get; set; }

        private PlayerPosition Position { get; set; }

        private Contract Contract { get; set; }

        public void StartNewGame(GameInfo gameInfo, PlayerPosition position)
        {
            Console.Clear();
            this.Position = position;
            this.Game = gameInfo;
            this.Game.PlayerBid += this.GameOnPlayerBid;
        }

        public void StartNewDeal(DealInfo dealInfo)
        {
            this.hand.Clear();
            this.Contract = new Contract();
            this.Deal = dealInfo;
            this.Draw();
        }

        public void AddCards(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                this.hand.Add(card);
            }

            this.Draw();
        }

        public BidType AskForBid(Contract currentContract, IList<BidType> allowedBids, IList<BidType> previousBids)
        {
            this.Contract = currentContract;
            while (true)
            {
                this.Draw();

                var availableBidsAsString = AvailableBidsAsString(allowedBids);

                ConsoleHelper.WriteOnPosition(availableBidsAsString, 0, 19);
                ConsoleHelper.WriteOnPosition("It's your turn! Please enter your bid: ", 0, 18);

                BidType bid;

                var playerContract = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(playerContract))
                {
                    continue;
                }

                switch (char.ToUpper(playerContract[0]))
                {
                    case 'A':
                        bid = BidType.AllTrumps;
                        break;
                    case 'N':
                        bid = BidType.NoTrumps;
                        break;
                    case 'S':
                        bid = BidType.Spades;
                        break;
                    case 'H':
                        bid = BidType.Hearts;
                        break;
                    case 'D':
                        bid = BidType.Diamonds;
                        break;
                    case 'C':
                        bid = BidType.Clubs;
                        break;
                    case 'P':
                        bid = BidType.Pass;
                        break;
                    case '2':
                        bid = BidType.Double;
                        break;
                    case '4':
                        bid = BidType.ReDouble;
                        break;
                    default:
                        continue;
                }

                if (allowedBids.Contains(bid))
                {
                    return bid;
                }
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
            for (int i = 0; i < this.hand.Count; i++)
            {
                sb.AppendFormat("{0}({1}); ", i + 1, this.hand[i]);
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
                    if (cardIndex > 0 && cardIndex <= this.hand.Count)
                    {
                        // TODO: Ask player if he wants to announce belote
                        var cardToPlay = this.hand[cardIndex - 1];
                        this.hand.RemoveAt(cardIndex - 1);
                        return new PlayAction(cardToPlay);
                    }
                }
            }
        }

        private static string AvailableBidsAsString(IEnumerable<BidType> availableBids)
        {
            var availableBidsAsString = new StringBuilder();
            foreach (var availableBid in availableBids)
            {
                switch (availableBid)
                {
                    case BidType.Pass:
                        availableBidsAsString.Append("P(ass)");
                        break;
                    case BidType.Clubs:
                        availableBidsAsString.Append("C(♣)");
                        break;
                    case BidType.Diamonds:
                        availableBidsAsString.Append("D(♦)");
                        break;
                    case BidType.Hearts:
                        availableBidsAsString.Append("H(♥)");
                        break;
                    case BidType.Spades:
                        availableBidsAsString.Append("S(♠)");
                        break;
                    case BidType.NoTrumps:
                        availableBidsAsString.Append("N(o)");
                        break;
                    case BidType.AllTrumps:
                        availableBidsAsString.Append("A(ll)");
                        break;
                    case BidType.Double:
                        availableBidsAsString.Append("2(double)");
                        break;
                    case BidType.ReDouble:
                        availableBidsAsString.Append("4(re double)");
                        break;
                }

                availableBidsAsString.Append(", ");
            }

            return availableBidsAsString.ToString().Trim(' ', ',');
        }

        private void GameOnPlayerBid(BidEventArgs bidEventArgs)
        {
            this.Contract = bidEventArgs.CurrentContract;
            this.Draw();

            if (bidEventArgs.Position == this.Position)
            {
                // Current player bid event
                return;
            }

            if (bidEventArgs.Position == PlayerPosition.East)
            {
                ConsoleHelper.DrawTextBoxTopRight(bidEventArgs.Bid.ToString(), 80 - 2 - this.Game[PlayerPosition.East].Name.Length - 2, 8);
            }

            if (bidEventArgs.Position == PlayerPosition.North)
            {
                ConsoleHelper.DrawTextBoxTopLeft(bidEventArgs.Bid.ToString(), 40 - 1 - (bidEventArgs.Bid.ToString().Length / 2), 4);
            }

            if (bidEventArgs.Position == PlayerPosition.West)
            {
                ConsoleHelper.DrawTextBoxTopLeft(bidEventArgs.Bid.ToString(), this.Game[PlayerPosition.West].Name.Length + 3, 8);
            }

            ConsoleHelper.WriteOnPosition(string.Format("{0} from {1} ({2} player)", bidEventArgs.Bid, this.Game[bidEventArgs.Position].Name, bidEventArgs.Position), 0, 18);
            ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, 19);
            Console.ReadLine();
        }

        private void Draw()
        {
            // TODO: Refactor (extract constants, improve code)
            ConsoleHelper.ClearAndResetConsole();
            ConsoleHelper.DrawTextBoxTopRight(string.Format("{0} - {1}", this.Game.SouthNorthScore, this.Game.EastWestScore), Console.WindowWidth - 1, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            string contractString = (this.Contract.IsAvailable ? this.Game[this.Contract.PlayerPosition].Name + ": " : string.Empty) + this.Contract.ToString();
            ConsoleHelper.DrawTextBoxTopLeft(contractString, 0, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            string dealNumberString = string.Format("Deal №{0}", this.Game.DealNumber);
            ConsoleHelper.WriteOnPosition(dealNumberString, 40 - (dealNumberString.Length / 2), 0, ConsoleColor.Gray);
            string firstPlayerString = string.Format("(First: {0})", this.Game[this.Deal.FirstPlayerPosition].Name);
            ConsoleHelper.WriteOnPosition(firstPlayerString, 40 - (firstPlayerString.Length / 2), 1, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.West].Name, 2, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.East].Name, 80 - 2 - this.Game[PlayerPosition.East].Name.Length, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.North].Name, 40 - (this.Game[PlayerPosition.North].Name.Length / 2), 3, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.South].Name, 40 - (this.Game[PlayerPosition.South].Name.Length / 2), 16, ConsoleColor.Black, ConsoleColor.Gray);

            int left = 40 - (this.hand.ToString().Replace(" ", string.Empty).Length / 2);
            this.hand.Sort(ContractType.AllTrumps);
            foreach (var card in this.hand)
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

                ConsoleHelper.WriteOnPosition(cardAsString, left, 15, color, ConsoleColor.White);
                left += cardAsString.Length;
            }
        }
    }
}