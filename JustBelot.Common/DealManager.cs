using System.Collections.Generic;
namespace JustBelot.Common
{
    /// <summary>
    /// Responsible for one deal
    /// </summary>
    internal class DealManager
    {
        private readonly GameManager game;

        private int playerNumber = 0;

        private List<Card> cardDeck;

        public DealManager(GameManager game, int dealNumber)
        {
            this.game = game;
            this.playerNumber = dealNumber % 4;
        }

        public void StartNewDeal()
        {


            // TODO: Deal 5 cards
            // TODO: Ask for announcements and check if announcements are correct
            int passesLeft = 4;
            while (passesLeft > 0)
            {
                var announcement = this.game[this.playerNumber % 4].AskForAnnouncement();
                if (announcement == AnnouncementType.Pass)
                {
                    passesLeft--;
                }

                this.playerNumber++;
            }

            // TODO: Deal 3 more cards
            // TODO: Play cards and get announcements (terca, 50, 100, 150, 200, belot, etc.)
            // TODO: Check if played card is valid in the current context
            // TODO: Evaluate game result and return it
        }
    }
}
