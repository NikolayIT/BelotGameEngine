namespace Belot.UI.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly ValidAnnouncesService announcesService;

        private readonly List<Bid> visualizedBids = new List<Bid>();
        private readonly List<PlayCardAction> visualizedActions = new List<PlayCardAction>();
        private int lastRoundNumber;
        private int lastTrickNumber;

        public ConsoleHumanPlayer()
        {
            this.announcesService = new ValidAnnouncesService();
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.NewRoundCheck(context);
            this.DrawLastBids(context);
            while (true)
            {
                this.Draw(context);

                var availableBidsList = new List<string> { "P(ass)" };
                if (context.AvailableBids.HasFlag(BidType.Clubs))
                {
                    availableBidsList.Add("C(♣)");
                }

                if (context.AvailableBids.HasFlag(BidType.Diamonds))
                {
                    availableBidsList.Add("D(♦)");
                }

                if (context.AvailableBids.HasFlag(BidType.Hearts))
                {
                    availableBidsList.Add("H(♥)");
                }

                if (context.AvailableBids.HasFlag(BidType.Spades))
                {
                    availableBidsList.Add("S(♠)");
                }

                if (context.AvailableBids.HasFlag(BidType.NoTrumps))
                {
                    availableBidsList.Add("N(o)");
                }

                if (context.AvailableBids.HasFlag(BidType.AllTrumps))
                {
                    availableBidsList.Add("A(ll)");
                }

                if (context.AvailableBids.HasFlag(BidType.Double))
                {
                    availableBidsList.Add("2(double)");
                }

                if (context.AvailableBids.HasFlag(BidType.ReDouble))
                {
                    availableBidsList.Add("4(re double)");
                }

                var availableBidsAsString = string.Join(", ", availableBidsList);
                ConsoleHelper.WriteOnPosition(availableBidsAsString, 0, Console.WindowHeight - 2);
                ConsoleHelper.WriteOnPosition("It's your turn! Please enter your bid: ", 0, Console.WindowHeight - 3);

                var playerContract = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(playerContract))
                {
                    continue;
                }

                playerContract = playerContract.Trim();
                BidType bid;
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

                if (context.AvailableBids.HasFlag(bid))
                {
                    return bid;
                }
            }
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            this.NewRoundCheck(context);
            this.DrawLastBids(context);
            this.DrawLastPlayedCards(context, context.CurrentTrickActions);
            var availableAnnounces = context.AvailableAnnounces.ToList();
            if (!availableAnnounces.Any())
            {
                ConsoleHelper.WriteOnPosition("No card combinations available.", 0, Console.WindowHeight - 3);
                return availableAnnounces;
            }

            var availableCombinationsAsString = availableAnnounces.Count == 1
                                                    ? $"You have {availableAnnounces[0]}. Press [enter] to announce it or press 0 and enter to skip it."
                                                    : $"Press 0 to skip, 1 for {availableAnnounces[0]}, 2 for {availableAnnounces[1]} or [enter] for all";

            ConsoleHelper.WriteOnPosition(availableCombinationsAsString, 0, Console.WindowHeight - 2);
            ConsoleHelper.WriteOnPosition("Choose which combinations you want to announce ([enter] for all): ", 0, Console.WindowHeight - 3);

            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return availableAnnounces;
            }

            if (line.Trim() == "0")
            {
                return new List<Announce>();
            }

            if (line.Trim() == "1" && availableAnnounces.Count >= 1)
            {
                return new List<Announce> { availableAnnounces[0] };
            }

            if (line.Trim() == "2" && availableAnnounces.Count >= 2)
            {
                return new List<Announce> { availableAnnounces[1] };
            }

            return availableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.NewRoundCheck(context);
            this.DrawLastBids(context);
            this.DrawLastPlayedCards(context, context.RoundActions);

            var sb = new StringBuilder();
            var allowedCardsList = new List<Card>(context.AvailableCardsToPlay);
            for (var i = 0; i < allowedCardsList.Count; i++)
            {
                sb.AppendFormat("{0}({1}); ", i + 1, allowedCardsList[i]);
            }

            while (true)
            {
                ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Console.WindowHeight - 3);
                ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Console.WindowHeight - 2);
                ConsoleHelper.WriteOnPosition(sb.ToString().Trim(), 0, Console.WindowHeight - 2);
                ConsoleHelper.WriteOnPosition("It's your turn! Please select card to play: ", 0, Console.WindowHeight - 3);
                var cardIndexAsString = Console.ReadLine();
                if (int.TryParse(cardIndexAsString, out var cardIndex))
                {
                    if (cardIndex > 0 && cardIndex <= allowedCardsList.Count)
                    {
                        var cardToPlay = allowedCardsList[cardIndex - 1];
                        var announceBelote = false;
                        if (this.announcesService.IsBeloteAllowed(context.MyCards, context.CurrentContract.Type, context.CurrentTrickActions.ToList(), cardToPlay))
                        {
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Console.WindowHeight - 3);
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Console.WindowHeight - 2);
                            ConsoleHelper.WriteOnPosition(new string(' ', 78), 0, Console.WindowHeight - 1);
                            ConsoleHelper.WriteOnPosition("Y(es) / N(o)", 0, Console.WindowHeight - 2);
                            ConsoleHelper.WriteOnPosition("You have belote! Do you want to announce it? Y/N ", 0, Console.WindowHeight - 3);
                            var answer = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(answer) && answer.Trim()[0] != 'N')
                            {
                                announceBelote = true;
                            }
                        }

                        return new PlayCardAction(cardToPlay, announceBelote);
                    }
                }
            }
        }

        private void NewRoundCheck(BasePlayerContext context)
        {
            if (context.RoundNumber != this.lastRoundNumber)
            {
                this.lastRoundNumber = context.RoundNumber;
                this.lastTrickNumber = 0;
                this.visualizedActions.Clear();
                this.visualizedBids.Clear();
                this.Draw(context);
            }
        }

        private void Draw(BasePlayerContext context, bool drawPlayerCards = true)
        {
            ConsoleHelper.ClearAndResetConsole();
            ConsoleHelper.DrawTextBoxTopRight(
                $"(SN){context.SouthNorthPoints} - {context.EastWestPoints}(EW)",
                Console.WindowWidth - 1,
                0,
                ConsoleColor.Black,
                ConsoleColor.DarkGray);
            var contractString =
                (context.CurrentContract.Type != BidType.Pass ? $"{context.CurrentContract.Player}: " : string.Empty)
                + context.CurrentContract.Type;
            ConsoleHelper.DrawTextBoxTopLeft(contractString, 0, 0, ConsoleColor.Black, ConsoleColor.DarkGray);
            var dealNumberString = $"Round №{context.RoundNumber}";
            ConsoleHelper.WriteOnPosition(dealNumberString, 40 - (dealNumberString.Length / 2), 0, ConsoleColor.Gray);
            var firstPlayerString = $"(First: {context.FirstToPlayInTheRound})";
            ConsoleHelper.WriteOnPosition(firstPlayerString, 40 - (firstPlayerString.Length / 2), 1, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(PlayerPosition.West.ToString(), 2, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(PlayerPosition.East.ToString(), 80 - 2 - PlayerPosition.East.ToString().Length, 9, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(PlayerPosition.North.ToString(), 40 - (PlayerPosition.North.ToString().Length / 2), 3, ConsoleColor.Black, ConsoleColor.Gray);
            ConsoleHelper.WriteOnPosition(PlayerPosition.South.ToString(), 40 - (PlayerPosition.South.ToString().Length / 2), Console.WindowHeight - 5, ConsoleColor.Black, ConsoleColor.Gray);

            if (drawPlayerCards)
            {
                var left = 40 - (string.Join(string.Empty, context.MyCards).Length / 2);
                foreach (var card in context.MyCards)
                {
                    var cardAsString = card.ToString();
                    ConsoleColor color;
                    if (card.Suit == CardSuit.Diamond || card.Suit == CardSuit.Heart)
                    {
                        color = ConsoleColor.Red;
                    }
                    else
                    {
                        color = ConsoleColor.Black;
                    }

                    ConsoleHelper.WriteOnPosition(
                        cardAsString,
                        left,
                        Console.WindowHeight - 6,
                        color,
                        ConsoleColor.White);
                    left += cardAsString.Length;
                }
            }
        }

        private void DrawLastBids(BasePlayerContext context)
        {
            foreach (var bid in context.Bids)
            {
                if (this.visualizedBids.Contains(bid))
                {
                    continue;
                }

                this.visualizedBids.Add(bid);
                this.Draw(context, false);
                if (bid.Player == PlayerPosition.South)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(bid.Type.ToString(), 40 - (bid.Type.ToString().Length / 2), 12);
                }

                if (bid.Player == PlayerPosition.East)
                {
                    ConsoleHelper.DrawTextBoxTopRight(
                        bid.Type.ToString(),
                        80 - 2 - PlayerPosition.East.ToString().Length - 2,
                        8);
                }

                if (bid.Player == PlayerPosition.North)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(bid.Type.ToString(), 40 - (bid.Type.ToString().Length / 2), 4);
                }

                if (bid.Player == PlayerPosition.West)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(bid.Type.ToString(), PlayerPosition.West.ToString().Length + 3, 8);
                }

                ConsoleHelper.WriteOnPosition(
                    $"{bid.Type} from {bid.Player} player        ",
                    0,
                    Console.WindowHeight - 3);
                ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, Console.WindowHeight - 2);
                Console.ReadLine();
            }
        }

        private void DrawLastPlayedCards(BasePlayerContext context, IEnumerable<PlayCardAction> allActions)
        {
            foreach (var action in allActions)
            {
                if (this.visualizedActions.Any(x => x.Card == action.Card))
                {
                    continue;
                }

                this.visualizedActions.Add(action);
                if (action.TrickNumber != this.lastTrickNumber)
                {
                    this.Draw(context);
                    this.lastTrickNumber = action.TrickNumber;
                }

                if (action.Player == PlayerPosition.South)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(action.Card.ToString(), 40 - (action.Card.ToString().Length / 2), Console.WindowHeight - 9);
                    if (action.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 40 - ("Belote".Length / 2), Console.WindowHeight - 10);
                    }
                }

                if (action.Player == PlayerPosition.East)
                {
                    ConsoleHelper.DrawTextBoxTopRight(action.Card.ToString(), 80 - 2 - PlayerPosition.East.ToString().Length - 2, 8);
                    if (action.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 80 - 2 - PlayerPosition.East.ToString().Length - 2 - action.Card.ToString().Length - 2, 8);
                    }
                }

                if (action.Player == PlayerPosition.North)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(action.Card.ToString(), 40 - (action.Card.ToString().Length / 2), 4);
                    if (action.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", 40 - ("Belote".Length / 2), 7);
                    }
                }

                if (action.Player == PlayerPosition.West)
                {
                    ConsoleHelper.DrawTextBoxTopLeft(action.Card.ToString(), PlayerPosition.West.ToString().Length + 3, 8);
                    if (action.Belote)
                    {
                        ConsoleHelper.DrawTextBoxTopLeft("Belote", PlayerPosition.West.ToString().Length + 3 + action.Card.ToString().Length + 2, 8);
                    }
                }

                ConsoleHelper.WriteOnPosition(
                    $"Player {action.Player} played {action.Card}{(action.Belote ? " (with belote)" : string.Empty)}.",
                    0,
                    Console.WindowHeight - 3);
                ConsoleHelper.WriteOnPosition("Press enter to continue...", 0, Console.WindowHeight - 2);
                Console.ReadLine();
            }
        }
    }
}
