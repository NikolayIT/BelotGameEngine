namespace Belot.AI.DummyPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class RandomPlayer : IPlayer
    {
        private readonly List<BidType> allBids = new List<BidType>
                                                     {
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
            return ThreadSafeRandom.Next(0, 2) == 0
                       ? BidType.Pass // In 50% of the cases announce Pass
                       : this.allBids.Where(x => context.AvailableBids.HasFlag(x)).RandomElement();
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return new PlayCardAction(context.AvailableCardsToPlay.RandomElement());
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
