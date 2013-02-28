namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using JustBelot.Common.Extensions;

    /// <summary>
    /// Responsible for one deal (one particular allocation of 32 cards to the four players including the bidding, the play of the cards and the scoring based on those cards. 
    /// </summary>
    internal class DealManager
    {
        private readonly GameManager game;

        private readonly Queue<Card> cardDeck;

        private readonly Card[,] playerCards; // We are keeping local information about cards to prevent cheating from players (e.g. playing card that they don't own)

        private readonly List<Card> southNorthPlayersCardsTaken;
        private readonly List<Card> eastWestPlayersCardsTaken;

        private Contract finalContract;

        public DealManager(GameManager game)
        {
            this.game = game;

            this.cardDeck = new Queue<Card>(CardsHelper.GetFullCardDeck());
            Debug.Assert(this.cardDeck != null && this.cardDeck.Count == 32, "The card deck is not complete!");

            this.playerCards = new Card[4, 8]; // 4 players, 8 cards for each player

            this.southNorthPlayersCardsTaken = new List<Card>();
            this.eastWestPlayersCardsTaken = new List<Card>();
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
            var dealWillBePlayed = this.Bidding();
            if (!dealWillBePlayed)
            {
                return new DealResult(false);
            }

            // 4. Deal 3 more cards to each player
            this.DealCardsToAllPlayers(3);

            // 5. Play the game
            this.PlayCards();

            // 6. Count the result
            return this.PrepareDealResult();
        }

        // TODO: Ask for bids and check if bids are legal
        private bool Bidding()
        {
            var currentPlayer = this.game.GetFirstPlayerForTheDeal();
            IList<BidType> previousBids = new List<BidType>();
            var currentContract = new Contract();

            var passesLeft = 4;
            while (passesLeft > 0)
            {
                var availableBids = currentContract.GetAvailableBidsAfterThisContract(this.game[this.game.GetTeamMate(currentPlayer)]);
                var bid = currentPlayer.AskForBid(currentContract, availableBids, previousBids);
                if (bid == BidType.Pass)
                {
                    passesLeft--;
                }
                else
                {
                    if (!availableBids.Contains(bid))
                    {
                        throw new InvalidPlayerActionException(currentPlayer, string.Format("Invalid bid: {0}", bid));
                    }

                    switch (bid)
                    {
                        case BidType.Clubs:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.Clubs);
                            break;
                        case BidType.Diamonds:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.Diamonds);
                            break;
                        case BidType.Hearts:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.Hearts);
                            break;
                        case BidType.Spades:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.Spades);
                            break;
                        case BidType.NoTrumps:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.NoTrumps);
                            break;
                        case BidType.AllTrumps:
                            currentContract = new Contract(this.game[currentPlayer], ContractType.AllTrumps);
                            break;
                        case BidType.Double:
                            currentContract = new Contract(this.game[currentPlayer], currentContract.Type, currentContract.PlayerPosition, true, false);
                            break;
                        case BidType.ReDouble:
                            currentContract = new Contract(this.game[currentPlayer], currentContract.Type, currentContract.PlayerPosition, false, true);
                            break;
                    }

                    passesLeft = 3;
                }

                this.game.GameInfo.InformForBid(new BidEventArgs(this.game[currentPlayer], bid, currentContract));

                previousBids.Add(bid);
                currentPlayer = this.game.GetNextPlayer(currentPlayer);
            }

            return currentContract.IsAvailable;
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

                if (trickNumber == 8)
                {
                    // TODO: Save who takes the last trick
                }

                // southNorthPlayersCardsTaken.Add(); or eastWestPlayersCardsTaken.Add();
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

            int southNorthCardPointsSum = this.southNorthPlayersCardsTaken.Sum(card => card.GetValue(this.finalContract.Type));
            int eastWestCardPointsSum = this.eastWestPlayersCardsTaken.Sum(card => card.GetValue(this.finalContract.Type));


            //result.SouthNorthPoints = 0;
            //result.EastWestPoints = 0;

            return result;
        }
    }
}
