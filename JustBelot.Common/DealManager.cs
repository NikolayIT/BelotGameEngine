namespace JustBelot.Common
{
    using System.Collections.Generic;
    using System.Linq;

    using JustBelot.Common.Extensions;

    /// <summary>
    /// Responsible for one deal (one particular allocation of 32 cards to the four players including the bidding, the play of the cards and the scoring based on those cards. 
    /// </summary>
    internal class DealManager
    {
        private readonly GameManager game;

        private readonly Queue<Card> cardDeck;

        private readonly CardsCollection[] playerCards; // We are keeping local information about cards to prevent cheating from players (e.g. playing card that they don't own)

        private readonly CardsCollection southNorthPlayersCardsTaken;
        private readonly CardsCollection eastWestPlayersCardsTaken;

        public DealManager(GameManager game)
        {
            this.game = game;

            this.cardDeck = new Queue<Card>(CardsCollection.GetFullCardDeck());

            this.playerCards = new[] { new CardsCollection(), new CardsCollection(), new CardsCollection(), new CardsCollection() }; // 4 players

            this.southNorthPlayersCardsTaken = new CardsCollection();
            this.eastWestPlayersCardsTaken = new CardsCollection();
        }

        public DealResult PlayDeal()
        {
            // 1. Shuffle the card deck two times
            this.cardDeck.Shuffle();
            this.cardDeck.Shuffle();

            // 2. Deal 5 cards to each player
            this.DealCardsToAllPlayers(5);

            // 3. Ask for bidding and check if the bids are legal
            var contract = this.Bidding();
            if (!contract.IsAvailable)
            {
                return new DealResult(false);
            }

            // 4. Deal 3 more cards to each player
            this.DealCardsToAllPlayers(3);

            // 5. Play the game
            this.PlayCards(contract);

            // 6. Count the result
            return this.PrepareDealResult(contract);
        }

        private Contract Bidding()
        {
            var currentPlayer = this.game.GetFirstPlayerForTheDeal();
            IList<BidType> previousBids = new List<BidType>();
            var currentContract = new Contract();

            var passesLeft = 4;
            while (passesLeft > 0)
            {
                var allowedBids = currentContract.GetAvailableBidsAfterThisContract(this.game[this.game.GetTeamMate(currentPlayer)]);
                var bid = currentPlayer.AskForBid(currentContract, allowedBids, previousBids);
                if (bid == BidType.Pass)
                {
                    passesLeft--;
                }
                else
                {
                    if (!allowedBids.Contains(bid))
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
                            currentContract = new Contract(this.game[currentPlayer], currentContract.Type, currentContract.OriginalBidder, false, true);
                            break;
                    }

                    passesLeft = 3;
                }

                this.game.GameInfo.InformForBid(new BidEventArgs(this.game[currentPlayer], bid, currentContract));

                previousBids.Add(bid);
                currentPlayer = this.game.GetNextPlayer(currentPlayer);
            }

            return currentContract;
        }

        private void DealCardsToAllPlayers(int cardsCount)
        {
            for (int player = 0; player < 4; player++)
            {
                var cards = new List<Card>();
                for (int i = 0; i < cardsCount; i++)
                {
                    cards.Add(this.cardDeck.Peek());
                    this.playerCards[player].Add(this.cardDeck.Peek());
                    this.cardDeck.Dequeue();
                }

                this.game[player].AddCards(cards);
            }
        }

        private void PlayCards(Contract contract)
        {
            for (int trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                for (int player = 0; player < 4; player++)
                {
                    if (trickNumber == 1)
                    {
                        if (contract.Type != ContractType.NoTrumps)
                        {
                            var playerDeclarations = this.game[player].AskForDeclarations();
                        }
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

        private DealResult PrepareDealResult(Contract contract)
        {
            // TODO: Evaluate game result and don't forget the "last 10" rule and announcements
            var result = new DealResult();

            int southNorthCardPointsSum = this.southNorthPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));
            int eastWestCardPointsSum = this.eastWestPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));


            //result.SouthNorthPoints = 0;
            //result.EastWestPoints = 0;

            return result;
        }
    }
}
