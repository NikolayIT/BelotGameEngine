namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JustBelot.Common.Extensions;

    public class Hand : CardsCollection
    {
        public Hand()
            : base()
        {
        }

        public IEnumerable<CardsCombination> FindAvailableCardsCombinations()
        {
            var cards = this.ToList();
            var combinations = new List<CardsCombination>();

            #region Four of a kind
            var countOfCardTypes = new Dictionary<CardType, int>();
            foreach (var card in cards)
            {
                if (!countOfCardTypes.ContainsKey(card.Type))
                {
                    countOfCardTypes.Add(card.Type, 0);
                }

                countOfCardTypes[card.Type]++;
            }

            foreach (var cardType in countOfCardTypes.Keys)
            {
                if (countOfCardTypes[cardType] == 4)
                {
                    switch (cardType)
                    {
                        case CardType.Jack:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfJacks, cardType));
                            break;
                        case CardType.Nine:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfNines, cardType));
                            break;
                        case CardType.Ace:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfAKind, cardType));
                            break;
                        case CardType.King:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfAKind, cardType));
                            break;
                        case CardType.Queen:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfAKind, cardType));
                            break;
                        case CardType.Ten:
                            combinations.Add(new CardsCombination(CardsCombinationType.FourOfAKind, cardType));
                            break;
                    }

                    if (cardType != CardType.Seven && cardType != CardType.Eight)
                    {
                        cards.RemoveAll(x => x.Type == cardType);
                    }
                }
            }
            #endregion

            #region Sequential combinations
            var cardsBySuit = new Dictionary<CardSuit, List<Card>>
                                       {
                                           { CardSuit.Clubs, new List<Card>() },
                                           { CardSuit.Diamonds, new List<Card>() },
                                           { CardSuit.Hearts, new List<Card>() },
                                           { CardSuit.Spades, new List<Card>() }
                                       };
            foreach (var card in cards)
            {
                cardsBySuit[card.Suit].Add(card);
            }

            foreach (var cardsBySuitKeyValue in cardsBySuit)
            {
                var suitedCards = cardsBySuitKeyValue.Value;
                if (suitedCards.Count == 0)
                {
                    continue;
                }

                suitedCards.Sort();
                int previousCardValue = (int)suitedCards[0].Type;
                int count = 1;
                for (int i = 1; i < suitedCards.Count; i++)
                {
                    if ((int)suitedCards[i].Type == previousCardValue + 1)
                    {
                        count++;
                    }
                    else
                    {
                        if (count == 3)
                        {
                            combinations.Add(new CardsCombination(CardsCombinationType.Tierce, (CardType)previousCardValue, cardsBySuitKeyValue.Key));
                        }

                        if (count == 4)
                        {
                            combinations.Add(new CardsCombination(CardsCombinationType.Quart, (CardType)previousCardValue, cardsBySuitKeyValue.Key));
                        }

                        if (count >= 5)
                        {
                            combinations.Add(new CardsCombination(CardsCombinationType.Quint, (CardType)previousCardValue, cardsBySuitKeyValue.Key));
                        }

                        count = 1;
                    }

                    previousCardValue = (int)suitedCards[i].Type;
                }

                if (count == 3)
                {
                    combinations.Add(new CardsCombination(CardsCombinationType.Tierce, suitedCards[suitedCards.Count - 1].Type, cardsBySuitKeyValue.Key));
                }

                if (count == 4)
                {
                    combinations.Add(new CardsCombination(CardsCombinationType.Quart, suitedCards[suitedCards.Count - 1].Type, cardsBySuitKeyValue.Key));
                }

                if (count >= 5)
                {
                    combinations.Add(new CardsCombination(CardsCombinationType.Quint, suitedCards[suitedCards.Count - 1].Type, cardsBySuitKeyValue.Key));
                }
            }
            #endregion

            return combinations;
        }

        public IEnumerable<Card> GetAllowedCards(Contract contract, IList<Card> currentTrickCards)
        {
            if (currentTrickCards == null || !currentTrickCards.Any())
            {
                // The player is first and can play any card
                return this;
            }

            var firstCard = currentTrickCards[0];

            // For all trumps the player should play bigger card from the same suit if available.
            // If bigger card is not available, the player should play any card of the same suit if available.
            if (contract.Type == ContractType.AllTrumps)
            {
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    var bestCard = currentTrickCards.Where(card => card.Suit == firstCard.Suit).OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();

                    if (this.Any(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps()))
                    {
                        // Has bigger card(s)
                        return this.Where(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps());
                    }

                    // Any other card from the same suit
                    return this.Where(card => card.Suit == firstCard.Suit);
                }

                // No card of the same suit available
                return this;
            }

            // For no trumps the player should play card from the same suit if available.
            if (contract.Type == ContractType.NoTrumps)
            {
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    return this.Where(x => x.Suit == firstCard.Suit);
                }

                // No card of the same suit available
                return this;
            }

            // Playing Clubs, Diamonds, Hearts or Spades
            if (firstCard.Suit == contract.Type.ToCardSuit())
            {
                // Trump card played
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    var bestCard = currentTrickCards.Where(card => card.Suit == firstCard.Suit).OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();

                    if (this.Any(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps()))
                    {
                        // Has bigger card(s)
                        return this.Where(card => card.Suit == firstCard.Suit && card.Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps());
                    }

                    // Any other card from the same suit
                    return this.Where(card => card.Suit == firstCard.Suit);
                }

                // No card of the same suit available
                return this;
            }
            else
            {
                // Non-trump card played
                if (this.Any(x => x.Suit == firstCard.Suit))
                {
                    // If the player has the same card suit, he should play a card from the suit
                    return this.Where(x => x.Suit == firstCard.Suit);
                }
                else
                {
                    // The player has not a card with the same suit
                    if (this.Any(x => x.Suit == contract.Type.ToCardSuit()))
                    {
                        var currentPlayerTeamIsCurrentTrickWinner = false;
                        if (currentTrickCards.Count > 1)
                        {
                            // The teammate played card
                            Card bestCard;
                            if (currentTrickCards.Any(x => x.Suit == contract.Type.ToCardSuit()))
                            {
                                // Someone played trump
                                bestCard = currentTrickCards.Where(x => x.Suit == contract.Type.ToCardSuit()).OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();
                            }
                            else
                            {
                                // No one played trump
                                bestCard = currentTrickCards.OrderByDescending(x => x.Type.GetOrderForNoTrumps()).First();
                            }

                            if (currentTrickCards[currentTrickCards.Count - 2] == bestCard)
                            {
                                // The teammate has the best card in current trick
                                currentPlayerTeamIsCurrentTrickWinner = true;
                            }
                        }

                        // The player has trump card
                        if (currentPlayerTeamIsCurrentTrickWinner)
                        {
                            // The current trick winner is the player or his teammate.
                            // The player is not obligatory to play any trump
                            return this;
                        }
                        else
                        {
                            // The current trick winner is the rivals of the current player
                            if (currentTrickCards.Any(x => x.Suit == contract.Type.ToCardSuit()))
                            {
                                // Someone of the rivals has played trump card and is winning the trick
                                var biggestTrumpCard = currentTrickCards.OrderByDescending(x => x.Type.GetOrderForAllTrumps()).First();
                                if (this.Any(x => x.Suit == contract.Type.ToCardSuit() && x.Type.GetOrderForAllTrumps() > biggestTrumpCard.Type.GetOrderForAllTrumps()))
                                {
                                    // The player has bigger trump card(s) and should play one of them
                                    return this.Where(x => x.Suit == contract.Type.ToCardSuit() && x.Type.GetOrderForAllTrumps() > biggestTrumpCard.Type.GetOrderForAllTrumps());
                                }

                                // The player hasn't any bigger trump card so he can play any card
                                return this;
                            }
                            else
                            {
                                // No one played trump card, but the player should play one of them
                                return this.Where(x => x.Suit == contract.Type.ToCardSuit());
                            }
                        }
                    }

                    // The player has not any trump card or card from the played suit
                    return this;
                }
            }
        }

        public bool IsBeloteAllowed(Contract contract, IList<Card> currentTrickCards, Card card)
        {
            var belote = false;
            if (contract.Type != ContractType.NoTrumps)
            {
                if (contract.Type == ContractType.AllTrumps)
                {
                    if (currentTrickCards.Count == 0)
                    {
                        // The player is first
                        belote = true;
                    }
                    else
                    {
                        if (currentTrickCards[0].Suit == card.Suit)
                        {
                            // Belote is allowed only when playing card from the same suit
                            belote = true;
                        }
                    }
                }
                else
                {
                    // Clubs, Diamonds, Hearts or Spades
                    if (card.Suit == contract.Type.ToCardSuit())
                    {
                        // Only if belote is from the trump suit
                        belote = true;
                    }
                }
            }

            if (belote)
            {
                return this.IsCombinationOfQueenAndKingAvailable(card);
            }

            return false;
        }

        private bool IsCombinationOfQueenAndKingAvailable(Card playedCard)
        {
            if (!this.Contains(playedCard))
            {
                return false;
            }

            if (playedCard.Type == CardType.King)
            {
                return this.Any(x => x.Type == CardType.Queen && x.Suit == playedCard.Suit);
            }

            if (playedCard.Type == CardType.Queen)
            {
                return this.Any(x => x.Type == CardType.King && x.Suit == playedCard.Suit);
            }

            return false;
        }
    }
}
