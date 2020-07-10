namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class TrumpPlayingLastPlayStrategy : IPlayStrategy
    {
        private readonly TrickWinnerService trickWinnerService;

        public TrumpPlayingLastPlayStrategy(TrickWinnerService trickWinnerService)
        {
            this.trickWinnerService = trickWinnerService;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var winner = this.trickWinnerService.GetWinner(context.CurrentContract, context.CurrentTrickActions.ToList());
            if (winner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(x => x.Suit != context.CurrentContract.Type.ToCardSuit() && x.Type != CardType.Ace))
            {
                return new PlayCardAction(
                    context.AvailableCardsToPlay.Where(x => x.Suit != context.CurrentContract.Type.ToCardSuit() && x.Type != CardType.Ace)
                        .OrderByDescending(x => x.GetValue(context.CurrentContract.Type)).FirstOrDefault());
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type))
                    .FirstOrDefault());
        }
    }
}
