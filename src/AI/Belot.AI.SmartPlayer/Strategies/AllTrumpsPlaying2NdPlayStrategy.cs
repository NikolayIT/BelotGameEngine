namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class AllTrumpsPlaying2NdPlayStrategy : IPlayStrategy
    {
        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var firstCardSuit = context.CurrentTrickActions.First().Card.Suit;
            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Jack));
            }

            if (context.AvailableCardsToPlay.Contains(Card.GetCard(firstCardSuit, CardType.Nine))
                && playedCards.Contains(Card.GetCard(firstCardSuit, CardType.Jack)))
            {
                return new PlayCardAction(Card.GetCard(firstCardSuit, CardType.Nine));
            }

            return new PlayCardAction(context.AvailableCardsToPlay.OrderBy(x => x.TrumpOrder).FirstOrDefault());
        }
    }
}
