using System.Text.Json;

namespace BelotV2
{
    /// <summary>
    /// Differential verification against golden vectors produced by EXECUTING the original
    /// belot.exe code under emulation (see tools/emu.py + tools/gen_vectors.py).
    ///
    /// Checks three things against the real binary's behaviour:
    ///   cardtables : the strength/points caches the game builds at runtime
    ///   announces  : FUN_00479EE8 declaration detection
    ///   validcards : FUN_004767F8 legal-move predicate
    /// Exit code is non-zero if anything diverges.
    /// </summary>
    public static class Verify
    {
        public static int Run(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"golden vector file not found: {path}");
                return 2;
            }

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            int failures = 0;
            if (root.TryGetProperty("trickwinner", out JsonElement tw))
            {
                failures += CheckTrickWinner(tw);
            }

            if (root.TryGetProperty("playai", out JsonElement pa))
            {
                failures += CheckPlayAi(pa);
            }

            if (root.TryGetProperty("bidchoice", out JsonElement bc))
            {
                failures += CheckBidChoice(bc);
            }

            if (!root.TryGetProperty("cardtables", out _))
            {
                Console.WriteLine(failures == 0
                    ? "ALL GOLDEN VECTORS MATCH the original binary."
                    : $"{failures} MISMATCH(ES) against the original binary.");
                return failures == 0 ? 0 : 1;
            }

            failures += CheckCardTables(root.GetProperty("cardtables"));
            failures += CheckAnnounces(root.GetProperty("announces"));
            if (root.TryGetProperty("bidscores", out JsonElement bs))
            {
                failures += CheckBidScores(bs);
            }

            failures += CheckValidCards(root.GetProperty("validcards"));

