namespace Belot.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays the cards with single-observer Information Set Monte-Carlo Tree Search (SO-ISMCTS,
    /// Cowling/Powley/Whitehouse), as Santase's strongest player does: one tree keyed by the
    /// public play, and every iteration deals the unseen cards anew (consistently with what the
    /// play has shown, see <see cref="RoundKnowledge"/>), walks the tree choosing among the cards
    /// legal in that deal, adds one node, and finishes the deal with a greedy rollout. A node's
    /// statistics pool many deals, so the search cannot count on a line that works only when it
    /// sees the cards. Selection is UCB with availability counts: a child's exploration term
    /// uses how often it was legal (n') rather than the parent's visits, value + C * sqrt(ln(n') / n).
    /// The partner's cards are chosen for the team's reward, the opponents' against it.
    ///
    /// Bidding is by Monte Carlo (<see cref="BidEvaluator"/>): the contracts the player may bid
    /// (and with <see cref="MayDouble"/> the double or redouble) and the contract that stands if
    /// it passes are played out over the same random deals, the others' first cards fitting their
    /// bids, and the best is bid when it beats passing by <see cref="BidMargin"/>.
    /// The player declares every combination it is offered and every belote.
    /// </summary>
    public class ClaudePlayerIsmcts : IPlayer
    {
        private const int NodeCapacity = 1 << 17;
        private const int NoNode = -1;

        // Deals tried per iteration for one that explains the auction (then the last one is used).
        private const int MaxAuctionAttempts = 30;

        private static readonly BidType[] ContractBids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        private readonly SmartPlayer smartPlayer = new SmartPlayer();
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly WorldSampler sampler = new WorldSampler();
        private readonly DeclaredAnnounce[] worldAnnounces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly BidEvaluator bidEvaluator = new BidEvaluator();
        private readonly BidModel bidModel = new BidModel();

        // The bidding candidates: the contract played and its declarer, and the bid that makes it.
        private readonly BidType[] candidateContracts = new BidType[ContractBids.Length + 2];
        private readonly int[] candidateDeclarers = new int[ContractBids.Length + 2];
        private readonly BidType[] candidateBids = new BidType[ContractBids.Length + 2];
        private readonly double[] candidateValues = new double[ContractBids.Length + 2];

        private Node[] nodes;
        private int[] path;
        private int nodeCount;
        private int hangingPoints;

        // Per decision: the searching player's team, the other seats whose bids tell something,
        // and the reward scale in game points.
        private int searchTeam;
        private int biddingSeats;
        private double rewardScale;

        public string Name => "Claude Player (ISMCTS)";

        /// <summary>Gets or sets the time budget per card, in milliseconds.</summary>
        public int TimeLimitMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets a cap on the iterations per card. With a generous time limit and a seeded
        /// <see cref="Rng"/> this makes the search deterministic (tests, benchmarks).
        /// </summary>
        public int MaxIterations { get; set; } = 5_000_000;

        /// <summary>
        /// Gets or sets the UCB exploration constant (rewards are in [0, 1]). Tuned in mirrored
        /// self-play: 0.1 and 0.2 lose to 0.4, which loses to 0.7; 1.2 is no better than 0.7.
        /// </summary>
        public double ExplorationConstant { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets a value indicating whether the deals respect what the play has shown about
        /// the hands (voids, missing trumps, belotes). Off, the unseen cards are dealt uniformly.
        /// </summary>
        public bool UsePlayInference { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the deals explain the auction: each other
        /// seat's first five cards should make its bids and passes the way SmartPlayer would
        /// (see <see cref="BidModel"/>).
        /// </summary>
        public bool UseBidInference { get; set; }

        public Random Rng { get; set; } = new Random();

        /// <summary>Gets or sets a value indicating whether to bid by Monte Carlo (else like SmartPlayer).</summary>
        public bool UseMonteCarloBidding { get; set; } = true;

        /// <summary>Gets or sets how many random deals value the bidding candidates.</summary>
        public int BiddingDeals { get; set; } = 300;

        /// <summary>Gets or sets how many game points a bid must be worth over passing.</summary>
        public double BidMargin { get; set; }

        /// <summary>Gets or sets a value indicating whether the Monte Carlo bidding may double and redouble.</summary>
        public bool MayDouble { get; set; }

        /// <summary>
        /// Gets or sets the chance that a rollout plays a random legal card instead of the greedy
        /// one (the variety helps a little: ~52% against none, twice).
        /// </summary>
        public double RolloutRandomness { get; set; } = 0.1;

        /// <summary>Gets how many decisions fell back to SmartPlayer (an inconsistent context).</summary>
        public int Fallbacks { get; private set; }

        /// <summary>Gets how many iterations the last search ran.</summary>
        public int LastIterations { get; private set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            if (context.RoundNumber == 1)
            {
                this.hangingPoints = 0;
            }

            return this.UseMonteCarloBidding ? this.ChooseBid(context) : this.smartPlayer.GetBid(context);
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var available = context.AvailableCardsToPlay;
            if (available.Count == 1)
            {
                return new PlayCardAction(available.FirstOrDefault());
            }

            var card = this.Search(context);
            if (card < 0)
            {
                this.Fallbacks++;
                return this.smartPlayer.PlayCard(context);
            }

            return new PlayCardAction(Card.AllCards[card]);
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
            this.hangingPoints = roundResult.HangingPoints;
        }

        public void EndOfGame(GameResult gameResult)
        {
            this.hangingPoints = 0;
        }

        internal BidType ChooseBid(PlayerGetBidContext context)
        {
            var me = context.MyPosition.Index();
            var current = context.CurrentContract;
            var available = context.AvailableBids;
            var count = 0;

            // What passing leads to if nobody bids again: the current contract, or a new deal (0).
            if (current.Type != BidType.Pass)
            {
                this.AddCandidate(ref count, current.Type, current.Player.Index(), BidType.Pass);
            }

            var bids = count;
            foreach (var bid in ContractBids)
            {
                if (available.HasFlag(bid))
                {
                    this.AddCandidate(ref count, bid, me, bid);
                }
            }

            var plain = current.Type & ~(BidType.Double | BidType.ReDouble);
            if (available.HasFlag(BidType.Double) && this.MayDouble)
            {
                this.AddCandidate(ref count, plain | BidType.Double, current.Player.Index(), BidType.Double);
            }

            if (available.HasFlag(BidType.ReDouble) && this.MayDouble)
            {
                this.AddCandidate(ref count, plain | BidType.ReDouble, current.Player.Index(), BidType.ReDouble);
            }

            if (count == bids)
            {
                return BidType.Pass;
            }

            this.bidEvaluator.Evaluate(
                context,
                this.candidateContracts,
                this.candidateDeclarers,
                count,
                this.BiddingDeals,
                this.hangingPoints,
                this.Rng,
                this.candidateValues);
            var best = BidType.Pass;
            var bestValue = (bids > 0 ? this.candidateValues[0] : 0) + this.BidMargin;
            for (var i = bids; i < count; i++)
            {
                if (this.candidateValues[i] > bestValue)
                {
                    best = this.candidateBids[i];
                    bestValue = this.candidateValues[i];
                }
            }

            return best;
        }

        /// <summary>Searches the decision; -1 when the context cannot be modelled.</summary>
        internal int Search(PlayerPlayCardContext context)
        {
            var contract = context.CurrentContract;
            this.simulator.SetContract(contract.Type, contract.Player.Index(), this.hangingPoints);
            if (!this.knowledge.Build(context, this.simulator, this.UsePlayInference) || !this.sampler.Configure(this.knowledge))
            {
                return -1;
            }

            var available = 0u;
            foreach (var card in context.AvailableCardsToPlay)
            {
                available |= 1u << card.GetHashCode();
            }

            if (this.simulator.LegalMoves(in this.knowledge.Root) != available)
            {
                return -1;
            }

            this.searchTeam = this.knowledge.Me & 1;
            this.biddingSeats = 0;
            if (this.UseBidInference)
            {
                this.bidModel.Read(context.Bids, context.FirstToPlayInTheRound);
                this.biddingSeats = this.bidModel.InformativeSeats & ~(1 << this.knowledge.Me);
            }

            // A deal is worth up to ~26 game points (more with a capot or combinations) in a
            // suit contract and ~35 otherwise; the doubling multiplies everything.
            this.rewardScale = (this.simulator.Kind < SimTables.NoTrumps ? 26.0 : 36.0) * this.simulator.Coefficient;
            this.rewardScale += this.hangingPoints;

            this.EnsurePool();
            this.nodeCount = 0;
            this.NewNode(-1);

            var start = Stopwatch.GetTimestamp();
            var limit = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            var iterations = 0;
            do
            {
                var state = this.knowledge.Root;
                this.SampleDeal(ref state);
                this.GetAnnouncePoints(in state, out var southNorthAnnounces, out var eastWestAnnounces);
                this.RunIteration(state, southNorthAnnounces, eastWestAnnounces);
                iterations++;
            }
            while (iterations < this.MaxIterations && Stopwatch.GetTimestamp() - start < limit);

            this.LastIterations = iterations;
            return this.MostVisitedRootMove();
        }

        private static int RandomCard(uint legal, Random random)
        {
            for (var skip = random.Next(BitOperations.PopCount(legal)); skip > 0; skip--)
            {
                legal &= legal - 1;
            }

            return BitOperations.TrailingZeroCount(legal);
        }

        // Deals the unseen cards, retrying (a few times) until the deal explains the auction.
        private void SampleDeal(ref SimState state)
        {
            for (var attempt = 0; attempt < MaxAuctionAttempts; attempt++)
            {
                this.sampler.Sample(ref state, this.Rng);
                if (this.ExplainsTheAuction(in state))
                {
                    return;
                }
            }
        }

        private bool ExplainsTheAuction(in SimState state)
        {
            for (var seats = this.biddingSeats; seats != 0; seats &= seats - 1)
            {
                var seat = BitOperations.TrailingZeroCount(seats);
                if (!this.bidModel.FitsRandomFive(seat, state.Hands[seat] | this.knowledge.PlayedBy[seat], this.Rng))
                {
                    return false;
                }
            }

            return true;
        }

        // The combinations that score in this deal: the declared ones (hidden ranks drawn at
        // random) plus, in the first trick, those the players still to declare hold in it.
        private void GetAnnouncePoints(in SimState state, out int southNorth, out int eastWest)
        {
            var knowledge = this.knowledge;
            var count = knowledge.AnnounceCount;
            Array.Copy(knowledge.Announces, this.worldAnnounces, count);
            for (var seats = knowledge.SeatsToDeclare; seats != 0; seats &= seats - 1)
            {
                var seat = BitOperations.TrailingZeroCount(seats);
                count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, this.worldAnnounces, count);
            }

            AnnounceScorer.ResolveHiddenRanks(this.worldAnnounces, count, this.Rng);
            AnnounceScorer.GetPoints(this.worldAnnounces, count, out southNorth, out eastWest);
        }

        private void RunIteration(SimState state, int southNorthAnnounces, int eastWestAnnounces)
        {
            var simulator = this.simulator;
            var nodes = this.nodes;
            var nodeId = 0;
            var pathLength = 0;
            this.path[pathLength++] = nodeId;
            while (state.TricksPlayed < 8)
            {
                var legal = simulator.LegalMoves(in state);
                var moverInMyTeam = (state.Turn & 1) == this.searchTeam;

                // Walk the children once: count the availability of those legal in this deal,
                // pick the best of them by UCB, and note which legal cards are in the tree.
                var covered = 0u;
                var bestChild = NoNode;
                var bestUcb = double.NegativeInfinity;
                for (var child = nodes[nodeId].FirstChild; child != NoNode; child = nodes[child].NextSibling)
                {
                    ref var node = ref nodes[child];
                    var bit = 1u << node.Move;
                    if ((legal & bit) == 0)
                    {
                        continue;
                    }

                    covered |= bit;
                    var availability = ++node.Availability;
                    var mean = node.Value / node.Visits;
                    var exploit = moverInMyTeam ? mean : 1d - mean;
                    var ucb = exploit + (this.ExplorationConstant * Math.Sqrt(Math.Log(availability) / node.Visits));
                    if (ucb > bestUcb)
                    {
                        bestUcb = ucb;
                        bestChild = child;
                    }
                }

                var untried = legal & ~covered;
                if (untried != 0 && this.nodeCount < NodeCapacity)
                {
                    var move = BitOperations.TrailingZeroCount(untried);
                    simulator.Play(ref state, move, legal);
                    var childId = this.NewNode(move);
                    nodes[childId].Availability = 1;
                    nodes[childId].NextSibling = nodes[nodeId].FirstChild;
                    nodes[nodeId].FirstChild = childId;
                    this.path[pathLength++] = childId;
                    break;
                }

                if (bestChild == NoNode)
                {
                    break;
                }

                simulator.Play(ref state, nodes[bestChild].Move, legal);
                nodeId = bestChild;
                this.path[pathLength++] = nodeId;
            }

            var reward = this.Rollout(ref state, southNorthAnnounces, eastWestAnnounces);
            for (var i = 0; i < pathLength; i++)
            {
                ref var node = ref nodes[this.path[i]];
                node.Visits++;
                node.Value += reward;
            }
        }

        // Plays the deal out with the greedy policy; the reward is the searching team's game
        // points minus the other team's, mapped to [0, 1].
        private double Rollout(ref SimState state, int southNorthAnnounces, int eastWestAnnounces)
        {
            var simulator = this.simulator;
            while (state.TricksPlayed < 8)
            {
                var legal = simulator.LegalMoves(in state);
                var card = this.RolloutRandomness > 0 && this.Rng.NextDouble() < this.RolloutRandomness
                    ? RandomCard(legal, this.Rng)
                    : simulator.ChooseRolloutMove(in state, legal);
                simulator.Play(ref state, card, legal);
            }

            simulator.Score(in state, southNorthAnnounces, eastWestAnnounces, out var southNorth, out var eastWest, out _);
            var difference = this.searchTeam == 0 ? southNorth - eastWest : eastWest - southNorth;
            var reward = 0.5 + (difference / (2 * this.rewardScale));
            return reward < 0 ? 0 : reward > 1 ? 1 : reward;
        }

        private void AddCandidate(ref int count, BidType contract, int declarer, BidType bid)
        {
            this.candidateContracts[count] = contract;
            this.candidateDeclarers[count] = declarer;
            this.candidateBids[count] = bid;
            count++;
        }

        private int MostVisitedRootMove()
        {
            var best = -1;
            var bestVisits = -1;
            var bestMean = double.NegativeInfinity;
            for (var child = this.nodes[0].FirstChild; child != NoNode; child = this.nodes[child].NextSibling)
            {
                var visits = this.nodes[child].Visits;
                var mean = visits > 0 ? this.nodes[child].Value / visits : 0;
                if (visits > bestVisits || (visits == bestVisits && mean > bestMean))
                {
                    best = this.nodes[child].Move;
                    bestVisits = visits;
                    bestMean = mean;
                }
            }

            return best;
        }

        private int NewNode(int move)
        {
            var id = this.nodeCount++;
            this.nodes[id] = new Node { Move = move, FirstChild = NoNode, NextSibling = NoNode };
            return id;
        }

        private void EnsurePool()
        {
            if (this.nodes == null)
            {
                this.nodes = new Node[NodeCapacity];

                // A deal has 32 cards, so a path has at most 33 nodes.
                this.path = new int[33];
            }
        }

        private struct Node
        {
            // Sum of the rewards (the searching team's view, in [0, 1]) of the iterations through here.
            public double Value;

            public int Visits;

            // Iterations in which this node's card was legal while its parent was selecting.
            public int Availability;

            // The card played from the parent to reach this node (-1 at the root).
            public int Move;

            public int FirstChild;

            public int NextSibling;
        }
    }
}
