namespace BelotArena
{
    using System.Diagnostics;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using V2 = BelotV2;

    /// <summary>
    /// SmartPlayer against the 2001 game's AI, inside the modern engine.
    ///
    /// The modern engine hosts the match, which is the fair way round: it owns the rules, it
    /// validates every bid and every card, and SmartPlayer plays with the contexts it was written
    /// for. The 2001 AI is wrapped (see OriginalEnginePlayer) rather than reimplemented, so what
    /// is measured is the original's judgement, not a translation of it.
    ///
    ///   arena &lt;south-north&gt; &lt;east-west&gt; [games] [seed]
    ///
    /// where each side is one of: smart, dummy, random, original (the C# transcription) or
    /// originalemu (the real x86 routine under emulation, ~0.5s per decision).
    ///
    /// Always run the controls before believing a result: `smart smart` must land near 50% or
    /// the harness itself is biased.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: arena <smart|dummy|random|original|originalemu> <same> [games] [seed]");
                return 2;
            }

            int games = args.Length > 2 ? int.Parse(args[2]) : 100;
            uint seed = args.Length > 3 ? uint.Parse(args[3]) : 1;

            var toDispose = new List<IDisposable>();
            try
            {
                IPlayer south = Make(args[0], seed, toDispose);
                IPlayer north = Make(args[0], seed + 10, toDispose);
                IPlayer east = Make(args[1], seed + 20, toDispose);
                IPlayer west = Make(args[1], seed + 30, toDispose);
                return Play(args[0], args[1], games, south, east, north, west);
            }
            finally
            {
                foreach (IDisposable d in toDispose)
                {
                    d.Dispose();
                }
            }
        }

        private static IPlayer Make(string kind, uint seed, List<IDisposable> toDispose)
        {
            switch (kind)
            {
                case "smart":
                    return new SmartPlayer();
                case "dummy":
                    return new DummyPlayer();
                case "random":
                    return new RandomPlayer();
                case "original":
                    return new OriginalEnginePlayer(
                        "original", ctx => V2.OriginalPlayAdapter.Play(ctx, new V2.DelphiRandom(seed)), seed);
                case "originalemu":
                {
                    // One emulator for both seats: it keeps no per-seat state beyond the declarer,
                    // and a second one would only pay the warm-up again.
                    if (Server == null)
                    {
                        string path = Path.GetFullPath("../tools/ai_server.py");
                        if (!File.Exists(path))
                        {
                            throw new FileNotFoundException($"ai_server.py not found at {path}");
                        }

                        Console.WriteLine($"  starting emulator: {path}");
                        Server = new V2.OriginalAiPlayer("original", path, "python");
                        toDispose.Add(Server);
                    }

                    V2.OriginalAiPlayer server = Server;
                    var rng = new V2.DelphiRandom(seed);
                    return new OriginalEnginePlayer("originalemu", ctx =>
                    {
                        // Ask the transcription the same question before handing it to the real
                        // routine. The emulator's answer is the one played, so this is genuinely
                        // the original x86 code driving the match; the transcription's answer is
                        // free and worth having, because these positions come from SmartPlayer's
                        // play and so are exactly the ones the golden vectors could not cover --
                        // those were recorded with the original playing against itself.
                        server.Declarer = ctx.Contract.Declarer + 1;
                        V2.Card real;
                        try
                        {
                            real = server.PlayCard(ctx);
                        }
                        catch (InvalidOperationException ex)
                        {
                            V2.Card transcribed = V2.OriginalPlayAdapter.Play(ctx, rng);
                            // The server fabricates a fresh Delphi object graph for every call
                            // rather than keeping a round in progress, and some positions ask it
                            // for something that graph does not hold. Record the position, play
                            // the transcription's card, and carry on -- silently substituting
                            // would make the run look cleaner than it is.
                            EmulatorFailures++;
                            LogFailure(ctx, ex.Message);
                            return transcribed;
                        }

                        // Same question, same starting RandSeed: now a disagreement means the
                        // transcription is wrong, not that the two flipped different coins.
                        rng.Seed = server.LastSeed;
                        V2.Card mine = V2.OriginalPlayAdapter.Play(ctx, rng);
                        EmulatedDecisions++;
                        if (mine.Equals(real))
                        {
                            Agreements++;
                        }
                        else
                        {
                            Console.WriteLine($"    DISAGREE seat {ctx.Seat + 1} contract {ctx.Contract.Type}: " +
                                              $"transcription {mine}, binary {real}");
                        }

                        return real;
                    });
                }

                // Hybrids, to tell bidding apart from card play: "smartbid" bids like SmartPlayer
                // and plays like the original, "origbid" the other way round.
                case "smartbid":
                    return new MixedPlayer(new SmartPlayer(), Make("original", seed, toDispose));
                case "origbid":
                    return new MixedPlayer(Make("original", seed, toDispose), new SmartPlayer());
                default:
                    throw new ArgumentException($"unknown player '{kind}'");
            }
        }

        private static int EmulatedDecisions { get; set; }

        private static int Agreements { get; set; }

        private static int EmulatorFailures { get; set; }

        private static V2.OriginalAiPlayer? Server { get; set; }

        /// <summary>Writes the first few unsupported positions out so they can be replayed.</summary>
        private static void LogFailure(V2.PlayContext ctx, string message)
        {
            if (EmulatorFailures > 5)
            {
                return;
            }

            string cards = string.Join(" ", ctx.Hand);
            string trick = string.Join(" ", ctx.CurrentTrick.Select(p => $"{p.Seat + 1}:{p.Card}"));
            Console.WriteLine($"    EMULATOR FAILED seat {ctx.Seat + 1} contract {ctx.Contract.Type} " +
                              $"declarer {ctx.Contract.Declarer + 1} trick[{ctx.CurrentTrick.Count}] {trick}");
            Console.WriteLine($"      hand: {cards}   played {ctx.PlayedHistory.Count}   {message}");
        }

        private static int Play(
            string northSouthKind, string eastWestKind, int games, IPlayer south, IPlayer east, IPlayer north, IPlayer west)
        {
            // Tallies the round outcomes, which is the only way to see WHY a side is winning:
            // who is taking the contracts, in what, and how many rounds nobody bids at all.
            var stats = new StatsObserver();
            var southObserved = new ObservingPlayer(south, stats);

            Console.WriteLine($"{northSouthKind} (S+N)  vs  {eastWestKind} (E+W)   -   {games} games");
            var wins = new int[2];
            var points = new long[2];
            long rounds = 0;
            var sw = Stopwatch.StartNew();
            int progressEvery = Math.Max(1, games / 10);

            for (var i = 0; i < games; i++)
            {
                var game = new BelotGame(southObserved, east, north, west);

                // Alternate who opens, so neither side keeps the first-to-play advantage.
                GameResult result = game.PlayGame((i & 1) == 0 ? PlayerPosition.South : PlayerPosition.East);

                points[0] += result.SouthNorthPoints;
                points[1] += result.EastWestPoints;
                rounds += result.RoundsPlayed;
                wins[result.Winner == PlayerPosition.SouthNorthTeam ? 0 : 1]++;

                if ((i + 1) % progressEvery == 0)
                {
                    Console.WriteLine($"  {i + 1,5}/{games}   {wins[0],5} - {wins[1],-5}   ({sw.Elapsed.TotalSeconds,6:F1}s)");
                }
            }

            sw.Stop();
            double pct = (double)wins[0] / games;
            Console.WriteLine();
            Console.WriteLine($"  {northSouthKind,-12} {wins[0],6} wins   {points[0],9} points   (S+N)");
            Console.WriteLine($"  {eastWestKind,-12} {wins[1],6} wins   {points[1],9} points   (E+W)");
            Console.WriteLine($"  win rate for {northSouthKind}: {pct:P1}    {rounds} rounds ({(double)rounds / games:F1}/game), {sw.Elapsed.TotalSeconds:F1}s");
            if (pct > 0 && pct < 1)
            {
                Console.WriteLine($"  ELO difference: {-400.0 * Math.Log10((1.0 / pct) - 1.0):+0;-0} for {northSouthKind}");
            }

            stats.Print(rounds);
            if (EmulatedDecisions > 0)
            {
                Console.WriteLine($"  decisions taken by the real x86 routine: {EmulatedDecisions}");
                Console.WriteLine($"  transcription agreed on {Agreements}/{EmulatedDecisions} " +
                                  $"({(double)Agreements / EmulatedDecisions:P2}) of them");
                Console.WriteLine($"  positions the emulator could not serve: {EmulatorFailures} " +
                                  $"({(double)EmulatorFailures / (EmulatedDecisions + EmulatorFailures):P1}) " +
                                  "- played from the transcription instead");
            }

            int rejected = (east as OriginalEnginePlayer)?.RejectedBids ?? 0;
            rejected += (west as OriginalEnginePlayer)?.RejectedBids ?? 0;
            rejected += (south as OriginalEnginePlayer)?.RejectedBids ?? 0;
            rejected += (north as OriginalEnginePlayer)?.RejectedBids ?? 0;
            if (rejected > 0)
            {
                Console.WriteLine($"  note: {rejected} bids by the original were not legal here and became passes");
            }

            return 0;
        }
    }
}
