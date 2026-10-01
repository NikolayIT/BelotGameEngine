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

    using Belot.AI.ClaudePlayer;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Where the heuristic player loses: whole matches of team A (settings.Player, a heuristic
    /// profile) against team B, a neural teacher (settings.Teacher, by default the fast profile)
    /// valuing every legal card and bid of team A's decisions. The regret of a choice is the
    /// teacher's best value minus the value of the choice, in game points; it is summed per
    /// situation (contract, side, seat in the trick, tricks, the rule that chose the card), the
    /// largest totals first. A development tool: the teacher only grades.
    /// </summary>
    internal static class Regret
    {
        public static void Run(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? AI.ClaudePlayer.Neural.NeuralModels.Embedded : AI.ClaudePlayer.Neural.NeuralModels.Load(settings.In);
            var teamA = OpponentCatalog.Factory(settings.Player, models);
            var teamB = OpponentCatalog.Factory(settings.Opponent, models);
            var teacherName = settings.Teacher == "ismcts" ? "fast" : settings.Teacher;
            var teacherFactory = OpponentCatalog.Factory(teacherName, models);
            var totals = new Dictionary<string, Stat>();
            var examples = new Dictionary<string, List<string>>();
            var clock = Stopwatch.StartNew();
            var done = 0;
            Parallel.For(0, settings.Pairs, new ParallelOptions { MaxDegreeOfParallelism = settings.Threads }, pair =>
            {
                var seed = unchecked((settings.Seed * 100_000) + pair);
                var teacher = (ClaudePlayerNeural)teacherFactory(seed);
                var local = new Dictionary<string, Stat>();
                for (var leg = 0; leg < 2; leg++)
                {
                    var a = new[] { teamA(seed * 2), teamA((seed * 2) + 1) };
                    var b = new[] { teamB(seed * 2), teamB((seed * 2) + 1) };
                    var players = leg == 0 ? new[] { a[0], b[0], a[1], b[1] } : new[] { b[0], a[0], b[1], a[1] };
                    var match = new BelotMatch(new BelotMatchOptions { FirstToPlay = (PlayerPosition)(1 << (pair % 4)), Random = new Random(seed) });
                    match.Start();
                    while (!match.IsFinished)
                    {
                        var seat = match.ToMove;
                        var view = match.GetView(seat);
                        var player = players[seat.Index()];
                        var teamASeat = ((seat.Index() & 1) == 0) == (leg == 0);
                        BelotAction action;
                        if (view.Decision == BelotDecision.PlayCard)
                        {
                            var context = view.CreatePlayCardContext();
                            var card = player.PlayCard(context);
                            action = BelotAction.PlayCard(card.Card, card.Belote);
                            if (teamASeat && context.AvailableCardsToPlay.Count > 1 && player is ClaudePlayerHeuristic heuristic)
                            {
                                var values = teacher.EvaluateCards(view.CreatePlayCardContext());
                                var best = values[0].Value;
                                var chosen = values.First(x => x.Card == card.Card).Value;
                                var key = CardKey(context, heuristic.LastRule);
                                Add(local, key, best - chosen);
                                if (best - chosen > 2)
                                {
                                    lock (examples)
                                    {
                                        if (!examples.TryGetValue(key, out var list))
                                        {
                                            examples[key] = list = new List<string>();
                                        }

                                        if (list.Count < 4)
                                        {
                                            list.Add(Describe(context, card.Card, values));
                                        }
                                    }
                                }
                            }
                        }
                        else if (view.Decision == BelotDecision.Bid)
                        {
                            var context = view.CreateBidContext();
                            var bid = player.GetBid(context);
                            action = BelotAction.Bid(bid);
                            if (teamASeat)
                            {
                                var values = teacher.EvaluateBids(view.CreateBidContext());
                                var chosen = values.FirstOrDefault(x => x.Bid == bid);
                                if (values.Count > 1 && chosen.Bid == bid)
                                {
                                    var key = BidKey(context, bid, values[0].Bid);
                                    Add(local, key, values[0].Value - chosen.Value);
                                    if (values[0].Value - chosen.Value > 3)
                                    {
                                        lock (examples)
                                        {
                                            if (!examples.TryGetValue(key, out var list))
                                            {
                                                examples[key] = list = new List<string>();
                                            }

                                            if (list.Count < 6)
                                            {
                                                var names = new[] { "S", "E", "N", "W" };
                                                list.Add($"me {names[context.MyPosition.Index()]}, first {names[context.FirstToPlayInTheRound.Index()]}, hand {string.Join(" ", context.MyCards)}; bids {string.Join(" ", context.Bids.Select(x => $"{names[x.Player.Index()]}:{x.Type}"))}; chose {bid}; teacher: {string.Join(" ", values.Select(x => $"{x.Bid}={x.Value:0.0}"))}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            action = player.Decide(view);
                        }

                        if (match.Act(seat, action) != BelotActResult.Ok)
                        {
                            throw new InvalidOperationException($"Illegal action by {player.GetType().Name}.");
                        }
                    }
                }

                lock (totals)
                {
                    foreach (var entry in local)
                    {
                        if (!totals.TryGetValue(entry.Key, out var stat))
                        {
                            totals[entry.Key] = stat = new Stat();
                        }

                        stat.Merge(entry.Value);
                    }
                }

                var count = Interlocked.Increment(ref done);
                if (count % Math.Max(1, settings.Pairs / 10) == 0)
                {
                    Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {count}/{settings.Pairs} pairs");
                }
            });

            var report = new StringBuilder();
            report.AppendLine($"regret: {settings.Player} (graded by {teacherName}) vs {settings.Opponent}, {settings.Pairs} mirrored pairs, seed {settings.Seed}, {clock.Elapsed}");
            Write(report, "cards by situation and rule", totals.Where(x => x.Key.StartsWith("card ", StringComparison.Ordinal)), 60);
            Write(report, "cards by rule", Group(totals, "card ", key => key.Split(' ')[^1]), 40);
            Write(report, "cards by contract, side and seat", Group(totals, "card ", key => string.Join(' ', key.Split(' ').Skip(1).Take(3))), 40);
            Write(report, "bids", totals.Where(x => x.Key.StartsWith("bid ", StringComparison.Ordinal)), 50);
            report.AppendLine();
            report.AppendLine("examples of the largest bid regrets:");
            foreach (var entry in totals.Where(x => x.Key.StartsWith("bid ", StringComparison.Ordinal)).OrderByDescending(x => x.Value.Sum).Take(6))
            {
                if (examples.TryGetValue(entry.Key, out var list))
                {
                    report.AppendLine($"  {entry.Key}:");
                    foreach (var example in list)
                    {
                        report.AppendLine($"    {example}");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("examples of the largest card regrets:");
            foreach (var entry in totals.Where(x => x.Key.StartsWith("card ", StringComparison.Ordinal)).OrderByDescending(x => x.Value.Sum).Take(12))
            {
                if (examples.TryGetValue(entry.Key, out var list))
                {
                    report.AppendLine($"  {entry.Key}:");
                    foreach (var example in list)
                    {
                        report.AppendLine($"    {example}");
                    }
                }
            }

            Write(report, "bids by choice", Group(totals, "bid ", key => key.Split(' ')[^2]), 20);
            Console.WriteLine(report.ToString());
            if (!string.IsNullOrEmpty(settings.Data))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
                File.WriteAllText(settings.Data + ".regret.txt", report.ToString());
            }
        }

        private static void Add(Dictionary<string, Stat> stats, string key, double regret)
        {
            if (!stats.TryGetValue(key, out var stat))
            {
                stats[key] = stat = new Stat();
            }

            stat.Add(regret);
        }

        private static string CardKey(PlayerPlayCardContext context, string rule)
        {
            var type = context.CurrentContract.Type;
            var kind = type.HasFlag(BidType.AllTrumps) ? "AT" : type.HasFlag(BidType.NoTrumps) ? "NT" : "suit";
            var side = context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition) ? "ours" : "theirs";
            var seat = context.CurrentTrickActions.Count + 1;
            var tricks = context.CurrentTrickNumber <= 3 ? "t1-3" : context.CurrentTrickNumber <= 6 ? "t4-6" : "t7-8";
            return $"card {kind} {side} seat{seat} {tricks} {rule}";
        }

        private static string Describe(PlayerPlayCardContext context, Engine.Cards.Card played, IReadOnlyList<CardValue> values)
        {
            var names = new[] { "S", "E", "N", "W" };
            var trick = string.Join(" ", context.CurrentTrickActions.Select(x => $"{names[x.Player.Index()]}:{x.Card}"));
            var history = string.Join(" | ", context.RoundActions.Where(x => x.TrickNumber < context.CurrentTrickNumber)
                .GroupBy(x => x.TrickNumber).Select(g => string.Join(" ", g.Select(x => $"{names[x.Player.Index()]}:{x.Card}"))));
            var bids = string.Join(" ", context.Bids.Select(x => $"{names[x.Player.Index()]}:{Short(x.Type)}"));
            var options = string.Join(" ", values.Select(x => $"{x.Card}={x.Value:0.0}"));
            return $"{context.CurrentContract.Type} by {names[context.CurrentContract.Player.Index()]}, me {names[context.MyPosition.Index()]}, trick {context.CurrentTrickNumber}, hand {string.Join(" ", context.MyCards)}; bids {bids}; played: {history}; now: [{trick}]; chose {played}; teacher: {options}";
        }

        private static string BidKey(PlayerGetBidContext context, BidType bid, BidType best)
        {
            var contract = context.CurrentContract.Type & ~(BidType.Double | BidType.ReDouble);
            var state = contract == BidType.Pass ? "open" : context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition) ? "partner's" : "theirs";
            return $"bid {state} {Short(bid)} best:{Short(best)}";
        }

        private static string Short(BidType bid) =>
            bid == BidType.AllTrumps ? "AT" : bid == BidType.NoTrumps ? "NT" : bid == BidType.Pass ? "pass" :
            bid == BidType.Double ? "double" : bid == BidType.ReDouble ? "redouble" : "suit";

        private static IEnumerable<KeyValuePair<string, Stat>> Group(Dictionary<string, Stat> totals, string prefix, Func<string, string> by)
        {
            var groups = new Dictionary<string, Stat>();
            foreach (var entry in totals.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                var key = prefix + by(entry.Key);
                if (!groups.TryGetValue(key, out var stat))
                {
                    groups[key] = stat = new Stat();
                }

                stat.Merge(entry.Value);
            }

            return groups;
        }

        private static void Write(StringBuilder report, string title, IEnumerable<KeyValuePair<string, Stat>> stats, int top)
        {
            var list = stats.OrderByDescending(x => x.Value.Sum).ToList();
            var all = list.Sum(x => x.Value.Sum);
            var count = list.Sum(x => x.Value.Count);
            report.AppendLine();
            report.AppendLine($"{title}: {count} decisions, total regret {all:0} game points ({(count > 0 ? all / count : 0):0.000} each)");
            foreach (var entry in list.Take(top))
            {
                report.AppendLine($"  {entry.Value.Sum,9:0.0} {entry.Value.Sum / Math.Max(1, all),6:P1}  n={entry.Value.Count,7}  mean {entry.Value.Sum / entry.Value.Count,6:0.000}  >1pt {entry.Value.Big / (double)entry.Value.Count,6:P1}  {entry.Key}");
            }
        }

        private sealed class Stat
        {
            public int Count { get; private set; }

            public double Sum { get; private set; }

            public int Big { get; private set; }

            public void Add(double regret)
            {
                this.Count++;
                this.Sum += regret;
                if (regret > 1)
                {
                    this.Big++;
                }
            }

            public void Merge(Stat other)
            {
                this.Count += other.Count;
                this.Sum += other.Sum;
                this.Big += other.Big;
            }
        }
    }
}
