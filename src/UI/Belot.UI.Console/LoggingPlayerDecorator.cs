namespace Belot.UI.Console
{
    using System;
    using System.Collections.Generic;

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
            Console.WriteLine($"[#{context.RoundNumber}][{context.MyPosition}]: ({context.CurrentContract}) Available bids: {context.AvailableBids} => {bid}");
            return bid;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return this.player.GetAnnounces(context);
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return this.player.PlayCard(context);
        }
    }
}
