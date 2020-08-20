namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class TrumpOursContractStrategy : IPlayStrategy
    {
        public PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards)
        {
            //// if (context.AvailableCardsToPlay.HasAnyOfSuit(context.CurrentContract.Type.ToCardSuit()))
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

        public PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                    .FirstOrDefault());
        }

        public PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                    .FirstOrDefault());
        }

        public PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner)
        {
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (trickWinner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(
                    x => x.Suit != trumpSuit && x.Type != CardType.Ace))
            {
                return new PlayCardAction(
                    context.AvailableCardsToPlay.Where(x => x.Suit != trumpSuit && x.Type != CardType.Ace)
                        .OrderByDescending(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                        .FirstOrDefault());
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder)
                    .FirstOrDefault());
        }
    }
}
