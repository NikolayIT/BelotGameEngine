namespace JustBelot.Common
{
    using System;
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

        private readonly Hand[] playerCards; // We are keeping local information about cards to prevent cheating from players (e.g. playing card that they don't own)

        private readonly CardsCollection southNorthPlayersCardsTaken;
        private readonly CardsCollection eastWestPlayersCardsTaken;

        private int southNorthBelotes;
        private int eastWestBelotes;

        private bool? southNorthTeamTakesLastHand;

        public DealManager(GameManager game)
        {
            this.game = game;

            this.cardDeck = new Queue<Card>(CardsCollection.GetFullCardDeck());

            this.playerCards = new[] { new Hand(), new Hand(), new Hand(), new Hand() }; // 4 players

            this.southNorthPlayersCardsTaken = new CardsCollection();
            this.eastWestPlayersCardsTaken = new CardsCollection();

            this.southNorthBelotes = 0;
            this.eastWestBelotes = 0;

            this.southNorthTeamTakesLastHand = null;
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
                return new DealResult(false, contract);
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
            foreach (int playerId in Enum.GetValues(typeof(PlayerPosition)))
            {
                var cards = new List<Card>();
                for (int i = 0; i < cardsCount; i++)
                {
                    cards.Add(this.cardDeck.Peek());
                    this.playerCards[playerId].Add(this.cardDeck.Peek());
                    this.cardDeck.Dequeue();
                }

                this.game[playerId].AddCards(cards);
            }
        }

        private void PlayCards(Contract contract)
        {
            var currentPlayer = this.game.GetFirstPlayerForTheDeal();

            for (int trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                var currentTrick = new Trick(contract, this.game[currentPlayer]);

                for (int i = 0; i < 4; i++)
                {
                    if (trickNumber == 1 && contract.Type != ContractType.NoTrumps)
                    {
                        var declarations = this.AskForDeclarations(currentPlayer);
                    }

                    var playAction = this.PlayCard(currentPlayer, contract, currentTrick);
                    currentTrick.Add(playAction.Card);

                    this.game.GameInfo.InformForPlayedCard(new CardPlayedEventArgs(this.game[currentPlayer], playAction, currentTrick.ToList()));
                    currentPlayer = this.game.GetNextPlayer(currentPlayer);
                }

                var trickWinner = currentTrick.WinnerPlayer;

                if (trickWinner == PlayerPosition.South || trickWinner == PlayerPosition.North)
                {
                    this.southNorthPlayersCardsTaken.Add(currentTrick);
                }
                else
                {
                    this.eastWestPlayersCardsTaken.Add(currentTrick);
                }

                if (trickNumber == 8)
                {
                    if (trickWinner == PlayerPosition.South || trickWinner == PlayerPosition.North)
                    {
                        this.southNorthTeamTakesLastHand = true;
                    }
                    else
                    {
                        this.southNorthTeamTakesLastHand = false;
                    }
                }

                currentPlayer = this.game[trickWinner];
            }
        }

        private PlayAction PlayCard(IPlayer player, Contract contract, IList<Card> currentTrickCards)
        {
            // Prepare played cards and allowed cards
            var currentPlayerCards = this.playerCards[(int)this.game[player]];
            var allowedCards = new CardsCollection(currentPlayerCards.GetAllowedCards(contract, currentTrickCards));

            // Play card
            var playAction = player.PlayCard(allowedCards.ToList(), currentTrickCards.ToList());
            
            // Check for invalid card
            if (!allowedCards.Contains(playAction.Card))
            {
                throw new InvalidPlayerActionException(player, string.Format("Invalid card: {0}", playAction.Card));
            }

            // Save belote to team points
            playAction.Belote = false;
            if (playAction.AnnounceBeloteIfAvailable && currentPlayerCards.IsBeloteAllowed(contract, currentTrickCards, playAction.Card))
            {
                switch (this.game[player])
                {
                    case PlayerPosition.South:
                        this.southNorthBelotes++;
                        break;
                    case PlayerPosition.East:
                        this.eastWestBelotes++;
                        break;
                    case PlayerPosition.North:
                        this.southNorthBelotes++;
                        break;
                    case PlayerPosition.West:
                        this.eastWestBelotes++;
                        break;
                }

                playAction.Belote = true;
            }
            
            // Remove played card from the players cards
            this.playerCards[(int)this.game[player]].Remove(playAction.Card);
            return playAction;
        }

        private IEnumerable<Declaration> AskForDeclarations(IPlayer player)
        {
            // TODO: Check announcements (terca, 50, 100, 150, 200, belot, etc.)
            var allowedDeclarations = new List<Declaration>(); // TODO: List of valid declarations
            var playerDeclarations = player.AskForDeclarations(allowedDeclarations);
            return playerDeclarations;
        }

        private DealResult PrepareDealResult(Contract contract)
        {
            // TODO: Evaluate game result and don't forget the "last 10" rule and announcements
            var result = new DealResult(true, contract);

            int southNorthCardPointsSum = this.southNorthPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));
            int eastWestCardPointsSum = this.eastWestPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));


            //result.SouthNorthPoints = 0;
            //result.EastWestPoints = 0;

            return result;
        }
    }
}
