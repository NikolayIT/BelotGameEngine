namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrumpPlaying3RdPlayStrategy : IPlayStrategy
    {
        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                    .FirstOrDefault());
        }
    }
}
