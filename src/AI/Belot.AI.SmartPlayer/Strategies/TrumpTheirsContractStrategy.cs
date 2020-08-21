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
