namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays whole matches of one profile in all four seats and records every deal's award (the
    /// game points each team got, whether a team took no trick, whether it was passed out), for
    /// the match-equity table. Writes "south-north,east-west,capot,passed,count" lines.
    /// </summary>
    internal static class Outcomes
    {
        public static void Run(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? AI.ClaudePlayer.Neural.NeuralModels.Embedded : AI.ClaudePlayer.Neural.NeuralModels.Load(settings.In);
            var factory = OpponentCatalog.Factory(settings.Player, models);
            var counts = new Dictionary<(int SouthNorth, int EastWest, bool Capot, bool Passed), long>();
            var clock = Stopwatch.StartNew();
            var done = 0;
            Parallel.For(0, settings.Pairs, new ParallelOptions { MaxDegreeOfParallelism = settings.Threads }, game =>
            {
                var seed = unchecked((settings.Seed * 100_000) + game);
                var recorder = new Recorder(factory(seed * 4));
                var match = new BelotGame(recorder, factory((seed * 4) + 1), factory((seed * 4) + 2), factory((seed * 4) + 3), new Random(seed));
                match.PlayGame((PlayerPosition)(1 << (game % 4)));
                lock (counts)
                {
                    foreach (var key in recorder.Results)
                    {
                        counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
                    }
                }

                var finished = Interlocked.Increment(ref done);
                if (finished % Math.Max(1, settings.Pairs / 10) == 0)
                {
                    Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {finished}/{settings.Pairs} games");
                }
            });

            var text = new StringBuilder();
            foreach (var entry in counts.OrderBy(x => x.Key.SouthNorth).ThenBy(x => x.Key.EastWest))
            {
                text.AppendLine($"{entry.Key.SouthNorth},{entry.Key.EastWest},{(entry.Key.Capot ? 1 : 0)},{(entry.Key.Passed ? 1 : 0)},{entry.Value}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
            File.WriteAllText(settings.Data + ".outcomes.csv", text.ToString());
            Console.WriteLine($"{counts.Values.Sum()} deals, {counts.Count} distinct outcomes ({clock.Elapsed})");
        }

        private sealed class Recorder : IPlayer
        {
            private readonly IPlayer player;

            public Recorder(IPlayer player)
            {
                this.player = player;
            }

            public List<(int SouthNorth, int EastWest, bool Capot, bool Passed)> Results { get; } = new List<(int SouthNorth, int EastWest, bool Capot, bool Passed)>();

            public BidType GetBid(PlayerGetBidContext context) => this.player.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.player.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.player.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.player.EndOfTrick(trickActions);

            public void EndOfRound(RoundResult roundResult)
            {
                this.Results.Add((roundResult.SouthNorthPoints, roundResult.EastWestPoints, roundResult.NoTricksForOneOfTheTeams, roundResult.Contract.Type == BidType.Pass));
                this.player.EndOfRound(roundResult);
            }

            public void EndOfGame(GameResult gameResult) => this.player.EndOfGame(gameResult);
        }
    }
}
