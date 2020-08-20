namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class AllTrumpsOursContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            foreach (var card in context.AvailableCardsToPlay)
            {
                if (card.Type == CardType.Jack && context.MyCards.Count(x => x.Suit == card.Suit) > 2)
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Nine
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Ace
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Ten
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.King
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Queen
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Eight
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Queen))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.King))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                    && playedCards.Contains(Card.GetCard(card.Suit, CardType.Jack)))
                {
                    return new PlayCardAction(card);
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
                    return new PlayCardAction(card);
                }
            }

            var teammate = context.MyPosition.GetTeammate();

            foreach (var cardSuit in Card.AllSuits)
            {
                if (context.Bids.Any(x => x.Player == teammate && x.Type == cardSuit.ToBidType())
                    && context.AvailableCardsToPlay.HasAnyOfSuit(cardSuit))
                {
                    return new PlayCardAction(context.AvailableCardsToPlay.Where(x => x.Suit == cardSuit).Lowest(x => x.TrumpOrder));
                }
            }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.TrumpOrder));
        }

        public PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var firstCardSuit = context.CurrentTrickActions[0].Card.Suit;
            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Jack));
            }

            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Nine))
                && playedCards.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Nine));
            }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.TrumpOrder));
        }

        public PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var firstCardSuit = context.CurrentTrickActions[0].Card.Suit;
            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Jack));
            }

            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Nine))
                && playedCards.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Nine));
            }

            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Ace))
                && playedCards.Contains(Card.GetCard(firstCardSuit, CardType.Nine))
                && playedCards.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Ace));
            }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.TrumpOrder));
        }

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            //// var firstCardSuit = context.CurrentTrickActions[0].Card.Suit;
            //// if (trickWinner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.HasAnyOfSuit(firstCardSuit))
            //// {
            ////     return new PlayCardAction(context.AvailableCardsToPlay.Where(x => x.Suit == firstCardSuit).Highest(x => x.TrumpOrder));
            //// }

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.TrumpOrder));
        }
    }
}
