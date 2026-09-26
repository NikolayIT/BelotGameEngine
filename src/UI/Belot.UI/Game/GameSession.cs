namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// One game of Belot on the device: a <see cref="BelotMatch"/> played by a single async flow.
    /// Nothing waits on a thread. The person (South) decides by an awaited tap
    /// (<see cref="TryBid"/>, <see cref="TryDeclare"/>, <see cref="TryPlay"/>), the computer seats
    /// by an awaited thinking pause and decision, a finished trick stays on the table for an awaited
    /// pause, and a finished deal waits for <see cref="Continue"/>.
    /// <para>
    /// Start it on the UI thread: the flow then resumes there after every await, so every event is
    /// raised on the UI thread, in play order, and handlers can update the screen directly. Only the
    /// computer's decision is computed on the thread pool, from a snapshot view of its seat.
    /// </para>
    /// <para>
    /// One decision can make the rules play on (passes for the seats that can only pass, forced
    /// cards, the end of the deal): <see cref="ActReplay"/> turns it into the events in order, with
    /// a short pause before each forced move. The events carry everything the table shows; the live
    /// view (<see cref="GetView"/>) is ahead of them while they are replayed.
    /// </para>
    /// <para>No MAUI types here: the UI tests compile this file and play whole games with it.</para>
    /// </summary>
    [SuppressMessage("Design", "CA1001", Justification = "The stop token source has no timer or wait handle to release, and a run may still check its token after Stop.")]
    public sealed class GameSession
    {
        private readonly string[] names;

        private readonly Func<PlayerPosition, IPlayer> createBot;

        private readonly Func<IPlayer>? hintFactory;

        private readonly Random random;

        private IPlayer? hintPlayer;

        private BelotMatch? match;

        private CancellationTokenSource? stopping;

        private Task running = Task.CompletedTask;

        private Task hinting = Task.CompletedTask;

        // Each Start begins a new run; a stopped run that wakes up late leaves the new one alone.
        private int runId;

        // Counts Start calls (runId also moves on Stop).
        private int starts;

        // Moves on every decision the person is asked for (and on Stop): a hint for an earlier one is dropped.
        private int turnId;

        private TaskCompletionSource<BelotAction>? pendingMove;

        private BelotDecision pendingDecision;

        private TaskCompletionSource? pendingContinue;

        /// <param name="names">The seats' names, by seat index (South, East, North, West).</param>
        /// <param name="createBot">Builds the player of a computer seat (East, North or West) for each game.</param>
        /// <param name="pace">The pauses.</param>
        /// <param name="hintFactory">Builds the player that answers the person's hints; null for the strongest one.</param>
        /// <param name="random">The source of the deals and of who deals first; null for a new one.</param>
        public GameSession(
            IReadOnlyList<string> names,
            Func<PlayerPosition, IPlayer> createBot,
            GamePace pace,
            Func<IPlayer>? hintFactory = null,
            Random? random = null)
        {
            if (names == null || names.Count != 4)
            {
                throw new ArgumentException("Four names, one for each seat.", nameof(names));
            }

            this.names = names.ToArray();
            this.createBot = createBot ?? throw new ArgumentNullException(nameof(createBot));
            this.hintFactory = hintFactory;
            this.Pace = pace;
            this.random = random ?? new Random();
        }

        /// <summary>Fires when a deal has been dealt (five cards each, before the bids).</summary>
        public event Action<DealInfo>? RoundStarted;

        /// <summary>Fires when a seat has to decide; for the person the game then waits for their tap.</summary>
        public event Action<TurnInfo>? TurnStarted;

        public event Action<BidInfo>? BidMade;

        /// <summary>Fires when the auction ends in a contract and the last three cards are dealt.</summary>
        public event Action<ContractInfo>? ContractSettled;

        /// <summary>Fires when a seat declares its combinations in the first trick.</summary>
        public event Action<AnnounceInfo>? AnnouncesDeclared;

        public event Action<CardInfo>? CardPlayed;

        /// <summary>Fires when a finished trick leaves the table, after the table pause.</summary>
        public event Action<TrickInfo>? TrickCollected;

        /// <summary>Fires after a deal that did not end the game; the game waits for <see cref="Continue"/>.</summary>
        public event Action<RoundEndInfo>? RoundFinished;

        /// <summary>Fires once when the game is won, with the last deal.</summary>
        public event Action<GameOverInfo>? GameOver;

        /// <summary>Fires if the game stops on an unexpected error.</summary>
        public event Action<Exception>? GameError;

        public GamePace Pace { get; }

        /// <summary>Gets a value indicating whether a game is in progress (started, not over, not stopped).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Gets the game in progress, for callers that want to await its end.</summary>
        public Task Completion => this.running;

        public int PointsToWin => BelotMatch.PointsToWin;

        /// <summary>Gets the decision the game waits for from the person, or None.</summary>
        public BelotDecision AwaitedDecision => this.pendingMove == null ? BelotDecision.None : this.pendingDecision;

        public string GetName(PlayerPosition seat) => this.names[seat.Index()];

        /// <summary>
        /// What <paramref name="seat"/> may see now, or null before the first deal. It is live:
        /// while the events of a decision are being raised it is already past them.
        /// </summary>
        public BelotSeatView? GetView(PlayerPosition seat) => this.match?.GetView(seat);

        public bool IsAwaiting(BelotDecision decision) => this.pendingMove != null && this.pendingDecision == decision;

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            // The computer players and the hint player are built per game: a stopped game's
            // player may still be finishing a decision on the thread pool, and the new game must
            // not share it.
            var bots = new IPlayer?[4];
            foreach (var seat in Seats.All.Where(s => s != Seats.Person))
            {
                bots[seat.Index()] = this.createBot(seat) ?? throw new InvalidOperationException($"No player for {seat}.");
            }

            this.hintPlayer = this.hintFactory?.Invoke() ?? new ClaudePlayerIsmcts();
            this.hinting = Task.CompletedTask;
            var game = new BelotMatch(new BelotMatchOptions
            {
                FirstToPlay = Seats.All[this.random.Next(4)],
                Random = new Random(this.random.Next()),
            });

            this.match = game;
            this.stopping = new CancellationTokenSource();
            this.IsRunning = true;
            var start = ++this.starts;
            var run = this.RunAsync(game, bots, ++this.runId, this.stopping.Token);

            // The run raises the deal and the first turn before it returns here. If a handler of
            // those restarted the game, the new game's run is already in place: keep it.
            if (start == this.starts)
            {
                this.running = run;
            }
        }

        /// <summary>Stops the game in progress (leaving the table). Nothing more is raised for it.</summary>
        public void Stop()
        {
            this.runId++;
            this.turnId++;
            this.stopping?.Cancel();
            this.stopping = null;
            this.pendingMove = null;
            this.pendingContinue = null;
            this.IsRunning = false;
        }

        public void Restart()
        {
            this.Stop();
            this.Start();
        }

        /// <summary>The person bids. False, changing nothing, when they are not bidding now or may not bid that.</summary>
        public bool TryBid(BidType bid) => this.Submit(BelotDecision.Bid, BelotAction.Bid(bid));

        /// <summary>
        /// The person declares these of the combinations offered (<see cref="BelotSeatView.AvailableAnnounces"/>).
        /// False, changing nothing, when they are not declaring now or one of them is not on offer.
        /// </summary>
        public bool TryDeclare(IReadOnlyCollection<BelotAnnounce> combinations)
        {
            var view = this.match?.GetView(Seats.Person);
            if (view == null || combinations == null
                || combinations.Any(c => c == null || !view.AvailableAnnounces.Any(a => a.Type == c.Type && a.Card == c.Card)))
            {
                return false;
            }

            return this.Submit(BelotDecision.Announce, BelotAction.Declare(combinations.Select(c => new Announce(c.Type, c.Card))));
        }

        /// <summary>
        /// The person plays a card, claiming the belote when it makes one. False, changing
        /// nothing, when they are not playing now or may not play that card.
        /// </summary>
        public bool TryPlay(Card card) => card != null && this.Submit(BelotDecision.PlayCard, BelotAction.PlayCard(card, true));

        /// <summary>Deals the next deal after <see cref="RoundFinished"/>.</summary>
        public void Continue()
        {
            var next = this.pendingContinue;
            this.pendingContinue = null;
            next?.TrySetResult();
        }

        /// <summary>
        /// What the hint player would do in the person's place now, from the person's own view;
        /// null when the person has nothing to decide, or decided (or the game stopped) meanwhile.
        /// </summary>
        public async Task<BelotAction?> GetHintAsync()
        {
            var player = this.hintPlayer;
            if (player == null || this.match == null || this.pendingMove == null)
            {
                return null;
            }

            var turn = this.turnId;
            var view = this.match.GetView(Seats.Person);

            // One hint at a time: the hint player is one object.
            var hint = HintAsync(this.hinting, player, view);
            this.hinting = hint;
            var action = await hint;
            return turn == this.turnId && this.pendingMove != null ? action : null;
        }

        /// <summary>The whole game, hidden cards included, once it is over.</summary>
        public BelotMatchRecord? GetRecord() => this.match is { IsFinished: true } game ? game.GetRecord() : null;

        private static async Task<BelotAction?> HintAsync(Task previous, IPlayer player, BelotSeatView view)
        {
            try
            {
                await previous;
            }
            catch
            {
                // An earlier hint's failure is not this one's.
            }

            return await Task.Run(() => player.Decide(view));
        }

        private static DealInfo DealOf(BelotSeatView view) =>
            new(
                view.RoundNumber,
                view.FirstToPlayInTheRound,
                view.Hand.ToArray(),
                view.CardCounts.ToArray(),
                view.SouthNorthPoints,
                view.EastWestPoints,
                view.HangingPoints);

        private bool Submit(BelotDecision decision, BelotAction action)
        {
            var move = this.pendingMove;
            if (move == null || this.pendingDecision != decision || this.match == null)
            {
                return false;
            }

            if (this.match.Validate(Seats.Person, action) != BelotActResult.Ok)
            {
                return false;
            }

            this.pendingMove = null;
            return move.TrySetResult(action);
        }

        private async Task RunAsync(BelotMatch game, IReadOnlyList<IPlayer?> bots, int id, CancellationToken stop)
        {
            try
            {
                game.Start();
                this.RoundStarted?.Invoke(DealOf(game.GetView(Seats.Person)));
                while (true)
                {
                    // A handler of the last event may have stopped the game (or restarted it). The
                    // same check follows every await: a wait that ended just before a stop has
                    // already queued its continuation, which must not raise anything.
                    stop.ThrowIfCancellationRequested();

                    var seat = game.ToMove;
                    var decision = game.Decision;
                    BelotAction action;
                    if (seat == Seats.Person)
                    {
                        // Waiting for the tap already when TurnStarted fires, so its handlers can
                        // ask for a hint or decide at once.
                        var tap = this.ExpectMove(decision, stop);
                        this.TurnStarted?.Invoke(new TurnInfo(seat, decision, true));
                        action = await tap;
                    }
                    else
                    {
                        this.TurnStarted?.Invoke(new TurnInfo(seat, decision, false));
                        action = await this.ThinkAsync(game, seat, bots[seat.Index()]!, stop);
                    }

                    stop.ThrowIfCancellationRequested();

                    var before = game.GetView(Seats.Person);
                    var result = game.Act(seat, action);
                    if (result != BelotActResult.Ok)
                    {
                        throw new InvalidOperationException($"{this.GetName(seat)}'s {decision} ({action.Type}) was refused: {result}.");
                    }

                    foreach (var step in ActReplay.Steps(before, game.GetView(Seats.Person), seat, action))
                    {
                        var pause = step switch
                        {
                            BidInfo { IsAuto: true } => this.Pace.AutoMoveDelayMs,
                            CardInfo { IsAuto: true } => this.Pace.AutoMoveDelayMs,
                            TrickInfo => this.Pace.TrickSettleMs,
                            _ => 0,
                        };
                        if (pause > 0)
                        {
                            await Task.Delay(pause, stop);
                            stop.ThrowIfCancellationRequested();
                        }

                        if (step is RoundEndInfo round)
                        {
                            if (round.IsGameOver)
                            {
                                this.IsRunning = false;
                                this.GameOver?.Invoke(new GameOverInfo(game.Winner, round));
                                return;
                            }

                            var next = this.ExpectContinue(stop);
                            this.RoundFinished?.Invoke(round);
                            await next;
                            stop.ThrowIfCancellationRequested();

                            // Nothing has been decided in the new deal yet, so the view is its deal.
                            this.RoundStarted?.Invoke(DealOf(game.GetView(Seats.Person)));
                        }
                        else
                        {
                            this.Raise(step);
                        }

                        stop.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // Stopped: leaving the table is not an error.
            }
            catch (Exception ex)
            {
                // A stopped run has nobody left to tell.
                if (id == this.runId)
                {
                    this.IsRunning = false;
                    this.GameError?.Invoke(ex);
                }
            }
        }

        private void Raise(GameEvent step)
        {
            switch (step)
            {
                case BidInfo bid:
                    this.BidMade?.Invoke(bid);
                    break;
                case ContractInfo contract:
                    this.ContractSettled?.Invoke(contract);
                    break;
                case AnnounceInfo announces:
                    this.AnnouncesDeclared?.Invoke(announces);
                    break;
                case CardInfo card:
                    this.CardPlayed?.Invoke(card);
                    break;
                case TrickInfo trick:
                    this.TrickCollected?.Invoke(trick);
                    break;
            }
        }

        // The decision a Try method will deliver (cancelled when the game is stopped).
        private Task<BelotAction> ExpectMove(BelotDecision decision, CancellationToken stop)
        {
            var move = new TaskCompletionSource<BelotAction>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.turnId++;
            this.pendingDecision = decision;
            this.pendingMove = move;
            return move.Task.WaitAsync(stop);
        }

        // The Continue after a finished deal (cancelled when the game is stopped).
        private Task ExpectContinue(CancellationToken stop)
        {
            var next = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            this.pendingContinue = next;
            return next.Task.WaitAsync(stop);
        }

        // The computer thinks during the pause: the wait is the longer of the two, not their sum.
        private async Task<BelotAction> ThinkAsync(BelotMatch game, PlayerPosition seat, IPlayer player, CancellationToken stop)
        {
            var view = game.GetView(seat);
            var thinking = Task.Run(() => player.Decide(view));
            await Task.Delay(this.Pace.ThinkDelayMs, stop);
            stop.ThrowIfCancellationRequested();
            return await thinking.WaitAsync(stop);
        }
    }
}
