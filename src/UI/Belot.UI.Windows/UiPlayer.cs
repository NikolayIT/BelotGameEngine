namespace Belot.UI.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class UiPlayer : IPlayer
    {
        private const int WaitTimeForAction = 50;

        public event EventHandler<PlayerGetBidContext> InfoChangedInGetBid;

        public event EventHandler<PlayerGetAnnouncesContext> InfoChangedInGetAnnounces;

        public event EventHandler<PlayerPlayCardContext> InfoChangedInPlayCard;

        public BidType? GetBidAction { get; set; }

        public IList<Announce> GetAnnouncesAction { get; set; }

        public PlayCardAction PlayCardAction { get; set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.InfoChangedInGetBid?.Invoke(this, context);
            while (this.GetBidAction == null)
            {
                Task.Delay(WaitTimeForAction);
            }

            var returnAction = this.GetBidAction.Value;
            this.GetBidAction = null;
            return returnAction;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            this.InfoChangedInGetAnnounces?.Invoke(this, context);
            return context.AvailableAnnounces;

            /*
            while (this.GetAnnouncesAction == null)
            {
                Task.Delay(WaitTimeForAction);
            }

            var returnAction = this.GetAnnouncesAction;
            this.GetAnnouncesAction = null;
            return returnAction;
            */
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.InfoChangedInPlayCard?.Invoke(this, context);
            while (this.PlayCardAction == null || !context.AvailableCardsToPlay.Contains(this.PlayCardAction.Card))
            {
                Task.Delay(WaitTimeForAction);
            }

            var returnAction = this.PlayCardAction;
            this.PlayCardAction = null;
            return returnAction;
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
