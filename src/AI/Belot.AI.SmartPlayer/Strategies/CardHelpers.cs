namespace Belot.AI.SmartPlayer.Strategies
{
    using System;

    using Belot.Engine.Cards;

    public static class CardHelpers
    {
        /// <summary>
        /// One cached ordering delegate per trump suit for suit contracts: trumps rank above
        /// every plain card (TrumpOrder + 8), plain cards rank by NoTrumpOrder. An inline
        /// lambda with this shape captures the trump suit and allocates on every call.
        /// </summary>
        public static readonly Func<Card, int>[] SuitContractOrderBySuit =
        {
            x => x.Suit == CardSuit.Club ? x.TrumpOrder + 8 : x.NoTrumpOrder,
            x => x.Suit == CardSuit.Diamond ? x.TrumpOrder + 8 : x.NoTrumpOrder,
            x => x.Suit == CardSuit.Heart ? x.TrumpOrder + 8 : x.NoTrumpOrder,
            x => x.Suit == CardSuit.Spade ? x.TrumpOrder + 8 : x.NoTrumpOrder,
        };

        /// <summary>
        /// Allocation-free count of the cards of the given suit (a predicate lambda here would
        /// capture the suit and allocate a closure on every call).
        /// </summary>
        public static int CountOfSuit(CardCollection cards, CardSuit suit)
        {
            var count = 0;
            foreach (var card in cards)
            {
                if (card.Suit == suit)
                {
                    count++;
                }
            }

            return count;
        }

        public static Card GetCardThatSurelyWinsATrickInAllTrumps(
            CardCollection availableCardsToPlay,
            CardCollection playerCards,
            CardCollection playedCards,
            int cardsThreshold)
        {
            foreach (var card in availableCardsToPlay)
            {
                if (card.Type == CardType.Jack &&
                    CountOfSuit(playedCards, card.Suit) +
                    CountOfSuit(playerCards, card.Suit) > cardsThreshold)
                {
                    return card;
                }

                if (card.Type == CardType.Nine
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.Ace
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.Ten
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.King
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.Queen
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.Eight
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }

                if (card.Type == CardType.Seven
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Eight))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return card;
                }
            }

            return null;
        }

        public static Card GetCardThatSurelyWinsATrickInNoTrumps(
            CardCollection availableCardsToPlay,
            CardCollection playerCards,
            CardCollection playedCards)
        {
            foreach (var card in availableCardsToPlay)
            {
                if (card.Type == CardType.Ace &&
                    CountOfSuit(playedCards, card.Suit) +
                    CountOfSuit(playerCards, card.Suit) > 4)
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
