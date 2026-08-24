namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrumpOursContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (CardHelpers.CountOfSuit(playedCards, trumpSuit)
                + CardHelpers.CountOfSuit(context.MyCards, trumpSuit) == 8)
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

            //// if (context.AvailableCardsToPlay.HasAnyOfSuit(context.CurrentContract.Type.ToCardSuit()))
            //// {
            ////     Interlocked.Increment(ref GlobalCounters.Counters[1]);
            ////     return new PlayCardAction(
            ////         context.AvailableCardsToPlay.Where(x => x.Suit == context.CurrentContract.Type.ToCardSuit())
            ////             .Highest(x => x.TrumpOrder));
            //// }

            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(CardHelpers.SuitContractOrderBySuit[(int)trumpSuit]));
        }

        public PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(CardHelpers.SuitContractOrderBySuit[(int)trumpSuit]));
        }

        public PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(CardHelpers.SuitContractOrderBySuit[(int)trumpSuit]));
        }

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (trickWinner.IsInSameTeamWith(context.MyPosition))
            {
                // The trick is ours: give it the biggest non-trump, non-ace card. Single pass
                // with the same predicate, key (non-trumps rank by NoTrumpOrder) and
                // first-on-ties rule as the old Any/Where/Highest chain, without allocating.
                Card best = null;
                var bestKey = 0;
                foreach (var card in context.AvailableCardsToPlay)
                {
                    if (card.Suit == trumpSuit || card.Type == CardType.Ace)
                    {
                        continue;
                    }

                    if (best == null || card.NoTrumpOrder > bestKey)
                    {
                        best = card;
                        bestKey = card.NoTrumpOrder;
                    }
                }

                if (best != null)
                {
                    return new PlayCardAction(best);
                }
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(CardHelpers.SuitContractOrderBySuit[(int)trumpSuit]));
        }
    }
}
