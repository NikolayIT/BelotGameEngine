namespace JustBelot.Common
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Responsible for one deal
    /// </summary>
    internal class DealManager
    {
        private readonly GameManager game;

        private readonly List<Card> cardDeck;

        private readonly Card[,] playerCards; // We are keeping local information about cards to prevent cheating from players (e.g. playing card that they don't own)

        public DealManager(GameManager game)
        {
            this.game = game;

            this.cardDeck = CardsHelper.GetFullCardDeck();
            Debug.Assert(this.cardDeck != null && this.cardDeck.Count == 32, "The card deck is not complete!");

            this.playerCards = new Card[4, 8]; // 4 players, 8 cards for each player
        }

        public void PlayDeal()
        {
            // 1. Shuffle the card deck two times
            this.cardDeck.Shuffle();
            this.cardDeck.Shuffle();

            // 2. Deal 3 + 2 cards to each player (like the real world game ;))
            int cardNumber = 0;
            for (int player = 0; player < 4; player++)
            {
                for (int i = 0; i < 3; i++)
                {
                    this.game[player].AddCard(this.cardDeck[cardNumber]);
                    this.playerCards[player, i] = this.cardDeck[cardNumber];
                    cardNumber++;
                }
            }

            for (int player = 0; player < 4; player++)
            {
                for (int i = 0; i < 2; i++)
                {
                    this.game[player].AddCard(this.cardDeck[cardNumber]);
                    this.playerCards[player, 3 + i] = this.cardDeck[cardNumber];
                    cardNumber++;
                }
            }

            // 3. Ask for contracts and check if the contracts are correct
            this.AskForContracts();

            // 4. Deal 3 more cards to each player
            for (int player = 0; player < 4; player++)
            {
                for (int i = 0; i < 3; i++)
                {
                    this.game[player].AddCard(this.cardDeck[cardNumber]);
                    this.playerCards[player, 5 + i] = this.cardDeck[cardNumber];
                    cardNumber++;
                }
            }

            // 5. Play first hand and ask for announcements like "terca", 50, 100, 150, 200, belot, et.
            // TODO: Play cards and check announcements (terca, 50, 100, 150, 200, belot, etc.)
            // TODO: Check if the played card is valid

            // 6. Play the other 7 hands
            // TODO: Play cards and check announcements (belot)
            // TODO: Check if the played card is valid

            // 7. Count the result
            // TODO: Evaluate game result and don't forget the "last 10" rule and announcements
        }

        private void AskForContracts()
        {
            // TODO: Ask for contracts and check if contracts are correct
            var currentPlayer = this.game.GetFirstPlayerForTheDeal();
            var lastContract = ContractType.Pass;

            var passesLeft = 4;
            while (passesLeft > 0)
            {
                var contract = currentPlayer.AskForContract();

                if (contract == ContractType.Pass)
                {
                    passesLeft--;
                }
                else
                {
                    if (contract == ContractType.ReDouble && lastContract != ContractType.Double)
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid contract: ReDouble without Double! Last contract: {0}", lastContract));
                    }

                    if (contract == ContractType.Double && lastContract == ContractType.Pass)
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid contract: Double on Pass! Last contract: {0}", lastContract));
                    }


                    // TODO: prevent the same team members to announce contract and then double
                    if (contract <= lastContract && lastContract != ContractType.Double && lastContract != ContractType.ReDouble) // TODO: needs improvements
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid contract: {0}! Last contract: {1}", contract, lastContract));
                    }

                    passesLeft = 3;
                }

                lastContract = contract;
                currentPlayer = this.game.GetNextPlayer(currentPlayer);

                // TODO: Inform game for the contracts (or the game may get contracts via deal manager) OR give last contracts as a parameter to Player.AskForContract()
            }
        }
    }
}
