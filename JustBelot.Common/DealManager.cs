namespace JustBelot.Common
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using JustBelot.Common.Extensions;

    /// <summary>
    /// Responsible for one deal (one particular allocation of 32 cards to the four players including the bidding, the play of the cards and the scoring based on those cards. 
    /// </summary>
    internal class DealManager
    {
        private readonly GameManager game;

        private readonly Queue<Card> cardDeck;

        private readonly Card[,] playerCards; // We are keeping local information about cards to prevent cheating from players (e.g. playing card that they don't own)

        public DealManager(GameManager game)
        {
            this.game = game;

            this.cardDeck = new Queue<Card>(CardsHelper.GetFullCardDeck());
            Debug.Assert(this.cardDeck != null && this.cardDeck.Count == 32, "The card deck is not complete!");

            this.playerCards = new Card[4, 8]; // 4 players, 8 cards for each player
        }

        public DealResult PlayDeal()
        {
            // 1. Shuffle the card deck two times
            this.cardDeck.Shuffle();
            this.cardDeck.Shuffle();

            // 2. Deal 3 + 2 cards to each player (like in the real world game ;))
            this.DealCardsToAllPlayers(3);
            this.DealCardsToAllPlayers(2);

            // 3. Ask for bidding and check if the bidding are legal
            this.Bidding();

            // 4. Deal 3 more cards to each player
            this.DealCardsToAllPlayers(3);

            // 5. Play the game
            this.PlayCards();

            // 6. Count the result
            return this.PrepareDealResult();
        }

        private void Bidding()
        {
            // TODO: Ask for bids and check if bids are legal
            var currentPlayer = this.game.GetFirstPlayerForTheDeal();
            var lastBid = BidType.Pass;

            var passesLeft = 4;
            while (passesLeft > 0)
            {
                var bid = currentPlayer.AskForBid();

                if (bid == BidType.Pass)
                {
                    passesLeft--;
                }
                else
                {
                    if (bid == BidType.ReDouble && lastBid != BidType.Double)
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid bid: ReDouble without Double! Previous bid: {0}", lastBid));
                    }

                    if (bid == BidType.Double && lastBid == BidType.Pass)
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid bid: Double on Pass! Previous bid: {0}", lastBid));
                    }


                    // TODO: prevent the same team members to make some bid and then double
                    if (bid <= lastBid && lastBid != BidType.Double && lastBid != BidType.ReDouble) // TODO: needs improvements
                    {
                        throw new InvalidPlayerActionException(
                            currentPlayer,
                            string.Format("Invalid bid: {0}! Last bid: {1}", bid, lastBid));
                    }

                    passesLeft = 3;
                }

                this.game.GameInfo.InformForBid(new BidEventArgs(this.game[currentPlayer], bid));

                lastBid = bid;
                currentPlayer = this.game.GetNextPlayer(currentPlayer);

                // TODO: Inform game for the contracts (or the game may get contracts via deal manager) OR give last contracts as a parameter to Player.AskForContract()
            }
        }

        private void DealCardsToAllPlayers(int cardsCount)
        {
            for (int player = 0; player < 4; player++)
            {
                for (int i = 0; i < cardsCount; i++)
                {
                    this.game[player].AddCard(this.cardDeck.Peek());
                    this.playerCards[player, i] = this.cardDeck.Peek();
                    this.cardDeck.Dequeue();
                }
            }
        }

        private void PlayCards()
        {
            for (int trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                for (int player = 0; player < 4; player++)
                {
                    if (trickNumber == 1)
                    {
                        var playerDeclarations = this.game[player].AskForDeclarations();
                    }

                    var playedCard = this.game[player].PlayCard();
                }
            }

            // 5. Play first hand and ask for announcements like "terca", 50, 100, 150, 200, belot, et.
            // TODO: Play cards and check announcements (terca, 50, 100, 150, 200, belot, etc.)
            // TODO: Check if the played card is valid

            // 6. Play the other 7 hands
            // TODO: Play cards and check announcements (belot)
            // TODO: Check if the played card is valid
            // Trick is called a set of 4 cards played by each player in turn, during the play of a hand
        }

        private DealResult PrepareDealResult()
        {
            // TODO: Evaluate game result and don't forget the "last 10" rule and announcements
            var result = new DealResult();

            return result;
        }
    }
}
