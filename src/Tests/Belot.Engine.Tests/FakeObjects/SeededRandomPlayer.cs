namespace Belot.Engine.Tests.FakeObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Deterministic pseudo-random player: plays a uniformly chosen card from the legal set and
    /// declares the offered announces on a seeded coin flip. Reproducible per seed, so suites
    /// built on it can never be flaky.
    /// </summary>
    public class SeededRandomPlayer : IPlayer
    {
        private static readonly BidType[] SingleBids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
            BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble,
        };

        private readonly Random random;
        private readonly bool bidRandomly;

        public SeededRandomPlayer(int seed, bool bidRandomly = false)
        {
            this.random = new Random(seed);
            this.bidRandomly = bidRandomly;
        }

        /// <summary>Gets how many times EndOfGame was called on this player.</summary>
        public int EndOfGameCalls { get; private set; }

        public GameResult LastGameResult { get; private set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            if (!this.bidRandomly)
            {
                return BidType.Pass;
            }

            // Pass is weighted so auctions stay short but contracts still happen regularly.
            var options = new List<BidType> { BidType.Pass, BidType.Pass, BidType.Pass };
            foreach (var bid in SingleBids)
            {
                if (context.AvailableBids.HasFlag(bid))
                {
                    options.Add(bid);
                }
            }

            return options[this.random.Next(options.Count)];
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) =>
            this.random.Next(2) == 0
                ? context.AvailableAnnounces
                : new List<Announce>();

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var index = this.random.Next(context.AvailableCardsToPlay.Count);
            return new PlayCardAction(context.AvailableCardsToPlay.Skip(index).First());
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
            this.EndOfGameCalls++;
            this.LastGameResult = gameResult;
        }
    }
}
