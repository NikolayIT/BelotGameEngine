namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// A whole Belot game (deals until a team wins with 151 or more) that is driven from outside,
    /// one decision at a time. Nothing inside ever waits for a player: after <see cref="Start"/>,
    /// read <see cref="ToMove"/> and <see cref="Decision"/>, give that player the matching context
    /// (<see cref="CreateBidContext"/>, <see cref="CreateAnnouncesContext"/> or
    /// <see cref="CreatePlayCardContext"/>) and pass their answer to <see cref="Act"/>. The match
    /// then plays forward (passes for a seat that can only pass, plays a forced card, finishes
    /// the trick, scores the deal, deals the next one) up to the next decision. Between decisions
    /// it is just an object in memory, which is what a UI or a game server needs: no thread is
    /// held while a person thinks.
    /// <para>
    /// Optional observers (one per seat) receive every <see cref="IPlayer"/> callback that is not
    /// a decision: EndOfTrick, EndOfRound and EndOfGame, in the same order <see cref="BelotGame"/>
    /// delivers them. <see cref="BelotGame.PlayGame"/> is this loop with four <see cref="IPlayer"/>s.
    /// </para>
    /// <para>
    /// For a UI or a server: <see cref="GetView"/> is what one seat may see (show it to that
    /// player, or give it to a bot: the view rebuilds the exact contexts), <see cref="GetFinalView"/>
    /// and <see cref="GetRecord"/> the whole match once it is over, <see cref="Validate"/> checks a
    /// decision without making it and <see cref="Stop"/> ends a match early.
    /// </para>
    /// <para>Not thread-safe: drive a match from one thread at a time.</para>
    /// </summary>
    public sealed class BelotMatch
    {
        public const int PointsToWin = 151;

        private static readonly ValidAnnouncesService AnnouncesService = new ValidAnnouncesService();

        private readonly IPlayer[] observers;

        private readonly Deck deck;

        private readonly CardCollection[] hands =
        {
            new CardCollection(), new CardCollection(), new CardCollection(), new CardCollection(),
        };

        // (roundNumber, firstToPlay, southNorthPoints, eastWestPoints, hangingPoints) => result:
        // rounds scripted by tests instead of played; null in real matches.
        private readonly Func<int, PlayerPosition, int, int, int, RoundResult> scriptedRounds;

        private readonly bool recordHistory;

        private readonly List<BelotRoundRecord> finishedRounds = new List<BelotRoundRecord>();

        private Round round;

        private bool started;

        private PlayerPosition firstInRound;

        private int roundNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="BelotMatch"/> class without observers.
        /// </summary>
        /// <param name="options">The match settings; null uses the defaults.</param>
        public BelotMatch(BelotMatchOptions options = null)
            : this(null, null, null, null, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BelotMatch"/> class.
        /// </summary>
        /// <param name="southObserver">Receives South's callbacks that are not decisions; may be null.</param>
        /// <param name="eastObserver">Receives East's callbacks that are not decisions; may be null.</param>
        /// <param name="northObserver">Receives North's callbacks that are not decisions; may be null.</param>
        /// <param name="westObserver">Receives West's callbacks that are not decisions; may be null.</param>
        /// <param name="options">The match settings; null uses the defaults.</param>
        public BelotMatch(IPlayer southObserver, IPlayer eastObserver, IPlayer northObserver, IPlayer westObserver, BelotMatchOptions options = null)
            : this(
                new[] { southObserver, eastObserver, northObserver, westObserver },
                new Deck((options ?? new BelotMatchOptions()).Random),
                (options ?? new BelotMatchOptions()).FirstToPlay,
                null,
                (options ?? new BelotMatchOptions()).RecordHistory)
        {
        }

        internal BelotMatch(
            IPlayer[] observers,
            Deck deck,
            PlayerPosition firstToPlay,
            Func<int, PlayerPosition, int, int, int, RoundResult> scriptedRounds,
            bool recordHistory = false)
        {
            if (firstToPlay != PlayerPosition.South && firstToPlay != PlayerPosition.East
                                                    && firstToPlay != PlayerPosition.North
                                                    && firstToPlay != PlayerPosition.West)
            {
                throw new ArgumentOutOfRangeException(nameof(firstToPlay), firstToPlay, "The first to play must be one seat.");
            }

            this.observers = observers;
            this.deck = deck;
            this.scriptedRounds = scriptedRounds;
            this.recordHistory = recordHistory;
            this.firstInRound = firstToPlay;
        }

        /// <summary>Gets South-North's game points.</summary>
        public int SouthNorthPoints { get; private set; }

        /// <summary>Gets East-West's game points.</summary>
        public int EastWestPoints { get; private set; }

        /// <summary>Gets the points hanging from earlier deals, for the winner of the next played deal.</summary>
        public int HangingPoints { get; private set; }

        /// <summary>Gets the number of finished deals (passed-out ones included).</summary>
        public int RoundsPlayed { get; private set; }

        /// <summary>Gets a value indicating whether the match is over.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Gets the winning team (<see cref="PlayerPosition.SouthNorthTeam"/> or
        /// <see cref="PlayerPosition.EastWestTeam"/>) once the match is over;
        /// <see cref="PlayerPosition.Unknown"/> before, and for a match ended by <see cref="Stop"/>.
        /// </summary>
        public PlayerPosition Winner { get; private set; }

        /// <summary>Gets a value indicating whether <see cref="Stop"/> ended the match before the rules did.</summary>
        public bool IsStopped { get; private set; }

        /// <summary>Gets the result the observers' EndOfGame received; null until the rules end the match.</summary>
        public GameResult Result { get; private set; }

        /// <summary>
        /// Gets the seat that must decide now, or <see cref="PlayerPosition.Unknown"/> before
        /// <see cref="Start"/> and after the match.
        /// </summary>
        public PlayerPosition ToMove => this.IsFinished || this.round == null ? PlayerPosition.Unknown : this.round.ToMove;

        /// <summary>Gets what the seat to move decides (<see cref="BelotDecision.None"/> when nobody is to move).</summary>
        public BelotDecision Decision => this.IsFinished || this.round == null ? BelotDecision.None : this.round.Decision;

        internal Round CurrentRound => this.round;

        /// <summary>Starts the match: the first deal, up to the first bid.</summary>
        public void Start()
        {
            if (this.started)
            {
                throw new InvalidOperationException("The match has already been started.");
            }

            this.started = true;
            this.roundNumber = 1;
            this.StartRound();
        }

        /// <summary>
        /// Creates what <see cref="IPlayer.GetBid"/> would receive now. It is a copy: changing it
        /// does not affect the match.
        /// </summary>
        /// <returns>A new context.</returns>
        public PlayerGetBidContext CreateBidContext()
        {
            this.EnsureDecision(BelotDecision.Bid);
            var live = this.round.Auction.Context;
            var context = new PlayerGetBidContext { AvailableBids = live.AvailableBids };
            CopyBase(live, context);
            return context;
        }

        /// <summary>
        /// Creates what <see cref="IPlayer.GetAnnounces"/> would receive now. It is a copy.
        /// </summary>
        /// <returns>A new context.</returns>
        public PlayerGetAnnouncesContext CreateAnnouncesContext()
        {
            this.EnsureDecision(BelotDecision.Announce);
            var live = this.round.Tricks.AnnounceContext;
            var context = new PlayerGetAnnouncesContext
            {
                Announces = CopyAnnounces(live.Announces),
                CurrentTrickActions = CopyActions(live.CurrentTrickActions),
                AvailableAnnounces = CopyAnnounces(live.AvailableAnnounces),
            };
            CopyBase(live, context);
            return context;
        }

        /// <summary>
        /// Creates what <see cref="IPlayer.PlayCard"/> would receive now. It is a copy.
        /// </summary>
        /// <returns>A new context.</returns>
        public PlayerPlayCardContext CreatePlayCardContext()
        {
            this.EnsureDecision(BelotDecision.PlayCard);
            var live = this.round.Tricks.PlayContext;
            var context = new PlayerPlayCardContext
            {
                Announces = CopyAnnounces(live.Announces),
                CurrentTrickActions = CopyActions(live.CurrentTrickActions),
                RoundActions = CopyActions(live.RoundActions),
                AvailableCardsToPlay = new CardCollection(live.AvailableCardsToPlay),
                CurrentTrickNumber = live.CurrentTrickNumber,
            };
            CopyBase(live, context);
            return context;
        }

        /// <summary>
        /// Applies the decision of <paramref name="player"/>. Only <see cref="BelotActResult.Ok"/>
        /// changes anything; the other results leave the match exactly as it was.
        /// </summary>
        /// <param name="player">The seat deciding.</param>
        /// <param name="action">A <see cref="BelotAction"/> of the kind <see cref="Decision"/> asks for.</param>
        /// <returns>What happened.</returns>
        public BelotActResult Act(PlayerPosition player, BelotAction action)
        {
            var check = this.Validate(player, action);
            if (check != BelotActResult.Ok)
            {
                return check;
            }

            switch (action.Type)
            {
                case BelotActionType.Bid:
                    this.round.ApplyBid(action.BidType);
                    break;
                case BelotActionType.Announce:
                    var announces = new List<Announce>(action.Announces.Count);
                    foreach (var announce in action.Announces)
                    {
                        if (announce != null)
                        {
                            announces.Add(new Announce(announce.Type, announce.Card));
                        }
                    }

                    this.round.ApplyAnnounces(announces);
                    break;
                default:
                    this.round.TryPlayCard(new PlayCardAction(action.Card, action.Belote));
                    break;
            }

            this.ContinueAfterRoundStep();
            return BelotActResult.Ok;
        }

        /// <summary>
        /// Checks a decision without making it: returns what <see cref="Act"/> would return, and
        /// changes nothing.
        /// </summary>
        /// <param name="player">The seat that would decide.</param>
        /// <param name="action">The action.</param>
        /// <returns>What <see cref="Act"/> would return.</returns>
        public BelotActResult Validate(PlayerPosition player, BelotAction action)
        {
            if (!this.started)
            {
                throw new InvalidOperationException("Start the match before acting.");
            }

            if (this.IsFinished)
            {
                return BelotActResult.MatchFinished;
            }

            if (player != this.round.ToMove)
            {
                return BelotActResult.NotYourTurn;
            }

            if (action == null)
            {
                return BelotActResult.InvalidAction;
            }

            switch (this.round.Decision)
            {
                case BelotDecision.Bid:
                    return action.Type == BelotActionType.Bid && this.round.Auction.Check(action.BidType) == BidCheck.Ok
                        ? BelotActResult.Ok
                        : BelotActResult.InvalidAction;
                case BelotDecision.Announce:
                    return action.Type == BelotActionType.Announce ? BelotActResult.Ok : BelotActResult.InvalidAction;
                default:
                    return action.Type == BelotActionType.PlayCard && this.round.Tricks.CanPlay(action.Card)
                        ? BelotActResult.Ok
                        : BelotActResult.InvalidAction;
            }
        }

        /// <summary>
        /// Ends the match before the rules do: a resignation, a timeout, an abandoned table. No
        /// more decisions are accepted and the observers get no more callbacks; there is no
        /// <see cref="Winner"/> (who won is the caller's decision). Does nothing on a match that is
        /// already over. Called from an observer's callback, it takes effect once the step that
        /// made the callback is done: its remaining callbacks still arrive, but a deal it finished
        /// is not added to the score and no further deal is dealt.
        /// </summary>
        public void Stop()
        {
            if (!this.started)
            {
                throw new InvalidOperationException("Start the match first.");
            }

            if (this.IsFinished)
            {
                return;
            }

            this.IsFinished = true;
            this.IsStopped = true;
        }

        /// <summary>What <paramref name="seat"/> may see now: its own hand and options plus the public state.</summary>
        /// <param name="seat">South, East, North or West.</param>
        /// <returns>A new view.</returns>
        public BelotSeatView GetView(PlayerPosition seat)
        {
            if (seat != PlayerPosition.South && seat != PlayerPosition.East && seat != PlayerPosition.North && seat != PlayerPosition.West)
            {
                throw new ArgumentOutOfRangeException(nameof(seat), seat, "A view belongs to one seat.");
            }

            return this.BuildView(seat, null);
        }

        /// <summary>
        /// The view after the match (or after <see cref="Stop"/>): no seat and no hand, but with
        /// the full <see cref="BelotMatchRecord"/>.
        /// </summary>
        /// <returns>A new view.</returns>
        public BelotSeatView GetFinalView() => this.BuildView(PlayerPosition.Unknown, this.GetRecord());

        /// <summary>The whole match, hidden cards included. Only once it is over (or stopped).</summary>
        /// <returns>A new record.</returns>
        public BelotMatchRecord GetRecord()
        {
            this.EnsureHistory();
            if (!this.IsFinished)
            {
                throw new InvalidOperationException("The record reveals every hand; it is available once the match is over (or stopped).");
            }

            var rounds = new List<BelotRoundRecord>(this.finishedRounds);
            if (this.IsStopped && (rounds.Count == 0 || rounds[rounds.Count - 1].RoundNumber != this.round.RoundNumber))
            {
                // The deal the match was stopped in, as far as it got, unscored.
                rounds.Add(BuildRoundRecord(this.round, null));
            }

            return new BelotMatchRecord
            {
                Rounds = rounds.ToArray(),
                Winner = this.Winner,
                SouthNorthPoints = this.SouthNorthPoints,
                EastWestPoints = this.EastWestPoints,
                HangingPoints = this.HangingPoints,
            };
        }

        /// <summary>Plays the match to its end, asking these players for every decision.</summary>
        internal void PlayWith(IPlayer[] players)
        {
            while (!this.IsFinished)
            {
                PlayerDriver.Step(players, this.round);
                this.ContinueAfterRoundStep();
            }
        }

        private static BelotRoundRecord BuildRoundRecord(Round round, RoundResult result)
        {
            return new BelotRoundRecord
            {
                RoundNumber = round.RoundNumber,
                FirstToPlay = round.FirstToPlay,
                Deal = round.DealOrder ?? Array.Empty<Card>(),
                Bids = CopyBids(round.Auction.Bids),
                Contract = new Bid(round.Auction.Contract.Player, round.Auction.Contract.Type),
                Announces = ToBelotAnnounces(round.Tricks?.Announces, PlayerPosition.Unknown, true),
                Tricks = BuildTricks(round.Tricks, !round.IsFinished),
                Result = result == null ? null : BuildSummary(round, result),
            };
        }

        private static BelotRoundSummary BuildSummary(Round round, RoundResult result) =>
            new BelotRoundSummary
            {
                Contract = new Bid(result.Contract.Player, result.Contract.Type),
                SouthNorthPoints = result.SouthNorthPoints,
                EastWestPoints = result.EastWestPoints,
                SouthNorthTotalInRoundPoints = result.SouthNorthTotalInRoundPoints,
                EastWestTotalInRoundPoints = result.EastWestTotalInRoundPoints,
                NoTricksForOneOfTheTeams = result.NoTricksForOneOfTheTeams,
                HangingPoints = result.HangingPoints,
                Announces = ToBelotAnnounces(round.Tricks?.Announces, PlayerPosition.Unknown, true),
                Bids = CopyBids(round.Auction.Bids),
                Tricks = BuildTricks(round.Tricks, false),
            };

        // The declared combinations as a seat sees them: every card and score once the deal is
        // over; before, only its own combinations' cards and the belotes' (played in the open).
        private static BelotAnnounce[] ToBelotAnnounces(List<Announce> announces, PlayerPosition seat, bool reveal)
        {
            if (announces == null)
            {
                return Array.Empty<BelotAnnounce>();
            }

            var list = new BelotAnnounce[announces.Count];
            for (var i = 0; i < list.Length; i++)
            {
                var announce = announces[i];
                var shown = reveal || announce.Type == AnnounceType.Belot || announce.Player == seat;
                list[i] = new BelotAnnounce
                {
                    Player = announce.Player,
                    Type = announce.Type,
                    Card = shown ? announce.Card : null,
                    IsScored = reveal || announce.Type == AnnounceType.Belot ? announce.IsActive : null,
                };
            }

            return list;
        }

        // The combinations a seat is offered: its own, so with their cards.
        private static BelotAnnounce[] OwnCombinations(IList<Announce> offered, PlayerPosition seat)
        {
            var list = new BelotAnnounce[offered.Count];
            for (var i = 0; i < list.Length; i++)
            {
                list[i] = new BelotAnnounce { Player = seat, Type = offered[i].Type, Card = offered[i].Card };
            }

            return list;
        }

        // The finished tricks, and with withCurrent the cards of an unfinished one (no winner).
        private static BelotTrick[] BuildTricks(TrickPlay tricks, bool withCurrent)
        {
            if (tricks == null)
            {
                return Array.Empty<BelotTrick>();
            }

            var finished = tricks.TrickWinners.Count;
            var extra = withCurrent && tricks.Actions.Count > finished * 4 ? 1 : 0;
            var list = new BelotTrick[finished + extra];
            for (var i = 0; i < list.Length; i++)
            {
                list[i] = new BelotTrick
                {
                    Cards = ToPlayedCards(tricks.Actions, i * 4, Math.Min(4, tricks.Actions.Count - (i * 4))),
                    Winner = i < finished ? tricks.TrickWinners[i] : PlayerPosition.Unknown,
                };
            }

            return list;
        }

        private static BelotPlayedCard[] ToPlayedCards(List<PlayCardAction> actions, int start, int count)
        {
            var cards = new BelotPlayedCard[count];
            for (var i = 0; i < count; i++)
            {
                var action = actions[start + i];
                cards[i] = new BelotPlayedCard { Player = action.Player, Card = action.Card, Belote = action.Belote };
            }

            return cards;
        }

        private static Bid[] CopyBids(IEnumerable<Bid> bids)
        {
            var list = new List<Bid>(8);
            foreach (var bid in bids)
            {
                list.Add(new Bid(bid.Player, bid.Type));
            }

            return list.ToArray();
        }

        private static Card[] ToCards(IEnumerable<Card> cards) => new List<Card>(cards).ToArray();

        private static void CopyBase(BasePlayerContext from, BasePlayerContext to)
        {
            to.RoundNumber = from.RoundNumber;
            to.HangingPoints = from.HangingPoints;
            to.FirstToPlayInTheRound = from.FirstToPlayInTheRound;
            to.MyPosition = from.MyPosition;
            to.SouthNorthPoints = from.SouthNorthPoints;
            to.EastWestPoints = from.EastWestPoints;
            to.MyCards = new CardCollection(from.MyCards);
            var bids = new List<Bid>(8);
            foreach (var bid in from.Bids)
            {
                bids.Add(new Bid(bid.Player, bid.Type));
            }

            to.Bids = bids;
            to.CurrentContract = new Bid(from.CurrentContract.Player, from.CurrentContract.Type);
        }

        private static List<Announce> CopyAnnounces(IEnumerable<Announce> announces)
        {
            var copies = new List<Announce>();
            foreach (var announce in announces)
            {
                copies.Add(new Announce(announce.Type, announce.Card) { Player = announce.Player, IsActive = announce.IsActive });
            }

            return copies;
        }

        private static List<PlayCardAction> CopyActions(IEnumerable<PlayCardAction> actions)
        {
            var copies = new List<PlayCardAction>(32);
            foreach (var action in actions)
            {
                copies.Add(new PlayCardAction(action.Card, action.Belote) { Player = action.Player, TrickNumber = action.TrickNumber });
            }

            return copies;
        }

        private void EnsureHistory()
        {
            if (!this.started)
            {
                throw new InvalidOperationException("Start the match first.");
            }

            if (!this.recordHistory || this.round == null)
            {
                throw new InvalidOperationException("This match does not record its history (BelotMatchOptions.RecordHistory).");
            }
        }

        private BelotSeatView BuildView(PlayerPosition seat, BelotMatchRecord record)
        {
            this.EnsureHistory();
            var current = this.round;
            var toMove = this.ToMove;
            var decision = this.Decision;
            var deciding = seat != PlayerPosition.Unknown && seat == toMove;
            var tricks = current.Tricks;
            var hand = seat == PlayerPosition.Unknown ? Array.Empty<Card>() : ToCards(current.Hands[seat.Index()]);

            var playable = Array.Empty<Card>();
            var beloteCards = new List<Card>();
            if (deciding && decision == BelotDecision.PlayCard)
            {
                playable = ToCards(tricks.PlayContext.AvailableCardsToPlay);
                foreach (var card in playable)
                {
                    if (AnnouncesService.IsBeloteAllowed(current.Hands[seat.Index()], current.Auction.Contract.Type, tricks.TrickActions, card))
                    {
                        beloteCards.Add(card);
                    }
                }
            }

            var finishedTricks = BuildTricks(tricks, false);
            var lastTrick = finishedTricks.Length > 0 ? finishedTricks[finishedTricks.Length - 1] : null;
            for (var i = this.finishedRounds.Count - 1; lastTrick == null && i >= 0; i--)
            {
                var earlier = this.finishedRounds[i].Tricks;
                lastTrick = earlier.Count > 0 ? earlier[earlier.Count - 1] : null;
            }

            var previousRounds = new List<BelotRoundSummary>(this.finishedRounds.Count);
            foreach (var finished in this.finishedRounds)
            {
                if (finished.Result != null)
                {
                    previousRounds.Add(finished.Result);
                }
            }

            var currentTrick = Array.Empty<BelotPlayedCard>();
            if (tricks != null && !tricks.IsFinished && tricks.Actions.Count > tricks.TrickWinners.Count * 4)
            {
                var start = tricks.TrickWinners.Count * 4;
                currentTrick = ToPlayedCards(tricks.Actions, start, tricks.Actions.Count - start);
            }

            return new BelotSeatView
            {
                Seat = seat,
                ToMove = toMove,
                Decision = decision,
                IsMatchFinished = this.IsFinished,
                MatchWinner = this.Winner,
                SouthNorthPoints = this.SouthNorthPoints,
                EastWestPoints = this.EastWestPoints,
                HangingPoints = this.HangingPoints,
                RoundNumber = current.RoundNumber,
                FirstToPlayInTheRound = current.FirstToPlay,
                PreviousRounds = previousRounds.ToArray(),
                Bids = CopyBids(current.Auction.Bids),
                Contract = new Bid(current.Auction.Contract.Player, current.Auction.Contract.Type),
                Announces = ToBelotAnnounces(tricks?.Announces, seat, current.IsFinished),
                Tricks = finishedTricks,
                CurrentTrick = currentTrick,
                LastTrick = lastTrick,
                CardCounts = new[] { current.Hands[0].Count, current.Hands[1].Count, current.Hands[2].Count, current.Hands[3].Count },
                Hand = hand,
                AvailableBids = deciding && decision == BelotDecision.Bid ? current.Auction.Context.AvailableBids : BidType.Pass,
                AvailableAnnounces = deciding && decision == BelotDecision.Announce
                    ? OwnCombinations(tricks.AnnounceContext.AvailableAnnounces, seat)
                    : Array.Empty<BelotAnnounce>(),
                PlayableCards = playable,
                BeloteCards = beloteCards.ToArray(),
                Record = record,
            };
        }

        private void EnsureDecision(BelotDecision decision)
        {
            if (this.Decision != decision)
            {
                throw new InvalidOperationException($"The pending decision is {this.Decision}, not {decision}.");
            }
        }

        // After a step of the current deal: a finished deal is scored (unless the match was
        // stopped meanwhile) and the next one dealt, or the match ends.
        private void ContinueAfterRoundStep()
        {
            if (this.round.IsFinished && !this.IsStopped)
            {
                this.FinishRound(this.round.Result);
            }
        }

        private void StartRound()
        {
            while (this.scriptedRounds != null)
            {
                // Test seam: the deal's result comes from the script and is scored right away.
                var result = this.scriptedRounds(
                    this.roundNumber,
                    this.firstInRound,
                    this.SouthNorthPoints,
                    this.EastWestPoints,
                    this.HangingPoints);
                if (this.FinishRound(result))
                {
                    return;
                }
            }

            this.round = new Round(
                this.observers,
                this.deck,
                this.hands,
                this.roundNumber,
                this.firstInRound,
                this.SouthNorthPoints,
                this.EastWestPoints,
                this.HangingPoints,
                this.recordHistory);
        }

        // Adds the deal to the score and ends the match or deals the next one; true when the match
        // is over (or goes on in a scripted deal already started).
        private bool FinishRound(RoundResult result)
        {
            if (this.recordHistory && this.scriptedRounds == null)
            {
                this.finishedRounds.Add(BuildRoundRecord(this.round, result));
            }

            this.SouthNorthPoints += result.SouthNorthPoints;
            this.EastWestPoints += result.EastWestPoints;
            this.HangingPoints = result.HangingPoints;
            this.RoundsPlayed++;

            // A team wins with 151+ and more points than the other team, on a deal in which it
            // scored. A capot deal never ends the game ("С капо не се излиза"), and neither does
            // a passed-out one, so after them the leader must score again in a later deal.
            var winner = PlayerPosition.Unknown;
            if (this.SouthNorthPoints >= PointsToWin
                && this.SouthNorthPoints > this.EastWestPoints
                && result.SouthNorthPoints > 0
                && !result.NoTricksForOneOfTheTeams
                && result.Contract.Type != BidType.Pass)
            {
                winner = PlayerPosition.SouthNorthTeam;
            }
            else if (this.EastWestPoints >= PointsToWin
                     && this.EastWestPoints > this.SouthNorthPoints
                     && result.EastWestPoints > 0
                     && !result.NoTricksForOneOfTheTeams
                     && result.Contract.Type != BidType.Pass)
            {
                winner = PlayerPosition.EastWestTeam;
            }

            if (winner != PlayerPosition.Unknown)
            {
                this.IsFinished = true;
                this.Winner = winner;
                this.Result = new GameResult
                {
                    RoundsPlayed = this.roundNumber,
                    SouthNorthPoints = this.SouthNorthPoints,
                    EastWestPoints = this.EastWestPoints,
                };
                for (var i = 0; i < 4; i++)
                {
                    this.observers[i]?.EndOfGame(this.Result);
                }

                return true;
            }

            this.roundNumber++;
            this.firstInRound = this.firstInRound.Next();
            if (this.scriptedRounds == null)
            {
                this.StartRound();
            }

            return false;
        }
    }
}
