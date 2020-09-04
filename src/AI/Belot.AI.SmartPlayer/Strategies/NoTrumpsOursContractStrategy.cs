namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class NoTrumpsOursContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var card = CardHelpers.GetCardThatSurelyWinsTheTrickInNoTrumps(
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
