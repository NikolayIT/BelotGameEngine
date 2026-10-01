namespace Belot.AI.ClaudePlayer
{
    using Belot.AI.ClaudePlayer.Neural;

    /// <summary>Shared player configurations for the app, ratings and training opponents.</summary>
    public static class ClaudePlayerProfiles
    {
        /// <summary>
        /// The Expert plays the most natural card (see <see cref="Human.HumanPreference"/>) among
        /// those valued within this many game points of the best: weaker, but never absurd.
        /// </summary>
        public const double ExpertTolerance = 5;

        /// <summary>
        /// The September 29 Expert's action temperature in game points: it played loose instead of
        /// in the human style. Kept for hosts that configure such a player themselves.
        /// </summary>
        public const double ExpertTemperature = 1.5;

        /// <summary>The largest action-value loss the September 29 Expert accepted (see <see cref="ExpertTemperature"/>).</summary>
        public const double ExpertMaxRegret = 4;

        /// <summary>The human-style players double or redouble only when it is worth this many game points more than any other bid.</summary>
        public const double DoubleMargin = 2;

        /// <summary>The Master's rollouts per card decision in the first three tricks.</summary>
        public const int MasterSearchDeals = 32;

        /// <summary>The Master's rollout budget per card in milliseconds (at least twelve deals).</summary>
        public const int MasterSearchMilliseconds = 40;

        /// <summary>The Master's endgame search budget in milliseconds.</summary>
        public const int MasterMilliseconds = 24;

        /// <summary>The September 29 Master's endgame search budget in milliseconds.</summary>
        public const int NeuralMasterMilliseconds = 8;

        /// <summary>The previous Master's number of sampled full-deal rollouts.</summary>
        public const int RolloutMasterSearchDeals = 100;

        /// <summary>The previous Master's rollout budget in milliseconds.</summary>
        public const int RolloutMasterMilliseconds = 400;

        /// <summary>The heuristic player's endgame horizon: its last tricks played out exactly (see HEURISTIC_PLAYER.md).</summary>
        public const int HeuristicEndgameTricks = 5;

        /// <summary>
        /// Creates the heuristic player: the rules of the belot.bg academy and the other sources,
        /// with its last five tricks played out exactly over the deals the play allows (at most
        /// 150,000 solver nodes and 8 ms a card). See HEURISTIC_PLAYER.md.
        /// </summary>
        public static ClaudePlayerHeuristic CreateHeuristic()
        {
            var player = new ClaudePlayerHeuristic();
            player.Settings.EndgameTricks = HeuristicEndgameTricks;
            return player;
        }

        /// <summary>Creates the heuristic player with the rules alone: no search of any kind.</summary>
        public static ClaudePlayerHeuristic CreateRulesOnly() => new ClaudePlayerHeuristic();

        /// <summary>Creates a fresh fast player (the app's hints) using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateFast() => CreateFast(NeuralModels.Embedded);

        /// <summary>Creates a fresh Expert using the embedded networks.</summary>
        public static ClaudePlayerNeural CreateExpert() => CreateExpert(NeuralModels.Embedded);

        /// <summary>Creates a fresh Master using the embedded networks (see HUMAN_PLAY.md).</summary>
        public static ClaudePlayerNeural CreateMaster() => CreateMaster(NeuralModels.Embedded);

        /// <summary>Creates the September 29 Master (belief5-v2-ensemble), for comparison.</summary>
        public static ClaudePlayerNeural CreateNeuralMaster() => CreateNeuralMaster(NeuralModels.Embedded);

        /// <summary>Creates the previous Master with 100 full-deal neural rollouts, for comparison.</summary>
        public static ClaudePlayerNeural CreateRolloutMaster() => CreateRolloutMaster(NeuralModels.Embedded);

        /// <summary>
        /// The network with bounded three-trick endings; it bids naturally, near-ties go to the card
        /// a strong player prefers and equal endings keep their raw card points.
        /// </summary>
        internal static ClaudePlayerNeural CreateFast(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            HumanStyle = true,
            NaturalBidding = true,
            EndgameRawTieBreak = true,
            DoubleMargin = DoubleMargin,
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        /// <summary>The fast player, choosing the most natural card within <see cref="ExpertTolerance"/> of the best.</summary>
        internal static ClaudePlayerNeural CreateExpert(NeuralModels models)
        {
            var player = CreateFast(models);
            player.HumanNetworkTolerance = ExpertTolerance;
            player.HumanSearchTolerance = ExpertTolerance;
            return player;
        }

        /// <summary>
        /// The human-style Master: in the first three tricks the network's three best cards (within
        /// three game points of its best) are played out in up to 32 ownership-weighted worlds, by
        /// the networks to the last five tricks and exactly from there; later tricks use
        /// belief-weighted five-trick endgames. Near-ties go to the card a strong player prefers,
        /// equal endings keep their raw card points, the partner's discards are read by the
        /// convention and its own follow it, it bids naturally and doubles only with a clear margin.
        /// </summary>
        internal static ClaudePlayerNeural CreateMaster(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            HumanStyle = true,
            NaturalBidding = true,
            HumanDiscardTolerance = 0.5,
            EndgameRawTieBreak = true,
            EndgameSignalWeight = 0.3,
            DoubleMargin = DoubleMargin,
            CardSuitEnsemble = true,
            SearchDeals = MasterSearchDeals,
            SearchDoubleDummyTricks = 5,
            SearchCandidateCards = 3,
            SearchCandidateMargin = 3,
            SearchTimeLimitMilliseconds = MasterSearchMilliseconds,
            SearchMinimumDeals = 12,
            SearchOwnershipModel = CardOwnershipModel.Embedded,
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 5,
            EndgameThreeTrickWorldLimit = 1680,
            EndgameSampledWorlds = 256,
            EndgameNodeLimit = 1500000,
            EndgameTimeLimitMilliseconds = MasterMilliseconds,
            EndgameUseTranspositions = true,
            EndgameOwnershipModel = CardOwnershipModel.Embedded,
            EndgameOwnershipPower = 1,
            EndgameOwnershipUniformMix = 0.1,
        };

        /// <summary>The September 29 Master: the network, then belief-weighted five-trick endgames in 8 ms.</summary>
        internal static ClaudePlayerNeural CreateNeuralMaster(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            CardSuitEnsemble = true,
            UseEndgameSearch = true,
            EndgameUseDeclarations = true,
            EndgameTricks = 5,
            EndgameThreeTrickWorldLimit = 1680,
            EndgameSampledWorlds = 128,
            EndgameNodeLimit = 250000,
            EndgameTimeLimitMilliseconds = NeuralMasterMilliseconds,
            EndgameUseTranspositions = true,
            EndgameOwnershipModel = CardOwnershipModel.Embedded,
            EndgameOwnershipPower = 1,
            EndgameOwnershipUniformMix = 0.1,
        };

        internal static ClaudePlayerNeural CreateRolloutMaster(NeuralModels models) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            SearchDeals = RolloutMasterSearchDeals,
            SearchTimeLimitMilliseconds = RolloutMasterMilliseconds,
        };
    }
}
