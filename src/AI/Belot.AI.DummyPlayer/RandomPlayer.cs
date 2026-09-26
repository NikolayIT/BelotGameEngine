namespace Belot.AI.DummyPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class RandomPlayer : IPlayer
    {
        private readonly Random random;

        private readonly List<BidType> allBids = new List<BidType>
                                                     {
                                                         BidType.Clubs,
                                                         BidType.Diamonds,
                                                         BidType.Hearts,
                                                         BidType.Spades,
                                                         BidType.NoTrumps,
                                                         BidType.AllTrumps,
                                                     };

        public RandomPlayer()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomPlayer"/> class.
        /// </summary>
        /// <param name="random">The random source (a seeded one replays the same choices); null
        /// for the shared per-thread one.</param>
        public RandomPlayer(Random random)
        {
            this.random = random;
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var roll = this.random?.Next(0, 100) ?? ThreadSafeRandom.Next(0, 100);
            return roll <= 75
                       ? BidType.Pass // In 75% of the cases announce Pass
                       : this.Pick(this.allBids.Where(x => context.AvailableBids.HasFlag(x)));
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return new PlayCardAction(this.Pick(context.AvailableCardsToPlay));
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

        // A uniformly chosen item, or the default when there is none.
        private T Pick<T>(IEnumerable<T> items)
        {
            if (this.random == null)
            {
                return items.RandomElement();
            }

            var list = items.ToList();
            return list.Count == 0 ? default : list[this.random.Next(list.Count)];
        }
    }
}
