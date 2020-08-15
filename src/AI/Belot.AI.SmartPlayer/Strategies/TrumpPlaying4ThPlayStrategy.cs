namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class TrumpPlaying4ThPlayStrategy : IPlayStrategy
    {
        private readonly TrickWinnerService trickWinnerService;

        public TrumpPlaying4ThPlayStrategy(TrickWinnerService trickWinnerService)
        {
            this.trickWinnerService = trickWinnerService;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var winner = this.trickWinnerService.GetWinner(context.CurrentContract, context.CurrentTrickActions.ToList());
            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            if (winner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(
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
