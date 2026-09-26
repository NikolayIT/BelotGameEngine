namespace Belot.AI.ClaudePlayer.Tests.TestHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// A player for driving deals: plays its scripted cards in order while they are legal, then
    /// a random legal card (or the inner player's choice), declares every combination and every
    /// belote (except for the cards listed), and lets the test look at each decision's context
    /// first.
    /// </summary>
    public class TestPlayer : IPlayer
    {
        private readonly Queue<Card> script;
        private readonly Random random;
        private readonly IPlayer inner;

        public TestPlayer(Random random, string script = "", IPlayer inner = null)
        {
            this.random = random;
            this.script = new Queue<Card>(Deal.Cards(script));
            this.inner = inner;
        }

        /// <summary>Gets or sets what to do with each card decision's context, before deciding.</summary>
        public Action<PlayerPlayCardContext> OnDecision { get; set; }

        /// <summary>Gets the cards played without claiming the belote.</summary>
        public ISet<Card> WithoutBelote { get; } = new HashSet<Card>();

        /// <summary>Gets the legal cards offered, by the number of cards played before the decision.</summary>
        public IDictionary<int, uint> Offered { get; } = new Dictionary<int, uint>();

        public IList<PlayCardAction> RoundActions { get; private set; }

        public BidType GetBid(PlayerGetBidContext context) => this.inner?.GetBid(context) ?? BidType.Pass;

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.OnDecision?.Invoke(context);
            this.RoundActions = (IList<PlayCardAction>)context.RoundActions;
            this.Offered[this.RoundActions.Count] = Deal.Mask(context.AvailableCardsToPlay);
            while (this.script.Count > 0 && !context.MyCards.Contains(this.script.Peek()))
            {
                // Played already, by the engine when it was the only legal card.
                this.script.Dequeue();
            }

            Card card;
            if (this.script.Count > 0 && context.AvailableCardsToPlay.Contains(this.script.Peek()))
            {
                card = this.script.Dequeue();
            }
            else if (this.inner != null)
            {
                card = this.inner.PlayCard(context).Card;
            }
            else
            {
                card = context.AvailableCardsToPlay.Skip(this.random.Next(context.AvailableCardsToPlay.Count)).First();
            }

            return new PlayCardAction(card, !this.WithoutBelote.Contains(card));
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
