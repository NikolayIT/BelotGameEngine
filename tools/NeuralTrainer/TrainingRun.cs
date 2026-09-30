namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.SmartPlayer;
    using Belot.Engine.Players;

    /// <summary>
    /// A self-play training run (the trainer's "train" command): actor threads play deals with
    /// the current networks and label decisions by rolling every action out
    /// (<see cref="SelfPlayActor"/>); a learner thread trains the four networks on the newest
    /// samples and hands the actors fresh copies every few seconds; now and then a copy joins
    /// the pool of past opponents, and the networks are saved and measured in whole games
    /// against SmartPlayer and ClaudePlayerIsmcts, keeping the best.
    /// </summary>
    internal sealed class TrainingRun
    {
        private static readonly string[] Names = { "bid", "trump", "notrumps", "alltrumps" };

        private readonly TrainingSettings settings;
        private readonly Mlp[] networks = new Mlp[4];
        private readonly SampleBuffer[] buffers = new SampleBuffer[4];
        private readonly long[] trained = new long[4];
        private readonly double[] losses = new double[4];
        private readonly List<NeuralModels> pool = new List<NeuralModels>();
        private readonly Stopwatch clock = new Stopwatch();
        private SelfPlayActor[] actors;
        private volatile NeuralModels current;
        private volatile bool stopping;
        private long steps;
        private string logPath;

        public TrainingRun(TrainingSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>The networks to start from: a folder's, or new random ones.</summary>
        public static Mlp[] CreateNetworks(TrainingSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.In))
            {
                var models = NeuralModels.Load(settings.In);
                return models.Networks.Select(x => new Mlp(x)).ToArray();
            }

            var random = new Random(settings.Seed);
            var cardSizes = TrainingSettings.ParseSizes(settings.Sizes);
            var bidSizes = TrainingSettings.ParseSizes(settings.BidSizes);
            var result = new Mlp[4];
            result[0] = new Mlp(0, new[] { FeatureEncoder.BidInputs }.Concat(bidSizes).Append(FeatureEncoder.BidOutputs).ToArray(), random);
            for (var tag = 1; tag < 4; tag++)
            {
                result[tag] = new Mlp(tag, new[] { FeatureEncoder.CardInputs }.Concat(cardSizes).Append(FeatureEncoder.CardOutputs).ToArray(), random);
            }

            return result;
        }

        public static NeuralModels ToModels(Mlp[] networks) =>
            new NeuralModels(networks[0].ToNetwork(), networks[1].ToNetwork(), networks[2].ToNetwork(), networks[3].ToNetwork());

        /// <summary>Plays the networks against SmartPlayer and ClaudePlayerIsmcts.</summary>
        public static (MatchResult Smart, MatchResult Ismcts) Measure(NeuralModels models, TrainingSettings settings, int seed)
        {
            IPlayer Neural() => new ClaudePlayerNeural(models) { NaturalBidding = settings.NaturalBidding };
            IPlayer Smart() => new SmartPlayer();
            IPlayer Ismcts() => new ClaudePlayerIsmcts { TimeLimitMilliseconds = settings.IsmctsMilliseconds };
            var smart = settings.SmartPairs > 0
                ? Evaluation.MirrorMatch(Neural, Neural, Smart, Smart, settings.SmartPairs, settings.EvaluationThreads, seed)
                : null;
            var ismcts = settings.IsmctsPairs > 0
                ? Evaluation.MirrorMatch(Neural, Neural, Ismcts, Ismcts, settings.IsmctsPairs, settings.EvaluationThreads, seed)
                : null;
            return (smart, ismcts);
        }

        public void Run()
        {
            Directory.CreateDirectory(this.settings.Out);
            this.logPath = Path.Combine(this.settings.Out, "log.csv");
            this.Log($"settings: {this.settings}");
            var initial = CreateNetworks(this.settings);
            for (var tag = 0; tag < 4; tag++)
            {
                this.networks[tag] = initial[tag];
                this.buffers[tag] = new SampleBuffer(
                    tag == 0 ? this.settings.BidCapacity : this.settings.Capacity,
                    tag == 0 ? FeatureEncoder.BidOutputs : FeatureEncoder.CardOutputs);
                this.Log($"{Names[tag]}: {string.Join("-", initial[tag].Sizes)}");
            }

            this.current = ToModels(this.networks);
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                this.stopping = true;
                Console.WriteLine("Stopping after this step...");
            };

            this.clock.Start();
            this.actors = new SelfPlayActor[this.settings.Actors];
            var threads = new List<Thread>();
            for (var i = 0; i < this.actors.Length; i++)
            {
                var id = i;
                this.actors[i] = new SelfPlayActor(this.settings, (this.settings.Seed * 1000) + id);
                threads.Add(new Thread(() => this.Act(id)) { IsBackground = true, Name = $"actor {id}" });
            }

            threads.Add(new Thread(this.Learn) { IsBackground = true, Name = "learner" });
            threads.ForEach(x => x.Start());

            var deadline = TimeSpan.FromHours(this.settings.Hours);
            var nextStatus = TimeSpan.FromMinutes(1);
            var nextPool = TimeSpan.FromMinutes(this.settings.PoolMinutes);
            var nextEvaluation = TimeSpan.FromMinutes(this.settings.EvaluateMinutes);
            var evaluations = 0;
            var best = double.NegativeInfinity;
            var sinceBest = 0;
            while (!this.stopping && this.clock.Elapsed < deadline)
            {
                Thread.Sleep(1000);
                var now = this.clock.Elapsed;
                if (now >= nextStatus)
                {
                    this.Status();
                    nextStatus = now + TimeSpan.FromMinutes(1);
                }

                if (now >= nextPool)
                {
                    lock (this.pool)
                    {
                        this.pool.Add(this.current);
                        if (this.pool.Count > this.settings.PoolSize)
                        {
                            this.pool.RemoveAt(0);
                        }
                    }

                    nextPool = now + TimeSpan.FromMinutes(this.settings.PoolMinutes);
                }

                if (now >= nextEvaluation)
                {
                    var (score, evaluated) = this.Evaluate(++evaluations);
                    if (score > best)
                    {
                        best = score;
                        sinceBest = 0;
                        evaluated.Save(Path.Combine(this.settings.Out, "best"));
                        this.Log($"new best ({score:0.000}), saved to {Path.Combine(this.settings.Out, "best")}");
                    }
                    else if (!double.IsNaN(score) && this.settings.Patience > 0 && ++sinceBest >= this.settings.Patience)
                    {
                        this.Log($"no new best in {sinceBest} evaluations: stopping");
                        break;
                    }

                    nextEvaluation = this.clock.Elapsed + TimeSpan.FromMinutes(this.settings.EvaluateMinutes);
                }
            }

            this.stopping = true;
            threads.ForEach(x => x.Join());
            this.Status();
            this.current.Save(Path.Combine(this.settings.Out, "final"));
            this.Log($"saved the final networks to {Path.Combine(this.settings.Out, "final")}");
        }

        private void Act(int id)
        {
            var actor = this.actors[id];
            var random = new Random((this.settings.Seed * 7919) + id);
            var models = new NeuralModels[4];
            while (!this.stopping)
            {
                var networks = this.current;
                NeuralModels opponents = null;
                if (this.settings.PoolChance > 0 && random.NextDouble() < this.settings.PoolChance)
                {
                    lock (this.pool)
                    {
                        opponents = this.pool.Count > 0 ? this.pool[random.Next(this.pool.Count)] : null;
                    }
                }

                var learners = 0b1111;
                var opponentTeam = random.Next(2);
                for (var seat = 0; seat < 4; seat++)
                {
                    models[seat] = opponents != null && (seat & 1) == opponentTeam ? opponents : networks;
                }

                if (opponents != null)
                {
                    learners = opponentTeam == 0 ? 0b1010 : 0b0101;
                }

                actor.PlayDeal(models, learners, this.buffers);
            }
        }

        private void Learn()
        {
            var random = new Random(this.settings.Seed);
            var workers = this.networks.Select(n => Enumerable.Range(0, this.settings.Learners).Select(_ => new MlpWorker(n.Sizes)).ToArray()).ToArray();
            var batches = this.buffers.Select(b => new Batch(this.settings.Batch, b.Outputs)).ToArray();
            var published = Stopwatch.StartNew();
            while (!this.stopping)
            {
                var worked = false;
                for (var tag = 0; tag < 4; tag++)
                {
                    var buffer = this.buffers[tag];
                    if (buffer.Count < this.settings.Warmup || this.trained[tag] >= this.settings.Replay * buffer.Written)
                    {
                        continue;
                    }

                    buffer.Sample(batches[tag], this.settings.Batch, random);
                    var loss = this.networks[tag].Train(
                        batches[tag],
                        workers[tag],
                        (float)this.LearningRate(),
                        (float)this.settings.MaxNorm,
                        (float)this.settings.Huber,
                        tag == NeuralModels.BidTag ? -1 : (float)this.settings.CardValueWeight);
                    this.losses[tag] = this.trained[tag] == 0 ? loss : (0.98 * this.losses[tag]) + (0.02 * loss);
                    this.trained[tag] += batches[tag].Count;
                    Interlocked.Increment(ref this.steps);
                    worked = true;
                }

                if (published.Elapsed.TotalSeconds >= this.settings.PublishSeconds)
                {
                    this.current = ToModels(this.networks);
                    published.Restart();
                }

                if (!worked)
                {
                    Thread.Sleep(20);
                }
            }

            this.current = ToModels(this.networks);
        }

        // The learning rate now: constant, or falling linearly to the final one over the run.
        private double LearningRate()
        {
            if (this.settings.FinalLearningRate < 0)
            {
                return this.settings.LearningRate;
            }

            var progress = Math.Min(1, this.clock.Elapsed.TotalHours / this.settings.Hours);
            return this.settings.LearningRate + ((this.settings.FinalLearningRate - this.settings.LearningRate) * progress);
        }

        private (double Score, NeuralModels Models) Evaluate(int index)
        {
            var models = this.current;
            var folder = Path.Combine(this.settings.Out, index.ToString("0000", CultureInfo.InvariantCulture));
            models.Save(folder);
            var stopwatch = Stopwatch.StartNew();
            var (smart, ismcts) = Measure(models, this.settings, 1_000_000 + (index * 10_000));
            if (smart == null && ismcts == null)
            {
                this.Log($"checkpoint {index} ({folder}): evaluation disabled");
                return (double.NaN, models);
            }

            this.Log($"evaluation {index} ({folder}, {stopwatch.Elapsed:mm\\:ss}): vs SmartPlayer {smart}; vs ISMCTS {this.settings.IsmctsMilliseconds} ms {ismcts}");
            return (ismcts?.Score ?? smart?.Score ?? 0, models);
        }

        private void Status()
        {
            var deals = this.actors.Sum(x => x.Deals);
            var seconds = Math.Max(1, this.clock.Elapsed.TotalSeconds);
            var samples = string.Join(
                ", ",
                Enumerable.Range(0, 4).Select(t =>
                    $"{Names[t]} {this.buffers[t].Written / 1000}k (loss {this.losses[t]:0.0000}, x{(double)this.trained[t] / Math.Max(1, this.buffers[t].Written):0.0})"));
            this.Log($"{this.clock.Elapsed:hh\\:mm\\:ss} deals {deals} ({deals / seconds:0}/s), steps {this.steps}, rate {this.LearningRate():0.0e0}, samples: {samples}");
        }

        private void Log(string line)
        {
            var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}";
            Console.WriteLine(text);
            File.AppendAllText(this.logPath, text + Environment.NewLine);
        }
    }
}
