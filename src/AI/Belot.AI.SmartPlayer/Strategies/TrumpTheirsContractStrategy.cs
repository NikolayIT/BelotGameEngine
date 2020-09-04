namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrumpTheirsContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (playedCards.GetCount(x => x.Suit == trumpSuit)
                + context.MyCards.GetCount(x => x.Suit == trumpSuit) == 8)
            {
                // No trump cards in other players
                foreach (var card in context.AvailableCardsToPlay)
                {
                    if (card.Suit != trumpSuit && card.Type == CardType.Ace)
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Ten
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.King
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Queen
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Jack
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Nine
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Eight
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                               && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace)))
                    {
                        return new PlayCardAction(card);
                    }

                    if (card.Suit != trumpSuit && card.Type == CardType.Seven
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
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
        }

        public PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
        }

        public PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
        }

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (trickWinner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(
                    x => x.Suit != trumpSuit && x.Type != CardType.Ace))
            {
                return new PlayCardAction(
                    context.AvailableCardsToPlay.Where(x => x.Suit != trumpSuit && x.Type != CardType.Ace)
                        .Highest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
        }
    }
}
