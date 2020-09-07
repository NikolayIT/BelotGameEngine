namespace Belot.Engine.Tests.FakeObjects
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class FakePlayer : IPlayer
    {
        private readonly Queue<BidType> bidTypes = new Queue<BidType>();
        private readonly Card fakeCard;
        private readonly Announce fakeAnnounce;

        public FakePlayer(params BidType[] bidTypes)
        {
            foreach (var element in bidTypes)
            {
                this.bidTypes.Enqueue(element);
            }
        }

        public FakePlayer(Card fakeCard, params BidType[] bidTypes)
            : this(bidTypes)
        {
            this.fakeCard = fakeCard;
        }

        public FakePlayer(Announce fakeAnnounce, params BidType[] bidTypes)
            : this(bidTypes)
        {
            this.fakeAnnounce = fakeAnnounce;
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.bidTypes.TryDequeue(out BidType bidType);

            return bidType;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            if (this.fakeAnnounce != null)
            {
                return new List<Announce>
                {
                    this.fakeAnnounce,
                };
            }

            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            Card playCardAction;

            if (this.fakeCard != null)
            {
                return new PlayCardAction(this.fakeCard);
            }

            if (context.CurrentContract.Type == BidType.AllTrumps ||
                context.CurrentContract.Type == (BidType.AllTrumps | BidType.Double) ||
                context.CurrentContract.Type == (BidType.AllTrumps | BidType.ReDouble))
            {
                playCardAction = context.AvailableCardsToPlay
                    .OrderByDescending(x => x.TrumpOrder)
                    .FirstOrDefault();
            }
            else if (context.CurrentContract.Type == BidType.NoTrumps ||
                     context.CurrentContract.Type == (BidType.NoTrumps | BidType.Double) ||
                     context.CurrentContract.Type == (BidType.NoTrumps | BidType.ReDouble))
            {
                playCardAction = context.AvailableCardsToPlay
                    .OrderByDescending(x => x.NoTrumpOrder)
                    .FirstOrDefault();
            }
            else
            {
                playCardAction = context.AvailableCardsToPlay
                    .OrderByDescending(x => x.Suit)
                    .FirstOrDefault();
            }

            return new PlayCardAction(playCardAction);
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