            Console.WriteLine(failures == 0
                ? "ALL GOLDEN VECTORS MATCH the original binary."
                : $"{failures} MISMATCH(ES) against the original binary.");
            return failures == 0 ? 0 : 1;
        }

        private static Suit ParseSuit(string s) => s switch
        {
            "C" => Suit.Clubs,
            "D" => Suit.Diamonds,
            "H" => Suit.Hearts,
            "S" => Suit.Spades,
            _ => throw new ArgumentException($"bad suit {s}"),
        };

        private static BidType ParseContract(string s) => s switch
        {
            "Clubs" => BidType.Clubs,
            "Diamonds" => BidType.Diamonds,
            "Hearts" => BidType.Hearts,
            "Spades" => BidType.Spades,
            "NoTrumps" => BidType.NoTrumps,
            "AllTrumps" => BidType.AllTrumps,
            _ => throw new ArgumentException($"bad contract {s}"),
        };

        private static Card ParseCard(JsonElement e) =>
            new(ParseSuit(e[0].GetString()!), (Rank)e[1].GetInt32());

        // ---- 1. strength / point tables ----
        private static int CheckCardTables(JsonElement rows)
        {
            int bad = 0, n = 0;
            foreach (JsonElement r in rows.EnumerateArray())
            {
                var contract = new Contract(ParseContract(r.GetProperty("contract").GetString()!), 0, false, false);
                var card = new Card(ParseSuit(r.GetProperty("suit").GetString()!), (Rank)r.GetProperty("rank").GetInt32());
                int expStrength = r.GetProperty("strength").GetInt32();
                int expPoints = r.GetProperty("points").GetInt32();

                int gotStrength = contract.IsTrump(card.Suit)
                    ? Cards.TrumpStrength(card) : Cards.PlainStrength(card);
                int gotPoints = Rules.PointValue(card, contract);
                n++;
                if (gotStrength != expStrength || gotPoints != expPoints)
                {
                    if (bad < 5)
                    {
                        Console.WriteLine($"  cardtable MISMATCH {card} under {contract.Type}: " +
                                          $"strength {gotStrength} vs {expStrength}, points {gotPoints} vs {expPoints}");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"cardtables : {n - bad}/{n} match");
            return bad;
        }

        // ---- 2. announcement detection ----
        private static int CheckAnnounces(JsonElement cases)
        {
            int bad = 0, n = 0;
            var contract = new Contract(BidType.AllTrumps, 0, false, false);
            foreach (JsonElement c in cases.EnumerateArray())
            {
                var hand = c.GetProperty("hand").EnumerateArray().Select(ParseCard).ToList();
                int[] desc = c.GetProperty("desc").EnumerateArray().Select(x => x.GetInt32()).ToArray();

                var expCareta = new[] { desc[0], desc[1] }.Where(x => x != 0).OrderBy(x => x).ToList();
                var expTerca = new[] { desc[2], desc[3] }.Where(x => x != 0).OrderBy(x => x).ToList();
                var expQuarte = new[] { desc[4], desc[5] }.Where(x => x != 0).OrderBy(x => x).ToList();
                var expQuinte = new[] { desc[6] }.Where(x => x != 0).OrderBy(x => x).ToList();

                var mine = Announces.Detect(hand, 0, contract);
                var gotCareta = Tops(mine, AnnounceKind.Careta);
                var gotTerca = Tops(mine, AnnounceKind.Terca);
                var gotQuarte = Tops(mine, AnnounceKind.Quarte);
                var gotQuinte = Tops(mine, AnnounceKind.Quinte);

                n++;
                if (!Same(gotCareta, expCareta) || !Same(gotTerca, expTerca)
                    || !Same(gotQuarte, expQuarte) || !Same(gotQuinte, expQuinte))
                {
                    if (bad < 5)
                    {
                        Console.WriteLine($"  announce MISMATCH hand=[{string.Join(",", hand)}]");
                        Console.WriteLine($"    careta {Fmt(gotCareta)} vs {Fmt(expCareta)}; terca {Fmt(gotTerca)} vs {Fmt(expTerca)}; " +
                                          $"quarte {Fmt(gotQuarte)} vs {Fmt(expQuarte)}; quinte {Fmt(gotQuinte)} vs {Fmt(expQuinte)}");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"announces  : {n - bad}/{n} match");
            return bad;
        }

        private static List<int> Tops(List<Announce> anns, AnnounceKind kind)
            => anns.Where(a => a.Kind == kind).Select(a => (int)a.TopRank).OrderBy(x => x).ToList();

        private static bool Same(List<int> a, List<int> b) => a.Count == b.Count && !a.Where((t, i) => t != b[i]).Any();

        private static string Fmt(List<int> v) => "[" + string.Join(",", v) + "]";

        // ---- trick winner (FUN_004766A8) ----
        private static int CheckTrickWinner(JsonElement cases)
        {
            int bad = 0, n = 0;
            foreach (JsonElement c in cases.EnumerateArray())
            {
                var contract = new Contract(ParseContract(c.GetProperty("contract").GetString()!), 0, false, false);
                var plays = new List<Play>();
                foreach (JsonElement p in c.GetProperty("plays").EnumerateArray())
                {
                    plays.Add(new Play(p[0].GetInt32() - 1,
                        new Card(ParseSuit(p[1].GetString()!), (Rank)p[2].GetInt32())));
                }

                int expected = c.GetProperty("winner").GetInt32() - 1;
                int got = Rules.WinnerSeat(plays, contract);
                n++;
                if (got != expected)
                {
                    if (bad < 5)
                    {
                        Console.WriteLine($"  trickwinner MISMATCH {contract.Type} " +
                                          $"[{string.Join(" ", plays.Select(p => $"{p.Seat + 1}:{p.Card}"))}] " +
                                          $"mine=seat{got + 1} orig=seat{expected + 1}");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"trickwinner: {n - bad}/{n} match");
            return bad;
        }

        // ---- card-play AI (player2BeforePlay) ----
        // ---- what the original actually bids (choosegame's decision, not just its scoring) ----
        private static int CheckBidChoice(JsonElement cases)
        {
            int bad = 0, n = 0;
            var byKind = new Dictionary<string, (int total, int bad)>();
            foreach (JsonElement c in cases.EnumerateArray())
            {
                var hand = c.GetProperty("hand").EnumerateArray().Select(ParseCard).ToList();
                int seat = c.GetProperty("seat").GetInt32();
                int holder = c.GetProperty("holder").GetInt32();
                int[] bids = new int[5];
                int i = 1;
                foreach (JsonElement b in c.GetProperty("bids").EnumerateArray())
                {
                    bids[i++] = b.GetInt32();
                }

                int[] board = new int[3];
                i = 1;
                foreach (JsonElement b in c.GetProperty("board").EnumerateArray())
                {
                    board[i++] = b.GetInt32();
                }

                int expected = c.GetProperty("bid").GetInt32();
                var rng = new DelphiRandom(c.GetProperty("rngSeed").GetUInt32());
                int opener = c.TryGetProperty("opener", out JsonElement op) ? op.GetInt32() : 0;
                int got = BiddingAi.Choose(hand, seat, holder, bids, board, opener, rng);

                n++;
                string kind = bids[holder] == 0 ? "opening"
                    : (((holder ^ seat) & 1) == 0 ? "over-partner" : "over-opponents");
                var agg = byKind.GetValueOrDefault(kind);
                byKind[kind] = (agg.total + 1, agg.bad + (got == expected ? 0 : 1));
                if (got != expected)
                {
                    if (bad < 8)
                    {
                        Console.WriteLine($"  bid DIFF seat{seat} holder={holder} " +
                                          $"bids=[{string.Join(",", bids.Skip(1))}] " +
                                          $"hand=[{string.Join(",", hand)}] mine={got} orig={expected}");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"bidchoice  : {n - bad}/{n} match ({100.0 * (n - bad) / Math.Max(n, 1):F1}%)");
            foreach (var g in byKind.OrderByDescending(k => k.Value.bad))
            {
                Console.WriteLine($"    {g.Key,-22} {g.Value.total - g.Value.bad}/{g.Value.total} " +
                                  $"({100.0 * (g.Value.total - g.Value.bad) / g.Value.total:F0}%)");
            }

            return bad;
        }

        /// <summary>
        /// The suit each seat named in the auction, as the vector recorded it. The AI remembers
        /// these into the play phase, so a comparison that leaves them out is asking the port a
        /// different question from the one the binary answered.
        /// </summary>
        private static IReadOnlyList<Suit?>? BidSuitsOf(JsonElement c)
        {
            if (!c.TryGetProperty("bidSuits", out JsonElement bs))
            {
                return null;
            }

            var suits = new List<Suit?>();
            foreach (JsonElement e in bs.EnumerateArray())
            {
                suits.Add(e.GetInt32() switch
                {
                    0 => Suit.Clubs,
                    1 => Suit.Diamonds,
                    2 => Suit.Spades,
                    3 => Suit.Hearts,
                    _ => null,
                });
            }

            return suits;
        }

        private static int CheckPlayAi(JsonElement cases)
        {
            int bad = 0, n = 0;
            var rng = new DelphiRandom(1);
            var byContext = new Dictionary<string, (int total, int bad)>();
            foreach (JsonElement c in cases.EnumerateArray())
            {
                // The declarer and the doubling level are part of the position: the AI reads
                // both, so a check that defaults them is not asking the same question.
                int declarer = c.TryGetProperty("declarer", out JsonElement dc) ? dc.GetInt32() - 1 : 0;
                int level = c.TryGetProperty("level", out JsonElement lvl) ? lvl.GetInt32() : 1;
                var contract = new Contract(ParseContract(c.GetProperty("contract").GetString()!),
                                            declarer, level >= 2, level == 4);
                int seat = c.GetProperty("seat").GetInt32() - 1;
                var hand = c.GetProperty("hand").EnumerateArray().Select(ParseCard).ToList();
                var trick = new List<Play>();
                foreach (JsonElement t in c.GetProperty("trick").EnumerateArray())
                {
                    trick.Add(new Play(t[0].GetInt32() - 1,
                        new Card(ParseSuit(t[1].GetString()!), (Rank)t[2].GetInt32())));
                }

                var history = new List<Play>();
                foreach (JsonElement h in c.GetProperty("history").EnumerateArray())
                {
                    history.Add(new Play(h[0].GetInt32() - 1,
                        new Card(ParseSuit(h[1].GetString()!), (Rank)h[2].GetInt32())));
                }

                Card expected = hand[c.GetProperty("chosen").GetInt32()];
                var legal = Rules.ValidCards(hand, trick, contract, seat);

                // Drive the RNG from the seed the original had on entry, so the branches that
                // pick at random are reproducible rather than a coin flip.
                if (c.TryGetProperty("internals", out JsonElement inner)
                    && inner.TryGetProperty("rngSeed", out JsonElement seed))
                {
                    rng.Seed = seed.GetUInt32();
                }

                // Seat 1 (index 0) is the human in the game the vectors came from.
                Card got = OriginalPlayAdapter.Play(new PlayContext
                {
                    Seat = seat,
                    Hand = hand,
                    Contract = contract,
                    CurrentTrick = trick,
                    PlayedHistory = history,
                    Legal = legal,
                    HumanSeat = 0,
                    BidSuits = BidSuitsOf(c),
                }, rng);

                n++;
                string ctx = trick.Count == 0 ? "lead"
                    : (Seats.SameTeam(seat, trick[Rules.WinnerIndex(trick, contract)].Seat)
                        ? $"seat{trick.Count + 1}-partner-wins" : $"seat{trick.Count + 1}-opp-wins");
                var agg = byContext.GetValueOrDefault(ctx);
                byContext[ctx] = (agg.total + 1, agg.bad + (got.Equals(expected) ? 0 : 1));
                if (!got.Equals(expected))
                {
                    if (bad < 6)
                    {
                        Console.WriteLine($"  playai DIFF {contract.Type} seat{seat + 1} " +
                                          $"trick=[{string.Join(" ", trick.Select(p => $"{p.Seat + 1}:{p.Card}"))}] " +
                                          $"hand=[{string.Join(",", hand)}] mine={got} orig={expected}");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"playai     : {n - bad}/{n} match ({100.0 * (n - bad) / Math.Max(n, 1):F1}%)");
            foreach (var g in byContext.OrderByDescending(k => k.Value.bad))
            {
                Console.WriteLine($"    {g.Key,-22} {g.Value.total - g.Value.bad}/{g.Value.total} " +
                                  $"({100.0 * (g.Value.total - g.Value.bad) / g.Value.total:F0}%)");
            }

            return bad;
        }

        // ---- 3. bidding valuation ----
        private static int CheckBidScores(JsonElement cases)
        {
            int bad = 0, n = 0;
            foreach (JsonElement c in cases.EnumerateArray())
            {
                var hand = c.GetProperty("hand").EnumerateArray().Select(ParseCard).ToList();
                foreach (JsonProperty p in c.GetProperty("scores").EnumerateObject())
                {
                    BidType type = ParseContract(p.Name);
                    int expected = p.Value.GetInt32();
                    int got = BiddingAi.ScoreContract(type, hand);
                    n++;
                    if (got != expected)
                    {
                        if (bad < 8)
                        {
                            Console.WriteLine($"  bidscore MISMATCH [{string.Join(",", hand)}] " +
                                              $"{type}: mine={got} orig={expected}");
                        }

                        bad++;
                    }
                }
            }

            Console.WriteLine($"bidscores  : {n - bad}/{n} match");
            return bad;
        }

        // ---- 4. legal-move predicate ----
        private static int CheckValidCards(JsonElement cases)
        {
            int bad = 0, n = 0;
            foreach (JsonElement c in cases.EnumerateArray())
            {
                var contract = new Contract(ParseContract(c.GetProperty("contract").GetString()!), 0, false, false);
                int seat = c.GetProperty("seat").GetInt32() - 1;   // original players are 1..4
                var hand = c.GetProperty("hand").EnumerateArray().Select(ParseCard).ToList();
                var trick = new List<Play>();
                foreach (JsonElement t in c.GetProperty("trick").EnumerateArray())
                {
                    trick.Add(new Play(t[0].GetInt32() - 1,
                        new Card(ParseSuit(t[1].GetString()!), (Rank)t[2].GetInt32())));
                }

                var expected = c.GetProperty("legal").EnumerateArray().Select(x => x.GetInt32()).OrderBy(x => x).ToList();
                var legal = Rules.ValidCards(hand, trick, contract, seat);
                var got = legal.Select(card => hand.IndexOf(card)).OrderBy(x => x).ToList();

                n++;
                if (!Same(got, expected))
                {
                    if (bad < 8)
                    {
                        Console.WriteLine($"  validcards MISMATCH contract={contract.Type} seat={seat + 1}");
                        Console.WriteLine($"    hand  = [{string.Join(",", hand)}]");
                        Console.WriteLine($"    trick = [{string.Join(" ", trick.Select(p => $"{p.Seat + 1}:{p.Card}"))}]");
                        Console.WriteLine($"    mine  = {Fmt(got)} ({string.Join(",", got.Select(i => hand[i]))})");
                        Console.WriteLine($"    orig  = {Fmt(expected)} ({string.Join(",", expected.Select(i => hand[i]))})");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"validcards : {n - bad}/{n} match");
            return bad;
        }
    }
}
