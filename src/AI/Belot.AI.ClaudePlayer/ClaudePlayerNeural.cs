namespace Belot.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays by neural networks trained with reinforcement learning (see NEURAL_NETWORK.md): a
    /// bidding network and a card network for each kind of contract (the suits, no trumps, all
    /// trumps) value every action open to the player, in game points (its team's points from the
    /// deal minus the other team's, as if everybody played on like the networks), and the player
    /// takes the best. Normally a decision uses one forward pass. Optional sampled search
    /// or bounded endgame search improves the values by simulating continuations.
    ///
    /// <see cref="Temperature"/> and <see cref="MaxRegret"/> make it weaker on purpose: it then
    /// sometimes takes an action that is nearly as good (never one worse than the best by more
    /// than MaxRegret points). It declares every combination it is offered and every belote.
    /// </summary>
    public class ClaudePlayerNeural : IPlayer
    {
        private readonly SmartPlayer smartPlayer = new SmartPlayer();
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly NeuralEvaluator evaluator;
        private readonly NeuralSearch search = new NeuralSearch();
        private readonly EndgameSearch endgame = new EndgameSearch();
        private readonly float[] cardValues = new float[FeatureEncoder.CardOutputs];
        private readonly float[] bidValues = new float[FeatureEncoder.BidOutputs];
        private readonly float[] preferences = new float[FeatureEncoder.CardOutputs];
        private readonly float[] networkValues = new float[FeatureEncoder.CardOutputs];
        private LateCardCorrectionModel.Evaluator lateCorrection;
        private bool lastRollout;
        private SuitEnsembleEvaluator suitEnsemble;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaudePlayerNeural"/> class with the
        /// trained networks built into the assembly.
        /// </summary>
        public ClaudePlayerNeural()
            : this(NeuralModels.Embedded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaudePlayerNeural"/> class with the
        /// networks in a folder (the trainer's checkpoints: bid.bin, trump.bin, notrumps.bin,
        /// alltrumps.bin).
        /// </summary>
        public ClaudePlayerNeural(string weightsDirectory)
            : this(NeuralModels.LoadCached(weightsDirectory))
        {
        }

        internal ClaudePlayerNeural(NeuralModels models)
        {
            this.evaluator = new NeuralEvaluator(models);
        }

        public string Name => "Claude Player (neural)";

        /// <summary>
        /// Gets or sets how freely the player takes an action other than the best, in game points:
        /// an action worth d points less than the best is taken e^(-d / Temperature) times as
        /// often. 0 (the default) always takes the best.
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// Gets or sets the most, in game points, that an action the player takes may be worth
        /// less than the best (with a <see cref="Temperature"/> above 0).
        /// </summary>
        public double MaxRegret { get; set; } = double.PositiveInfinity;

        public Random Rng { get; set; } = new Random();

        /// <summary>Gets or sets a value indicating whether the player may double and redouble.</summary>
        public bool MayDouble { get; set; } = true;

        /// <summary>
        /// Gets or sets how many game points a double or redouble must be worth over every other
        /// bid before the player makes it (0, the default, takes it whenever it is the best).
        /// </summary>
        public double DoubleMargin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player bids only what a person would read
        /// its bids as (see <see cref="Human.NaturalBidding"/>): a suit with its jack or nine and
        /// another card of it, no trumps with an ace, all trumps with a jack. On by default: the
        /// embedded networks were fine-tuned for it and no longer value the other bids. Turn it
        /// off only for networks trained without it (the September 29 files).
        /// </summary>
        public bool NaturalBidding { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether ordinary card values average every suit
        /// permutation that fixes trump and all suits bid in the auction. Disabled by default;
        /// bidding, successful search results and rollout policies retain their existing paths.
        /// </summary>
        public bool CardSuitEnsemble { get; set; }

        /// <summary>
        /// Gets or sets how many deals a card decision plays out (<see cref="NeuralSearch"/>): 0,
        /// the default, disables sampled rollouts; with N deals every legal
        /// card is played out in N deals of the unseen cards, by the networks for every seat, and
        /// valued by the average result (slower: about N times the cards times the rest of the deal).
        /// </summary>
        public int SearchDeals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enumerate and solve bounded endings
        /// within <see cref="EndgameTricks"/> tricks. Disabled by default; values use perfect-
        /// information continuations in each accepted hidden deal. Successful endgame
        /// evaluation takes priority over <see cref="SearchDeals"/>.
        /// </summary>
        public bool UseEndgameSearch { get; set; }

        /// <summary>
        /// Gets or sets whether endgame worlds also match the observed declarations, assuming
        /// each seat declares every available combination as the bots do. This resolves hidden
        /// announcement ranks from each hypothetical original hand. Disabled by default.
        /// </summary>
        public bool EndgameUseDeclarations
        {
            get => this.endgame.UseDeclarations;
            set => this.endgame.UseDeclarations = value;
        }

        /// <summary>
        /// Gets or sets the endgame horizon (two to five tricks, default two).
        /// Four or five tricks require <see cref="EndgameSampledWorlds"/>.
        /// </summary>
        public int EndgameTricks
        {
            get => this.endgame.Tricks;
            set => this.endgame.Tricks = value >= 2 && value <= 5 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the maximum worlds to solve with three tricks left (1 to 1680, default 8).
        /// More worlds extend endgame coverage at higher cost. Two-trick coverage is unchanged.
        /// </summary>
        public int EndgameThreeTrickWorldLimit
        {
            get => this.endgame.ThreeTrickWorldLimit;
            set => this.endgame.ThreeTrickWorldLimit = value >= 1 && value <= 1680 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the sampled worlds for longer endings or enumeration overflow (0 disables sampling).</summary>
        public int EndgameSampledWorlds
        {
            get => this.endgame.SampledWorlds;
            set => this.endgame.SampledWorlds = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the endgame tree node budget per decision (0 = unlimited).</summary>
        public int EndgameNodeLimit
        {
            get => this.endgame.NodeLimit;
            set => this.endgame.NodeLimit = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the endgame time budget in milliseconds (0 = unlimited).</summary>
        public int EndgameTimeLimitMilliseconds
        {
            get => this.endgame.TimeLimitMilliseconds;
            set => this.endgame.TimeLimitMilliseconds = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets whether the solver merges equivalent zero-point cards inside hypothetical continuations.</summary>
        public bool EndgamePruneEquivalentCards
        {
            get => this.endgame.PruneEquivalentCards;
            set => this.endgame.PruneEquivalentCards = value;
        }

        /// <summary>Gets or sets whether endgame solving reuses fully verified trick-boundary positions.</summary>
        public bool EndgameUseTranspositions
        {
            get => this.endgame.UseTranspositions;
            set => this.endgame.UseTranspositions = value;
        }

        /// <summary>Gets or sets how many recent public plays weight endgame worlds (0 disables policy inference).</summary>
        public int EndgamePolicyActions
        {
            get => this.endgame.PolicyActions;
            set => this.endgame.PolicyActions = value >= 0 && value <= 32 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the historical action model's temperature in game points.</summary>
        public double EndgamePolicyTemperature
        {
            get => this.endgame.PolicyTemperature;
            set => this.endgame.PolicyTemperature = double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the historical action model's uniform choice probability.</summary>
        public double EndgamePolicyUniformMix
        {
            get => this.endgame.PolicyUniformMix;
            set => this.endgame.PolicyUniformMix = double.IsFinite(value) && value >= 0 && value <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets the power of the historical action likelihood (0 keeps uniform world weights).</summary>
        public double EndgamePolicyPower
        {
            get => this.endgame.PolicyPower;
            set => this.endgame.PolicyPower = double.IsFinite(value) && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets extra heuristic worlds for the experimental rollout control variate (0 disables it).</summary>
        public int SearchControlVariateDeals
        {
            get => this.search.ControlVariateDeals;
            set => this.search.ControlVariateDeals = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets completed tricks before a neural value bootstrap (0 plays the full deal).</summary>
        public int SearchRolloutTricks
        {
            get => this.search.RolloutTricks;
            set => this.search.RolloutTricks = value >= 0 && value <= 8 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets whether truncated rollouts wait for the deciding seat's next turn before valuing the leaf.</summary>
        public bool SearchRolloutRootLeaf
        {
            get => this.search.RolloutRootLeaf;
            set => this.search.RolloutRootLeaf = value;
        }

        /// <summary>Gets or sets exact perfect-information tricks at a rollout leaf (0, 2 or 3).</summary>
        public int SearchDoubleDummyTricks
        {
            get => this.search.DoubleDummyTricks;
            set
            {
                this.search.DoubleDummyTricks = value == 0 || (value >= 2 && value <= 5) ? value : throw new ArgumentOutOfRangeException(nameof(value));
                this.search.LeafSolver = value > 3 ? new EndgameSearch { UseTranspositions = true, PruneEquivalentCards = true, Equity = this.endgame.Equity } : null;
            }
        }

        /// <summary>Gets or sets how many of the network's best cards the rollouts play out (0: all legal cards).</summary>
        public int SearchCandidateCards
        {
            get => this.search.CandidateCards;
            set => this.search.CandidateCards = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>Gets or sets how far below the network's best a card may be and still be played out (0: no limit).</summary>
        public double SearchCandidateMargin
        {
            get => this.search.CandidateMargin;
            set => this.search.CandidateMargin = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Gets or sets how many of the network's best cards an endgame decision may choose from (0: all):
        /// the solver's perfect-information values can favour a card only because every world is
        /// solved knowing the others' hands, and the network's ranking keeps such cards out.
        /// </summary>
        public int EndgameCandidateCards { get; set; }

        /// <summary>Gets or sets how far below the network's best a card may be and still be chosen by the endgame (0: no limit).</summary>
        public double EndgameCandidateMargin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rollouts choose their candidate cards by the
        /// suit-ensemble values (see <see cref="CardSuitEnsemble"/>) instead of one forward pass.
        /// </summary>
        public bool SearchEnsembleCandidates
        {
            get => this.search.PriorEnsemble != null;
            set => this.search.PriorEnsemble = value ? new SuitEnsembleEvaluator() : null;
        }

        /// <summary>Gets or sets whether rollouts after the first trick condition on all declared combinations.</summary>
        public bool SearchUseDeclarations
        {
            get => this.search.UseDeclarations;
            set => this.search.UseDeclarations = value;
        }

        /// <summary>Gets or sets completed worlds before the rollout time budget may stop sampling.</summary>
        public int SearchMinimumDeals
        {
            get => this.search.MinimumDeals;
            set => this.search.MinimumDeals = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a time budget per card for the search, in milliseconds (0 = none): it plays
        /// no new deal once the budget is spent (at least eight), so a slower device plays fewer.
        /// </summary>
        public int SearchTimeLimitMilliseconds
        {
            get => this.search.TimeLimitMilliseconds;
            set => this.search.TimeLimitMilliseconds = value;
        }

        /// <summary>Gets or sets how many deals the card network's value counts as in the search's averages.</summary>
        public double SearchPriorDeals
        {
            get => this.search.PriorDeals;
            set => this.search.PriorDeals = value;
        }

        /// <summary>
        /// Gets or sets how far below the best (game points) a card may be after half the search's
        /// deals and still be played out in the rest; 0 plays every card in every deal.
        /// </summary>
        public double SearchPruneMargin
        {
            get => this.search.PruneMargin;
            set => this.search.PruneMargin = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player chooses among nearly equal cards the
        /// way a strong human would (<see cref="HumanPreference"/>): by the points, the masters,
        /// the trumps and the conventions of the game, instead of by the noise of a network or the
        /// order of the cards. Off by default. Values within <see cref="HumanNetworkTolerance"/>
        /// of the best (network values) or <see cref="HumanSearchTolerance"/> (searched values)
        /// count as equal.
        /// </summary>
        public bool HumanStyle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a human-style player, before its exact endgames,
        /// never plays a card <see cref="HumanPreference.Dominated"/> excludes (on by default).
        /// </summary>
        public bool HumanDominance { get; set; } = true;

        /// <summary>Gets or sets how many game points below the best network value a card may be and still count as equal.</summary>
        public double HumanNetworkTolerance { get; set; } = 0.3;

        /// <summary>Gets or sets how many game points below the best searched value a card may be and still count as equal.</summary>
        public double HumanSearchTolerance { get; set; } = 0.02;

        /// <summary>Gets or sets how many game points below the best rollout value (<see cref="SearchDeals"/>) a card may be and still count as equal.</summary>
        public double HumanRolloutTolerance { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets how many game points below the best a card thrown away while the
        /// opponents hold the trick may be and still count as equal: the partner reads the suit
        /// thrown (see <see cref="EndgameSignalWeight"/>), so the convention is worth a little.
        /// </summary>
        public double HumanDiscardTolerance { get; set; } = 0.02;

        /// <summary>
        /// Gets or sets how many game points below the best a lead may be and still count as equal:
        /// leading the suit the partner asked for, and not the one it threw away, reads its signals.
        /// </summary>
        public double HumanLeadTolerance { get; set; } = 0.02;

        /// <summary>
        /// Gets or sets a value indicating whether the searches value a finished deal by the chance
        /// it leaves to win the match (<see cref="MatchEquity"/>) instead of its game points.
        /// </summary>
        public bool PlayForMatch
        {
            get => this.endgame.Equity != null;
            set
            {
                this.endgame.Equity = value ? MatchEquity.Embedded : null;
                if (this.search.LeafSolver != null)
                {
                    this.search.LeafSolver.Equity = this.endgame.Equity;
                }
            }
        }

        /// <summary>Gets or sets the endgame trust in the partner's discards (see <see cref="EndgameSearch.PartnerSignalWeight"/>).</summary>
        public double EndgameSignalWeight
        {
            get => this.endgame.PartnerSignalWeight;
            set => this.endgame.PartnerSignalWeight = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether endgame results equal in game points are ordered
        /// by the raw card points each team takes (see <see cref="EndgameSearch.RawTieBreak"/>).
        /// </summary>
        public bool EndgameRawTieBreak
        {
            get => this.endgame.RawTieBreak;
            set => this.endgame.RawTieBreak = value;
        }

        /// <summary>Gets how many decisions fell back to SmartPlayer (a context that did not add up).</summary>
        public int Fallbacks { get; private set; }

        internal NeuralModels Models => this.evaluator.Models;

        internal LateCardCorrectionModel CardCorrectionModel
        {
            set => this.lateCorrection = value?.CreateEvaluator();
        }

        internal CardOwnershipModel EndgameOwnershipModel
        {
            set => this.endgame.Ownership = value?.CreateEvaluator();
        }

        /// <summary>Sets the ownership model that weights the rollout worlds of <see cref="SearchDeals"/> (null: uniform).</summary>
        internal CardOwnershipModel SearchOwnershipModel
        {
            set => this.search.Ownership = value?.CreateEvaluator();
        }

        internal double EndgameOwnershipPower
        {
            get => this.endgame.OwnershipPower;
            set => this.endgame.OwnershipPower = double.IsFinite(value) && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal double EndgameOwnershipUniformMix
        {
            get => this.endgame.OwnershipUniformMix;
            set => this.endgame.OwnershipUniformMix = double.IsFinite(value) && value >= 0 && value <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal long EndgameDecisions { get; private set; }

        internal long EndgameNodes { get; private set; }

        internal long EndgameWorlds { get; private set; }

        internal long EndgameSampleAttempts { get; private set; }

        internal long EndgameIncompleteWorlds { get; private set; }

        internal long EndgameLikelihoodEvaluations { get; private set; }

        internal double EndgameEffectiveWorlds { get; private set; }

        internal long EndgameTranspositionProbes { get; private set; }

        internal long EndgameTranspositionHits { get; private set; }

        internal long EndgameTranspositionCutoffs { get; private set; }

        internal double SearchNeuralVariance { get; private set; }

        internal double SearchResidualVariance { get; private set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            if (!NeuralDeal.FromBidContext(context, out var deal) || deal.AvailableBids() != context.AvailableBids)
            {
                this.Fallbacks++;
                return this.smartPlayer.GetBid(context);
            }

            this.evaluator.EvaluateBids(in deal, this.bidValues);
            var available = this.MayDouble ? context.AvailableBids : context.AvailableBids & ~(BidType.Double | BidType.ReDouble);
            if (this.NaturalBidding)
            {
                available &= Human.NaturalBidding.Allowed(deal.Play.Hands[deal.ToBid]);
            }

            if (this.DoubleMargin > 0 && (available & (BidType.Double | BidType.ReDouble)) != 0)
            {
                // Double or redouble only when it is clearly better than every other bid, as people do.
                var others = NeuralEvaluator.BidCandidates(available & ~(BidType.Double | BidType.ReDouble));
                var best = this.bidValues[NeuralEvaluator.Best(this.bidValues, others)];
                foreach (var doubling in new[] { BidType.Double, BidType.ReDouble })
                {
                    if (available.HasFlag(doubling) && this.bidValues[FeatureEncoder.BidIndex(doubling)] < best + this.DoubleMargin)
                    {
                        available &= ~doubling;
                    }
                }
            }

            return FeatureEncoder.BidOfIndex(this.Choose(this.bidValues, NeuralEvaluator.BidCandidates(available)));
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var available = context.AvailableCardsToPlay;
            if (available.Count == 1)
            {
                return new PlayCardAction(available.FirstOrDefault());
            }

            if (!this.Evaluate(context, out var legal, out var deal, out var searched))
            {
                this.Fallbacks++;
                return this.smartPlayer.PlayCard(context);
            }

            // A human-style player plays its cards like a person even when it plays loose (the
            // Expert): the temperature then only varies its bids. Before the exact endgames
            // (network or rollout values) it never gives away a card a person would keep.
            var choices = legal;
            if (this.HumanStyle && this.HumanDominance && (!searched || this.lastRollout))
            {
                var dominated = HumanPreference.Dominated(in deal, legal);
                choices = dominated != legal ? legal & ~dominated : legal;
            }

            var card = this.HumanStyle
                ? this.ChooseLikeHuman(context, in deal, choices, this.Tolerance(in deal, legal, searched, searched && this.lastRollout))
                : this.Choose(this.cardValues, legal);
            return new PlayCardAction(Card.AllCards[card]);
        }

        /// <summary>
        /// Values every card the player may play, best first: the game points its team gets from
        /// the deal minus the other team's, if it plays that card.
        /// </summary>
        public IReadOnlyList<CardValue> EvaluateCards(PlayerPlayCardContext context)
        {
            if (!this.Evaluate(context, out var legal, out _, out _))
            {
                throw new ArgumentException("The context does not add up.", nameof(context));
            }

            var result = new List<CardValue>(BitOperations.PopCount(legal));
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                result.Add(new CardValue(Card.AllCards[card], this.cardValues[card]));
            }

            result.Sort((x, y) => y.Value.CompareTo(x.Value));
            return result;
        }

        /// <summary>
        /// Values every bid open to the player (with <see cref="NaturalBidding"/>, the natural
        /// ones), best first: the game points its team gets from the deal minus the other team's,
        /// if it makes that bid.
        /// </summary>
        public IReadOnlyList<BidValue> EvaluateBids(PlayerGetBidContext context)
        {
            if (!NeuralDeal.FromBidContext(context, out var deal))
            {
                throw new ArgumentException("The context does not add up.", nameof(context));
            }

            this.evaluator.EvaluateBids(in deal, this.bidValues);
            var available = this.NaturalBidding ? context.AvailableBids & Human.NaturalBidding.Allowed(deal.Play.Hands[deal.ToBid]) : context.AvailableBids;
            var candidates = NeuralEvaluator.BidCandidates(available);
            var result = new List<BidValue>(BitOperations.PopCount(candidates));
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                result.Add(new BidValue(FeatureEncoder.BidOfIndex(index), this.bidValues[index]));
            }

            result.Sort((x, y) => y.Value.CompareTo(x.Value));
            return result;
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

        private bool Evaluate(PlayerPlayCardContext context, out uint legal, out NeuralDeal deal, out bool searched)
        {
            legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            searched = false;
            this.lastRollout = false;
            if (!NeuralDeal.FromPlayContext(context, this.simulator, out deal)
                || this.simulator.LegalMoves(in deal.Play) != legal)
            {
                return false;
            }

            if (this.UseEndgameSearch)
            {
                var solved = this.endgame.Evaluate(context, in deal, legal, this.simulator, this.cardValues, this.Rng, this.evaluator);
                this.EndgameNodes += this.endgame.Nodes;
                this.EndgameWorlds += this.endgame.Worlds;
                this.EndgameSampleAttempts += this.endgame.SampleAttempts;
                this.EndgameIncompleteWorlds += this.endgame.IncompleteWorlds;
                this.EndgameLikelihoodEvaluations += this.endgame.LikelihoodEvaluations;
                this.EndgameEffectiveWorlds += this.endgame.EffectiveWorlds;
                this.EndgameTranspositionProbes += this.endgame.TranspositionProbes;
                this.EndgameTranspositionHits += this.endgame.TranspositionHits;
                this.EndgameTranspositionCutoffs += this.endgame.TranspositionCutoffs;
                if (solved)
                {
                    this.EndgameDecisions++;
                    searched = true;
                    if (this.EndgameCandidateCards > 0 || this.EndgameCandidateMargin > 0)
                    {
                        this.KeepNetworkCandidates(in deal, legal);
                    }

                    return true;
                }
            }

            if (this.SearchDeals > 0
                && this.search.Evaluate(context, in deal, legal, this.SearchDeals, this.evaluator, this.simulator, this.Rng, this.cardValues))
            {
                this.SearchNeuralVariance += this.search.NeuralDifferenceVariance;
                this.SearchResidualVariance += this.search.ResidualDifferenceVariance;
                searched = true;
                this.lastRollout = true;
                return true;
            }

            if (this.CardSuitEnsemble)
            {
                this.suitEnsemble ??= new SuitEnsembleEvaluator();
                this.suitEnsemble.EvaluateCards(in deal, legal, this.evaluator, this.cardValues);
            }
            else
            {
                this.evaluator.EvaluateCards(in deal, legal, this.cardValues);
            }

            this.lateCorrection?.Apply(in deal, legal, this.cardValues);
            return true;
        }

        // Values the cards the network ranks outside its best (by count and margin) far below the others.
        private void KeepNetworkCandidates(in NeuralDeal deal, uint legal)
        {
            this.evaluator.EvaluateCards(in deal, legal, this.networkValues);
            var best = NeuralEvaluator.Best(this.networkValues, legal);
            var kept = 1u << best;
            var limit = this.EndgameCandidateCards > 0 ? this.EndgameCandidateCards : 32;
            while (BitOperations.PopCount(kept) < limit)
            {
                var next = -1;
                for (var rest = legal & ~kept; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    if ((this.EndgameCandidateMargin <= 0 || this.networkValues[card] >= this.networkValues[best] - this.EndgameCandidateMargin)
                        && (next < 0 || this.networkValues[card] > this.networkValues[next]))
                    {
                        next = card;
                    }
                }

                if (next < 0)
                {
                    break;
                }

                kept |= 1u << next;
            }

            for (var rest = legal & ~kept; rest != 0; rest &= rest - 1)
            {
                this.cardValues[BitOperations.TrailingZeroCount(rest)] -= 1000;
            }
        }

        // How close to the best a card must be valued to count as equal: a lead or a discard while
        // the opponents hold the trick is a signal, so the convention may cost a little there.
        private double Tolerance(in NeuralDeal deal, uint legal, bool searched, bool rolledOut)
        {
            var tolerance = rolledOut ? this.HumanRolloutTolerance : searched ? this.HumanSearchTolerance : this.HumanNetworkTolerance;
            ref readonly var play = ref deal.Play;
            if (play.TrickCards == 0)
            {
                return Math.Max(tolerance, this.HumanLeadTolerance);
            }

            if (((play.WinnerSeat ^ play.Turn) & 1) != 0
                && (legal & SimTables.SuitMasks[play.LedSuit]) == 0
                && (deal.Kind >= SimTables.NoTrumps || (legal & SimTables.SuitMasks[deal.Kind]) != legal))
            {
                tolerance = Math.Max(tolerance, this.HumanDiscardTolerance);
            }

            return tolerance;
        }

        // Among the cards valued within the tolerance of the best, the one a strong human prefers.
        private int ChooseLikeHuman(PlayerPlayCardContext context, in NeuralDeal deal, uint legal, double tolerance)
        {
            var best = NeuralEvaluator.Best(this.cardValues, legal);
            var floor = this.cardValues[best] - tolerance;
            var near = 0u;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (this.cardValues[card] >= floor)
                {
                    near |= 1u << card;
                }
            }

            if ((near & (near - 1)) == 0)
            {
                return best;
            }

            var signals = PlaySignals.Read(context.RoundActions, deal.Kind);
            HumanPreference.Score(in deal, in signals, near, this.preferences);
            var choice = best;
            for (var rest = near; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (this.preferences[card] > this.preferences[choice]
                    || (this.preferences[card] == this.preferences[choice] && this.cardValues[card] > this.cardValues[choice]))
                {
                    choice = card;
                }
            }

            return choice;
        }

        // The best candidate, or with a temperature a random one near it.
        private int Choose(float[] values, uint candidates)
        {
            var best = NeuralEvaluator.Best(values, candidates);
            if (this.Temperature <= 0)
            {
                return best;
            }

            var floor = values[best] - this.MaxRegret;
            var total = 0d;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                if (values[index] >= floor)
                {
                    total += Math.Exp((values[index] - values[best]) / this.Temperature);
                }
            }

            var pick = this.Rng.NextDouble() * total;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                if (values[index] >= floor)
                {
                    pick -= Math.Exp((values[index] - values[best]) / this.Temperature);
                    if (pick < 0)
                    {
                        return index;
                    }
                }
            }

            return best;
        }
    }
}
