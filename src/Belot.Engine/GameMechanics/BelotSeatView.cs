namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// What one seat may see of a <see cref="BelotMatch"/>: its own hand and options, and the
    /// public state (never another seat's cards, nor which cards make another seat's declared
    /// combinations before the deal is over). A plain model: its public properties are all there
    /// is, so a copy built from them works the same (a host can map it to its own model and
    /// back). Seats are absolute; <see cref="Seat"/> says whose view this is.
    /// </summary>
    public sealed class BelotSeatView
    {
        /// <summary>Gets or sets the seat this view belongs to (<see cref="PlayerPosition.Unknown"/> for the final view).</summary>
        public PlayerPosition Seat { get; set; }

        /// <summary>Gets or sets who must decide now (<see cref="PlayerPosition.Unknown"/> after the match).</summary>
        public PlayerPosition ToMove { get; set; }

        /// <summary>Gets or sets what the seat to move decides.</summary>
        public BelotDecision Decision { get; set; }

        public bool IsMatchFinished { get; set; }

        /// <summary>Gets or sets the winning team once the match is over (<see cref="PlayerPosition.Unknown"/> for a stopped one).</summary>
        public PlayerPosition MatchWinner { get; set; }

        /// <summary>Gets or sets South-North's game points before this deal.</summary>
        public int SouthNorthPoints { get; set; }

        /// <summary>Gets or sets East-West's game points before this deal.</summary>
        public int EastWestPoints { get; set; }

        /// <summary>Gets or sets the points hanging from earlier deals.</summary>
        public int HangingPoints { get; set; }

        /// <summary>Gets or sets the 1-based number of the deal shown (after the match: the last one).</summary>
        public int RoundNumber { get; set; }

        /// <summary>Gets or sets who bid and led first in this deal.</summary>
        public PlayerPosition FirstToPlayInTheRound { get; set; }

        /// <summary>Gets or sets the results of the finished deals, in order.</summary>
        public IReadOnlyList<BelotRoundSummary> PreviousRounds { get; set; }

        /// <summary>Gets or sets this deal's auction so far, in order.</summary>
        public IReadOnlyList<Bid> Bids { get; set; }

        /// <summary>Gets or sets the contract so far: its declarer and type (Pass while nobody has bid).</summary>
        public Bid Contract { get; set; }

        /// <summary>Gets or sets the combinations and belotes declared in this deal.</summary>
        public IReadOnlyList<BelotAnnounce> Announces { get; set; }

        /// <summary>Gets or sets this deal's finished tricks, in order.</summary>
        public IReadOnlyList<BelotTrick> Tricks { get; set; }

        /// <summary>Gets or sets the cards of the trick in progress.</summary>
        public IReadOnlyList<BelotPlayedCard> CurrentTrick { get; set; }

        /// <summary>Gets or sets the most recently finished trick of the match (possibly the last of the previous deal), or null.</summary>
        public BelotTrick LastTrick { get; set; }

        /// <summary>Gets or sets how many cards each seat holds, by seat index (South, East, North, West).</summary>
        public IReadOnlyList<int> CardCounts { get; set; }

        /// <summary>Gets or sets this seat's cards (empty for the final view).</summary>
        public IReadOnlyList<Card> Hand { get; set; }

        /// <summary>Gets or sets the bids open to this seat; Pass unless it bids now.</summary>
        public BidType AvailableBids { get; set; }

        /// <summary>Gets or sets the combinations this seat may declare; empty unless it declares now.</summary>
        public IReadOnlyList<BelotAnnounce> AvailableAnnounces { get; set; }

        /// <summary>Gets or sets the cards this seat may play; empty unless it plays now.</summary>
        public IReadOnlyList<Card> PlayableCards { get; set; }

        /// <summary>Gets or sets the playable cards that would make a belote.</summary>
        public IReadOnlyList<Card> BeloteCards { get; set; }

        /// <summary>Gets or sets the full record of the match, hidden cards included; only in the final view.</summary>
        public BelotMatchRecord Record { get; set; }

        /// <summary>
        /// Creates what <see cref="IPlayer.GetBid"/> would receive for this seat, so a bot can
        /// decide from the view alone.
        /// </summary>
        /// <returns>A new context.</returns>
        public PlayerGetBidContext CreateBidContext()
        {
            this.EnsureDecision(BelotDecision.Bid);
            var context = new PlayerGetBidContext { AvailableBids = this.AvailableBids };
            this.FillBase(context);
            return context;
        }

        /// <summary>Creates what <see cref="IPlayer.GetAnnounces"/> would receive for this seat.</summary>
        /// <returns>A new context.</returns>
        public PlayerGetAnnouncesContext CreateAnnouncesContext()
        {
            this.EnsureDecision(BelotDecision.Announce);
            var context = new PlayerGetAnnouncesContext
            {
                Announces = ToAnnounces(this.Announces),
                CurrentTrickActions = ToActions(this.CurrentTrick, this.Tricks.Count + 1),
                AvailableAnnounces = ToAnnounces(this.AvailableAnnounces),
            };
            this.FillBase(context);
            return context;
        }

        /// <summary>Creates what <see cref="IPlayer.PlayCard"/> would receive for this seat.</summary>
        /// <returns>A new context.</returns>
        public PlayerPlayCardContext CreatePlayCardContext()
        {
            this.EnsureDecision(BelotDecision.PlayCard);
            var roundActions = new List<PlayCardAction>(32);
            for (var i = 0; i < this.Tricks.Count; i++)
            {
                roundActions.AddRange(ToActions(this.Tricks[i].Cards, i + 1));
            }

            roundActions.AddRange(ToActions(this.CurrentTrick, this.Tricks.Count + 1));
            var available = new CardCollection();
            foreach (var card in this.PlayableCards)
            {
                available.Add(card);
            }

            var context = new PlayerPlayCardContext
            {
                Announces = ToAnnounces(this.Announces),
                CurrentTrickActions = ToActions(this.CurrentTrick, this.Tricks.Count + 1),
                RoundActions = roundActions,
                AvailableCardsToPlay = available,
                CurrentTrickNumber = this.Tricks.Count + 1,
            };
            this.FillBase(context);
            return context;
        }

        private static List<Announce> ToAnnounces(IReadOnlyList<BelotAnnounce> announces)
        {
            var list = new List<Announce>(announces.Count);
            foreach (var announce in announces)
            {
                list.Add(new Announce(announce.Type, announce.Card)
                {
                    Player = announce.Player,
                    IsActive = announce.Type == AnnounceType.Belot ? true : announce.IsScored,
                });
            }

            return list;
        }

        private static List<PlayCardAction> ToActions(IReadOnlyList<BelotPlayedCard> cards, int trickNumber)
        {
            var list = new List<PlayCardAction>(4);
            foreach (var card in cards)
            {
                list.Add(new PlayCardAction(card.Card, card.Belote) { Player = card.Player, TrickNumber = trickNumber });
            }

            return list;
        }

        private void EnsureDecision(BelotDecision decision)
        {
            if (this.Seat == PlayerPosition.Unknown || this.Seat != this.ToMove || this.Decision != decision)
            {
                throw new InvalidOperationException($"This view's seat has no {decision} decision to make now.");
            }
        }

        private void FillBase(BasePlayerContext context)
        {
            context.RoundNumber = this.RoundNumber;
            context.FirstToPlayInTheRound = this.FirstToPlayInTheRound;
            context.MyPosition = this.Seat;
            context.SouthNorthPoints = this.SouthNorthPoints;
            context.EastWestPoints = this.EastWestPoints;
            context.HangingPoints = this.HangingPoints;
            var cards = new CardCollection();
            foreach (var card in this.Hand)
            {
                cards.Add(card);
            }

            context.MyCards = cards;
            var bids = new List<Bid>(this.Bids.Count);
            foreach (var bid in this.Bids)
            {
                bids.Add(new Bid(bid.Player, bid.Type));
            }

            context.Bids = bids;
            context.CurrentContract = new Bid(this.Contract.Player, this.Contract.Type);
        }
    }
}
