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

        public BidType GetBid(PlayerGetBidContext context) =>
            this.bids.Count > 0 ? this.bids.Dequeue() : BidType.Pass;

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) =>
            this.ReturnAvailableAnnounces ? context.AvailableAnnounces : new List<Announce>();

        public PlayCardAction PlayCard(PlayerPlayCardContext context) =>
            new PlayCardAction(this.cards.Dequeue());

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
