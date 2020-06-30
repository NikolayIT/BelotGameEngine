namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    public interface IPlayer
    {
        BidType GetBid(PlayerGetBidContext context);

        IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context);

        PlayCardAction PlayCard(PlayerPlayCardContext context);
    }
}
