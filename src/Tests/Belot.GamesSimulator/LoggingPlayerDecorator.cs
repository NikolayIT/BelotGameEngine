namespace Belot.GamesSimulator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class LoggingPlayerDecorator : IPlayer
    {
        private readonly IPlayer player;

        private readonly ConsoleColor color;

        public LoggingPlayerDecorator(IPlayer player, ConsoleColor color)
        {
            this.player = player;
            this.color = color;
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var bid = this.player.GetBid(context);
            Console.ForegroundColor = this.color;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.SouthNorthPoints}-{context.EastWestPoints}][-][{context.MyPosition,-5}]: {context.CurrentContract.Type,-9} | "
                + $"{string.Join(" ", context.MyCards),-18} "
                + $"Available bids: {context.AvailableBids} => {bid}");
            Console.ResetColor();
            return bid;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            var announces = this.player.GetAnnounces(context);
            Console.ForegroundColor = this.color;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.SouthNorthPoints}-{context.EastWestPoints}][-][{context.MyPosition,-5}]: "
                + $"{string.Join(" ", context.MyCards),-27} "
                + $"Actions: {string.Join(" ", context.CurrentTrickActions.Select(x => x.Card)),-11} "
                + $"Available announces: {string.Join(" ", string.Join(",", context.AvailableAnnounces))} "
                + $"=> {string.Join(",", context.AvailableAnnounces)}");
            Console.ResetColor();
            return announces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var cardAction = this.player.PlayCard(context);
            Console.ForegroundColor = this.color;
            Console.WriteLine(
                $"[#{context.RoundNumber,-2}][{context.SouthNorthPoints}-{context.EastWestPoints}][{context.CurrentTrickNumber}][{context.MyPosition,-5}]: "
                + $"{string.Join(" ", context.MyCards),-27} "
                + $"Actions: {string.Join(" ", context.CurrentTrickActions.Select(x => x.Card)),-11} "
                + $"Playable: {string.Join(" ", context.AvailableCardsToPlay),-27} "
                + $"=> {cardAction.Card}");
            Console.ResetColor();
            return cardAction;
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
            Console.WriteLine(new string('-', Program.LineLength));
        }

        public void EndOfGame(GameResult gameResult)
        {
            Console.WriteLine(new string('=', Program.LineLength));
        }
    }
}
