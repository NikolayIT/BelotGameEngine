namespace Belot.AI.SmartPlayer.Strategies
{
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class NoTrumpsPlaying4ThPlayStrategy : IPlayStrategy
    {
        private readonly TrickWinnerService trickWinnerService;

        public NoTrumpsPlaying4ThPlayStrategy(TrickWinnerService trickWinnerService)
        {
            this.trickWinnerService = trickWinnerService;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards)
        {
            var winner = this.trickWinnerService.GetWinner(context.CurrentContract, context.CurrentTrickActions.ToList());
            if (winner.IsInSameTeamWith(context.MyPosition) && context.AvailableCardsToPlay.Any(x => x.Type != CardType.Ace && x.Type != CardType.Ten))
            {
                return new PlayCardAction(
                    context.AvailableCardsToPlay.Where(x => x.Type != CardType.Ace && x.Type != CardType.Ten)
                        .OrderByDescending(x => x.GetValue(context.CurrentContract.Type)).FirstOrDefault());
            }

            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type))
                    .FirstOrDefault());
        }
    }
}
