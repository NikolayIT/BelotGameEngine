namespace Belot.GamesSimulator
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
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.MyPosition}]: {context.CurrentContract.Type,-9} | "
                + $"Available bids: {context.AvailableBids} => {bid}");
            Console.ResetColor();
            return bid;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            var announces = this.player.GetAnnounces(context);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.MyPosition}]: {context.CurrentContract.Type,-9} | "
                + $"{string.Join(" ", context.MyCards),-27} "
                + $"Actions: {string.Join(" ", context.CurrentTrickActions.Select(x => x.Card)),-11} "
                + $"Available announces: {string.Join(" ", string.Join(",", context.AvailableAnnounces.Select(x => $"{x.AnnounceType}({x.Card})")))} "
                + $"=> {string.Join(",", context.AvailableAnnounces.Select(x => $"{x.AnnounceType}({x.Card})"))}");
            Console.ResetColor();
            return announces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var cardAction = this.player.PlayCard(context);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.MyPosition}]: {context.CurrentContract.Type,-9} | "
                + $"{string.Join(" ", context.MyCards),-27} "
                + $"Actions: {string.Join(" ", context.CurrentTrickActions.Select(x => x.Card)),-11} "
                + $"Playable: {string.Join(" ", context.AvailableCardsToPlay),-27} "
                + $"=> {cardAction.Card}");
            Console.ResetColor();
            return cardAction;
        }
    }
}
