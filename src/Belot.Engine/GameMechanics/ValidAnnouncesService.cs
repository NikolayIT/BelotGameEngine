namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ValidAnnouncesService
    {
        public bool IsBeloteAllowed(CardCollection playerCards, BidType contract, IList<PlayCardAction> currentTrickActions, Card playedCard)
        {
            if (playedCard.Type != CardType.Queen && playedCard.Type != CardType.King)
            {
                return false;
            }

            if (contract.HasFlag(BidType.NoTrumps))
            {
                return false;
            }

            if (contract.HasFlag(BidType.AllTrumps))
            {
                if (currentTrickActions.Count > 0 && currentTrickActions[0].Card.Suit != playedCard.Suit)
                {
                    // Belote is only allowed when playing card from the same suit as the first card played
                    return false;
                }
            }
            else
            {
                // Clubs, Diamonds, Hearts or Spades
                if (playedCard.Suit != contract.ToCardSuit())
                {
                    // Belote is only allowed when playing card from the trump suit
                    return false;
                }
            }

            if (playedCard.Type == CardType.Queen)
            {
                return playerCards.Contains(Card.GetCard(playedCard.Suit, CardType.King));
            }

            if (playedCard.Type == CardType.King)
            {
                return playerCards.Contains(Card.GetCard(playedCard.Suit, CardType.Queen));
            }

            return false;
        }

        public ICollection<Announce> GetAvailableAnnounces(CardCollection playerCards)
        {
            var cards = new CardCollection(playerCards);

            var combinations = new List<Announce>();
            FindFourOfAKindAnnounces(cards, combinations);
            FindSequentialAnnounces(cards, combinations);
            return combinations;
        }

        private static void FindFourOfAKindAnnounces(CardCollection cards, ICollection<Announce> combinations)
        {
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
                            combinations.Add(
                                new Announce(AnnounceType.FourJacks, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.Nine:
                            combinations.Add(
                                new Announce(AnnounceType.FourNines, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.Ace:
                            combinations.Add(
                                new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.King:
                            combinations.Add(
                                new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.Queen:
                            combinations.Add(
                                new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.Ten:
                            combinations.Add(
                                new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, cardType)));
                            break;
                        case CardType.Seven:
                            continue;
                        case CardType.Eight:
                            continue;
                    }

                    var newCards = new CardCollection();
                    if (cardType != CardType.Seven && cardType != CardType.Eight)
                    {
                        foreach (var card in cards)
                        {
                            if (card.Type != cardType)
                            {
                                newCards.Add(card);
                            }
                        }
                    }

                    cards = newCards;
                }
            }
        }

        private static void FindSequentialAnnounces(CardCollection cards, ICollection<Announce> combinations)
        {
            var cardsBySuit = new Dictionary<CardSuit, List<Card>>
                                  {
                                      { CardSuit.Club, new List<Card>() },
                                      { CardSuit.Diamond, new List<Card>() },
                                      { CardSuit.Heart, new List<Card>() },
                                      { CardSuit.Spade, new List<Card>() },
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

                suitedCards.Sort((card, card1) => card.Type.CompareTo(card1.Type));
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
                            combinations.Add(new Announce(AnnounceType.Tierce, suitedCards[i - 1]));
                        }

                        if (count == 4)
                        {
                            combinations.Add(new Announce(AnnounceType.Quarte, suitedCards[i - 1]));
                        }

                        if (count >= 5)
                        {
                            combinations.Add(new Announce(AnnounceType.Quinte, suitedCards[i - 1]));
                        }

                        count = 1;
                    }

                    previousCardValue = (int)suitedCards[i].Type;
                }

                if (count == 3)
                {
                    combinations.Add(new Announce(AnnounceType.Tierce, suitedCards[suitedCards.Count - 1]));
                }

                if (count == 4)
                {
                    combinations.Add(new Announce(AnnounceType.Quarte, suitedCards[suitedCards.Count - 1]));
                }

                if (count >= 5)
                {
                    combinations.Add(new Announce(AnnounceType.Quinte, suitedCards[suitedCards.Count - 1]));
                }
            }
        }
    }
}
