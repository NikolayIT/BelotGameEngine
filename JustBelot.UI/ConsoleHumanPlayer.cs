namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;

    public class ConsoleHumanPlayer : IPlayer
    {
        private readonly List<Card> cards = new List<Card>();

        private string playerName;

        public ConsoleHumanPlayer(string name)
        {
            this.playerName = name;
        }

        public string Name
        {
            get
            {
                return "Human player";
            }
        }

        public GameManager Game { private get; set; }

        public void AddCard(Card card)
        {
            this.cards.Add(card);
        }

        public AnnouncementType AskForAnnouncement()
        {
            Console.Write("It's your turn! Please enter your announcement(A, N, S, H, D, C, P): ");
            string announcement = Console.ReadLine();
            switch (announcement.ToUpper())
            {
                case "A": return AnnouncementType.AllTrumps;
                case "N": return AnnouncementType.NoTrumps;
                case "S": return AnnouncementType.Spades;
                case "H": return AnnouncementType.Hearts;
                case "D": return AnnouncementType.Diamonds;
                case "C": return AnnouncementType.Clubs;
                case "P": return AnnouncementType.Pass;
                default: return AnnouncementType.Pass;
            }
        }


        public Card PlayCard()
        {
            Console.Write("It's your turn! Please select the card you want to play: ");
            throw new NotImplementedException();
        }
    }
}