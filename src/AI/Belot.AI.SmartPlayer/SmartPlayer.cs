namespace Belot.AI.SmartPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class SmartPlayer : IPlayer
    {
        public BidType GetBid(PlayerGetBidContext context)
        {
            return context.AvailableBids.Where(x => x != BidType.Double && x != BidType.ReDouble).ToList()
                .RandomElement();
        }

        public IEnumerable<AnnounceType> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return new PlayCardAction(context.AvailableCardsToPlay.RandomElement());
        }
    }
}
