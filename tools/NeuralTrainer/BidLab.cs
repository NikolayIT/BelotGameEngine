namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Belot.AI.ClaudePlayer.Heuristic;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// How the bids do once the deal is over: first deals of matches (0:0) are played by the
    /// subject team (settings.Player, its partner settings.LabPartner when given) against
    /// settings.Opponent, both ways round (the same cards, the teams swapped). At every bid of
    /// a subject seat the deal is also replayed from the start with each other natural bid it
    /// could have made there, everybody deciding as usual afterwards, and played to the end: the
    /// game points each bid would have brought, on the same cards. One CSV row per decision
    /// (settings.Data + ".bids.csv"), values in game points for the subject's team (its points
    /// from the deal minus the opponents'; hanging points count nothing).
    /// </summary>
    internal static class BidLab
    {
        // The action slots of a row: pass, the four suits, no trumps, all trumps, double, redouble.
        private static readonly BidType[] Slots =
        {
            BidType.Pass, BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
            BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble,
        };

        public static void Run(TrainingSettings settings)
        {
            var models = string.IsNullOrEmpty(settings.In) ? NeuralModels.Embedded : NeuralModels.Load(settings.In);
            var subject = OpponentCatalog.Factory(settings.Player, models);
            var partner = string.IsNullOrEmpty(settings.LabPartner) ? subject : OpponentCatalog.Factory(settings.LabPartner, models);
            var opponent = OpponentCatalog.Factory(settings.Opponent, models);
            var branchPartner = string.IsNullOrEmpty(settings.LabPartner);
            Console.WriteLine($"bidlab: {settings.Player} (partner {(branchPartner ? "the same" : settings.LabPartner)}) vs {settings.Opponent}, {settings.Pairs} deals both ways, seed {settings.Seed}, threads {settings.Threads}");
            var path = settings.Data + ".bids.csv";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine("deal,leg,seat,first,hand,bids,avail,chosen,s0,s1,s2,s3,snt,sat,v_pass,v_c,v_d,v_h,v_s,v_nt,v_at,v_x,v_xx,contract,declarer,made,ours,theirs");
            var gate = new object();
            var clock = Stopwatch.StartNew();
            var done = 0;
            var decisions = 0L;
            var replays = 0L;
            Parallel.For(0, settings.Pairs, new ParallelOptions { MaxDegreeOfParallelism = settings.Threads }, deal =>
            {
                var dealSeed = unchecked((settings.Seed * 100_000) + deal);
                var first = (PlayerPosition)(1 << (deal % 4));
                var text = new StringBuilder();
                var localReplays = 0;
                var localDecisions = 0;
                for (var leg = 0; leg < 2; leg++)
                {
                    // Leg 0: the subject at South, its partner North; leg 1: East and West.
                    var factories = new Func<int, IPlayer>[4];
                    var branched = new bool[4];
                    var me = leg == 0 ? 0 : 1;
                    factories[me] = subject;
                    factories[me + 2] = partner;
                    factories[me ^ 1] = opponent;
                    factories[(me + 2) ^ 1] = opponent;
                    branched[me] = true;
                    branched[me + 2] = branchPartner;
                    var seeds = new int[4];
                    for (var seat = 0; seat < 4; seat++)
                    {
                        seeds[seat] = unchecked((dealSeed * 8) + (seat * 2) + leg);
                    }

                    var table = new Table(dealSeed, first, factories, seeds);
                    var points = new List<Decision>();
                    var outcome = table.Play(null, BidType.Pass, branched, points);
                    foreach (var decision in points)
                    {
                        var values = new double[Slots.Length];
                        Array.Fill(values, double.NaN);
                        var team = decision.Seat & 1;
                        for (var slot = 0; slot < Slots.Length; slot++)
                        {
                            var bid = Slots[slot];
                            if (!decision.Context.AvailableBids.HasFlag(bid) || (bid == BidType.Pass && slot != 0) || !Natural(decision.Hand, bid))
                            {
                                continue;
                            }

                            if (bid == decision.Chosen)
                            {
                                values[slot] = outcome.Value(team);
                                continue;
                            }

                            var replay = new Table(dealSeed, first, factories, seeds).Play(decision.Prefix, bid, null, null);
                            values[slot] = replay.Value(team);
                            localReplays++;
                        }

                        localDecisions++;
                        Write(text, dealSeed, leg, decision, values, outcome);
                    }
                }

                lock (gate)
                {
                    writer.Write(text.ToString());
                }

                Interlocked.Add(ref replays, localReplays);
                Interlocked.Add(ref decisions, localDecisions);
                var count = Interlocked.Increment(ref done);
                if (count % Math.Max(1, settings.Pairs / 20) == 0)
                {
                    Console.WriteLine($"{clock.Elapsed:hh\\:mm\\:ss} {count}/{settings.Pairs} deals, {Interlocked.Read(ref decisions)} decisions, {Interlocked.Read(ref replays)} replays");
                }
            });

            Console.WriteLine($"done: {decisions} decisions, {replays} replays in {clock.Elapsed}; {path}");
        }

        /// <summary>
        /// The numbers of <see cref="BidFeatures"/> for every bid a row of settings.Data + ".bids.csv"
        /// evaluated (pass and redouble apart), with the bid's gain over passing, to settings.Data +
        /// ".feat": a count, then per bid the row, the kind, the situation, the action slot, the cell
        /// (<see cref="BidFeatures.Cell"/>), the gain (float) and <see cref="BidFeatures.Count"/> floats.
        /// </summary>
        public static void Featurize(TrainingSettings settings)
        {
            var clock = Stopwatch.StartNew();
            var path = settings.Data + ".bids.csv";
            using var output = new BinaryWriter(File.Create(settings.Data + ".feat"));
            output.Write(0);
            var features = new float[BidFeatures.Count];
            var count = 0;
            var row = -1;
            foreach (var line in File.ReadLines(path))
            {
                if (row++ < 0)
                {
                    continue;
                }

                var cells = line.Split(',');
                var seat = int.Parse(cells[2], CultureInfo.InvariantCulture);
                var first = int.Parse(cells[3], CultureInfo.InvariantCulture);
                var situation = new BidSituation
                {
                    Hand = uint.Parse(cells[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    Position = (seat - first + 4) & 3,
                    Holder = -1,
                    Available = (BidType)int.Parse(cells[6], CultureInfo.InvariantCulture),
                };
                var bids = cells[5];
                for (var i = 0; i + 1 < bids.Length; i += 2)
                {
                    situation.Add(bids[i] - '0', Bid(bids[i + 1]));
                }

                var kind = BidFeatures.Class(situation);
                var pass = double.Parse(cells[14], CultureInfo.InvariantCulture);
                for (var slot = 1; slot < 8; slot++)
                {
                    var cell = cells[14 + slot];
                    if (cell.Length == 0)
                    {
                        continue;
                    }

                    int family;
                    if (slot <= 4)
                    {
                        family = BidFeatures.Suit;
                        BidFeatures.ForSuit(situation, slot - 1, features);
                    }
                    else if (slot == 5)
                    {
                        family = BidFeatures.NoTrumps;
                        BidFeatures.ForNoTrumps(situation, features);
                    }
                    else if (slot == 6)
                    {
                        family = BidFeatures.AllTrumps;
                        BidFeatures.ForAllTrumps(situation, features);
                    }
                    else
                    {
                        family = BidFeatures.Double;
                        BidFeatures.ForDouble(situation, features);
                    }

                    output.Write(row);
                    output.Write((byte)family);
                    output.Write((byte)kind);
                    output.Write((byte)slot);
                    output.Write((byte)BidFeatures.Cell(family, situation, features));
                    output.Write((float)(double.Parse(cell, CultureInfo.InvariantCulture) - pass));
                    foreach (var value in features)
                    {
                        output.Write(value);
                    }

                    count++;
                }
            }

            output.Seek(0, SeekOrigin.Begin);
            output.Write(count);
            Console.WriteLine($"{count} bids of {row} decisions in {clock.Elapsed}");
        }

        /// <summary>Whether a person reads the bid as what it is: a suit with its jack or nine and another card, no trumps with an ace, all trumps with a jack.</summary>
        internal static bool Natural(uint hand, BidType bid)
        {
            switch (bid)
            {
                case BidType.Clubs:
                case BidType.Diamonds:
                case BidType.Hearts:
                case BidType.Spades:
                    var suit = (int)bid.ToCardSuit();
                    var cards = (hand >> (suit * 8)) & 0xFFu;
                    return System.Numerics.BitOperations.PopCount(cards) >= 2 && (cards & ((1u << 2) | (1u << 4))) != 0;
                case BidType.NoTrumps:
                    return (hand & 0x80808080u) != 0;
                case BidType.AllTrumps:
                    return (hand & 0x10101010u) != 0;
                default:
                    return true;
            }
        }

        private static void Write(StringBuilder text, int dealSeed, int leg, Decision decision, double[] values, Outcome outcome)
        {
            var context = decision.Context;
            var me = decision.Seat;
            var bids = new StringBuilder();
            foreach (var bid in context.Bids)
            {
                bids.Append((char)('0' + ((bid.Player.Index() - me + 4) & 3)));
                bids.Append(Code(bid.Type));
            }

            var hand = decision.Hand;
            var culture = CultureInfo.InvariantCulture;
            text.Append(culture, $"{dealSeed},{leg},{me},{context.FirstToPlayInTheRound.Index()},{hand:X8},{bids},{(int)context.AvailableBids},{Code(decision.Chosen)}");
            for (var suit = 0; suit < 4; suit++)
            {
                var strength = HeuristicBidding.SuitStrength(hand, suit);
                text.Append(',');
                if (!double.IsNegativeInfinity(strength))
                {
                    text.Append(strength.ToString("0.##", culture));
                }
            }

            text.Append(',').Append(HeuristicBidding.NoTrumpsStrength(hand).ToString("0.##", culture));
            text.Append(',').Append(HeuristicBidding.AllTrumpsStrength(hand).ToString("0.##", culture));
            foreach (var value in values)
            {
                text.Append(',');
                if (!double.IsNaN(value))
                {
                    text.Append(value.ToString("0", culture));
                }
            }

            var declarer = outcome.Contract == BidType.Pass ? -1 : (outcome.Declarer - me + 4) & 3;
            text.Append(culture, $",{(int)outcome.Contract},{declarer},{outcome.Made},{outcome.Points[me & 1]},{outcome.Points[(me & 1) ^ 1]}");
            text.Append('\n');
        }

        private static BidType Bid(char code) => code switch
        {
            'P' => BidType.Pass,
            'C' => BidType.Clubs,
            'D' => BidType.Diamonds,
            'H' => BidType.Hearts,
            'S' => BidType.Spades,
            'N' => BidType.NoTrumps,
            'A' => BidType.AllTrumps,
            'X' => BidType.Double,
            'R' => BidType.ReDouble,
            _ => throw new FormatException($"Unknown bid code {code}."),
        };

        private static char Code(BidType bid) => bid switch
        {
            BidType.Pass => 'P',
            BidType.Clubs => 'C',
            BidType.Diamonds => 'D',
            BidType.Hearts => 'H',
            BidType.Spades => 'S',
            BidType.NoTrumps => 'N',
            BidType.AllTrumps => 'A',
            BidType.Double => 'X',
            BidType.ReDouble => 'R',
            _ => '?',
        };

        private sealed class Decision
        {
            public int Seat { get; set; }

            public uint Hand { get; set; }

            public PlayerGetBidContext Context { get; set; }

            public BidType Chosen { get; set; }

            /// <summary>Gets or sets the bids asked before this one (the seat and the bid), to replay the auction up to here.</summary>
            public List<(PlayerPosition Seat, BidType Bid)> Prefix { get; set; }
        }

        private sealed class Outcome
        {
            /// <summary>Gets the game points of South-North [0] and East-West [1] from the deal.</summary>
            public int[] Points { get; } = new int[2];

            public BidType Contract { get; set; }

            public int Declarer { get; set; }

            /// <summary>Gets or sets 1 when the declarers made it, 0 when they went inside, 2 when it hangs, -1 when all passed.</summary>
            public int Made { get; set; } = -1;

            public double Value(int team) => this.Points[team] - this.Points[team ^ 1];
        }

        // One deal, the first of a match: fresh players of the same seeds every time.
        private sealed class Table : IPlayer
        {
            private readonly int dealSeed;
            private readonly PlayerPosition first;
            private readonly IPlayer[] players = new IPlayer[4];
            private RoundResult result;

            public Table(int dealSeed, PlayerPosition first, Func<int, IPlayer>[] factories, int[] seeds)
            {
                this.dealSeed = dealSeed;
                this.first = first;
                for (var seat = 0; seat < 4; seat++)
                {
                    this.players[seat] = factories[seat](seeds[seat]);
                }
            }

            /// <summary>
            /// Plays the deal: the bids of <paramref name="prefix"/> as they were, then
            /// <paramref name="branch"/> by the seat to bid (when there is a prefix), then the
            /// players. The bids of <paramref name="branched"/> seats are collected in <paramref name="decisions"/>.
            /// </summary>
            public Outcome Play(List<(PlayerPosition Seat, BidType Bid)> prefix, BidType branch, bool[] branched, List<Decision> decisions)
            {
                var observers = new IPlayer[4];
                for (var seat = 0; seat < 4; seat++)
                {
                    observers[seat] = seat == 0 ? new Observer(this.players[0], this) : this.players[seat];
                }

                var match = new BelotMatch(observers[0], observers[1], observers[2], observers[3], new BelotMatchOptions
                {
                    FirstToPlay = this.first,
                    Random = new Random(this.dealSeed),
                    RecordHistory = false,
                });
                match.Start();
                var asked = new List<(PlayerPosition Seat, BidType Bid)>();
                if (prefix != null)
                {
                    foreach (var (seat, bid) in prefix)
                    {
                        Act(match, seat, BelotAction.Bid(bid));
                        asked.Add((seat, bid));
                    }

                    Act(match, match.ToMove, BelotAction.Bid(branch));
                }

                while (match.RoundsPlayed == 0)
                {
                    var seat = match.ToMove;
                    var player = this.players[seat.Index()];
                    switch (match.Decision)
                    {
                        case BelotDecision.Bid:
                            var context = match.CreateBidContext();
                            var bid = player.GetBid(context);
                            if (branched != null && branched[seat.Index()])
                            {
                                decisions.Add(new Decision
                                {
                                    Seat = seat.Index(),
                                    Hand = CardMemory.ToMask(context.MyCards),
                                    Context = context,
                                    Chosen = bid,
                                    Prefix = new List<(PlayerPosition Seat, BidType Bid)>(asked),
                                });
                            }

                            asked.Add((seat, bid));
                            Act(match, seat, BelotAction.Bid(bid));
                            break;
                        case BelotDecision.Announce:
                            Act(match, seat, BelotAction.Declare(player.GetAnnounces(match.CreateAnnouncesContext())));
                            break;
                        case BelotDecision.PlayCard:
                            var card = player.PlayCard(match.CreatePlayCardContext());
                            Act(match, seat, BelotAction.PlayCard(card.Card, card.Belote));
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected decision {match.Decision}.");
                    }
                }

                var outcome = new Outcome();
                var result = this.result ?? throw new InvalidOperationException("The deal ended without a result.");
                outcome.Points[0] = result.SouthNorthPoints;
                outcome.Points[1] = result.EastWestPoints;
                outcome.Contract = result.Contract.Type;
                if (result.Contract.Type != BidType.Pass)
                {
                    outcome.Declarer = result.Contract.Player.Index();
                    var declarers = outcome.Declarer & 1;
                    var ours = declarers == 0 ? result.SouthNorthTotalInRoundPoints : result.EastWestTotalInRoundPoints;
                    var theirs = declarers == 0 ? result.EastWestTotalInRoundPoints : result.SouthNorthTotalInRoundPoints;
                    outcome.Made = ours > theirs ? 1 : ours < theirs ? 0 : 2;
                }

                return outcome;
            }

            public BidType GetBid(PlayerGetBidContext context) => throw new NotSupportedException();

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => throw new NotSupportedException();

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => throw new NotSupportedException();

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
            }

            public void EndOfRound(RoundResult roundResult) => this.result ??= roundResult;

            public void EndOfGame(GameResult gameResult)
            {
            }

            private static void Act(BelotMatch match, PlayerPosition seat, BelotAction action)
            {
                var result = match.Act(seat, action);
                if (result != BelotActResult.Ok)
                {
                    throw new InvalidOperationException($"{seat} could not {action.Type} ({action.BidType}): {result}.");
                }
            }
        }

        // South's player with the deal's result taken on the way.
        private sealed class Observer : IPlayer
        {
            private readonly IPlayer inner;
            private readonly Table table;

            public Observer(IPlayer inner, Table table)
            {
                this.inner = inner;
                this.table = table;
            }

            public BidType GetBid(PlayerGetBidContext context) => this.inner.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.inner.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.inner.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.inner.EndOfTrick(trickActions);

            public void EndOfRound(RoundResult roundResult)
            {
                this.table.EndOfRound(roundResult);
                this.inner.EndOfRound(roundResult);
            }

            public void EndOfGame(GameResult gameResult) => this.inner.EndOfGame(gameResult);
        }
    }
}
