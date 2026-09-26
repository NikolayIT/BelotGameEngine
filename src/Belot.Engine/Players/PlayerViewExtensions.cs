namespace Belot.Engine.Players
{
    using System;

    using Belot.Engine.GameMechanics;

    public static class PlayerViewExtensions
    {
        /// <summary>
        /// Asks the player for the decision the view's seat has to make, with the context rebuilt
        /// from the view alone (see <see cref="BelotSeatView.CreateBidContext"/> and the others),
        /// so a server needs to keep no bot between decisions. That suits a player that decides
        /// from its context (every bot in this repository), not one that remembers what its
        /// other callbacks told it.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="view">The view of the seat to move (see <see cref="BelotMatch.GetView"/>).</param>
        /// <returns>The decision, ready for <see cref="BelotMatch.Act"/>.</returns>
        public static BelotAction Decide(this IPlayer player, BelotSeatView view)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            switch (view.Decision)
            {
                case BelotDecision.Bid:
                    return BelotAction.Bid(player.GetBid(view.CreateBidContext()));
                case BelotDecision.Announce:
                    return BelotAction.Declare(player.GetAnnounces(view.CreateAnnouncesContext()));
                case BelotDecision.PlayCard:
                    var action = player.PlayCard(view.CreatePlayCardContext());
                    return BelotAction.PlayCard(action.Card, action.Belote);
                default:
                    throw new InvalidOperationException("Nobody has a decision to make.");
            }
        }
    }
}
