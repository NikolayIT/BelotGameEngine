namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;

    public interface IPlayer
    {
        BidType GetBid(PlayerGetBidContext context);

        IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context);

        PlayCardAction PlayCard(PlayerPlayCardContext context);

        void EndOfTrick(IEnumerable<PlayCardAction> trickActions);

        void EndOfRound(RoundResult roundResult);

        void EndOfGame(GameResult gameResult);
    }
}
