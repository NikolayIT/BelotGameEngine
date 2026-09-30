namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Diagnostics;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Players;

    /// <summary>
    /// Values the legal cards by playing them out: it deals the unseen cards many times,
    /// consistently with what the play has shown (<see cref="RoundKnowledge"/>,
    /// <see cref="WorldSampler"/>, as ClaudePlayerIsmcts deals them), and in each deal plays
    /// every legal card and the rest of the deal with the networks deciding for every seat from
    /// what that seat can see. A card's value is its team's game points minus the other team's,
    /// averaged over the deals; every card is tried on the same deals, so the luck of the deals
    /// cancels out of their differences. Unlike ClaudePlayerIsmcts's greedy rollouts, nobody in
    /// the playouts sees the others' cards.
    /// </summary>
    internal sealed class NeuralSearch
    {
        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly WorldSampler sampler = new WorldSampler();
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly float[] values = new float[FeatureEncoder.CardOutputs];
        private readonly double[] sums = new double[FeatureEncoder.CardOutputs];
        private readonly int[] counts = new int[FeatureEncoder.CardOutputs];
        private readonly float[] prior = new float[FeatureEncoder.CardOutputs];
        private readonly double[] heuristicSums = new double[FeatureEncoder.CardOutputs];
        private readonly double[] overlapHeuristicSums = new double[FeatureEncoder.CardOutputs];
        private readonly double[] neuralDifferenceSums = new double[FeatureEncoder.CardOutputs];
        private readonly double[] neuralDifferenceSquares = new double[FeatureEncoder.CardOutputs];
        private readonly double[] residualDifferenceSums = new double[FeatureEncoder.CardOutputs];
        private readonly double[] residualDifferenceSquares = new double[FeatureEncoder.CardOutputs];
        private readonly float[] ownershipWeights = new float[CardOwnershipModel.Outputs];
        private DeclaredWorldSampler declaredSampler;
        private WeightedWorldSampler weightedSampler;
        private bool weighted;
        private uint searched;

        /// <summary>
        /// Gets or sets the card ownership model that weights the worlds (null deals the unseen
        /// cards uniformly): its predictions, from the auction and the play, make the likely hands
        /// likelier, as the endgame's worlds are.
        /// </summary>
        public CardOwnershipModel.Evaluator Ownership { get; set; }

        public double OwnershipPower { get; set; } = 1;

        public double OwnershipUniformMix { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets a value indicating whether worlds after the first trick are uniform
        /// assignments conditioned on all declared combination types. Assumes every seat
        /// declares all available combinations; hidden ranks follow from the accepted hands.
        /// </summary>
        public bool UseDeclarations { get; set; }

        public int DeclarationSampleAttempts { get; private set; }

        public int DeclarationRejectedWorlds { get; private set; }

        /// <summary>
        /// Gets or sets the total number of cheap, deterministic greedy-rollout worlds. Zero
        /// keeps ordinary neural search. A positive value must be at least the neural deal
        /// count and cannot be combined with a prior or pruning. The first neural-count worlds
        /// run both policies: mean(G, all worlds) + mean(N - G, overlapping worlds).
        /// </summary>
        public int ControlVariateDeals { get; set; }

        /// <summary>
        /// Gets or sets how many additional tricks to finish before using an eligible mover's
        /// best legal network value. Forced cards continue because the networks were trained
        /// only at choices. Zero plays through to the final score. The horizon value estimates
        /// the whole deal, so previously won points are not added again.
        /// </summary>
        public int RolloutTricks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether truncated rollouts continue until the root
        /// seat has a choice again, or the deal finishes. Its network then retains the root's
        /// private hand; another seat's value would condition on different private information.
        /// False permits that approximation as an experimental ablation, with team sign flipped.
        /// </summary>
        public bool RolloutRootLeaf { get; set; } = true;

        /// <summary>
        /// Gets or sets how many final tricks use exact partnership minimax inside each sampled
        /// world. Zero uses neural policies throughout; two or three switch to perfect information
        /// near the end. This is an approximate imperfect-information evaluator, not a Q bootstrap.
        /// </summary>
        public int DoubleDummyTricks { get; set; }

        /// <summary>Gets or sets the solver of the double-dummy leaves (null: plain minimax, fine for two or three tricks).</summary>
        public EndgameSearch LeafSolver { get; set; }

        /// <summary>
        /// Gets or sets how many of the cards the network values best are played out (0: all);
        /// the others keep the network's value less a thousand points, so they are never chosen.
        /// </summary>
        public int CandidateCards { get; set; }

        /// <summary>Gets or sets how far below the network's best a card may be and still be played out (0: no limit).</summary>
        public double CandidateMargin { get; set; }

        /// <summary>
        /// Gets or sets the suit ensemble that values the cards for the candidates and the prior
        /// (null: one forward pass); the rollouts themselves always use the plain network.
        /// </summary>
        public SuitEnsembleEvaluator PriorEnsemble { get; set; }

        public int DoubleDummyLeaves { get; private set; }

        public int NeuralDealsCompleted { get; private set; }

        public int ControlVariateDealsCompleted { get; private set; }

        /// <summary>Gets how many candidate bootstrap leaves were skipped because their only card was forced.</summary>
        public int SkippedForcedLeaves { get; private set; }

        /// <summary>
        /// Gets the average sample variance of each non-pivot action's neural return minus the
        /// lowest-index legal card's return. Zero when fewer than two overlap worlds finished.
        /// </summary>
        public double NeuralDifferenceVariance { get; private set; }

        /// <summary>
        /// Gets the same variance after subtracting the greedy action difference. Comparing
        /// this with <see cref="NeuralDifferenceVariance"/> measures the control's usefulness;
        /// it does not include the error of estimating the greedy mean from finitely many worlds.
        /// </summary>
        public double ResidualDifferenceVariance { get; private set; }

        /// <summary>
        /// Gets or sets how many deals the card network's own value counts as: it is averaged in
        /// with the playouts, which steadies a small number of deals.
        /// </summary>
        public double PriorDeals { get; set; }

        /// <summary>
        /// Gets or sets how far below the best, in game points, a card may fall after half the
        /// deals and still be played out in the rest (the others keep their first-half average);
        /// 0 plays every card in every deal.
        /// </summary>
        public double PruneMargin { get; set; }

        /// <summary>
        /// Gets or sets a time budget in milliseconds (0 = none): no new deal is started once it
        /// is spent, so a slow device plays fewer deals (at least <see cref="MinimumDeals"/>).
        /// </summary>
        public int TimeLimitMilliseconds { get; set; }

        public int MinimumDeals { get; set; } = 8;

        /// <summary>
        /// Fills the value of every legal card (indexed by card) from the given number of deals.
        /// </summary>
        /// <returns>False when the context cannot be modelled.</returns>
        public bool Evaluate(
            PlayerPlayCardContext context,
            in NeuralDeal deal,
            uint legal,
            int deals,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            float[] cardValues)
        {
            this.NeuralDealsCompleted = 0;
            this.ControlVariateDealsCompleted = 0;
            this.SkippedForcedLeaves = 0;
            this.DoubleDummyLeaves = 0;
            this.DeclarationSampleAttempts = 0;
            this.DeclarationRejectedWorlds = 0;
            this.NeuralDifferenceVariance = 0;
            this.ResidualDifferenceVariance = 0;
            if (this.RolloutTricks < 0 || this.RolloutTricks > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(this.RolloutTricks), "Rollout tricks must be between zero and eight.");
            }

            if (this.DoubleDummyTricks != 0 && (this.DoubleDummyTricks < 2 || this.DoubleDummyTricks > 5))
            {
                throw new ArgumentOutOfRangeException(nameof(this.DoubleDummyTricks), "Double-dummy tricks must be zero or two to five.");
            }

            if (this.DoubleDummyTricks > 0 && (this.RolloutTricks > 0 || this.ControlVariateDeals > 0 || this.PruneMargin > 0))
            {
                throw new InvalidOperationException("Double-dummy rollouts cannot use Q bootstrapping, control variates or root pruning.");
            }

            if (this.ControlVariateDeals < 0
                || (this.ControlVariateDeals > 0 && (deals <= 0 || this.ControlVariateDeals < deals)))
            {
                throw new ArgumentOutOfRangeException(nameof(this.ControlVariateDeals), "Control-variate worlds must cover a positive neural deal count.");
            }

            if (this.ControlVariateDeals > 0 && (this.PriorDeals > 0 || this.PruneMargin > 0))
            {
                throw new InvalidOperationException("Control-variate search cannot use prior deals or pruning.");
            }

            if (this.ControlVariateDeals > 0 && this.RolloutTricks > 0)
            {
                throw new InvalidOperationException("Control-variate search cannot use truncated rollouts.");
            }

            if (this.ControlVariateDeals > 0 && this.UseDeclarations)
            {
                throw new InvalidOperationException("Declaration-conditioned search cannot use control variates.");
            }

            // The deal gave the simulator its contract; the knowledge replays the play with it.
            if (!this.knowledge.Build(context, simulator, usePlayInference: true))
            {
                return false;
            }

            var conditionDeclarations = this.UseDeclarations && deal.Play.TricksPlayed > 0;
            if (conditionDeclarations)
            {
                this.declaredSampler ??= new DeclaredWorldSampler();
                if (!this.declaredSampler.Configure(context, this.knowledge, simulator.Kind))
                {
                    return false;
                }
            }
            else if (!this.sampler.Configure(this.knowledge))
            {
                return false;
            }

            this.weighted = false;
            if (!conditionDeclarations && this.Ownership != null && this.OwnershipPower > 0)
            {
                this.Ownership.Evaluate(in deal, legal, this.ownershipWeights, context);
                for (var i = 0; i < this.ownershipWeights.Length; i++)
                {
                    this.ownershipWeights[i] = (float)Math.Pow(
                        (this.OwnershipUniformMix / 3) + ((1 - this.OwnershipUniformMix) * this.ownershipWeights[i]),
                        this.OwnershipPower);
                }

                this.weightedSampler ??= new WeightedWorldSampler();
                this.weighted = this.weightedSampler.Configure(this.knowledge, this.ownershipWeights);
            }

            this.searched = legal;
            if (this.ControlVariateDeals > 0)
            {
                this.EvaluateControlVariate(in deal, legal, deals, evaluator, simulator, random, cardValues);
                return true;
            }

            var team = deal.Play.Turn & 1;
            this.LeafSolver?.BeginWorlds(simulator, team, context);
            var lastTrick = this.RolloutTricks == 0 ? 8 : Math.Min(8, deal.Play.TricksPlayed + this.RolloutTricks);
            Array.Clear(this.sums);
            Array.Clear(this.counts);
            if (this.PriorDeals > 0 || this.CandidateCards > 0 || this.CandidateMargin > 0)
            {
                if (this.PriorEnsemble != null)
                {
                    this.PriorEnsemble.EvaluateCards(in deal, legal, evaluator, this.prior);
                }
                else
                {
                    evaluator.EvaluateCards(in deal, legal, this.prior);
                }
            }

            var playing = this.CandidateCards > 0 || this.CandidateMargin > 0 ? this.Candidates(legal) : legal;
            this.searched = playing;
            var start = Stopwatch.GetTimestamp();
            var limit = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            var proposalLimit = (int)Math.Min(100000L, Math.Max(64L, 64L * deals));
            for (var i = 0; i < deals; i++)
            {
                if (this.TimeLimitMilliseconds > 0 && i >= this.MinimumDeals && Stopwatch.GetTimestamp() - start > limit)
                {
                    break;
                }

                var state = this.knowledge.Root;
                var southNorthAnnounces = 0;
                var eastWestAnnounces = 0;
                if (conditionDeclarations)
                {
                    var accepted = false;
                    while (this.DeclarationSampleAttempts < proposalLimit)
                    {
                        // Rejection must respect the deadline even below MinimumDeals.
                        // Always try one proposal; complete every root action of accepted worlds.
                        if (this.TimeLimitMilliseconds > 0 && this.DeclarationSampleAttempts > 0
                                                          && Stopwatch.GetTimestamp() - start > limit)
                        {
                            break;
                        }

                        this.DeclarationSampleAttempts++;
                        if (this.declaredSampler.TrySample(ref state, random, out southNorthAnnounces, out eastWestAnnounces))
                        {
                            accepted = true;
                            break;
                        }

                        this.DeclarationRejectedWorlds++;
                    }

                    if (!accepted)
                    {
                        break;
                    }
                }
                else if (this.weighted)
                {
                    this.weightedSampler.Sample(ref state, random);
                }
                else
                {
                    this.sampler.Sample(ref state, random);
                }

                var world = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    world.Play.Hands[seat] = state.Hands[seat];
                }

                if (conditionDeclarations)
                {
                    world.SouthNorthAnnounces = southNorthAnnounces;
                    world.EastWestAnnounces = eastWestAnnounces;
                }
                else
                {
                    this.Declarations(ref world, in state, random);
                }

                if (i == deals / 2 && this.PruneMargin > 0)
                {
                    playing = this.Contenders(legal, cardValues);
                }

                for (var rest = playing; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (!copy.IsFinished)
                    {
                        if (this.DoubleDummyTricks > 0 && copy.Play.TricksPlayed >= 8 - this.DoubleDummyTricks)
                        {
                            break;
                        }

                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        var forced = (moves & (moves - 1)) == 0;
                        var atHorizon = this.RolloutTricks > 0 && copy.Play.TricksPlayed >= lastTrick
                            && (!this.RolloutRootLeaf || copy.Play.Turn == deal.Play.Turn);
                        if (atHorizon && !forced)
                        {
                            break;
                        }

                        if (atHorizon)
                        {
                            this.SkippedForcedLeaves++;
                        }

                        var move = forced
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, this.values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    if (copy.IsFinished)
                    {
                        copy.Score(simulator, out var southNorth, out var eastWest, out _);
                        this.sums[card] += this.LeafSolver != null
                            ? this.LeafSolver.FinishedValue(southNorth, eastWest, copy.Play.SouthNorthTricks == 0 || copy.Play.EastWestTricks == 0)
                            : team == 0 ? southNorth - eastWest : eastWest - southNorth;
                    }
                    else if (this.DoubleDummyTricks > 0)
                    {
                        this.sums[card] += this.LeafSolver != null
                            ? this.LeafSolver.SolveWorld(in copy.Play, copy.SouthNorthAnnounces, copy.EastWestAnnounces)
                            : EndgameSearch.Solve(in copy.Play, simulator, team, copy.SouthNorthAnnounces, copy.EastWestAnnounces);
                        this.DoubleDummyLeaves++;
                    }
                    else
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        evaluator.EvaluateCards(in copy, moves, this.values);
                        var value = this.values[NeuralEvaluator.Best(this.values, moves)];
                        this.sums[card] += (copy.Play.Turn & 1) == team ? value : -value;
                    }

                    this.counts[card]++;
                }

                this.NeuralDealsCompleted++;
            }

            if (conditionDeclarations && this.NeuralDealsCompleted == 0)
            {
                return false;
            }

            this.Averages(legal, cardValues);
            return true;
        }

        private void EvaluateControlVariate(
            in NeuralDeal deal,
            uint legal,
            int deals,
            NeuralEvaluator evaluator,
            BelotSimulator simulator,
            Random random,
            float[] cardValues)
        {
            Array.Clear(this.sums);
            Array.Clear(this.heuristicSums);
            Array.Clear(this.overlapHeuristicSums);
            Array.Clear(this.neuralDifferenceSums);
            Array.Clear(this.neuralDifferenceSquares);
            Array.Clear(this.residualDifferenceSums);
            Array.Clear(this.residualDifferenceSquares);
            var team = deal.Play.Turn & 1;
            var pivot = BitOperations.TrailingZeroCount(legal);
            Span<int> neuralReturns = stackalloc int[FeatureEncoder.CardOutputs];
            Span<int> heuristicReturns = stackalloc int[FeatureEncoder.CardOutputs];
            var start = Stopwatch.GetTimestamp();
            var limit = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            for (var i = 0; i < this.ControlVariateDeals; i++)
            {
                // Complete every action of a world before considering the deadline. The
                // resulting estimator uses actual counts; stopping based on elapsed time can
                // correlate with the worlds, so unbiasedness is only claimed at fixed counts.
                if (this.TimeLimitMilliseconds > 0 && i >= Math.Max(1, this.MinimumDeals)
                                                   && Stopwatch.GetTimestamp() - start > limit)
                {
                    break;
                }

                var state = this.knowledge.Root;
                this.sampler.Sample(ref state, random);
                var world = deal;
                for (var seat = 0; seat < 4; seat++)
                {
                    world.Play.Hands[seat] = state.Hands[seat];
                }

                this.Declarations(ref world, in state, random);
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var greedy = world;
                    greedy.PlayCard(simulator, card, legal);
                    while (!greedy.IsFinished)
                    {
                        greedy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in greedy.Play);
                        greedy.PlayCard(simulator, simulator.ChooseRolloutMove(in greedy.Play, moves), moves);
                    }

                    greedy.Score(simulator, out var southNorth, out var eastWest, out _);
                    var greedyReturn = team == 0 ? southNorth - eastWest : eastWest - southNorth;
                    heuristicReturns[card] = greedyReturn;
                    this.heuristicSums[card] += greedyReturn;
                    if (i >= deals)
                    {
                        continue;
                    }

                    var copy = world;
                    copy.PlayCard(simulator, card, legal);
                    while (!copy.IsFinished)
                    {
                        copy.DeclareIfFirstCard();
                        var moves = simulator.LegalMoves(in copy.Play);
                        var move = (moves & (moves - 1)) == 0
                            ? BitOperations.TrailingZeroCount(moves)
                            : evaluator.BestCard(in copy, moves, this.values);
                        copy.PlayCard(simulator, move, moves);
                    }

                    copy.Score(simulator, out southNorth, out eastWest, out _);
                    var neuralReturn = team == 0 ? southNorth - eastWest : eastWest - southNorth;
                    neuralReturns[card] = neuralReturn;
                    this.sums[card] += neuralReturn;
                    this.overlapHeuristicSums[card] += greedyReturn;
                }

                this.ControlVariateDealsCompleted++;
                if (i < deals)
                {
                    this.NeuralDealsCompleted++;
                    for (var rest = legal & ~(1u << pivot); rest != 0; rest &= rest - 1)
                    {
                        var card = BitOperations.TrailingZeroCount(rest);
                        double neuralDifference = neuralReturns[card] - neuralReturns[pivot];
                        var residualDifference = neuralDifference - (heuristicReturns[card] - heuristicReturns[pivot]);
                        this.neuralDifferenceSums[card] += neuralDifference;
                        this.neuralDifferenceSquares[card] += neuralDifference * neuralDifference;
                        this.residualDifferenceSums[card] += residualDifference;
                        this.residualDifferenceSquares[card] += residualDifference * residualDifference;
                    }
                }
            }

            var neuralCount = this.NeuralDealsCompleted;
            var greedyCount = this.ControlVariateDealsCompleted;
            var differences = BitOperations.PopCount(legal) - 1;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var correction = (this.heuristicSums[card] / greedyCount) - (this.overlapHeuristicSums[card] / neuralCount);
                cardValues[card] = (float)((this.sums[card] / neuralCount) + correction);
                if (card != pivot && neuralCount > 1)
                {
                    var neuralCenteredSquares = this.neuralDifferenceSquares[card]
                        - (this.neuralDifferenceSums[card] * this.neuralDifferenceSums[card] / neuralCount);
                    var residualCenteredSquares = this.residualDifferenceSquares[card]
                        - (this.residualDifferenceSums[card] * this.residualDifferenceSums[card] / neuralCount);
                    this.NeuralDifferenceVariance += Math.Max(0, neuralCenteredSquares) / (neuralCount - 1) / differences;
                    this.ResidualDifferenceVariance += Math.Max(0, residualCenteredSquares) / (neuralCount - 1) / differences;
                }
            }
        }

        // The best cards by the network's values: at most CandidateCards, within CandidateMargin.
        private uint Candidates(uint legal)
        {
            var best = NeuralEvaluator.Best(this.prior, legal);
            var chosen = 1u << best;
            var limit = this.CandidateCards > 0 ? this.CandidateCards : 32;
            while (BitOperations.PopCount(chosen) < limit)
            {
                var next = -1;
                for (var rest = legal & ~chosen; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    if ((this.CandidateMargin <= 0 || this.prior[card] >= this.prior[best] - this.CandidateMargin)
                        && (next < 0 || this.prior[card] > this.prior[next]))
                    {
                        next = card;
                    }
                }

                if (next < 0)
                {
                    break;
                }

                chosen |= 1u << next;
            }

            return chosen;
        }

        // Each card's average so far, the network's value counted as PriorDeals deals.
        private void Averages(uint legal, float[] cardValues)
        {
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if ((this.searched & (1u << card)) == 0)
                {
                    cardValues[card] = this.prior[card] - 1000;
                    continue;
                }

                var weight = this.PriorDeals > 0 ? this.PriorDeals : 0;
                var total = this.sums[card] + (weight * this.prior[card]);
                cardValues[card] = (float)(total / Math.Max(1e-9, this.counts[card] + weight));
            }
        }

        // The cards within PruneMargin of the best so far.
        private uint Contenders(uint legal, float[] cardValues)
        {
            this.Averages(legal, cardValues);
            var best = float.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                best = Math.Max(best, cardValues[BitOperations.TrailingZeroCount(rest)]);
            }

            var contenders = 0u;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (cardValues[card] >= best - this.PruneMargin)
                {
                    contenders |= 1u << card;
                }
            }

            return contenders;
        }

        // The combinations of this deal: those declared (hidden ranks drawn at random), and in
        // the first trick those the seats still to declare hold in it, which they declare when
        // they play.
        private void Declarations(ref NeuralDeal world, in SimState state, Random random)
        {
            var count = this.knowledge.AnnounceCount;
            Array.Copy(this.knowledge.Announces, this.announces, count);
            for (var seats = this.knowledge.SeatsToDeclare; seats != 0; seats &= seats - 1)
            {
                var seat = BitOperations.TrailingZeroCount(seats);
                var start = count;
                count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat], seat, this.announces, count);
                world.ToDeclare[seat] = 0;
                for (var i = start; i < count; i++)
                {
                    world.ToDeclare[seat] |= NeuralDeal.DeclarationBit(this.announces[i].Type);
                }
            }

            AnnounceScorer.ResolveHiddenRanks(this.announces, count, random);
            AnnounceScorer.GetPoints(this.announces, count, out world.SouthNorthAnnounces, out world.EastWestAnnounces);
        }
    }
}
