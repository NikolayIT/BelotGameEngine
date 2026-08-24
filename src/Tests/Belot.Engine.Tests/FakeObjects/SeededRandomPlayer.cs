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
        private readonly Random random;

        public SeededRandomPlayer(int seed)
        {
            this.random = new Random(seed);
        }

        public BidType GetBid(PlayerGetBidContext context) => BidType.Pass;

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
        }
    }
}
