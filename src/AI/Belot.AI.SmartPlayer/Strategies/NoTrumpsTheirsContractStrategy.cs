namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class NoTrumpsTheirsContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var card = CardHelpers.GetCardThatSurelyWinsATrickInNoTrumps(
                context.AvailableCardsToPlay,
                context.MyCards,
                playedCards);
            if (card != null)
            {
                return new PlayCardAction(card);
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

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            if (trickWinner.IsInSameTeamWith(context.MyPosition))
            {
                // The trick is ours: give it the biggest card that is not an ace or a ten.
                // Single pass with the same predicate, key and first-on-ties rule as the old
                // Any/Where/Highest chain, without allocating the filtered collection.
                Card best = null;
                var bestKey = 0;
                foreach (var card in context.AvailableCardsToPlay)
                {
                    if (card.Type == CardType.Ace || card.Type == CardType.Ten)
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

            return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
        }
    }
}
