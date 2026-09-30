namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays whole matches and flags card choices a strong human would call mistakes, then checks
    /// each with hindsight: the double-dummy result (raw card points) of the rest of the deal after
    /// the card played and after the natural alternative. Team A (settings.Player) plays South-North
    /// in the first game of a pair and East-West in the second; team B is settings.Opponent.
    /// </summary>
    internal static class Audit
    {
        private const int Categories = 6;

        private static readonly string[] CategoryNames =
        {
            "same-suit donation (3rd/4th seat, trick lost, a lower card of the suit loses as well)",
            "discard donation (void, trick lost, a cheaper plain card elsewhere)",
            "master thrown into a lost trick",
            "overtook a partner who surely holds the trick",
            "gave nothing to a partner's sure trick (4th seat, a plain card worth 10+ was free)",
            "let a 10+ point trick go (4th seat, could win it with a card worth less than the trick)",
        };

        [ThreadStatic]
        private static Func<string> explain;

        public static void Run(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? AI.ClaudePlayer.Neural.NeuralModels.Embedded : AI.ClaudePlayer.Neural.NeuralModels.Load(settings.In);
            var teamA = settings.Player == "candidate" ? seed => OpponentCatalog.Configured(settings, models, seed) : OpponentCatalog.Factory(settings.Player, models);
            var opponentModels = string.IsNullOrEmpty(settings.OpponentIn) ? models : AI.ClaudePlayer.Neural.NeuralModels.Load(settings.OpponentIn);
            var teamB = OpponentCatalog.Factory(settings.Opponent, opponentModels);
            var stats = new[] { new TeamStats(settings.Player), new TeamStats(settings.Opponent) };
            var shown = new List<string>();
            var examples = new List<string>[2] { new List<string>(), new List<string>() };
            var clock = Stopwatch.StartNew();
            var done = 0;
            Parallel.For(0, settings.Pairs, new ParallelOptions { MaxDegreeOfParallelism = settings.Threads }, pair =>
            {
                var seed = unchecked((settings.Seed * 100_000) + pair);
                for (var leg = 0; leg < 2; leg++)
                {
                    var a = new[] { teamA(seed * 2), teamA((seed * 2) + 1) };
                    var b = new[] { teamB(seed * 2), teamB((seed * 2) + 1) };

                    // Seats South, East, North, West; team A is South-North in leg 0.
                    var players = leg == 0 ? new[] { a[0], b[0], a[1], b[1] } : new[] { b[0], a[0], b[1], a[1] };
                    var match = new BelotMatch(new BelotMatchOptions { FirstToPlay = (PlayerPosition)(1 << (pair % 4)), Random = new Random(seed) });
                    match.Start();
                    var contexts = new Dictionary<(int Round, int Card), PlayerPlayCardContext>();
                    while (!match.IsFinished)
                    {
                        var seat = match.ToMove;
                        var view = match.GetView(seat);
                        BelotAction action;
                        if (view.Decision == BelotDecision.PlayCard)
                        {
                            var context = view.CreatePlayCardContext();
                            contexts[(context.RoundNumber, context.RoundActions.Count())] = view.CreatePlayCardContext();
                            var card = players[seat.Index()].PlayCard(context);
                            action = BelotAction.PlayCard(card.Card, card.Belote);
                        }
                        else
                        {
                            action = players[seat.Index()].Decide(view);
                        }

                        if (match.Act(seat, action) != BelotActResult.Ok)
                        {
                            throw new InvalidOperationException($"Illegal action by {players[seat.Index()].GetType().Name}.");
                        }
                    }

                    var record = match.GetRecord();
                    if (settings.Deals > 0)
                    {
                        lock (shown)
                        {
                            foreach (var round in record.Rounds)
                            {
                                if (shown.Count < settings.Deals && round.Contract != null && round.Contract.Type != BidType.Pass)
                                {
                                    shown.Add(Show(round, leg, settings.Player, settings.Opponent));
                                }
                            }
                        }
                    }

                    var local = new[] { new TeamStats(null), new TeamStats(null) };
                    var localExamples = new[] { new List<string>(), new List<string>() };
                    var explainers = new Func<PlayerPlayCardContext, string>[] { Explainer(a[0]), Explainer(b[0]) };
                    foreach (var round in record.Rounds)
                    {
                        AnalyseRound(round, leg, local, localExamples, contexts, explainers);
                        CountBids(round, leg, local);
                    }

                    lock (stats)
                    {
                        for (var team = 0; team < 2; team++)
                        {
                            stats[team].Add(local[team]);
                            if (examples[team].Count < 60)
                            {
                                examples[team].AddRange(localExamples[team].Take(60 - examples[team].Count));
                            }
                        }
                    }
                }

                var count = Interlocked.Increment(ref done);
                if (count % Math.Max(1, settings.Pairs / 10) == 0)
                {
                    Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {count}/{settings.Pairs} pairs");
                }
            });

            var report = new StringBuilder();
            report.AppendLine($"audit: {settings.Player} (A) vs {settings.Opponent} (B), {settings.Pairs} mirrored pairs, seed {settings.Seed}, {clock.Elapsed}");
            for (var team = 0; team < 2; team++)
            {
                stats[team].Write(report, team == 0 ? "A" : "B");
            }

            for (var team = 0; team < 2; team++)
            {
                report.AppendLine();
                report.AppendLine($"Examples, team {(team == 0 ? "A" : "B")} ({stats[team].Name}):");
                foreach (var example in examples[team])
                {
                    report.AppendLine(example);
                }
            }

            foreach (var deal in shown)
            {
                report.AppendLine();
                report.Append(deal);
            }

            Console.WriteLine(report.ToString());
            if (!string.IsNullOrEmpty(settings.Data))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.Data)));
                File.WriteAllText(settings.Data + ".audit.txt", report.ToString());
            }
        }

        // A finished deal in full, as a person would read it.
        private static string Show(BelotRoundRecord round, int leg, string a, string b)
        {
            var names = new[] { "S", "E", "N", "W" };
            string Label(int seat) => ((seat & 1) == 0) == (leg == 0) ? $"A:{a}" : $"B:{b}";
            var text = new StringBuilder();
            var simulator = new BelotSimulator();
            simulator.SetContract(round.Contract.Type, round.Contract.Player.Index(), 0);
            var kind = simulator.Kind;
            text.AppendLine($"Deal {round.RoundNumber}: {round.Contract.Type} by {names[round.Contract.Player.Index()]} ({Label(round.Contract.Player.Index())}), first {names[round.FirstToPlay.Index()]}");
            var hands = new uint[4];
            for (var i = 0; i < 32; i++)
            {
                hands[i & 3] |= 1u << round.Deal[i].GetHashCode();
            }

            for (var seat = 0; seat < 4; seat++)
            {
                text.AppendLine($"  {names[seat]} {Label(seat),-14} {SortedCards(hands[seat], kind)}");
            }

            text.AppendLine($"  auction: {string.Join(", ", round.Bids.Select(x => $"{names[x.Player.Index()]} {x.Type}"))}");
            if (round.Announces != null && round.Announces.Count > 0)
            {
                text.AppendLine($"  declared: {string.Join(", ", round.Announces.Select(x => $"{names[x.Player.Index()]} {x.Type}"))}");
            }

            var number = 0;
            foreach (var trick in round.Tricks)
            {
                number++;
                var points = trick.Cards.Sum(x => simulator.Value(x.Card.GetHashCode()));
                text.AppendLine($"  {number}. {string.Join("  ", trick.Cards.Select(x => $"{names[x.Player.Index()]} {x.Card}{(x.Belote ? "(belote)" : string.Empty)}"))}  -> {names[trick.Winner.Index()]} ({points})");
            }

            if (round.Result != null)
            {
                text.AppendLine($"  result: S-N {round.Result.SouthNorthPoints} ({round.Result.SouthNorthTotalInRoundPoints}), E-W {round.Result.EastWestPoints} ({round.Result.EastWestTotalInRoundPoints}), hanging {round.Result.HangingPoints}");
            }

            return text.ToString();
        }

        private static string SortedCards(uint mask, int kind)
        {
            var parts = new List<string>();
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = new List<int>();
                for (var rest = mask & SimTables.SuitMasks[suit]; rest != 0; rest &= rest - 1)
                {
                    cards.Add(BitOperations.TrailingZeroCount(rest));
                }

                cards.Sort((x, y) => AI.ClaudePlayer.Human.HumanPreference.Order(kind, y).CompareTo(AI.ClaudePlayer.Human.HumanPreference.Order(kind, x)));
                if (cards.Count > 0)
                {
                    parts.Add(string.Join(" ", cards.Select(x => Card.AllCards[x].ToString())));
                }
            }

            return string.Join("  |  ", parts);
        }

        // The values a neural player gives the legal cards (a fresh player of the same settings would sample anew).
        private static Func<PlayerPlayCardContext, string> Explainer(IPlayer player)
        {
            if (player is not AI.ClaudePlayer.ClaudePlayerNeural neural)
            {
                return null;
            }

            return context =>
            {
                try
                {
                    return string.Join(", ", neural.EvaluateCards(context).Select(x => $"{x.Card} {x.Value:0.00}"));
                }
                catch (ArgumentException)
                {
                    return "(no values)";
                }
            };
        }

        private static void AnalyseRound(BelotRoundRecord round, int leg, TeamStats[] stats, List<string>[] examples, Dictionary<(int Round, int Card), PlayerPlayCardContext> contexts, Func<PlayerPlayCardContext, string>[] explainers)
        {
            if (round.Contract == null || round.Contract.Type == BidType.Pass || round.Tricks == null || round.Tricks.Count != 8)
            {
                return;
            }

            var simulator = new BelotSimulator();
            var declarer = round.Contract.Player.Index();
            simulator.SetContract(round.Contract.Type, declarer, 0);
            var state = default(SimState);
            for (var i = 0; i < 32; i++)
            {
                state.Hands[i & 3] |= 1u << round.Deal[i].GetHashCode();
            }

            state.Turn = round.FirstToPlay.Index();
            var played = 0u;
            var index = 0;
            var discardSuits = new int[4];
            var solver = new RawSolver(simulator);
            for (var trick = 0; trick < 8; trick++)
            {
                foreach (var playedCard in round.Tricks[trick].Cards)
                {
                    var seat = playedCard.Player.Index();
                    var card = playedCard.Card.GetHashCode();
                    if (seat != state.Turn)
                    {
                        throw new InvalidOperationException("The record does not replay.");
                    }

                    var legal = simulator.LegalMoves(in state);
                    if ((legal & (1u << card)) == 0)
                    {
                        throw new InvalidOperationException("An illegal card in the record.");
                    }

                    if ((legal & (legal - 1)) != 0)
                    {
                        // Team A is South-North (seat & 1 == 0) in leg 0.
                        var team = ((seat & 1) == 0) == (leg == 0) ? 0 : 1;
                        contexts.TryGetValue((round.RoundNumber, index), out var context);
                        explain = context == null || explainers[team] == null ? null : () => explainers[team](context);
                        Inspect(in state, simulator, solver, legal, card, played, trick, (seat & 1) == (declarer & 1), stats[team], examples[team]);
                        Conventions(in state, simulator, legal, card, played, discardSuits[(seat + 2) & 3], stats[team]);
                    }

                    if (state.TrickCards > 0 && (card >> 3) != state.LedSuit && !(simulator.Kind < SimTables.NoTrumps && (card >> 3) == simulator.Kind)
                        && ((state.WinnerSeat ^ seat) & 1) != 0)
                    {
                        discardSuits[seat] |= 1 << (card >> 3);
                    }

                    simulator.Play(ref state, card, legal);
                    played |= 1u << card;
                    index++;
                }
            }
        }

        // The conventions a partner can read: a free discard from a suit with no honour in it, and
        // leading a suit the partner has thrown away.
        private static void Conventions(in SimState state, BelotSimulator simulator, uint legal, int card, uint played, int partnerWeak, TeamStats stats)
        {
            var kind = simulator.Kind;
            var trumps = kind < SimTables.NoTrumps ? SimTables.SuitMasks[kind] : 0u;
            if (state.TrickCards == 0)
            {
                var avoidable = false;
                for (var suit = 0; suit < 4; suit++)
                {
                    avoidable |= (partnerWeak & (1 << suit)) == 0 && (legal & SimTables.SuitMasks[suit]) != 0;
                }

                // Leading one's own master of the suit is no appeal to the partner.
                var outside = ~(state.Hands[state.Turn] | played);
                var master = (HumanPreference.HigherInSuit(kind, card) & outside) == 0;
                if (partnerWeak != 0 && avoidable)
                {
                    stats.SignalLeads++;
                    if ((partnerWeak & (1 << (card >> 3))) != 0 && !master)
                    {
                        stats.SignalLeadsIgnored++;
                    }
                }

                return;
            }

            var hand = state.Hands[state.Turn];
            if ((card >> 3) == state.LedSuit || (trumps & (1u << card)) != 0 || ((state.WinnerSeat ^ state.Turn) & 1) == 0)
            {
                return;
            }

            // A free discard with a choice of suits.
            if (PlaySignals.SuitsOf(legal & ~trumps) is var suits && (suits & (suits - 1)) == 0)
            {
                return;
            }

            uint Honours(int suit) => kind == SimTables.AllTrumps || suit == kind
                ? (1u << ((suit * 8) + 4)) | (1u << ((suit * 8) + 2))
                : (1u << ((suit * 8) + 7)) | (1u << ((suit * 8) + 3));

            // Only count discards where some legal suit had no honour at all.
            var possible = false;
            for (var suit = 0; suit < 4; suit++)
            {
                possible |= (legal & ~trumps & SimTables.SuitMasks[suit]) != 0 && (hand & Honours(suit)) == 0;
            }

            if (!possible)
            {
                return;
            }

            stats.FreeDiscards++;
            if ((hand & Honours(card >> 3)) == 0)
            {
                stats.ConventionalDiscards++;
            }
        }

        private static void Inspect(in SimState state, BelotSimulator simulator, RawSolver solver, uint legal, int card, uint played, int trick, bool ours, TeamStats stats, List<string> examples)
        {
            var seat = state.Turn;
            var phase = trick < 3 ? 0 : 1;
            stats.Decisions[phase]++;
            if (state.TrickCards == 0)
            {
                return;
            }

            var kind = simulator.Kind;
            var row = ((kind * 4) + state.LedSuit) * 32;
            var beats = SimTables.BeatMasks[row + state.WinnerCard];
            var partnerHolds = ((state.WinnerSeat ^ seat) & 1) == 0;
            var position = state.TrickCards;
            var lost = !partnerHolds && position >= 2;
            var losing = legal & ~beats;
            var bit = 1u << card;
            var hand = state.Hands[seat];
            var ledMask = SimTables.SuitMasks[state.LedSuit];
            var trumpMask = kind < SimTables.NoTrumps ? SimTables.SuitMasks[kind] : 0u;
            var value = simulator.Value(card);

            // 0: the same suit holds a lower card that loses as well.
            if (lost && (losing & bit) != 0)
            {
                var suit = SimTables.SuitMasks[card >> 3];
                var cheaper = -1;
                for (var rest = losing & suit & ~bit; rest != 0; rest &= rest - 1)
                {
                    var other = BitOperations.TrailingZeroCount(rest);
                    if (simulator.Value(other) < value && (cheaper < 0 || simulator.Value(other) < simulator.Value(cheaper)))
                    {
                        cheaper = other;
                    }
                }

                if (cheaper >= 0)
                {
                    Flag(0, in state, simulator, solver, card, cheaper, played, phase, trick, ours, stats, examples);
                }
                else if ((ledMask & bit) == 0 && (trumpMask & bit) == 0)
                {
                    // 1: a void discard worth more than the cheapest plain card elsewhere.
                    var cheapest = -1;
                    for (var rest = losing & ~trumpMask & ~bit; rest != 0; rest &= rest - 1)
                    {
                        var other = BitOperations.TrailingZeroCount(rest);
                        if (simulator.Value(other) < value && (cheapest < 0 || simulator.Value(other) < simulator.Value(cheapest)))
                        {
                            cheapest = other;
                        }
                    }

                    if (cheapest >= 0)
                    {
                        Flag(1, in state, simulator, solver, card, cheapest, played, phase, trick, ours, stats, examples);
                    }
                }

                // 2: the card was the best left of its suit (not a trump that could still matter).
                var higherLeft = SimTables.BeatMasks[(((kind * 4) + (card >> 3)) * 32) + card] & SimTables.SuitMasks[card >> 3] & ~played & ~hand;
                if (higherLeft == 0 && value >= 10 && (trumpMask & bit) == 0)
                {
                    var alternative = LowestPlain(simulator, losing & ~bit, trumpMask);
                    if (alternative >= 0)
                    {
                        Flag(2, in state, simulator, solver, card, alternative, played, phase, trick, ours, stats, examples);
                    }
                }
            }

            // 3: the partner surely holds the trick and the card beats it needlessly.
            if (partnerHolds && position == 3 && (beats & bit) != 0 && (losing & ~bit) != 0)
            {
                var alternative = LowestPlain(simulator, losing, trumpMask);
                if (alternative < 0)
                {
                    alternative = BitOperations.TrailingZeroCount(losing);
                }

                Flag(3, in state, simulator, solver, card, alternative, played, phase, trick, ours, stats, examples);
            }

            // 4: the partner surely holds the trick (last seat), a plain 10 or ace was free, a cheap card went.
            if (partnerHolds && position == 3 && (beats & bit) == 0 && value < 10)
            {
                var rich = -1;
                for (var rest = losing & ~trumpMask & ~bit; rest != 0; rest &= rest - 1)
                {
                    var other = BitOperations.TrailingZeroCount(rest);
                    if (simulator.Value(other) >= 10 && (rich < 0 || simulator.Value(other) > simulator.Value(rich)))
                    {
                        rich = other;
                    }
                }

                if (rich >= 0)
                {
                    Flag(4, in state, simulator, solver, card, rich, played, phase, trick, ours, stats, examples);
                }
            }

            // 5: the last seat lets a rich trick go although a cheaper card would take it.
            if (!partnerHolds && position == 3 && (beats & bit) == 0 && state.TrickPoints >= 10)
            {
                var taker = -1;
                for (var rest = legal & beats; rest != 0; rest &= rest - 1)
                {
                    var other = BitOperations.TrailingZeroCount(rest);
                    if (simulator.Value(other) < state.TrickPoints && (taker < 0 || simulator.Value(other) < simulator.Value(taker)))
                    {
                        taker = other;
                    }
                }

                if (taker >= 0)
                {
                    Flag(5, in state, simulator, solver, card, taker, played, phase, trick, ours, stats, examples);
                }
            }
        }

        // The auction's statistics: contracts by kind and how they ended, doubles and redoubles.
        private static void CountBids(BelotRoundRecord round, int leg, TeamStats[] stats)
        {
            int TeamOf(int seat) => ((seat & 1) == 0) == (leg == 0) ? 0 : 1;
            if (round.Contract == null || round.Result == null)
            {
                return;
            }

            stats[0].Deals++;
            stats[1].Deals++;
            if (round.Contract.Type == BidType.Pass)
            {
                return;
            }

            // The hands bid on (the first five cards): how thin a person would call them.
            for (var i = 0; i < round.Bids.Count; i++)
            {
                var bid = round.Bids[i];
                var bidType = bid.Type & ~(BidType.Double | BidType.ReDouble);
                if (bid.Type == BidType.Pass || bid.Type == BidType.Double || bid.Type == BidType.ReDouble || bidType == BidType.Pass)
                {
                    continue;
                }

                var bidder = bid.Player.Index();
                var five = 0u;
                for (var card = bidder; card < 20; card += 4)
                {
                    five |= 1u << round.Deal[card].GetHashCode();
                }

                var bidKind = SimTables.ToKind(bidType);
                var thin = bidKind == SimTables.AllTrumps ? (five & 0x10101010u) == 0
                    : bidKind == SimTables.NoTrumps ? (five & 0x80808080u) == 0
                    : (five & ((1u << ((bidKind * 8) + 4)) | (1u << ((bidKind * 8) + 2)))) == 0;
                var group = bidKind < SimTables.NoTrumps ? 0 : bidKind == SimTables.NoTrumps ? 1 : 2;
                stats[TeamOf(bidder)].Bids[group]++;
                if (thin)
                {
                    stats[TeamOf(bidder)].ThinBids[group]++;
                    var final = round.Contract.Type & ~(BidType.Double | BidType.ReDouble);
                    var won = (round.Contract.Player.Index() & 1) == 0 ? round.Result.SouthNorthPoints > 0 && round.Result.EastWestPoints < round.Result.SouthNorthPoints : round.Result.EastWestPoints > 0 && round.Result.SouthNorthPoints < round.Result.EastWestPoints;
                    if (stats[TeamOf(bidder)].ThinExamples.Count < 25)
                    {
                        stats[TeamOf(bidder)].ThinExamples.Add(
                            $"  {bidType} on {Cards(five)} after [{string.Join(", ", round.Bids.Take(i).Select(x => $"{x.Player}:{x.Type}"))}] "
                            + $"-> final {round.Contract.Type} by {round.Contract.Player}{(final == bidType && round.Contract.Player.Index() == bidder ? " (its own)" : string.Empty)}, declarers {(won ? "made it" : "failed")}");
                    }
                }
            }

            var declarer = round.Contract.Player.Index();
            var team = TeamOf(declarer);
            var kind = SimTables.ToKind(round.Contract.Type);
            var column = kind < SimTables.NoTrumps ? 0 : kind == SimTables.NoTrumps ? 1 : 2;
            var ours = (declarer & 1) == 0 ? round.Result.SouthNorthPoints : round.Result.EastWestPoints;
            var theirs = (declarer & 1) == 0 ? round.Result.EastWestPoints : round.Result.SouthNorthPoints;
            stats[team].Contracts[column]++;
            if (ours == 0 && theirs > 0)
            {
                stats[team].Inside[column]++;
            }

            stats[team].ContractPoints[column] += ours - theirs;
            foreach (var bid in round.Bids)
            {
                var bidder = TeamOf(bid.Player.Index());
                if (bid.Type == BidType.Double)
                {
                    stats[bidder].Doubles++;
                    var doubledTeamPoints = (bid.Player.Index() & 1) == 0 ? round.Result.SouthNorthPoints : round.Result.EastWestPoints;
                    if (doubledTeamPoints > 0)
                    {
                        stats[bidder].DoublesWon++;
                    }
                }
                else if (bid.Type == BidType.ReDouble)
                {
                    stats[bidder].Redoubles++;
                }
            }
        }

        private static int LowestPlain(BelotSimulator simulator, uint cards, uint trumpMask)
        {
            var best = -1;
            for (var rest = cards & ~trumpMask; rest != 0; rest &= rest - 1)
            {
                var other = BitOperations.TrailingZeroCount(rest);
                if (best < 0 || simulator.Value(other) < simulator.Value(best))
                {
                    best = other;
                }
            }

            return best;
        }

        private static void Flag(int category, in SimState state, BelotSimulator simulator, RawSolver solver, int card, int alternative, uint played, int phase, int trick, bool ours, TeamStats stats, List<string> examples)
        {
            var legal = simulator.LegalMoves(in state);
            var team = state.Turn & 1;
            var withCard = state;
            simulator.Play(ref withCard, card, legal);
            var withAlternative = state;
            simulator.Play(ref withAlternative, alternative, legal);
            solver.Team = team;
            var cost = int.MinValue;
            if (trick >= 1)
            {
                var played1 = Gain(in state, in withCard, team) + solver.Solve(in withCard, -1000, 1000);
                var played2 = Gain(in state, in withAlternative, team) + solver.Solve(in withAlternative, -1000, 1000);
                cost = played2 - played1;
            }

            var cell = stats.Cells[category, phase];
            cell.Count++;
            cell.Points += simulator.Value(card) - simulator.Value(alternative);
            if (cost != int.MinValue)
            {
                cell.Solved++;
                cell.Cost += cost;
                if (cost > 0)
                {
                    cell.Worse++;
                }
                else if (cost < 0)
                {
                    cell.Better++;
                }
            }

            if (examples.Count < 60 && (category <= 2 || examples.Count < 40))
            {
                var text = new StringBuilder();
                text.Append($"[{category}] trick {trick + 1}, {SimTables.ToBidType(simulator.Kind)} ({(ours ? "ours" : "theirs")}), seat {state.Turn}: hand {Cards(state.Hands[state.Turn])}; trick so far");
                text.Append($" {state.TrickCards} cards, holding {Card.AllCards[state.WinnerCard]} ({((state.WinnerSeat ^ state.Turn) & 1) switch { 0 => "partner", _ => "opponent" }}); played {Card.AllCards[card]}, natural {Card.AllCards[alternative]}");
                text.Append(cost == int.MinValue ? string.Empty : $"; hindsight cost {cost:+0;-0;0} raw points");
                if (explain != null && category <= 1)
                {
                    text.Append($"; values now: {explain()}");
                }

                examples.Add(text.ToString());
            }

            _ = played;
        }

        private static int Gain(in SimState before, in SimState after, int team)
        {
            var gain = (after.SouthNorthPoints - before.SouthNorthPoints) - (after.EastWestPoints - before.EastWestPoints);
            if (after.TricksPlayed == 8)
            {
                gain += after.LastTrickTeam == 0 ? 10 : -10;
            }

            return team == 0 ? gain : -gain;
        }

        private static string Cards(uint mask)
        {
            var parts = new List<string>();
            for (var rest = mask; rest != 0; rest &= rest - 1)
            {
                parts.Add(Card.AllCards[BitOperations.TrailingZeroCount(rest)].ToString());
            }

            return string.Join(" ", parts);
        }

        /// <summary>Double-dummy solver of the rest of a deal in raw points (tricks, belotes, last 10), with a table at trick boundaries.</summary>
        private sealed class RawSolver
        {
            private readonly BelotSimulator simulator;
            private readonly Dictionary<(ulong, ulong, int), (int Lower, int Upper)> table = new Dictionary<(ulong, ulong, int), (int, int)>();

            public RawSolver(BelotSimulator simulator)
            {
                this.simulator = simulator;
            }

            public int Team { get; set; }

            /// <summary>The team's future raw points minus the other team's from the position.</summary>
            public int Solve(in SimState state, int alpha, int beta)
            {
                if (state.TricksPlayed == 8)
                {
                    return 0;
                }

                (ulong, ulong, int) key = default;
                var boundary = state.TrickCards == 0;
                if (boundary)
                {
                    key = (state.Hands[0] | ((ulong)state.Hands[1] << 32), state.Hands[2] | ((ulong)state.Hands[3] << 32), state.Turn | (this.Team << 2));
                    if (this.table.TryGetValue(key, out var entry))
                    {
                        if (entry.Lower == entry.Upper || entry.Lower >= beta)
                        {
                            return entry.Lower;
                        }

                        if (entry.Upper <= alpha)
                        {
                            return entry.Upper;
                        }

                        alpha = Math.Max(alpha, entry.Lower);
                        beta = Math.Min(beta, entry.Upper);
                    }
                }

                var originalAlpha = alpha;
                var originalBeta = beta;
                var maximizing = (state.Turn & 1) == this.Team;
                var best = maximizing ? int.MinValue : int.MaxValue;
                var legal = this.simulator.LegalMoves(in state);
                var first = this.simulator.ChooseRolloutMove(in state, legal);
                var remaining = legal;
                while (remaining != 0)
                {
                    var move = first >= 0 ? first : BitOperations.TrailingZeroCount(remaining);
                    first = -1;
                    remaining &= ~(1u << move);
                    var copy = state;
                    this.simulator.Play(ref copy, move, legal);
                    var gain = Gain(in state, in copy, this.Team);
                    var value = gain + this.Solve(in copy, alpha - gain, beta - gain);
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

                if (boundary)
                {
                    (int Lower, int Upper) entry = this.table.TryGetValue(key, out var old) ? old : (int.MinValue, int.MaxValue);
                    if (best <= originalAlpha)
                    {
                        entry.Upper = Math.Min(entry.Upper, best);
                    }
                    else if (best >= originalBeta)
                    {
                        entry.Lower = Math.Max(entry.Lower, best);
                    }
                    else
                    {
                        entry = (best, best);
                    }

                    this.table[key] = entry;
                }

                return best;
            }
        }

        private sealed class Cell
        {
            public long Count { get; set; }

            public long Points { get; set; }

            public long Solved { get; set; }

            public long Cost { get; set; }

            public long Worse { get; set; }

            public long Better { get; set; }
        }

        private sealed class TeamStats
        {
            public TeamStats(string name)
            {
                this.Name = name;
                for (var i = 0; i < Categories; i++)
                {
                    for (var phase = 0; phase < 2; phase++)
                    {
                        this.Cells[i, phase] = new Cell();
                    }
                }
            }

            public string Name { get; }

            public long[] Decisions { get; } = new long[2];

            public long Deals { get; set; }

            public long[] Contracts { get; } = new long[3];

            public long[] Bids { get; } = new long[3];

            public long[] ThinBids { get; } = new long[3];

            public List<string> ThinExamples { get; } = new List<string>();

            public long[] Inside { get; } = new long[3];

            public long[] ContractPoints { get; } = new long[3];

            public long Doubles { get; set; }

            public long DoublesWon { get; set; }

            public long Redoubles { get; set; }

            public long FreeDiscards { get; set; }

            public long ConventionalDiscards { get; set; }

            public long SignalLeads { get; set; }

            public long SignalLeadsIgnored { get; set; }

            public Cell[,] Cells { get; } = new Cell[Categories, 2];

            public void Add(TeamStats other)
            {
                for (var phase = 0; phase < 2; phase++)
                {
                    this.Decisions[phase] += other.Decisions[phase];
                    if (phase == 0)
                    {
                        this.Deals += other.Deals;
                        this.Doubles += other.Doubles;
                        this.DoublesWon += other.DoublesWon;
                        this.Redoubles += other.Redoubles;
                        this.FreeDiscards += other.FreeDiscards;
                        this.ConventionalDiscards += other.ConventionalDiscards;
                        this.SignalLeads += other.SignalLeads;
                        this.SignalLeadsIgnored += other.SignalLeadsIgnored;
                        if (this.ThinExamples.Count < 25)
                        {
                            this.ThinExamples.AddRange(other.ThinExamples.Take(25 - this.ThinExamples.Count));
                        }

                        for (var column = 0; column < 3; column++)
                        {
                            this.Contracts[column] += other.Contracts[column];
                            this.Bids[column] += other.Bids[column];
                            this.ThinBids[column] += other.ThinBids[column];
                            this.Inside[column] += other.Inside[column];
                            this.ContractPoints[column] += other.ContractPoints[column];
                        }
                    }

                    for (var i = 0; i < Categories; i++)
                    {
                        var cell = this.Cells[i, phase];
                        var o = other.Cells[i, phase];
                        cell.Count += o.Count;
                        cell.Points += o.Points;
                        cell.Solved += o.Solved;
                        cell.Cost += o.Cost;
                        cell.Worse += o.Worse;
                        cell.Better += o.Better;
                    }
                }
            }

            public void Write(StringBuilder report, string label)
            {
                report.AppendLine();
                var names = new[] { "suit", "no trumps", "all trumps" };
                report.AppendLine($"Team {label} auction over {this.Deals} deals: "
                    + string.Join(", ", Enumerable.Range(0, 3).Select(c => $"{names[c]} {this.Contracts[c]} ({(this.Contracts[c] == 0 ? 0 : 100.0 * this.Inside[c] / this.Contracts[c]):0.0}% inside, {(this.Contracts[c] == 0 ? 0 : (double)this.ContractPoints[c] / this.Contracts[c]):+0.0;-0.0} points)"))
                    + $"; doubles {this.Doubles} ({(this.Doubles == 0 ? 0 : 100.0 * this.DoublesWon / this.Doubles):0.0}% won), redoubles {this.Redoubles}");
                report.AppendLine($"Team {label} thin bids (first five cards): suit without its jack or nine {this.ThinBids[0]}/{this.Bids[0]}, "
                    + $"no trumps without an ace {this.ThinBids[1]}/{this.Bids[1]}, all trumps without a jack {this.ThinBids[2]}/{this.Bids[2]}");
                foreach (var example in this.ThinExamples)
                {
                    report.AppendLine(example);
                }

                report.AppendLine($"Team {label} conventions: {this.FreeDiscards} free discards that could follow the convention, {(this.FreeDiscards == 0 ? 0 : 100.0 * this.ConventionalDiscards / this.FreeDiscards):0.0}% from a suit with no honour; "
                    + $"{this.SignalLeads} leads with a partner's thrown suit avoidable, {(this.SignalLeads == 0 ? 0 : 100.0 * this.SignalLeadsIgnored / this.SignalLeads):0.0}% led a non-master of it anyway");
                report.AppendLine($"Team {label}: {this.Name}; decisions with a choice: tricks 1-3 {this.Decisions[0]}, tricks 4-8 {this.Decisions[1]}");
                for (var i = 0; i < Categories; i++)
                {
                    report.AppendLine($"  {CategoryNames[i]}");
                    for (var phase = 0; phase < 2; phase++)
                    {
                        var cell = this.Cells[i, phase];
                        var rate = this.Decisions[phase] == 0 ? 0 : 1000.0 * cell.Count / this.Decisions[phase];
                        var cost = cell.Solved == 0 ? double.NaN : (double)cell.Cost / cell.Solved;
                        report.AppendLine(
                            $"    {(phase == 0 ? "tricks 1-3" : "tricks 4-8")}: {cell.Count} ({rate:0.0} per 1000 decisions), card points {(cell.Count == 0 ? 0 : (double)cell.Points / cell.Count):+0.0;-0.0}, "
                            + $"hindsight: {cell.Solved} solved, mean cost {cost:+0.00;-0.00} raw points, worse {cell.Worse}, better {cell.Better}");
                    }
                }
            }
        }
    }
}
