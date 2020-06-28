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
        // Контрата отпада при капо?
        private const bool DoublesDoNotCountWhenNoTricks = true;

        // Валата (капото) на без коз удвоява ли се?
        private const int NoTrumpNoTricksValue = 90; // 180

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

            this.cardDeck = new Queue<Card>(CardsCollection.FullDeckOfCards);

            this.playerCards = new[] { new Hand(8), new Hand(8), new Hand(8), new Hand(8) }; // 4 players

            this.southNorthPlayersCardsTaken = new CardsCollection();
            this.eastWestPlayersCardsTaken = new CardsCollection();

            this.southNorthBelotes = 0;
            this.eastWestBelotes = 0;

            this.southNorthTeamTakesLastHand = null;
        }

        public DealResult PlayDeal()
        {
            // 1. Shuffle the card deck
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
            var dealResult = this.PrepareDealResult(contract);
            return dealResult;
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
            for (int playerId = 0; playerId < 4; playerId++)
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

                for (int i = 4; i > 0; i--)
                {
                    if (trickNumber == 1 && contract.Type != ContractType.NoTrumps)
                    {
                        var combinations = this.AskForCardCombinations(currentPlayer);
                        this.game.GameInfo.InformForCardCombinationsAnnounced(new CardCombinationsAnnouncedEventArgs(this.game[currentPlayer], combinations.Select(x => x.CombinationType)));
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
                    // Last hand
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

        private IEnumerable<CardsCombination> AskForCardCombinations(IPlayer player)
        {
            var currentPlayerHand = this.playerCards[(int)this.game[player]];
            var allowedCombinations = currentPlayerHand.FindAvailableCardsCombinations().ToList();
            var playerCombinaions = player.AskForCardsCombinations(allowedCombinations.ToList());

            var finalCombinations = new List<CardsCombination>();
            foreach (var cardCombination in playerCombinaions)
            {
                if (allowedCombinations.Contains(cardCombination))
                {
                    finalCombinations.Add(cardCombination);
                    allowedCombinations.Remove(cardCombination);
                }
            }

            return finalCombinations;
        }

        private DealResult PrepareDealResult(Contract contract)
        {
            // Count card values
            var southNorthPoints = this.southNorthPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));
            var eastWestPoints = this.eastWestPlayersCardsTaken.Sum(card => card.GetValue(contract.Type));
            
            // "last 10" rule
            if (this.southNorthTeamTakesLastHand == true)
            {
                southNorthPoints += 10;
            }
            else if (this.southNorthTeamTakesLastHand == false)
            {
                eastWestPoints += 10;
            }

            // Belotes
            southNorthPoints += 20 * this.southNorthBelotes;
            eastWestPoints += 20 * this.eastWestBelotes;

            // No trump
            if (contract.Type == ContractType.NoTrumps)
            {
                southNorthPoints *= 2;
                eastWestPoints *= 2;
            }

            // No tricks for one of the teams?
            var noTricksForOneOfTheTeams = this.southNorthPlayersCardsTaken.Count == 0 || this.eastWestPlayersCardsTaken.Count == 0;
            if (this.southNorthPlayersCardsTaken.Count == 0)
            {
                if (contract.Type == ContractType.NoTrumps)
                {
                    eastWestPoints += NoTrumpNoTricksValue;
                }
                else
                {
                    eastWestPoints += 90;
                }
            }
            else if (this.eastWestPlayersCardsTaken.Count == 0)
            {
                if (contract.Type == ContractType.NoTrumps)
                {
                    southNorthPoints += NoTrumpNoTricksValue;
                }
                else
                {
                    southNorthPoints += 90;
                }
            }

            // Card combinations
            // TODO: Count points from card combinations

            // Check if contract is kept and for hanging points
            var hangingPoints = 0;
            var contractNotKept = false;
            if (contract.PlayerPosition == PlayerPosition.South || contract.PlayerPosition == PlayerPosition.North)
            {
                if (southNorthPoints < eastWestPoints)
                {
                    // Contract not kept
                    contractNotKept = true;
                    eastWestPoints += southNorthPoints;
                    southNorthPoints = 0;
                }
                else if (southNorthPoints == eastWestPoints)
                {
                    // "Hanging" points
                    hangingPoints = southNorthPoints;
                    southNorthPoints = 0;
                }
            }
            else
            {
                if (eastWestPoints < southNorthPoints)
                {
                    // Contract not kept
                    contractNotKept = true;
                    southNorthPoints += eastWestPoints;
                    eastWestPoints = 0;
                }
                else if (southNorthPoints == eastWestPoints)
                {
                    // "Hanging" points
                    hangingPoints = eastWestPoints;
                    eastWestPoints = 0;
                }
            }

            // Round points
            southNorthPoints = contract.RoundPoints(southNorthPoints, southNorthPoints > eastWestPoints);
            eastWestPoints = contract.RoundPoints(eastWestPoints, eastWestPoints > southNorthPoints);
            hangingPoints = contract.RoundPoints(hangingPoints, false);

            // Double and re-double
            bool isGameDoubled = contract.IsReDoubled || contract.IsDoubled;
            if (DoublesDoNotCountWhenNoTricks && noTricksForOneOfTheTeams)
            {
                isGameDoubled = false;
            }

            if (isGameDoubled)
            {
                var weight = contract.IsReDoubled ? 4 : 2;
                if (southNorthPoints > eastWestPoints)
                {
                    southNorthPoints = weight * (southNorthPoints + eastWestPoints);
                    eastWestPoints = 0;
                }
                else if (eastWestPoints > southNorthPoints)
                {
                    southNorthPoints = 0;
                    eastWestPoints = weight * (southNorthPoints + eastWestPoints);
                }
                else
                {
                    hangingPoints = southNorthPoints + eastWestPoints + hangingPoints;
                    hangingPoints *= weight;
                    southNorthPoints = 0;
                    eastWestPoints = 0;
                }
            }

            // Final result
            var result = new DealResult(true, contract, southNorthPoints, eastWestPoints, hangingPoints, contractNotKept, noTricksForOneOfTheTeams);
            return result;
        }
    }
}
