namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>Frozen on-policy batches of complete deals, with a trajectory for each seat.</summary>
    internal static class PpoRecording
    {
        public const int Magic = 0x31505042; // BPP1.

        public static void Run(TrainingSettings settings)
        {
            if (settings.Deals <= 0 || settings.Threads <= 0 || !double.IsFinite(settings.Temperature) || settings.Temperature <= 0)
            {
                throw new ArgumentException("record-ppo requires positive --deals, --threads and --temperature.", nameof(settings));
            }

            var clock = Stopwatch.StartNew();
            var models = PpoTrainingFiles.Load(settings.In, settings.PpoFloat);
            var names = settings.PpoOpponents.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var opponents = names.Select(name => OpponentCatalog.Factory(name)).ToArray();
            if (!double.IsFinite(settings.PpoOpponentChance) || settings.PpoOpponentChance < 0 || settings.PpoOpponentChance > 1
                || (settings.PpoOpponentChance > 0 && opponents.Length == 0))
            {
                throw new ArgumentException("A PPO opponent chance in [0,1] requires named opponents when positive.", nameof(settings));
            }

            var workers = new Worker[settings.Threads];
            Parallel.For(0, workers.Length, new ParallelOptions { MaxDegreeOfParallelism = workers.Length }, index =>
            {
                var worker = new Worker(models, unchecked(settings.Seed + (index * 104729)), settings.Temperature, opponents, settings.PpoOpponentChance);
                workers[index] = worker;
                for (var deal = index; deal < settings.Deals; deal += workers.Length)
                {
                    worker.PlayDeal(deal);
                }
            });

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
            var counts = new int[3];
            for (var tag = 1; tag <= 3; tag++)
            {
                var name = Path.GetFileNameWithoutExtension(NeuralModels.FileNames[tag]);
                using var file = File.Create(settings.Data + "." + name + ".ppo");
                using var writer = new BinaryWriter(file);
                counts[tag - 1] = workers.Sum(worker => worker.Samples[tag - 1].Count);
                writer.Write(Magic);
                writer.Write(1);
                writer.Write(FeatureEncoder.LayoutVersion);
                writer.Write(tag);
                writer.Write(FeatureEncoder.CardInputs);
                writer.Write(96);
                writer.Write(counts[tag - 1]);
                var offset = 0;
                foreach (var worker in workers)
                {
                    foreach (var sample in worker.Samples[tag - 1])
                    {
                        Write(writer, sample, offset);
                    }

                    offset += worker.Samples[tag - 1].Count;
                }
            }

            var hashes = Enumerable.Range(0, 4).Select(tag =>
            {
                using var stream = File.OpenRead(Path.Combine(settings.In, PpoTrainingFiles.FileName(tag, settings.PpoFloat)));
                return Convert.ToHexString(SHA256.HashData(stream));
            }).ToArray();
            File.WriteAllText(settings.Data + ".ppo.json", JsonSerializer.Serialize(new
            {
                Version = 1,
                settings.Seed,
                settings.Deals,
                settings.Threads,
                settings.Temperature,
                settings.PpoFloat,
                settings.PpoOpponents,
                settings.PpoOpponentChance,
                OpponentDeals = names.Select((name, index) => new { Name = name, Deals = workers.Sum(worker => worker.OpponentDeals[index]) }).ToArray(),
                Counts = counts,
                Hashes = hashes,
                Seconds = clock.Elapsed.TotalSeconds,
            }));
            Console.WriteLine($"record-ppo: {settings.Deals} deals, {string.Join(",", counts)} decisions, {clock.Elapsed.TotalSeconds:0.00} s");
        }

        private static void Write(BinaryWriter writer, PpoSample sample, int offset)
        {
            writer.Write(sample.Deal);
            writer.Write(sample.Seat);
            writer.Write(sample.Mask);
            writer.Write(sample.Action);
            writer.Write(sample.LogProbability);
            writer.Write(sample.Outcome);
            writer.Write(sample.OldValue);
            writer.Write(sample.Next < 0 ? -1 : sample.Next + offset);
            writer.Write(sample.Owners);
            writer.Write((ushort)sample.Indices.Length);
            for (var i = 0; i < sample.Indices.Length; i++)
            {
                writer.Write((ushort)sample.Indices[i]);
                writer.Write(sample.Features[i]);
            }
        }

        internal sealed class Worker
        {
            private readonly NeuralModels models;
            private readonly NeuralEvaluator bidder;
            private readonly Random random;
            private readonly double temperature;
            private readonly Func<int, IPlayer>[] opponents;
            private readonly double opponentChance;
            private readonly Random opponentRandom;
            private readonly BelotSimulator simulator = new BelotSimulator();
            private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
            private readonly int[] deck = Enumerable.Range(0, 32).ToArray();
            private readonly int[] indices = new int[FeatureEncoder.MaxActive];
            private readonly float[] features = new float[FeatureEncoder.MaxActive];
            private readonly float[] values = new float[FeatureEncoder.CardOutputs];
            private int first;
            private int hanging;
            private int southNorthTotal;
            private int eastWestTotal;
            private IPlayer[] external;
            private TrainingDealContext history;

            public Worker(NeuralModels models, int seed, double temperature, Func<int, IPlayer>[] opponents = null, double opponentChance = 0)
            {
                this.models = models;
                this.bidder = new NeuralEvaluator(models);
                this.random = new Random(seed);
                this.temperature = temperature;
                this.first = this.random.Next(4);
                this.opponents = opponents ?? Array.Empty<Func<int, IPlayer>>();
                this.opponentChance = opponentChance;
                this.opponentRandom = new Random(unchecked(seed ^ 0x51c87ad));
                this.OpponentDeals = new int[this.opponents.Length];
            }

            public List<PpoSample>[] Samples { get; } = Enumerable.Range(0, 3).Select(_ => new List<PpoSample>()).ToArray();

            public int[] OpponentDeals { get; }

            public int LearnerMask { get; private set; } = 15;

            public void PlayDeal(int dealIndex)
            {
                for (var i = this.deck.Length - 1; i > 0; i--)
                {
                    var j = this.random.Next(i + 1);
                    (this.deck[i], this.deck[j]) = (this.deck[j], this.deck[i]);
                }

                var deal = NeuralDeal.Deal(this.deck, this.first, this.hanging);
                this.external = null;
                this.history = null;
                this.LearnerMask = 15;
                if (this.opponentChance > 0 && this.opponentRandom.NextDouble() < this.opponentChance)
                {
                    var index = this.opponentRandom.Next(this.opponents.Length);
                    this.OpponentDeals[index]++;
                    this.external = new IPlayer[4];
                    var parity = this.opponentRandom.Next(2);
                    this.LearnerMask = parity == 0 ? 10 : 5;
                    for (var seat = parity; seat < 4; seat += 2)
                    {
                        this.external[seat] = this.opponents[index](this.opponentRandom.Next());
                    }

                    this.history = new TrainingDealContext(dealIndex + 1, this.southNorthTotal, this.eastWestTotal);
                }

                while (!deal.AuctionFinished)
                {
                    if (this.history == null)
                    {
                        deal.Bid(this.bidder.BestBid(in deal, this.values));
                    }
                    else
                    {
                        var legal = deal.AvailableBids();
                        var opponent = this.external[deal.ToBid];
                        var bid = legal == BidType.Pass ? BidType.Pass : opponent == null
                            ? this.bidder.BestBid(in deal, this.values) : opponent.GetBid(this.history.BidContext(in deal));
                        if (bid != BidType.Pass && !legal.HasFlag(bid))
                        {
                            throw new InvalidOperationException("Training opponent returned an illegal bid.");
                        }

                        this.history.RecordBid(deal.ToBid, bid);
                        deal.RecordBid(bid);
                    }
                }

                if (deal.Contract != BidType.Pass)
                {
                    deal.StartPlay(this.simulator, this.announces);
                    this.history?.StartPlay(in deal);
                    this.PlayCards(dealIndex, ref deal);
                }

                deal.Score(this.simulator, out var southNorth, out var eastWest, out this.hanging);
                this.southNorthTotal += southNorth;
                this.eastWestTotal += eastWest;
                if (this.southNorthTotal >= 151 || this.eastWestTotal >= 151)
                {
                    this.southNorthTotal = this.eastWestTotal = this.hanging = 0;
                    this.first = this.random.Next(4);
                }
                else
                {
                    this.first = (this.first + 1) & 3;
                }
            }

            private void PlayCards(int dealIndex, ref NeuralDeal deal)
            {
                var network = FeatureEncoder.CardNetwork(deal.Kind);
                var samples = this.Samples[network];
                var start = samples.Count;
                var previous = new[] { -1, -1, -1, -1 };
                var rotation = FeatureEncoder.Rotation(deal.Kind);
                while (!deal.IsFinished)
                {
                    deal.DeclareIfFirstCard();
                    var legal = this.simulator.LegalMoves(in deal.Play);
                    int card;
                    var claimBelote = true;
                    if ((legal & (legal - 1)) == 0)
                    {
                        card = BitOperations.TrailingZeroCount(legal);
                    }
                    else if (this.external?[deal.Play.Turn] is IPlayer opponent)
                    {
                        var action = opponent.PlayCard(this.history.PlayContext(in deal, legal));
                        card = action.Card.GetHashCode();
                        claimBelote = action.Belote;
                        if ((legal & (1u << card)) == 0)
                        {
                            throw new InvalidOperationException("Training opponent returned an illegal card.");
                        }
                    }
                    else
                    {
                        var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.features);
                        this.models.Networks[network + 1].Forward(this.indices.AsSpan(0, count), this.features.AsSpan(0, count), this.values);
                        var mask = 0u;
                        for (var rest = legal; rest != 0; rest &= rest - 1)
                        {
                            mask |= 1u << FeatureEncoder.ToNetwork(BitOperations.TrailingZeroCount(rest), rotation);
                        }

                        var action = PpoPolicy.Sample(this.values, mask, this.temperature, this.random.NextDouble(), out var logProbability);
                        var seat = deal.Play.Turn;
                        if (previous[seat] >= 0)
                        {
                            samples[previous[seat]].Next = samples.Count;
                        }

                        previous[seat] = samples.Count;
                        samples.Add(new PpoSample
                        {
                            Deal = dealIndex,
                            Seat = (byte)seat,
                            Indices = this.indices[..count],
                            Features = this.features[..count],
                            Mask = mask,
                            Action = (byte)action,
                            LogProbability = logProbability,
                            OldValue = this.values[action],
                            Owners = CardOwnership.Encode(in deal),
                        });
                        card = FeatureEncoder.FromNetwork(action, rotation);
                    }

                    var belote = claimBelote && this.simulator.IsBelote(in deal.Play, card, legal);
                    this.history?.RecordCard(deal.Play.Turn, card, belote);
                    deal.PlayCard(this.simulator, card, belote);
                }

                deal.Score(this.simulator, out var southNorth, out var eastWest, out _);
                var result = (southNorth - eastWest) / NeuralEvaluator.ValueScale;
                for (var i = start; i < samples.Count; i++)
                {
                    samples[i].Outcome = (samples[i].Seat & 1) == 0 ? result : -result;
                }
            }
        }
    }
}
