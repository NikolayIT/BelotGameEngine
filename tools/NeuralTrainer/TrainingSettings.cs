namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// The trainer's settings, each overridable on the command line as --name value (for
    /// example --hours 10 --actors 12 --sizes 512,256,128).
    /// </summary>
    internal sealed class TrainingSettings
    {
        /// <summary>Gets or sets the folder to start from ("" = random networks of <see cref="Sizes"/>).</summary>
        public string In { get; set; } = string.Empty;

        /// <summary>Gets or sets the folder of the run: checkpoints, the best networks, the log.</summary>
        public string Out { get; set; } = "checkpoints/run";

        /// <summary>Gets or sets the hidden layers of new card networks.</summary>
        public string Sizes { get; set; } = "512,256,128";

        /// <summary>Gets or sets the hidden layers of a new bidding network.</summary>
        public string BidSizes { get; set; } = "256,128";

        public double Hours { get; set; } = 10;

        public int Actors { get; set; } = Math.Max(1, Environment.ProcessorCount - 8);

        /// <summary>Gets or sets the threads that sum the gradients of a batch.</summary>
        public int Learners { get; set; } = 8;

        public int Batch { get; set; } = 1024;

        public double LearningRate { get; set; } = 1e-4;

        /// <summary>Gets or sets the learning rate at the end of the run (it falls linearly to it; negative = constant).</summary>
        public double FinalLearningRate { get; set; } = -1;

        public double MaxNorm { get; set; } = 1.0;

        /// <summary>Gets or sets where the loss turns from squared to linear, in units of 26 game points.</summary>
        public double Huber { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the common value loss weight for cards (-1 = independent Huber losses;
        /// 0 or more = centred action losses plus this weight on the mean error).
        /// </summary>
        public double CardValueWeight { get; set; } = -1;

        /// <summary>Gets or sets how many times, on average, a sample is learned from.</summary>
        public double Replay { get; set; } = 4;

        public int Capacity { get; set; } = 1_000_000;

        public int BidCapacity { get; set; } = 300_000;

        /// <summary>Gets or sets how many samples a network needs before it learns.</summary>
        public int Warmup { get; set; } = 20_000;

        /// <summary>Gets or sets the chance a card decision (with a choice) becomes a sample.</summary>
        public double CardLabelChance { get; set; } = 0.5;

        public double BidLabelChance { get; set; } = 1.0;

        /// <summary>Gets or sets the chance the deal goes on with a random card instead of the best.</summary>
        public double CardExploration { get; set; } = 0.03;

        public double BidExploration { get; set; } = 0.03;

        /// <summary>Gets or sets the chance that one team of a deal plays with networks from the pool.</summary>
        public double PoolChance { get; set; } = 0.3;

        public double PoolMinutes { get; set; } = 20;

        public int PoolSize { get; set; } = 12;

        /// <summary>Gets or sets how often the networks the actors use are refreshed.</summary>
        public double PublishSeconds { get; set; } = 10;

        public double EvaluateMinutes { get; set; } = 30;

        public int SmartPairs { get; set; } = 400;

        public int IsmctsPairs { get; set; } = 100;

        public int IsmctsMilliseconds { get; set; } = 20;

        public int EvaluationThreads { get; set; } = 6;

        /// <summary>Gets or sets how many evaluations without a new best stop the run (0 = never).</summary>
        public int Patience { get; set; }

        public int Seed { get; set; } = 1;

        /// <summary>Gets or sets the sample files' path prefix ("distill" and "fit").</summary>
        public string Data { get; set; } = "data/distill";

        /// <summary>Gets or sets an independent validation sample prefix (empty = hold out 5% of Data).</summary>
        public string ValidationData { get; set; } = string.Empty;

        /// <summary>Gets or sets whether fit saves each completed epoch for subsequent match evaluation.</summary>
        public bool FitCheckpoints { get; set; }

        /// <summary>Gets or sets the distillation teacher: ismcts, neural sampled worlds, or endgame plus original network values elsewhere.</summary>
        public string Teacher { get; set; } = "ismcts";

        /// <summary>Gets or sets how often a labelled neural-teacher decision is played by the teacher (0 = student trajectories).</summary>
        public double TeacherPlayChance { get; set; } = 1;

        /// <summary>Gets or sets the loopback port of the optional training-only GPU inference server.</summary>
        public int GpuPort { get; set; } = 18731;

        /// <summary>Gets or sets whether every GPU rollout choice is checked against managed inference.</summary>
        public bool GpuVerify { get; set; }

        /// <summary>Gets or sets how many games of ClaudePlayerIsmcts "distill" records.</summary>
        public int Games { get; set; } = 3000;

        /// <summary>Gets or sets ClaudePlayerIsmcts's time per card in "distill".</summary>
        public int Milliseconds { get; set; } = 50;

        public int Epochs { get; set; } = 8;

        public double FitLearningRate { get; set; } = 3e-4;

        /// <summary>Gets or sets what "validate" plays against: smart, sharpbelot, belot206, ismcts:ms or a folder of networks.</summary>
        public string Opponent { get; set; } = "ismcts:100";

        /// <summary>Gets or sets a separate neural weight folder for arena's opposing team (empty = In).</summary>
        public string OpponentIn { get; set; } = string.Empty;

        /// <summary>Gets or sets the candidate name for arena.</summary>
        public string Player { get; set; } = "neural";

        /// <summary>Gets or sets the sampled worlds for an opponent loaded from a network folder.</summary>
        public int OpponentSearchDeals { get; set; }

        public int OpponentSearchMilliseconds { get; set; }

        /// <summary>Gets or sets whether a folder opponent uses the same endgame settings as the candidate.</summary>
        public bool OpponentEndgame { get; set; }

        public int Pairs { get; set; } = 200;

        /// <summary>Gets or sets how many labelled deals "bench" times (0 = time 20000 deals without labels).</summary>
        public int Deals { get; set; }

        /// <summary>Gets or sets the networks' temperature in "validate" (0 = always the best action).</summary>
        public double Temperature { get; set; }

        /// <summary>Gets or sets whether record-ppo reads training-only float32 actor snapshots.</summary>
        public bool PpoFloat { get; set; }

        /// <summary>Gets or sets the comma-separated named opponents used by record-ppo.</summary>
        public string PpoOpponents { get; set; } = string.Empty;

        /// <summary>Gets or sets the fraction of PPO deals with a sampled external opposing team.</summary>
        public double PpoOpponentChance { get; set; }

        /// <summary>Gets or sets the networks' MaxRegret in "validate", in game points.</summary>
        public double MaxRegret { get; set; } = double.PositiveInfinity;

        /// <summary>Gets or sets how many deals the networks' card decisions play out in "validate" (0 = none).</summary>
        public int SearchDeals { get; set; }

        public bool Endgame { get; set; }

        public bool EndgameDeclarations { get; set; }

        public int EndgameTricks { get; set; } = 2;

        public int EndgameWorlds { get; set; } = 8;

        public double SearchPriorDeals { get; set; }

        public double SearchPruneMargin { get; set; }

        public int SearchMilliseconds { get; set; }

        /// <summary>Gets or sets a value indicating whether the networks may double ("validate").</summary>
        public bool MayDouble { get; set; } = true;

        /// <summary>Gets or sets who bids for the networks in "validate": net, smart or ismcts (to judge the card play alone).</summary>
        public string Bidding { get; set; } = "net";

        public int Threads { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

        public static int[] ParseSizes(string text) =>
            text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();

        /// <summary>Reads --name value pairs into the settings' properties (case-insensitive, dashes ignored).</summary>
        public static T Parse<T>(IReadOnlyList<string> args, T settings)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(x => x.Name.ToUpperInvariant());
            for (var i = 0; i < args.Count; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Expected --name, got {args[i]}.");
                }

                var name = args[i].Substring(2).Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
                if (!properties.TryGetValue(name, out var property) || i + 1 >= args.Count)
                {
                    throw new ArgumentException($"Unknown setting or missing value: {args[i]}.");
                }

                property.SetValue(settings, Convert.ChangeType(args[++i], property.PropertyType, CultureInfo.InvariantCulture));
            }

            return settings;
        }

        public override string ToString() =>
            string.Join(
                " ",
                typeof(TrainingSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(x => $"{x.Name}={Convert.ToString(x.GetValue(this), CultureInfo.InvariantCulture)}"));
    }
}
