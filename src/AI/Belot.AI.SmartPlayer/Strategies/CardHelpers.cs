namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;

    public static class CardHelpers
    {
        public static Card GetCardThatSurelyWinsTheTrickInNoTrumps(
            CardCollection availableCardsToPlay,
            CardCollection playerCards,
            CardCollection playedCards)
        {
            foreach (var card in availableCardsToPlay)
            {
                if (card.Type == CardType.Ace &&
                    playedCards.GetCount(x => x.Suit == card.Suit) +
                    playerCards.GetCount(x => x.Suit == card.Suit) > 4)
                {
                    return card;
                }

                if (card.Type == CardType.Ten && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.King && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.Queen && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.Jack && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.Nine && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.Eight && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }

                if (card.Type == CardType.Seven && playedCards.Contains(Card.GetCard(card.Suit, CardType.Eight))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                                && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return card;
                }
            }

            return null;
        }
    }
}
