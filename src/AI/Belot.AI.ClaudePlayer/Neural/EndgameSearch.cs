namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Diagnostics;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Enumerates remaining deals in the final two or three tricks, and optionally samples
    /// larger endings. Solves each world by partnership minimax. Perfect-information results approximate
    /// the seat's values (PIMC); future decisions inside a world have perfect information.
    /// Declines inconsistent constraints. Unresolved announcement ranks require the optional
    /// model in which every seat declares all available combinations, as the bots do.
    /// </summary>
    internal sealed class EndgameSearch
    {
        /// <summary>Raw card points per game point when <see cref="RawTieBreak"/> orders equal results.</summary>
        public const int RawScale = 1024;

        /// <summary>Solver units per match won when <see cref="Equity"/> values the leaves.</summary>
        public const int EquityUnits = 100000;

        private const int TranspositionCapacity = 8192;

        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly int[] pool = new int[9];
        private readonly int[] needs = new int[4];
        private readonly double[] sums = new double[32];
        private readonly uint[] declaredCounts = new uint[4];
        private readonly uint[] worldCounts = new uint[4];
        private readonly DeclaredAnnounce[] worldAnnounces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly int[] worldValues = new int[32];
        private readonly float[] ownershipWeights = new float[CardOwnershipModel.Outputs];
        private readonly uint[] bidHonours = new uint[4];
        private UniformWorldSampler sampler;
        private WeightedWorldSampler weightedSampler;
        private PlayLikelihood likelihood;
        private TranspositionEntry[] transpositions;
        private uint transpositionGeneration;
        private SimState[] worlds = new SimState[90];
        private int[] worldSouthNorthAnnounces = new int[90];
        private int[] worldEastWestAnnounces = new int[90];
        private BelotSimulator simulator;
        private int poolCount;
        private int team;
        private int southNorthAnnounces;
        private int eastWestAnnounces;
        private int worldLimit;
        private int enumeratedWorlds;
        private bool overflow;
        private bool interrupted;
        private long deadline;
        private double totalWeight;
        private double squaredWeight;
        private int matchOurs;
        private int matchTheirs;
        private double pointsPerEquity = 1;
        private int signalSeat;
        private uint signalHonours0;
        private uint signalHonours1;
        private uint signalHonours2;
        private uint signalHonours3;

        public CardOwnershipModel.Evaluator Ownership { get; set; }

        public double OwnershipPower { get; set; } = 0.5;

        public double OwnershipUniformMix { get; set; } = 0.1;

        public int PolicyActions { get; set; }

        public double PolicyTemperature { get; set; } = 2;

        public double PolicyUniformMix { get; set; } = 0.1;

        public double PolicyPower { get; set; } = 0.5;

        public double EffectiveWorlds => this.squaredWeight > 0 ? this.totalWeight * this.totalWeight / this.squaredWeight : 0;

        public int LikelihoodEvaluations { get; private set; }

        public int Worlds { get; private set; }

        public int Nodes { get; private set; }

        public int SampleAttempts { get; private set; }

        public int IncompleteWorlds { get; private set; }

        public int TranspositionProbes { get; private set; }

        public int TranspositionHits { get; private set; }

        public int TranspositionCutoffs { get; private set; }

        /// <summary>Gets or sets whether exact trick-boundary states share alpha-beta bounds within a decision.</summary>
        public bool UseTranspositions { get; set; }

        /// <summary>Gets or sets whether to condition on the bots' policy of declaring every available combination.</summary>
        public bool UseDeclarations { get; set; }

        public int Tricks { get; set; } = 2;

        public int ThreeTrickWorldLimit { get; set; } = 8;

        /// <summary>Gets or sets sampled worlds after exact enumeration overflows, and for four or five tricks; zero disables sampling.</summary>
        public int SampledWorlds { get; set; }

        /// <summary>Gets or sets the maximum visited solver nodes per decision; zero is unlimited.</summary>
        public int NodeLimit { get; set; }

        /// <summary>Gets or sets the total decision budget; zero is unlimited.</summary>
        public int TimeLimitMilliseconds { get; set; }

        /// <summary>Gets or sets whether equivalent zero-point cards share one internal minimax branch.</summary>
        public bool PruneEquivalentCards { get; set; }

        /// <summary>
        /// Gets or sets whether results equal in game points are ordered by the raw card points
        /// (tricks, belotes and the last ten) the team takes: game points still decide, but where
        /// the rounding or a decided contract makes them equal, the solver keeps its points as a
        /// person would. Values stay in game points, raw points adding 1/<see cref="RawScale"/> each.
        /// </summary>
        public bool RawTieBreak { get; set; }

        /// <summary>
        /// Gets or sets how much less likely a world is for each suit the partner threw away (by
        /// the convention: while the opponents held the trick) in which that world gives the partner
        /// an honour (an ace or ten, in all trumps a jack or nine): 1 (the default) reads nothing,
        /// smaller values trust the convention more.
        /// </summary>
        public double PartnerSignalWeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets how much less likely a world is for each other seat that bid a suit as
        /// trumps and holds in it neither its jack nor its nine, of those still unseen (natural
        /// bids show one of them): 1 (the default) reads nothing from the bids.
        /// </summary>
        public double BidderHonourWeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets the match equity that values finished deals (null: the deal's game points):
        /// the chance to win the match from the scores the deal leaves. Values stay in game points,
        /// converted at the current scores' exchange rate.
        /// </summary>
        public MatchEquity Equity { get; set; }

        public bool Evaluate(PlayerPlayCardContext context, in NeuralDeal deal, uint legal, BelotSimulator simulator, float[] values, Random random = null, NeuralEvaluator evaluator = null)
        {
            this.Worlds = 0;
            this.Nodes = 0;
            this.totalWeight = 0;
            this.squaredWeight = 0;
            this.LikelihoodEvaluations = 0;
            this.SampleAttempts = 0;
            this.IncompleteWorlds = 0;
            this.TranspositionProbes = 0;
            this.TranspositionHits = 0;
            this.TranspositionCutoffs = 0;
            this.enumeratedWorlds = 0;
            this.interrupted = false;
            this.deadline = this.TimeLimitMilliseconds > 0
                ? Stopwatch.GetTimestamp() + ((long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L)
                : long.MaxValue;
            if (deal.Play.TricksPlayed < 8 - this.Tricks || !this.knowledge.Build(context, simulator, usePlayInference: true))
            {
                return false;
            }

            Array.Clear(this.declaredCounts);
            for (var i = 0; i < this.knowledge.AnnounceCount; i++)
            {
                var announce = this.knowledge.Announces[i];
                this.declaredCounts[announce.Seat] += 1u << (2 * (int)announce.Type);
                if (!this.UseDeclarations && announce.Rank < 0)
                {
                    return false;
                }
            }

            // RoundKnowledge reconstructs our melds under the bot's declare-all policy.
            // A hint can be requested for a human who withheld one: do not score it anyway.
            var observedOwn = 0u;
            foreach (var announce in context.Announces)
            {
                if (announce.Player.Index() == this.knowledge.Me && announce.Type != AnnounceType.Belot)
                {
                    observedOwn += 1u << (2 * (int)announce.Type);
                }
            }

            if (observedOwn != this.declaredCounts[this.knowledge.Me])
            {
                return false;
            }

            if (!this.UseDeclarations)
            {
                AnnounceScorer.GetPoints(this.knowledge.Announces, this.knowledge.AnnounceCount, out this.southNorthAnnounces, out this.eastWestAnnounces);
            }

            this.simulator = simulator;
            this.team = this.knowledge.Me & 1;
            this.SetMatch(context, this.team);
            if (this.UseTranspositions)
            {
                this.transpositions ??= new TranspositionEntry[TranspositionCapacity];
                this.transpositionGeneration = unchecked(this.transpositionGeneration + 1);
                if (this.transpositionGeneration == 0)
                {
                    Array.Clear(this.transpositions);
                    this.transpositionGeneration = 1;
                }
            }

            if (this.Ownership != null && this.OwnershipPower > 0)
            {
                this.Ownership.Evaluate(in deal, legal, this.ownershipWeights, context);
                for (var i = 0; i < this.ownershipWeights.Length; i++)
                {
                    this.ownershipWeights[i] = (float)Math.Pow(
                        (this.OwnershipUniformMix / 3) + ((1 - this.OwnershipUniformMix) * this.ownershipWeights[i]),
                        this.OwnershipPower);
                }
            }

            this.ReadSignals(context, simulator.Kind);
            this.ReadBids(context);
            if (this.PolicyActions > 0)
            {
                this.likelihood ??= new PlayLikelihood();
                this.likelihood.Temperature = this.PolicyTemperature;
                this.likelihood.UniformMix = this.PolicyUniformMix;
                this.likelihood.Power = this.PolicyPower;
                if (evaluator == null || !this.likelihood.Configure(context, simulator, evaluator, this.PolicyActions))
                {
                    return false;
                }
            }

            this.worldLimit = deal.Play.TricksPlayed < 6 ? this.ThreeTrickWorldLimit : 90;
            this.overflow = false;
            Array.Clear(this.sums);
            Array.Clear(this.needs);
            if (deal.Play.TricksPlayed < 5)
            {
                return this.EvaluateSamples(legal, values, random);
            }

            if (this.worldLimit > this.worlds.Length)
            {
                Array.Resize(ref this.worlds, this.worldLimit);
                Array.Resize(ref this.worldSouthNorthAnnounces, this.worldLimit);
                Array.Resize(ref this.worldEastWestAnnounces, this.worldLimit);
            }

            var state = this.knowledge.Root;
            var unseen = ~(this.knowledge.Played | this.knowledge.MyHand);
            var needed = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat == this.knowledge.Me)
                {
                    continue;
                }

                state.Hands[seat] = this.knowledge.Known[seat];
                unseen &= ~state.Hands[seat];
                this.needs[seat] = this.knowledge.HandCounts[seat] - BitOperations.PopCount(state.Hands[seat]);
                if (this.needs[seat] < 0)
                {
                    return false;
                }

                needed += this.needs[seat];
            }

            this.poolCount = BitOperations.PopCount(unseen);
            if (this.poolCount > this.pool.Length || this.poolCount != needed)
            {
                return false;
            }

            var index = 0;
            for (var rest = unseen; rest != 0; rest &= rest - 1)
            {
                this.pool[index++] = BitOperations.TrailingZeroCount(rest);
            }

            this.Enumerate(0, in state);
            if (this.interrupted || this.enumeratedWorlds == 0)
            {
                return false;
            }

            if (this.overflow)
            {
                return this.EvaluateSamples(legal, values, random);
            }

            for (var world = 0; world < this.enumeratedWorlds; world++)
            {
                if (!this.EvaluateWorld(in this.worlds[world], legal, this.worldSouthNorthAnnounces[world], this.worldEastWestAnnounces[world], this.OwnershipWeight(in this.worlds[world])))
                {
                    // Exact enumeration remains all-or-fallback, including solver cutoffs.
                    this.Worlds = 0;
                    this.totalWeight = 0;
                    this.squaredWeight = 0;
                    return false;
                }
            }

            return this.Averages(legal, values);
        }

        internal static int Solve(in SimState state, BelotSimulator simulator, int team, int southNorthAnnounces, int eastWestAnnounces) =>
            Solve(in state, simulator, team, southNorthAnnounces, eastWestAnnounces, int.MinValue, int.MaxValue);

        internal static uint EquivalentChoices(in SimState state, BelotSimulator simulator, uint legal)
        {
            var zeroRanks = simulator.Kind == SimTables.AllTrumps ? 0x03030303u : 0x07070707u;
            if (simulator.Kind < SimTables.NoTrumps)
            {
                zeroRanks &= ~(1u << ((simulator.Kind * 8) + 2));
            }

            var zeroCards = legal & zeroRanks;
            if ((zeroCards & ((zeroCards >> 1) | (zeroCards >> 2))) == 0)
            {
                return legal;
            }

            var others = 0u;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat != state.Turn)
                {
                    others |= state.Hands[seat];
                }
            }

            if (state.TrickCards > 0)
            {
                others |= 1u << state.WinnerCard;
            }

            var choices = legal;
            for (var suit = 0; suit < 4; suit++)
            {
                // Seven/eight are consecutive zero-point ranks; the plain nine joins them.
                // No belote card is removed. Zero-point ranks have the same order in every contract.
                var zero = zeroCards & SimTables.SuitMasks[suit];
                if ((zero & (zero - 1)) == 0)
                {
                    continue;
                }

                var representative = BitOperations.TrailingZeroCount(zero);
                for (var rest = zero & (zero - 1); rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var between = ((1u << card) - 1u) ^ ((1u << (representative + 1)) - 1u);
                    if ((others & between) == 0)
                    {
                        choices &= ~(1u << card);
                    }
                    else
                    {
                        representative = card;
                    }
                }
            }

            return choices;
        }

        /// <summary>Starts a new decision for <see cref="SolveWorld"/>: its table entries may not carry over.</summary>
        internal void BeginWorlds(BelotSimulator simulator, int team, BasePlayerContext context = null)
        {
            this.simulator = simulator;
            this.team = team;
            this.SetMatch(context, team);
            this.Nodes = 0;
            this.deadline = long.MaxValue;
            if (this.UseTranspositions)
            {
                this.transpositions ??= new TranspositionEntry[TranspositionCapacity];
                this.transpositionGeneration = unchecked(this.transpositionGeneration + 1);
                if (this.transpositionGeneration == 0)
                {
                    Array.Clear(this.transpositions);
                    this.transpositionGeneration = 1;
                }
            }
        }

        /// <summary>
        /// Solves one fully known position (every hand in it) by partnership minimax for the team of
        /// <see cref="BeginWorlds"/>, in game points (raw points order ties when <see cref="RawTieBreak"/>).
        /// </summary>
        internal double SolveWorld(in SimState state, int southNorthAnnounces, int eastWestAnnounces)
        {
            var limit = this.NodeLimit;
            var time = this.TimeLimitMilliseconds;
            this.NodeLimit = 0;
            this.TimeLimitMilliseconds = 0;
            this.SolveBounded(in state, southNorthAnnounces, eastWestAnnounces, int.MinValue, int.MaxValue, out var value);
            this.NodeLimit = limit;
            this.TimeLimitMilliseconds = time;
            return this.ToPoints(value);
        }

        /// <summary>The team's value of a finished deal's award, in the solver's units.</summary>
        internal int Utility(int southNorth, int eastWest, bool capot)
        {
            var gained = this.team == 0 ? southNorth : eastWest;
            var lost = this.team == 0 ? eastWest : southNorth;
            if (this.Equity == null)
            {
                return gained - lost;
            }

            return (int)Math.Round(this.Equity.AfterDeal(this.matchOurs, this.matchTheirs, gained, lost, capot) * EquityUnits);
        }

        /// <summary>The team's value of a finished deal's award, in game points (as <see cref="SolveWorld"/> gives them).</summary>
        internal double FinishedValue(int southNorth, int eastWest, bool capot)
        {
            var value = this.Utility(southNorth, eastWest, capot);
            return this.Equity == null ? value : (double)value / EquityUnits * this.pointsPerEquity;
        }

        /// <summary>A solver value (or an average of them) in game points.</summary>
        internal double ToPoints(double value)
        {
            if (this.RawTieBreak)
            {
                value /= RawScale;
            }

            return this.Equity == null ? value : value / EquityUnits * this.pointsPerEquity;
        }

        private static int Solve(in SimState state, BelotSimulator simulator, int team, int southNorthAnnounces, int eastWestAnnounces, int alpha, int beta)
        {
            if (state.TricksPlayed == 8)
            {
                simulator.Score(in state, southNorthAnnounces, eastWestAnnounces, out var southNorth, out var eastWest, out _);
                return team == 0 ? southNorth - eastWest : eastWest - southNorth;
            }

            var maximizing = (state.Turn & 1) == team;
            var best = maximizing ? int.MinValue : int.MaxValue;
            var legal = simulator.LegalMoves(in state);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var copy = state;
                simulator.Play(ref copy, BitOperations.TrailingZeroCount(rest), legal);
                var value = Solve(in copy, simulator, team, southNorthAnnounces, eastWestAnnounces, alpha, beta);
                if (maximizing)
                {
                    best = Math.Max(best, value);
                    alpha = Math.Max(alpha, best);
                }
                else
                {
                    best = Math.Min(best, value);
                    beta = Math.Min(beta, best);
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }

        private void SetMatch(BasePlayerContext context, int team)
        {
            if (this.Equity == null || context == null)
            {
                this.matchOurs = this.matchTheirs = 0;
                this.pointsPerEquity = 1;
                return;
            }

            this.matchOurs = team == 0 ? context.SouthNorthPoints : context.EastWestPoints;
            this.matchTheirs = team == 0 ? context.EastWestPoints : context.SouthNorthPoints;
            this.pointsPerEquity = this.Equity.PointsPerEquity(this.matchOurs, this.matchTheirs);
        }

        private bool EvaluateSamples(uint legal, float[] values, Random random)
        {
            if (this.SampledWorlds <= 0 || random == null)
            {
                return false;
            }

            var useOwnership = this.Ownership != null && this.OwnershipPower > 0;
            if (useOwnership)
            {
                this.weightedSampler ??= new WeightedWorldSampler();
                if (!this.weightedSampler.Configure(this.knowledge, this.ownershipWeights))
                {
                    return false;
                }
            }
            else
            {
                this.sampler ??= new UniformWorldSampler();
                if (!this.sampler.Configure(this.knowledge))
                {
                    return false;
                }
            }

            // Declaration rejection can be rare. Keep both its work and total solver work bounded.
            var attempts = (int)Math.Min(100000L, Math.Max(64L, (long)this.SampledWorlds * 64));
            for (var attempt = 0; attempt < attempts && this.Worlds < this.SampledWorlds; attempt++)
            {
                if (this.TimeExpired() || (this.NodeLimit > 0 && this.Nodes >= this.NodeLimit))
                {
                    break;
                }

                var state = this.knowledge.Root;
                this.SampleAttempts++;
                if (useOwnership)
                {
                    this.weightedSampler.Sample(ref state, random);
                }
                else
                {
                    this.sampler.Sample(ref state, random);
                }

                if (this.UseDeclarations && !this.ResolveDeclarations(in state))
                {
                    continue;
                }

                if (!this.EvaluateWorld(in state, legal, this.southNorthAnnounces, this.eastWestAnnounces))
                {
                    break;
                }
            }

            if (this.Worlds == 0)
            {
                return false;
            }

            return this.Averages(legal, values);
        }

        private double OwnershipWeight(in SimState state)
        {
            if (this.Ownership == null || this.OwnershipPower == 0)
            {
                return 1;
            }

            var weight = 1.0;
            for (var relative = 1; relative < 4; relative++)
            {
                var seat = (this.knowledge.Me + relative) & 3;
                for (var rest = state.Hands[seat] & ~this.knowledge.Known[seat]; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    weight *= this.ownershipWeights[(card * 3) + relative - 1];
                }
            }

            return weight;
        }

        private bool EvaluateWorld(in SimState state, uint legal, int southNorth, int eastWest, double ownershipWeight = 1)
        {
            if (this.TimeExpired())
            {
                this.IncompleteWorlds++;
                return false;
            }

            var weight = ownershipWeight * (this.PolicyActions > 0 ? this.likelihood.Weight(in state) : 1) * this.SignalWeight(in state) * this.BidWeight(in state);
            this.LikelihoodEvaluations = this.PolicyActions > 0 ? this.likelihood.Evaluations : 0;

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var copy = state;
                this.simulator.Play(ref copy, card, legal);
                if (!this.SolveBounded(in copy, southNorth, eastWest, int.MinValue, int.MaxValue, out this.worldValues[card]))
                {
                    this.IncompleteWorlds++;
                    return false;
                }
            }

            // A cutoff must never give different root actions different sampled worlds.
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                this.sums[card] += weight * this.worldValues[card];
            }

            this.Worlds++;
            this.totalWeight += weight;
            this.squaredWeight += weight * weight;
            return true;
        }

        private bool Averages(uint legal, float[] values)
        {
            if (this.totalWeight <= 0)
            {
                return false;
            }

            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                values[card] = (float)this.ToPoints(this.sums[card] / this.totalWeight);
            }

            return true;
        }

        // The honours of each suit the partner threw away while the opponents held the trick.
        private void ReadSignals(PlayerPlayCardContext context, int kind)
        {
            this.signalSeat = (this.knowledge.Me + 2) & 3;
            this.signalHonours0 = this.signalHonours1 = this.signalHonours2 = this.signalHonours3 = 0;
            if (this.PartnerSignalWeight >= 1)
            {
                return;
            }

            var signals = PlaySignals.Read(context.RoundActions, kind);
            var suits = signals.WeakSuits(this.signalSeat);
            var hidden = ~(this.knowledge.Played | this.knowledge.MyHand | this.knowledge.Known[this.signalSeat]);
            for (var suit = 0; suit < 4; suit++)
            {
                if ((suits & (1 << suit)) == 0)
                {
                    continue;
                }

                var trumpSuit = kind == SimTables.AllTrumps || suit == kind;
                var honours = trumpSuit
                    ? (1u << ((suit * 8) + 4)) | (1u << ((suit * 8) + 2))
                    : (1u << ((suit * 8) + 7)) | (1u << ((suit * 8) + 3));
                honours &= hidden;
                switch (suit)
                {
                    case 0:
                        this.signalHonours0 = honours;
                        break;
                    case 1:
                        this.signalHonours1 = honours;
                        break;
                    case 2:
                        this.signalHonours2 = honours;
                        break;
                    default:
                        this.signalHonours3 = honours;
                        break;
                }
            }
        }

        // The unseen jack and nine of each suit another seat bid as trumps, if it has shown neither.
        private void ReadBids(PlayerPlayCardContext context)
        {
            Array.Clear(this.bidHonours);
            if (this.BidderHonourWeight >= 1)
            {
                return;
            }

            var hidden = ~(this.knowledge.Played | this.knowledge.MyHand);
            foreach (var bid in context.Bids)
            {
                var type = bid.Type;
                var seat = bid.Player.Index();
                if (seat == this.knowledge.Me || (type != BidType.Clubs && type != BidType.Diamonds && type != BidType.Hearts && type != BidType.Spades))
                {
                    continue;
                }

                var suit = (int)type.ToCardSuit();
                var honours = (1u << ((suit * 8) + 4)) | (1u << ((suit * 8) + 2));
                if ((this.knowledge.PlayedBy[seat] & honours) == 0)
                {
                    this.bidHonours[seat] |= honours & hidden;
                }
            }
        }

        private double BidWeight(in SimState state)
        {
            if (this.BidderHonourWeight >= 1)
            {
                return 1;
            }

            var weight = 1.0;
            for (var seat = 0; seat < 4; seat++)
            {
                var honours = this.bidHonours[seat];
                if (honours != 0 && (state.Hands[seat] & honours) == 0)
                {
                    weight *= this.BidderHonourWeight;
                }
            }

            return weight;
        }

        private double SignalWeight(in SimState state)
        {
            if (this.PartnerSignalWeight >= 1)
            {
                return 1;
            }

            var hand = state.Hands[this.signalSeat];
            var weight = 1.0;
            if ((hand & this.signalHonours0) != 0)
            {
                weight *= this.PartnerSignalWeight;
            }

            if ((hand & this.signalHonours1) != 0)
            {
                weight *= this.PartnerSignalWeight;
            }

            if ((hand & this.signalHonours2) != 0)
            {
                weight *= this.PartnerSignalWeight;
            }

            if ((hand & this.signalHonours3) != 0)
            {
                weight *= this.PartnerSignalWeight;
            }

            return weight;
        }

        private bool TimeExpired() => this.TimeLimitMilliseconds > 0 && Stopwatch.GetTimestamp() >= this.deadline;

        private bool SolveBounded(in SimState state, int southNorth, int eastWest, int alpha, int beta, out int best)
        {
            best = 0;
            if ((this.NodeLimit > 0 && this.Nodes >= this.NodeLimit) || ((this.Nodes & 63) == 0 && this.TimeExpired()))
            {
                return false;
            }

            this.Nodes++;
            if (state.TricksPlayed == 8)
            {
                this.simulator.Score(in state, southNorth, eastWest, out var first, out var second, out _);
                best = this.Utility(first, second, state.SouthNorthTricks == 0 || state.EastWestTricks == 0);
                if (this.RawTieBreak)
                {
                    var raw = state.SouthNorthPoints - state.EastWestPoints + (state.LastTrickTeam == 0 ? 10 : -10);
                    best = (best * RawScale) + (this.team == 0 ? raw : -raw);
                }

                return true;
            }

            var originalAlpha = alpha;
            var originalBeta = beta;
            var tableSlot = -1;
            var tableKey = default(TranspositionKey);
            if (this.UseTranspositions && state.TrickCards == 0 && state.TricksPlayed < 7)
            {
                tableKey = new TranspositionKey(in state, southNorth, eastWest);
                tableSlot = tableKey.Slot();
                this.TranspositionProbes++;
                ref readonly var entry = ref this.transpositions[tableSlot];
                if (entry.Generation == this.transpositionGeneration && entry.Key.Matches(in tableKey))
                {
                    this.TranspositionHits++;
                    if (entry.Lower == entry.Upper || entry.Lower >= beta || entry.Upper <= alpha)
                    {
                        best = entry.Lower == entry.Upper || entry.Lower >= beta ? entry.Lower : entry.Upper;
                        this.TranspositionCutoffs++;
                        return true;
                    }

                    alpha = Math.Max(alpha, entry.Lower);
                    beta = Math.Min(beta, entry.Upper);
                }
            }

            var maximizing = (state.Turn & 1) == this.team;
            best = maximizing ? int.MinValue : int.MaxValue;
            var legal = this.simulator.LegalMoves(in state);
            var choices = this.PruneEquivalentCards && (legal & (legal - 1)) != 0 ? EquivalentChoices(in state, this.simulator, legal) : legal;
            var preferred = state.TricksPlayed < 7 && (choices & (choices - 1)) != 0
                ? this.simulator.ChooseRolloutMove(in state, choices)
                : BitOperations.TrailingZeroCount(choices);
            var remaining = choices;
            while (remaining != 0)
            {
                var card = preferred >= 0 ? preferred : BitOperations.TrailingZeroCount(remaining);
                preferred = -1;
                remaining &= ~(1u << card);
                var copy = state;
                this.simulator.Play(ref copy, card, legal);
                if (!this.SolveBounded(in copy, southNorth, eastWest, alpha, beta, out var value))
                {
                    return false;
                }

                if (maximizing)
                {
                    best = Math.Max(best, value);
                    alpha = Math.Max(alpha, best);
                }
                else
                {
                    best = Math.Min(best, value);
                    beta = Math.Min(beta, best);
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            if (tableSlot >= 0)
            {
                this.StoreTransposition(tableSlot, in tableKey, best, originalAlpha, originalBeta);
            }

            return true;
        }

        private void StoreTransposition(int slot, in TranspositionKey key, int value, int alpha, int beta)
        {
            ref var entry = ref this.transpositions[slot];
            if (entry.Generation != this.transpositionGeneration || !entry.Key.Matches(in key))
            {
                entry.Key = key;
                entry.Generation = this.transpositionGeneration;
                entry.Lower = int.MinValue;
                entry.Upper = int.MaxValue;
            }
            else if (entry.Lower == entry.Upper)
            {
                return;
            }

            if (value <= alpha)
            {
                entry.Upper = Math.Min(entry.Upper, value);
            }
            else if (value >= beta)
            {
                entry.Lower = Math.Max(entry.Lower, value);
            }
            else
            {
                entry.Lower = value;
                entry.Upper = value;
            }
        }

        private void Enumerate(int index, in SimState state)
        {
            if (this.overflow || this.interrupted)
            {
                return;
            }

            if (this.TimeExpired())
            {
                this.interrupted = true;
                return;
            }

            if (index == this.poolCount)
            {
                if (this.UseDeclarations && !this.ResolveDeclarations(in state))
                {
                    return;
                }

                if (this.enumeratedWorlds == this.worldLimit)
                {
                    this.overflow = true;
                    return;
                }

                this.worlds[this.enumeratedWorlds] = state;
                this.worldSouthNorthAnnounces[this.enumeratedWorlds] = this.southNorthAnnounces;
                this.worldEastWestAnnounces[this.enumeratedWorlds] = this.eastWestAnnounces;
                this.enumeratedWorlds++;

                return;
            }

            var bit = 1u << this.pool[index];
            for (var seat = 0; seat < 4; seat++)
            {
                if (this.needs[seat] == 0 || (this.knowledge.Excluded[seat] & bit) != 0)
                {
                    continue;
                }

                var copy = state;
                copy.Hands[seat] |= bit;
                this.needs[seat]--;
                this.Enumerate(index + 1, in copy);
                this.needs[seat]++;
            }
        }

        private bool ResolveDeclarations(in SimState state)
        {
            Array.Clear(this.worldCounts);
            var count = 0;
            if (this.simulator.Kind != SimTables.NoTrumps)
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    count = AnnounceScorer.AddDeclaredCombinations(state.Hands[seat] | this.knowledge.PlayedBy[seat], seat, this.worldAnnounces, count);
                }
            }

            for (var i = 0; i < count; i++)
            {
                var announce = this.worldAnnounces[i];
                this.worldCounts[announce.Seat] += 1u << (2 * (int)announce.Type);
            }

            for (var seat = 0; seat < 4; seat++)
            {
                if (this.worldCounts[seat] != this.declaredCounts[seat])
                {
                    return false;
                }
            }

            for (var i = 0; i < this.knowledge.AnnounceCount; i++)
            {
                var known = this.knowledge.Announces[i];
                if (known.Rank < 0)
                {
                    continue;
                }

                var found = false;
                for (var j = 0; j < count; j++)
                {
                    var candidate = this.worldAnnounces[j];
                    found |= candidate.Seat == known.Seat && candidate.Type == known.Type && candidate.Rank == known.Rank;
                }

                if (!found)
                {
                    return false;
                }
            }

            AnnounceScorer.GetPoints(this.worldAnnounces, count, out this.southNorthAnnounces, out this.eastWestAnnounces);
            return true;
        }

        /// <summary>
        /// At a nonterminal trick boundary, the old trick's suit, winner and points are stale:
        /// the next lead overwrites them. The final trick also overwrites LastTrickTeam. Every
        /// other field affecting future play or scoring is compared exactly, including the
        /// scored combinations, which can differ between sampled worlds. Contract, declarer,
        /// doubling, hanging points and root team are fixed within the table's generation.
        /// </summary>
        internal readonly struct TranspositionKey
        {
            private readonly ulong firstHands;
            private readonly ulong secondHands;
            private readonly int turn;
            private readonly int tricksPlayed;
            private readonly int southNorthPoints;
            private readonly int eastWestPoints;
            private readonly int southNorthTricks;
            private readonly int eastWestTricks;
            private readonly int southNorthAnnounces;
            private readonly int eastWestAnnounces;

            public TranspositionKey(in SimState state, int southNorth, int eastWest)
            {
                this.firstHands = state.Hands[0] | ((ulong)state.Hands[1] << 32);
                this.secondHands = state.Hands[2] | ((ulong)state.Hands[3] << 32);
                this.turn = state.Turn;
                this.tricksPlayed = state.TricksPlayed;
                this.southNorthPoints = state.SouthNorthPoints;
                this.eastWestPoints = state.EastWestPoints;
                this.southNorthTricks = state.SouthNorthTricks;
                this.eastWestTricks = state.EastWestTricks;
                this.southNorthAnnounces = southNorth;
                this.eastWestAnnounces = eastWest;
            }

            public bool Matches(in TranspositionKey other) =>
                this.firstHands == other.firstHands && this.secondHands == other.secondHands
                && this.turn == other.turn && this.tricksPlayed == other.tricksPlayed
                && this.southNorthPoints == other.southNorthPoints && this.eastWestPoints == other.eastWestPoints
                && this.southNorthTricks == other.southNorthTricks && this.eastWestTricks == other.eastWestTricks
                && this.southNorthAnnounces == other.southNorthAnnounces && this.eastWestAnnounces == other.eastWestAnnounces;

            public int Slot()
            {
                var points = (uint)this.southNorthPoints | ((ulong)(uint)this.eastWestPoints << 32);
                var announces = (uint)this.southNorthAnnounces | ((ulong)(uint)this.eastWestAnnounces << 32);
                var status = (uint)this.turn | ((uint)this.tricksPlayed << 2)
                    | ((uint)this.southNorthTricks << 6) | ((uint)this.eastWestTricks << 10);
                var hash = unchecked((this.firstHands * 11400714819323198485UL)
                                     ^ BitOperations.RotateLeft(this.secondHands * 14029467366897019727UL, 17)
                                     ^ (points * 1609587929392839161UL)
                                     ^ BitOperations.RotateLeft(announces * 9650029242287828579UL, 29)
                                     ^ ((ulong)status * 2870177450012600261UL));
                return (int)((hash ^ (hash >> 32)) & (TranspositionCapacity - 1));
            }
        }

        private struct TranspositionEntry
        {
            public TranspositionKey Key;

            public uint Generation;

            public int Lower;

            public int Upper;
        }
    }
}
