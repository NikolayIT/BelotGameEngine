namespace Belot.Engine.Tests.FakeObjects
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class FakePlayer : IPlayer
    {
        private readonly Queue<BidType> bidTypes = new Queue<BidType>();

        public FakePlayer(params BidType[] bidTypes)
        {
            foreach (var element in bidTypes)
            {
                this.bidTypes.Enqueue(element);
            }
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.bidTypes.TryDequeue(out BidType bidType);

            return bidType;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
            => throw new NotImplementedException();

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
            => throw new NotImplementedException();

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            => throw new NotImplementedException();

        public void EndOfRound(RoundResult roundResult)
            => throw new NotImplementedException();

        public void EndOfGame(GameResult gameResult)
            => throw new NotImplementedException();
    }
}