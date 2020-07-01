namespace Belot.UI.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class LoggingPlayerDecorator : IPlayer
    {
        private readonly IPlayer player;

        public LoggingPlayerDecorator(IPlayer player)
        {
            this.player = player;
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var bid = this.player.GetBid(context);
            Console.WriteLine(
                $"[#{context.RoundNumber}][{context.MyPosition}]: ({context.CurrentContract.Type}) "
                + $"Available bids: {context.AvailableBids} => {bid}");
            return bid;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return this.player.GetAnnounces(context);
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var cardAction = this.player.PlayCard(context);
            Console.WriteLine(
                $"[#{context.RoundNumber}][{context.MyPosition}]: ({context.CurrentContract.Type}) "
                + $"Available: {string.Join(" ", context.AvailableCardsToPlay)} __ "
                + $"My: {string.Join(" ", context.MyCards)} __ "
                + $"Actions: {string.Join(" ", context.CurrentTrickActions.Select(x => x.Card))} __ "
                + $"=> {cardAction.Card}");
            return cardAction;
        }
    }
}
