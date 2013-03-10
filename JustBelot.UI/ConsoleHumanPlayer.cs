namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using JustBelot.Common;
    using JustBelot.Common.Extensions;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly Hand hand;

        public ConsoleHumanPlayer(string name)
        {
            this.Name = name;
            this.hand = new Hand();
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
            this.Game.CardPlayed += this.GameOnCardPlayed;
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

                ConsoleHelper.WriteOnPosition(availableBidsAsString, 0, Settings.ConsoleHeight - 2);
                ConsoleHelper.WriteOnPosition("It's your turn! Please enter your bid: ", 0, Settings.ConsoleHeight - 3);

                BidType bid;

                var playerContract = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(playerContract))
                {
                    continue;
                }

                playerContract = playerContract.Trim();
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

        public IEnumerable<CardsCombination> AskForCardsCombinations(IEnumerable<CardsCombination> allowedCombinations)
        {
            var allowedCombinationsList = allowedCombinations.ToList();

            if (!allowedCombinationsList.Any())
            {
                ConsoleHelper.WriteOnPosition("No card combinations available.", 0, Settings.ConsoleHeight - 3);
                return allowedCombinationsList;
            }
            else
            {
                string availableCombinationsAsString;
                if (allowedCombinationsList.Count() == 1)
                {
                    availableCombinationsAsString =
                        string.Format(
                            "You have {0}. Press [enter] to announce it or press 0 and enter to skip it.",
                            allowedCombinationsList[0].CombinationType);
                }
                else
                {
                    availableCombinationsAsString =
                        string.Format("Press 0 to skip, 1 for {0} of {1} or 2 for {2} of {3}",
                        allowedCombinationsList[0].CombinationType, allowedCombinationsList[0].ToCardType,
                        allowedCombinationsList[1].CombinationType, allowedCombinationsList[1].ToCardType);
                }

                ConsoleHelper.WriteOnPosition(availableCombinationsAsString, 0, Settings.ConsoleHeight - 2);
                ConsoleHelper.WriteOnPosition("Choose which combinations you want to announce ([enter] for all): ", 0, Settings.ConsoleHeight - 3);
                
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return allowedCombinationsList;
                }
                else if (line.Trim() == "0")
                {
                    return new List<CardsCombination>();
                }
                else if (line.Trim() == "1" && allowedCombinationsList.Count >= 1)
                {
                    var list = new List<CardsCombination> { allowedCombinationsList[0] };
                    return list;

                }
                else if (line.Trim() == "2" && allowedCombinationsList.Count >= 2)
                {
                    var list = new List<CardsCombination> { allowedCombinationsList[1] };
                    return list;
                }
                else
                {
                    return allowedCombinationsList;
                }
            }
        }

        public PlayAction PlayCard(IList<Card> allowedCards, IList<Card> currentTrickCards)
        {
            var sb = new StringBuilder();
            var allowedCardsList = new CardsCollection(allowedCards);
            allowedCardsList.Sort(this.Contract.Type);
            for (int i = 0; i < allowedCardsList.Count; i++)
            {
                sb.AppendFormat("{0}({1}); ", i + 1, allowedCardsList[i]);
            }

            // this.Draw();
            while (true)
            {
                var action = new PlayAction();
                ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Settings.ConsoleHeight - 3);
                ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Settings.ConsoleHeight - 2);
                ConsoleHelper.WriteOnPosition(sb.ToString().Trim(), 0, Settings.ConsoleHeight - 2);
                ConsoleHelper.WriteOnPosition("It's your turn! Please select card to play: ", 0, Settings.ConsoleHeight - 3);
                var cardIndexAsString = Console.ReadLine();
                int cardIndex;

                if (int.TryParse(cardIndexAsString, out cardIndex))
                {
                    if (cardIndex > 0 && cardIndex <= allowedCardsList.Count)
                    {
                        var cardToPlay = allowedCardsList[cardIndex - 1];
                        action.Card = cardToPlay;

                        if (this.hand.IsBeloteAllowed(Contract, currentTrickCards, cardToPlay))
                        {
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Settings.ConsoleHeight - 3);
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Settings.ConsoleHeight - 2);
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Settings.ConsoleHeight - 1);
                            ConsoleHelper.WriteOnPosition("Y(es) / N(o)", 0, Settings.ConsoleHeight - 2);
                            ConsoleHelper.WriteOnPosition("You have belote! Do you want to announce it? Y/N ", 0, Settings.ConsoleHeight - 3);
                            var answer = Console.ReadLine();

                            if (!string.IsNullOrWhiteSpace(answer) && answer.Trim()[0] == 'N')
                            {
                                action.AnnounceBeloteIfAvailable = false;
                            }
                        }

                        this.hand.Remove(cardToPlay);
                        return action;
                    }
                }
            }
        }

        public void EndOfDeal(DealResult dealResult)
        {
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
                ConsoleHelper.DrawTextBoxTopLeft(bidEventArgs.Bid.ToString(), 40 - (bidEventArgs.Bid.ToString().Length / 2), 4);
            }

            if (bidEventArgs.Position == PlayerPosition.West)
            {
                ConsoleHelper.DrawTextBoxTopLeft(bidEventArgs.Bid.ToString(), this.Game[PlayerPosition.West].Name.Length + 3, 8);
            }

            ConsoleHelper.WriteOnPosition(string.Format("{0} from {1} ({2} player)", bidEventArgs.Bid, this.Game[bidEventArgs.Position].Name, bidEventArgs.Position), 0, Settings.ConsoleHeight - 3);
            ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, Settings.ConsoleHeight - 2);
            Console.ReadLine();
        }

        private void GameOnCardPlayed(CardPlayedEventArgs eventArgs)
        {
            this.Draw();

            if (eventArgs.Position == this.Position)
            {
                // Current player bid event
                return;
            }

            var position = eventArgs.Position;
            for (int i = eventArgs.CurrentTrickCards.Count() - 1; i >= 0; i--)
            {
                if (position == PlayerPosition.South)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(eventArgs.CurrentTrickCards.ToList()[i].ToString(), 40 - (eventArgs.CurrentTrickCards.ToList()[i].ToString().Length / 2), Settings.ConsoleHeight - 9);
                    if (position == eventArgs.Position && eventArgs.PlayAction.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 40 - ("Belote".Length / 2), Settings.ConsoleHeight - 10);
                    }
                }

                if (position == PlayerPosition.East)
                {
                    ConsoleHelper.DrawTextBoxTopRight(eventArgs.CurrentTrickCards.ToList()[i].ToString(), 80 - 2 - this.Game[PlayerPosition.East].Name.Length - 2, 8);
                    if (position == eventArgs.Position && eventArgs.PlayAction.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 80 - 2 - this.Game[PlayerPosition.East].Name.Length - 2 - eventArgs.CurrentTrickCards.ToList()[i].ToString().Length - 2, 8);
                    }
                }

                if (position == PlayerPosition.North)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(eventArgs.CurrentTrickCards.ToList()[i].ToString(), 40 - (eventArgs.CurrentTrickCards.ToList()[i].ToString().Length / 2), 4);
                    if (position == eventArgs.Position && eventArgs.PlayAction.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 40 - ("Belote".Length / 2), 7);
                    }
                }

                if (position == PlayerPosition.West)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(eventArgs.CurrentTrickCards.ToList()[i].ToString(), this.Game[PlayerPosition.West].Name.Length + 3, 8);
                    if (position == eventArgs.Position && eventArgs.PlayAction.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", this.Game[PlayerPosition.West].Name.Length + 3 + eventArgs.CurrentTrickCards.ToList()[i].ToString().Length + 2, 8);
                    }
                }

                position = position.PreviousPosition();
            }

            ConsoleHelper.WriteOnPosition(string.Format("Played {0} played {1}.", this.Game[eventArgs.Position].Name, eventArgs.PlayAction.Card), 0, Settings.ConsoleHeight - 3);
            ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, Settings.ConsoleHeight - 2);
            Console.ReadLine();
        }

        private void Draw()
        {
            // TODO: Refactor (extract constants, improve code)
            ConsoleHelper.ClearAndResetConsole();
            ConsoleHelper.DrawTextBoxTopRight(string.Format("(SN){0} - {1}(EW)", this.Game.SouthNorthScore, this.Game.EastWestScore), Console.WindowWidth - 1, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            var contractString = (this.Contract.IsAvailable ? this.Game[this.Contract.PlayerPosition].Name + ": " : string.Empty) + this.Contract.ToString();
            ConsoleHelper.DrawTextBoxTopLeft(contractString, 0, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            var dealNumberString = string.Format("Deal №{0}", this.Game.DealNumber);
            ConsoleHelper.WriteOnPosition(dealNumberString, 40 - (dealNumberString.Length / 2), 0, ConsoleColor.Gray);
            var firstPlayerString = string.Format("(First: {0})", this.Game[this.Deal.FirstPlayerPosition].Name);
            ConsoleHelper.WriteOnPosition(firstPlayerString, 40 - (firstPlayerString.Length / 2), 1, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.West].Name, 2, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.East].Name, 80 - 2 - this.Game[PlayerPosition.East].Name.Length, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.North].Name, 40 - (this.Game[PlayerPosition.North].Name.Length / 2), 3, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(this.Game[PlayerPosition.South].Name, 40 - (this.Game[PlayerPosition.South].Name.Length / 2), Settings.ConsoleHeight - 5, ConsoleColor.Black, ConsoleColor.Gray);

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

                ConsoleHelper.WriteOnPosition(cardAsString, left, Settings.ConsoleHeight - 6, color, ConsoleColor.White);
                left += cardAsString.Length;
            }
        }
    }
}