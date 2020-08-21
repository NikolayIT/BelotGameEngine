namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class NoTrumpsOursContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            foreach (var card in context.AvailableCardsToPlay)
            {
                if (card.Type == CardType.Ace && context.MyCards.GetCount(x => x.Suit == card.Suit) > 3)
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Ten
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.King
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Queen
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Jack
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Nine
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Eight
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Seven
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Eight))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                {
                    return new PlayCardAction(card);
                }
            }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
        }

        public PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards)
        {
            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
        }

        public PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
        }

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCard, PlayerPosition trickWinners)
        {
            if (trickWinners.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(x => x.Type != CardType.Ace && x.Type != CardType.Ten))
            {
                return new PlayCardAction(
                    context.AvailableCardsToPlay.Where(x => x.Type != CardType.Ace && x.Type != CardType.Ten).Highest(x => x.NoTrumpOrder));
            }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
        }
    }
}
