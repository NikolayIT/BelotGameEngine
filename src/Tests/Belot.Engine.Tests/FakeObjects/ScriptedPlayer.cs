namespace Belot.Engine.Tests.FakeObjects
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Deterministic player for rules tests. Cards are played verbatim from a queue, which is
    /// only consumed when the engine actually asks (auto-played single cards don't consume).
    /// Bids come from a queue as well (Pass when exhausted). Announces are declined unless
    /// <see cref="ReturnAvailableAnnounces"/> is set, in which case the player returns the
    /// context's AvailableAnnounces list itself, exactly like SmartPlayer/DummyPlayer/RandomPlayer do.
    /// </summary>
    public class ScriptedPlayer : IPlayer
    {
        private readonly Queue<BidType> bids;
        private readonly Queue<Card> cards;

        public ScriptedPlayer(params Card[] cards)
        {
            this.bids = new Queue<BidType>();
            this.cards = new Queue<Card>(cards);
        }

        public bool ReturnAvailableAnnounces { get; set; }

        /// <summary>Gets how many times the engine actually asked this player for a card.</summary>
        public int CardAsksCount { get; private set; }

        /// <summary>Gets how many times the engine asked this player for announces.</summary>
        public int AnnounceAsksCount { get; private set; }

        /// <summary>Gets how many times EndOfTrick was called on this player.</summary>
        public int EndOfTrickCalls { get; private set; }

        public BidType GetBid(PlayerGetBidContext context) =>
            this.bids.Count > 0 ? this.bids.Dequeue() : BidType.Pass;

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            this.AnnounceAsksCount++;
            return this.ReturnAvailableAnnounces ? context.AvailableAnnounces : new List<Announce>();
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.CardAsksCount++;
            return new PlayCardAction(this.cards.Dequeue());
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
            this.EndOfTrickCalls++;
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
