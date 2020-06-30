namespace Belot.AI.DummyPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class DummyPlayer : IPlayer
    {
        private readonly IList<BidType> allBids = new List<BidType>
                                                      {
                                                          BidType.Pass,
                                                          BidType.Pass,
                                                          BidType.Pass,
                                                          BidType.Pass,
                                                          BidType.Pass,
                                                          BidType.Pass,
                                                          BidType.Clubs,
                                                          BidType.Diamonds,
                                                          BidType.Hearts,
                                                          BidType.Spades,
                                                          BidType.NoTrumps,
                                                          BidType.AllTrumps,
                                                      };

        public BidType GetBid(PlayerGetBidContext context)
        {
            return this.allBids.Where(x => context.AvailableBids.HasFlag(x)).ToList().RandomElement();
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return new PlayCardAction(context.AvailableCardsToPlay.ToList().RandomElement());
        }
    }
}
