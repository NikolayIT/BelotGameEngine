namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class NoTrumpsPlayingFirstPlayStrategy : IPlayStrategy
    {
        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type))
                    .FirstOrDefault());
        }
    }
}
