namespace Belot.NeuralTrainer
{
    using System;
    using System.Globalization;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine.Players;
    using BelotLegacy;

    /// <summary>One named catalog for reproducible evaluation and training opponents.</summary>
    internal static class OpponentCatalog
    {
        public static Func<int, IPlayer> Factory(string name, NeuralModels models = null)
        {
            var and = name.IndexOf('&', StringComparison.Ordinal);
            if (and > 0)
            {
                // first&partner: a team of two different players. A team's two players are made
                // one after the other on one thread, so the calls alternate between the two.
                var first = Factory(name[..and], models);
                var partner = Factory(name[(and + 1)..], models);
                var next = new System.Threading.ThreadLocal<bool>();
                return seed =>
                {
                    var second = next.Value;
                    next.Value = !second;
                    return second ? partner(seed) : first(seed);
                };
            }

            var plus = name.IndexOf('+', StringComparison.Ordinal);
            if (plus > 0)
            {
                // profile+option=value+...: a neural profile with some settings changed.
                var profile = Factory(name[..plus], models);
                var options = name[(plus + 1)..].Split('+');
                return seed => Modify((ClaudePlayerNeural)profile(seed), options);
            }

            var bar = name.IndexOf('|', StringComparison.Ordinal);
            if (bar > 0)
            {
                // bidder|player: one player's bids with another's card play.
                var bidder = Factory(name[..bar], models);
                var player = Factory(name[(bar + 1)..], models);
                return seed => new MixedPlayer(bidder(seed), player(seed));
            }

            if (name.StartsWith("ismcts:", StringComparison.OrdinalIgnoreCase))
            {
                var milliseconds = int.Parse(name[7..], CultureInfo.InvariantCulture);
                if (milliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(name), "ISMCTS milliseconds must be positive.");
                }

                return seed => new ClaudePlayerIsmcts { Rng = new Random(seed), TimeLimitMilliseconds = milliseconds };
            }

            if (name.StartsWith("hybrid:", StringComparison.OrdinalIgnoreCase))
            {
                // hybrid:deals[:ddtricks]: the human-style Master with neural rollouts before its endgames.
                var parts = name.Split(':');
                var deals = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var tricks = parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 3;
                return seed =>
                {
                    var player = Seed(Human(models), seed);
                    player.SearchDeals = deals;
                    player.SearchDoubleDummyTricks = tricks;
                    if (parts.Length > 3 && parts[3] == "own")
                    {
                        player.SearchOwnershipModel = CardOwnershipModel.Embedded;
                    }

                    return player;
                };
            }

            return name.ToLowerInvariant() switch
            {
                "random" => seed => new RandomPlayer(new Random(seed)),
                "dummy" => _ => new DummyPlayer(),
                "smart" => _ => new SmartPlayer(),
                "sharpbelot" => seed => new SharpBelotPlayer(seed),
                "belot206" => seed => new Belot206Player(seed),
                "neural" => seed => Neural(models, seed, false),
                "fast" => seed => Seed(ClaudePlayerProfiles.CreateFast(models), seed),
                "sampled4" => seed => SampledFour(models, seed),
                "expert" => seed => Seed(ClaudePlayerProfiles.CreateExpert(models), seed),
                "master" => seed => Seed(ClaudePlayerProfiles.CreateMaster(models), seed),
                "neural-master" => seed => Seed(ClaudePlayerProfiles.CreateNeuralMaster(models), seed),
                "human" => seed => Seed(Human(models), seed),
                "master-raw" => seed => Raw(ClaudePlayerProfiles.CreateNeuralMaster(models), seed),
                "rollout-master" => seed => Seed(ClaudePlayerProfiles.CreateRolloutMaster(models), seed),
                _ => throw new ArgumentException($"Unknown player '{name}'. Use random, dummy, smart, sharpbelot, belot206, neural, fast, sampled4, expert, master, neural-master, human, rollout-master, ismcts:100, hybrid:deals[:tricks[:own]], bidder|player or profile+option=value.", nameof(name)),
            };
        }

        public static ClaudePlayerNeural Configured(TrainingSettings settings, NeuralModels models, int seed) => new ClaudePlayerNeural(models)
        {
            Rng = new Random(seed),
            MayDouble = settings.MayDouble,
            NaturalBidding = settings.NaturalBidding,
            Temperature = settings.Temperature,
            MaxRegret = settings.MaxRegret,
            SearchDeals = settings.SearchDeals,
            SearchPriorDeals = settings.SearchPriorDeals,
            SearchPruneMargin = settings.SearchPruneMargin,
            SearchTimeLimitMilliseconds = settings.SearchMilliseconds,
            SearchControlVariateDeals = settings.SearchControlVariateDeals,
            SearchRolloutTricks = settings.SearchRolloutTricks,
            SearchRolloutRootLeaf = settings.SearchRolloutRootLeaf,
            SearchDoubleDummyTricks = settings.SearchDoubleDummyTricks,
            SearchUseDeclarations = settings.SearchDeclarations,
            SearchMinimumDeals = settings.SearchMinimumDeals,
            UseEndgameSearch = settings.Endgame,
            EndgameUseDeclarations = settings.EndgameDeclarations,
            EndgameTricks = settings.EndgameTricks,
            EndgameThreeTrickWorldLimit = settings.EndgameWorlds,
            EndgameSampledWorlds = settings.EndgameSampledWorlds,
            EndgameNodeLimit = settings.EndgameNodes,
            EndgameTimeLimitMilliseconds = settings.EndgameMilliseconds,
            EndgamePruneEquivalentCards = settings.EndgamePruning,
            EndgameUseTranspositions = settings.EndgameTranspositions,
            EndgamePolicyActions = settings.EndgamePolicyActions,
            EndgamePolicyTemperature = settings.EndgamePolicyTemperature,
            EndgamePolicyUniformMix = settings.EndgamePolicyUniformMix,
            EndgamePolicyPower = settings.EndgamePolicyPower,
            EndgameOwnershipModel = string.IsNullOrEmpty(settings.EndgameOwnership) ? null : CardOwnershipModel.LoadCached(settings.EndgameOwnership),
            EndgameOwnershipPower = settings.EndgameOwnershipPower,
            EndgameOwnershipUniformMix = settings.EndgameOwnershipUniformMix,
            CardCorrectionModel = string.IsNullOrEmpty(settings.CardCorrection) ? null : LateCardCorrectionModel.LoadCached(settings.CardCorrection),
            CardSuitEnsemble = settings.CardSuitEnsemble,
        };

