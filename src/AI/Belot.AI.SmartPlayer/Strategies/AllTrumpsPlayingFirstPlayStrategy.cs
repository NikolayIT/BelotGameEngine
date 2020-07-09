namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;
    using System.Threading;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class AllTrumpsPlayingFirstPlayStrategy : IPlayStrategy
    {
        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            Interlocked.Increment(ref GlobalCounters.Counters[0]);
            foreach (var card in context.AvailableCardsToPlay)
            {
                if (card.Type == CardType.Nine
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[1]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Ace
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[2]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Ten
                    && playedCards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[3]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.King
                    && playedCards.Any(x => x.Type == CardType.Ten && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[4]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Queen
                    && playedCards.Any(x => x.Type == CardType.King && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ten && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[5]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Eight
                    && playedCards.Any(x => x.Type == CardType.Queen && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.King && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ten && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[6]);
                    return new PlayCardAction(card);
                }

                if (card.Type == CardType.Seven
                    && playedCards.Any(x => x.Type == CardType.Eight && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Queen && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.King && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ten && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                    && playedCards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit))
                {
                    Interlocked.Increment(ref GlobalCounters.Counters[7]);
                    return new PlayCardAction(card);
                }
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type))
                    .FirstOrDefault());
        }
    }
}
