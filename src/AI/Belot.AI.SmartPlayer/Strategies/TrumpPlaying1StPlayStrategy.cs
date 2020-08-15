namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;
    using System.Threading;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrumpPlaying1StPlayStrategy : IPlayStrategy
    {
        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            //// if (context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition)
            ////     && context.AvailableCardsToPlay.HasAnyOfSuit(context.CurrentContract.Type.ToCardSuit()))
            //// {
            ////     Interlocked.Increment(ref GlobalCounters.Counters[1]);
            ////     return new PlayCardAction(
            ////         context.AvailableCardsToPlay.Where(x => x.Suit == context.CurrentContract.Type.ToCardSuit())
            ////             .OrderByDescending(x => x.GetValue(context.CurrentContract.Type)).FirstOrDefault());
            //// }

            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                    .FirstOrDefault());
        }
    }
}