        private static ClaudePlayerNeural Neural(NeuralModels models, int seed, bool endgame) => new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
        {
            Rng = new Random(seed),
            UseEndgameSearch = endgame,
            EndgameUseDeclarations = true,
            EndgameTricks = 3,
            EndgameThreeTrickWorldLimit = 90,
        };

        private static ClaudePlayerNeural Modify(ClaudePlayerNeural player, string[] options)
        {
            foreach (var option in options)
            {
                var parts = option.Split('=');
                var value = parts.Length > 1 ? parts[1] : "1";
                double Number() => double.Parse(value, CultureInfo.InvariantCulture);
                switch (parts[0])
                {
                    case "ms":
                        player.EndgameTimeLimitMilliseconds = (int)Number();
                        break;
                    case "worlds":
                        player.EndgameSampledWorlds = (int)Number();
                        break;
                    case "nodes":
                        player.EndgameNodeLimit = (int)Number();
                        break;
                    case "tricks":
                        player.EndgameTricks = (int)Number();
                        break;
                    case "nodbl":
                        player.MayDouble = false;
                        break;
                    case "human":
                        player.HumanStyle = Number() != 0;
                        break;
                    case "raw":
                        player.EndgameRawTieBreak = Number() != 0;
                        break;
                    case "ntol":
                        player.HumanNetworkTolerance = Number();
                        break;
                    case "stol":
                        player.HumanSearchTolerance = Number();
                        break;
                    case "ens":
                        player.CardSuitEnsemble = Number() != 0;
                        break;
                    case "temp":
                        player.Temperature = Number();
                        break;
                    case "regret":
                        player.MaxRegret = Number();
                        break;
                    case "ecand":
                        player.EndgameCandidateCards = (int)Number();
                        break;
                    case "emargin":
                        player.EndgameCandidateMargin = Number();
                        break;
                    case "natural":
                        player.NaturalBidding = Number() != 0;
                        break;
                    case "own":
                        // A folder of ownership networks for both the endgames and the rollouts.
                        var ownership = CardOwnershipModel.LoadCached(value);
                        player.EndgameOwnershipModel = ownership;
                        if (player.SearchDeals > 0)
                        {
                            player.SearchOwnershipModel = ownership;
                        }

                        break;
                    case "dmargin":
                        player.DoubleMargin = Number();
                        break;
                    case "match":
                        player.PlayForMatch = Number() != 0;
                        break;
                    case "dom":
                        player.HumanDominance = Number() != 0;
                        break;
                    case "rtol":
                        player.HumanRolloutTolerance = Number();
                        break;
                    case "ltol":
                        player.HumanLeadTolerance = Number();
                        break;
                    case "dtol":
                        player.HumanDiscardTolerance = Number();
                        break;
                    case "sig":
                        player.EndgameSignalWeight = Number();
                        break;
                    case "ecands":
                        player.SearchEnsembleCandidates = Number() != 0;
                        break;
                    case "cand":
                        player.SearchCandidateCards = (int)Number();
                        break;
                    case "margin":
                        player.SearchCandidateMargin = Number();
                        break;
                    case "sms":
                        player.SearchTimeLimitMilliseconds = (int)Number();
                        break;
                    case "smin":
                        player.SearchMinimumDeals = (int)Number();
                        break;
                    case "prior":
                        player.SearchPriorDeals = Number();
                        break;
                    case "deals":
                        player.SearchDeals = (int)Number();
                        break;
                    default:
                        throw new ArgumentException($"Unknown player option '{option}'.", nameof(options));
                }
            }

            return player;
        }

        private static ClaudePlayerNeural Seed(ClaudePlayerNeural player, int seed)
        {
            player.Rng = new Random(seed);
            return player;
        }

        // The September 29 Master with the human style alone (no rollouts), the development baseline.
        private static ClaudePlayerNeural Human(NeuralModels models)
        {
            var player = ClaudePlayerProfiles.CreateNeuralMaster(models);
            player.HumanStyle = true;
            player.EndgameRawTieBreak = true;
            return player;
        }

        private static ClaudePlayerNeural Raw(ClaudePlayerNeural player, int seed)
        {
            player.Rng = new Random(seed);
            player.EndgameRawTieBreak = true;
            return player;
        }

        private static ClaudePlayerNeural SampledFour(NeuralModels models, int seed)
        {
            return new ClaudePlayerNeural(models ?? NeuralModels.Embedded)
            {
                Rng = new Random(seed),
                UseEndgameSearch = true,
                EndgameUseDeclarations = true,
                EndgameTricks = 4,
                EndgameThreeTrickWorldLimit = 1680,
                EndgameSampledWorlds = 128,
                EndgameNodeLimit = 250000,
            };
        }
    }
}
