namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    // Sits at a GameSession the way the game page does: listens to every event, makes a random
    // legal decision (from the person's own view) whenever the person is asked, and continues after
    // every deal. Records everything in order, with the thread and the time of each event.
    internal sealed class TableDriver
    {
        private readonly GameSession session;

        private readonly Random random;

        private readonly Stopwatch clock = Stopwatch.StartNew();

        public TableDriver(GameSession session, int seed)
        {
            this.session = session;
            this.random = new Random(seed);

            session.RoundStarted += this.Record;
            session.TurnStarted += this.OnTurnStarted;
            session.BidMade += this.Record;
            session.ContractSettled += this.Record;
            session.AnnouncesDeclared += this.Record;
            session.CardPlayed += this.Record;
            session.TrickCollected += this.Record;
            session.RoundFinished += round =>
            {
                this.Record(round);
                if (this.AutoContinue)
                {
                    session.Continue();
                }
            };
            session.GameOver += over =>
            {
                this.Record(over.LastRound);
                this.Over = over;
            };
            session.GameError += error => this.Errors.Add(error);
        }

        public event Action<TurnInfo>? Turn;

        public bool AutoPlay { get; set; } = true;

        public bool AutoContinue { get; set; } = true;

        // Every event, in order: DealInfo, TurnInfo, BidInfo, ContractInfo, AnnounceInfo, CardInfo,
        // TrickInfo, RoundEndInfo (the last deal's from GameOver).
        public List<object> Events { get; } = new();

        // When each event came (milliseconds since the driver was made), by index in Events.
        public List<long> Times { get; } = new();

        public HashSet<int> EventThreads { get; } = new();

        public List<Exception> Errors { get; } = new();

        public GameOverInfo? Over { get; private set; }

        public async Task PlayToTheEndAsync(int seconds = 120)
        {
            this.session.Start();

            // A restart replaces the game in progress: follow it to the game that is played out.
            Task run;
            do
            {
                run = this.session.Completion;
                await run.WaitAsync(TimeSpan.FromSeconds(seconds));
            }
            while (run != this.session.Completion);

            Assert.Empty(this.Errors);
            Assert.NotNull(this.Over);
        }

        // A random legal decision for the person, through the session's Try methods.
        public void Decide(BelotDecision decision)
        {
            var view = this.session.GetView(Seats.Person)!;
            var accepted = decision switch
            {
                BelotDecision.Bid => this.session.TryBid(RandomMoves.ChooseBid(view, this.random)),
                BelotDecision.Announce => this.session.TryDeclare(RandomMoves.ChooseAnnounces(view, this.random)),
                _ => this.session.TryPlay(view.PlayableCards[view.PlayableCards.Count == 1 ? 0 : this.random.Next(view.PlayableCards.Count)]),
            };
            Assert.True(accepted, $"The person's {decision} was refused.");
        }

        private void OnTurnStarted(TurnInfo turn)
        {
            this.Record(turn);
            this.Turn?.Invoke(turn);
            if (turn.IsHuman && this.AutoPlay && this.session.IsRunning)
            {
                // The session must already be waiting for the person when it says it is their turn.
                Assert.True(this.session.IsAwaiting(turn.Decision), $"The person's {turn.Decision} started, but none is awaited");
                this.Decide(turn.Decision);
            }
        }

        private void Record(object item)
        {
            this.Events.Add(item);
            this.Times.Add(this.clock.ElapsedMilliseconds);
            this.EventThreads.Add(Environment.CurrentManagedThreadId);
        }
    }
}
