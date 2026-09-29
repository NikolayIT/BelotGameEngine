namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// The eight tricks of one deal as a step-by-step state machine: it never asks anybody for
    /// anything. Whoever drives it reads <see cref="ToMove"/> and <see cref="Decision"/>: in the
    /// first trick a seat with combinations to declare first gets <see cref="AnnounceContext"/>
    /// (what GetAnnounces receives) and answers with <see cref="ApplyAnnounces"/>; then, unless
    /// only one card is legal (played for it, without a belote), it gets <see cref="PlayContext"/>
    /// (what PlayCard receives) and answers with <see cref="TryPlayCard"/>. The observers get
    /// EndOfTrick after every trick.
    /// </summary>
    internal sealed class TrickPlay
    {
        private static readonly TrickWinnerService TrickWinnerService = new TrickWinnerService();

        private static readonly ValidCardsService ValidCardsService = new ValidCardsService();

        private static readonly ValidAnnouncesService ValidAnnouncesService = new ValidAnnouncesService();

        private readonly IPlayer[] observers;

        private readonly IReadOnlyList<CardCollection> playerCards;

        private readonly Bid contract;

        private readonly PlayerPosition manualCardPlaySeats;

        private readonly List<Announce> announces;

        private readonly List<PlayCardAction> actions;

        private readonly List<PlayCardAction> trickActions;

        private readonly PlayerGetAnnouncesContext announceContext;

        private readonly PlayerPlayCardContext playContext;

        private readonly CardCollection southNorthTricks;

        private readonly CardCollection eastWestTricks;

        private readonly List<PlayerPosition> trickWinners = new List<PlayerPosition>(8);

        private PlayerPosition current;

        private int trickNumber;

        // Whether the seat to play has had its chance to declare in this turn.
        private bool announced;

        private CardCollection availableCards;

        public TrickPlay(
            IPlayer[] observers,
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            IReadOnlyList<CardCollection> playerCards,
            IList<Bid> bids,
            Bid contract,
            int hangingPoints = 0,
            PlayerPosition manualCardPlaySeats = PlayerPosition.Unknown)
        {
            this.observers = observers;
            this.playerCards = playerCards;
            this.contract = contract;
            this.manualCardPlaySeats = manualCardPlaySeats;
            this.announces = new List<Announce>(12);
            this.southNorthTricks = new CardCollection();
            this.eastWestTricks = new CardCollection();
            this.LastTrickWinner = firstToPlay;
            this.actions = new List<PlayCardAction>(8 * 4);
            this.trickActions = new List<PlayCardAction>(4);
            this.announceContext = new PlayerGetAnnouncesContext
            {
                RoundNumber = roundNumber,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                HangingPoints = hangingPoints,
                FirstToPlayInTheRound = firstToPlay,
                Bids = bids,
                CurrentContract = contract,
                CurrentTrickActions = this.trickActions,
                Announces = this.announces,
            };
            this.playContext = new PlayerPlayCardContext
            {
                RoundNumber = roundNumber,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                HangingPoints = hangingPoints,
                FirstToPlayInTheRound = firstToPlay,
                CurrentContract = contract,
                Bids = bids,
                Announces = this.announces,
                RoundActions = this.actions,
                CurrentTrickActions = this.trickActions,
            };

            this.current = firstToPlay;
            this.StartTrick(1);
            this.MoveToTheNextDecision();
        }

        public bool IsFinished { get; private set; }

        /// <summary>Gets the seat to decide, or <see cref="PlayerPosition.Unknown"/> once the tricks are over.</summary>
        public PlayerPosition ToMove => this.IsFinished ? PlayerPosition.Unknown : this.current;

        /// <summary>Gets what the seat to move decides: <see cref="BelotDecision.Announce"/> or <see cref="BelotDecision.PlayCard"/>.</summary>
        public BelotDecision Decision { get; private set; }

        /// <summary>Gets the context of an announce decision. Live.</summary>
        public PlayerGetAnnouncesContext AnnounceContext => this.announceContext;

        /// <summary>Gets the context of a card decision. Live.</summary>
        public PlayerPlayCardContext PlayContext => this.playContext;

        /// <summary>Gets the combinations and belotes declared so far. Live.</summary>
        public List<Announce> Announces => this.announces;

        /// <summary>Gets every card played so far, in order. Live.</summary>
        public List<PlayCardAction> Actions => this.actions;

        /// <summary>Gets the cards of the trick in progress. Live.</summary>
        public List<PlayCardAction> TrickActions => this.trickActions;

        public int TrickNumber => this.trickNumber;

        public CardCollection SouthNorthTricks => this.southNorthTricks;

        public CardCollection EastWestTricks => this.eastWestTricks;

        /// <summary>Gets the winners of the finished tricks, in order.</summary>
        public List<PlayerPosition> TrickWinners => this.trickWinners;

        /// <summary>Gets the winner of the last trick (the first to play until then).</summary>
        public PlayerPosition LastTrickWinner { get; private set; }

        /// <summary>
        /// Declares the combinations of the seat to move. A card may take part in only one
        /// combination, so of the declarations sharing a card only the first counts; anything
        /// not offered is ignored.
        /// </summary>
        public void ApplyAnnounces(IEnumerable<Announce> declared)
        {
            // Players may return the AvailableAnnounces list itself: snapshot it first.
            var playerAnnounces = declared.ToArray();
            var availableAnnounces = this.announceContext.AvailableAnnounces;
            var declaredCards = 0u;
            for (var i = 0; i < playerAnnounces.Length; i++)
            {
                var playerAnnounce = playerAnnounces[i];
                var isAvailable = false;
                for (var j = 0; j < availableAnnounces.Count; j++)
                {
                    if (availableAnnounces[j].Type == playerAnnounce.Type
                        && availableAnnounces[j].Card == playerAnnounce.Card)
                    {
                        isAvailable = true;
                        break;
                    }
                }

                if (!isAvailable)
                {
                    continue;
                }

                var cards = ValidAnnouncesService.GetCardsBitMask(playerAnnounce);
                if ((declaredCards & cards) != 0)
                {
                    continue;
                }

                declaredCards |= cards;
                playerAnnounce.Player = this.current;
                this.announces.Add(playerAnnounce);
            }

            this.MoveToTheNextDecision();
        }

        /// <summary>Whether the seat to move may play the card now.</summary>
        public bool CanPlay(Card card) => card != null && this.availableCards.Contains(card);

        /// <summary>
        /// Plays the card of the seat to move; false, changing nothing, when it is not legal. A
        /// belote claimed where none is allowed is dropped (the action's Belote becomes false).
        /// </summary>
        public bool TryPlayCard(PlayCardAction action)
        {
            if (!this.CanPlay(action.Card))
            {
                return false;
            }

            var cards = this.playerCards[this.current.Index()];
            if (action.Belote)
            {
                if (ValidAnnouncesService.IsBeloteAllowed(cards, this.contract.Type, this.trickActions, action.Card))
                {
                    // Belote may be declared in any trick, after UpdateActiveAnnounces (which runs
                    // once, before trick 2) has already passed, so it is activated at creation. It
                    // never competes with other announces.
                    this.announces.Add(
                        new Announce(AnnounceType.Belot, action.Card)
                        {
                            Player = this.current,
                            IsActive = true,
                        });
                }
                else
                {
                    action.Belote = false;
                }
            }

            this.Play(action);
            this.MoveToTheNextDecision();
            return true;
        }

        // Plays the card (already checked) and, after the fourth, finishes the trick.
        private void Play(PlayCardAction action)
        {
            this.playerCards[this.current.Index()].Remove(action.Card);
            action.Player = this.current;
            action.TrickNumber = this.trickNumber;
            this.actions.Add(action);
            this.trickActions.Add(action);
            this.current = this.current.Next();
            this.announced = false;
            if (this.trickActions.Count < 4)
            {
                return;
            }

            var winner = TrickWinnerService.GetWinner(this.contract, this.trickActions);
            this.trickWinners.Add(winner);
            var tricks = winner == PlayerPosition.South || winner == PlayerPosition.North
                ? this.southNorthTricks
                : this.eastWestTricks;
            tricks.Add(this.trickActions[0].Card);
            tricks.Add(this.trickActions[1].Card);
            tricks.Add(this.trickActions[2].Card);
            tricks.Add(this.trickActions[3].Card);
            if (this.trickNumber == 8)
            {
                this.LastTrickWinner = winner;
            }

            // The player that wins the trick plays first
            this.current = winner;
            for (var i = 0; i < 4; i++)
            {
                this.observers[i]?.EndOfTrick(this.trickActions);
            }

            if (this.trickNumber == 8)
            {
                this.IsFinished = true;
                this.Decision = BelotDecision.None;
                return;
            }

            this.StartTrick(this.trickNumber + 1);
        }

        private void StartTrick(int number)
        {
            this.trickNumber = number;
            this.trickActions.Clear();
            if (number == 2)
            {
                ValidAnnouncesService.UpdateActiveAnnounces(this.announces);
            }

            this.playContext.CurrentTrickNumber = number;
        }

        // Goes on (declaring nothing for a seat without combinations, playing a forced card) up
        // to the next decision somebody has to make, or to the end of the tricks.
        private void MoveToTheNextDecision()
        {
            while (!this.IsFinished)
            {
                var index = this.current.Index();
                if (!this.announced)
                {
                    this.announced = true;
                    if (this.trickNumber == 1 && !this.contract.Type.HasFlag(BidType.NoTrumps))
                    {
                        var availableAnnounces = ValidAnnouncesService.GetAvailableAnnounces(this.playerCards[index]);
                        if (availableAnnounces.Count > 0)
                        {
                            this.announceContext.MyPosition = this.current;
                            this.announceContext.MyCards = this.playerCards[index];
                            this.announceContext.AvailableAnnounces = availableAnnounces;
                            this.Decision = BelotDecision.Announce;
                            return;
                        }
                    }
                }

                this.availableCards = ValidCardsService.GetValidCards(this.playerCards[index], this.contract.Type, this.trickActions);
                if (this.availableCards.Count > 1 || (this.manualCardPlaySeats & this.current) != PlayerPosition.Unknown)
                {
                    this.playContext.MyPosition = this.current;
                    this.playContext.MyCards = this.playerCards[index];
                    this.playContext.AvailableCardsToPlay = this.availableCards;
                    this.Decision = BelotDecision.PlayCard;
                    return;
                }

                // Only 1 card is available. Play it. Belote is not available in this situation.
                this.Play(new PlayCardAction(this.availableCards.FirstOrDefault(), false));
            }
        }
    }
}
