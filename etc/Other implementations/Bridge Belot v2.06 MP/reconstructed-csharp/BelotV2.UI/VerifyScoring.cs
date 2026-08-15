namespace BelotV2
{
    using System.Text.Json;

    /// <summary>
    /// Replays rounds the ORIGINAL scored (tools/score_probe.py) against <see cref="Scoring"/>.
    ///
    /// Scoring was the last part written from the canonical rules rather than recovered, which
    /// made it the one place where the port could be a perfectly reasonable Belot implementation
    /// and still not be *this* Belot implementation. It no longer is: four of its rules turned
    /// out to differ from the textbook ones.
    ///
    /// The vectors carry the cards each seat took, the contract, who declared it, its doubling
    /// level, who took the last trick, any hanging points, and the two numbers the routine added
    /// to the match board. Sides are seats 1+3 (team 0 here) against 2+4.
    /// </summary>
    public static class VerifyScoring
    {
        public static int Run(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"vector file not found: {path}");
                return 2;
            }

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("scoring", out JsonElement cases))
            {
                Console.Error.WriteLine("no 'scoring' section");
                return 2;
            }

            int n = 0, bad = 0;
            var byKind = new Dictionary<string, (int total, int bad)>();
            foreach (JsonElement c in cases.EnumerateArray())
            {
                BidType type = ParseContract(c.GetProperty("contract").GetString()!);
                int level = c.GetProperty("level").GetInt32();
                var contract = new Contract(type, c.GetProperty("declarer").GetInt32() - 1,
                                            level >= 2, level == 4);

                var team0 = new List<Card>();
                var team1 = new List<Card>();
                JsonElement taken = c.GetProperty("taken");
                for (int seat = 1; seat <= 4; seat++)
                {
                    var into = (seat % 2 == 1) ? team0 : team1;
                    foreach (JsonElement e in taken.GetProperty(seat.ToString()).EnumerateArray())
                    {
                        into.Add(new Card(SuitOf(e[0].GetInt32()), (Rank)e[1].GetInt32()));
                    }
                }

                int[] want = c.GetProperty("board").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                RoundResult got = Scoring.Score(
                    contract, team0, team1, Announces.Resolve(DeclarationsOf(c)),
                    c.GetProperty("hanging").GetInt32(),
                    c.GetProperty("lastTrick").GetInt32() - 1);

                n++;
                bool ok = got.Team0Board == want[0] && got.Team1Board == want[1];
                string kind = level > 1 ? "doubled"
                    : team0.Count == 0 || team1.Count == 0 ? "capot"
                    : Bids.CategoryOf(type).ToString();
                var agg = byKind.GetValueOrDefault(kind);
                byKind[kind] = (agg.total + 1, agg.bad + (ok ? 0 : 1));
                if (!ok)
                {
                    if (bad < 8)
                    {
                        Console.WriteLine($"  scoring DIFF {type} declarer={contract.Declarer + 1} " +
                                          $"level={level} cards={team0.Count}/{team1.Count} " +
                                          $"hanging={c.GetProperty("hanging").GetInt32()} " +
                                          $"raw=[{got.Team0Raw},{got.Team1Raw}] mine=[{got.Team0Board},{got.Team1Board}] " +
                                          $"orig=[{want[0]},{want[1]}]");
                    }

                    bad++;
                }
            }

            Console.WriteLine($"scoring    : {n - bad}/{n} match ({100.0 * (n - bad) / Math.Max(n, 1):F1}%)");
            foreach (var g in byKind.OrderByDescending(k => k.Value.bad))
            {
                Console.WriteLine($"    {g.Key,-14} {g.Value.total - g.Value.bad}/{g.Value.total} " +
                                  $"({100.0 * (g.Value.total - g.Value.bad) / g.Value.total:F0}%)");
            }

            return bad;
        }

        /// <summary>
        /// Rebuild each seat's declarations from the descriptor the original recorded:
        /// [careta1, careta2, terca1, terca2, quarte1, quarte2, quinte], each a top rank or 0.
        /// Running these through Announces.Resolve is what puts the resolution rule itself — only
        /// the side holding the single strongest declaration scores, an exact tie cancels both —
        /// under test alongside the arithmetic.
        /// </summary>
        private static List<Announce> DeclarationsOf(JsonElement c)
        {
            var all = new List<Announce>();
            if (!c.TryGetProperty("decl", out JsonElement decl))
            {
                return all;
            }

            // Sequences and fours of a kind do not count in no-trumps, so the game never has any
            // to resolve there however the descriptors happen to read.
            if (ParseContract(c.GetProperty("contract").GetString()!) == BidType.NoTrumps)
            {
                return all;
            }

            for (int seat = 1; seat <= 4; seat++)
            {
                int[] rec = decl.GetProperty(seat.ToString()).EnumerateArray()
                                .Select(x => x.GetInt32()).ToArray();
                void Add(int slot, AnnounceKind kind, int value)
                {
                    if (rec[slot] == 0)
                    {
                        return;
                    }

                    var rank = (Rank)rec[slot];
                    int v = kind == AnnounceKind.Careta
                        ? rank switch { Rank.Jack => 200, Rank.Nine => 150, _ => 100 }
                        : value;
                    all.Add(new Announce(kind, seat - 1, rank, v));
                }

                Add(0, AnnounceKind.Careta, 0);
                Add(1, AnnounceKind.Careta, 0);
                Add(2, AnnounceKind.Terca, 20);
                Add(3, AnnounceKind.Terca, 20);
                Add(4, AnnounceKind.Quarte, 50);
                Add(5, AnnounceKind.Quarte, 50);
                Add(6, AnnounceKind.Quinte, 100);
            }

            return all;
        }

        private static Suit SuitOf(int s) => s switch
        {
            0 => Suit.Clubs,
            1 => Suit.Diamonds,
            2 => Suit.Spades,
            _ => Suit.Hearts,
        };

        private static BidType ParseContract(string s) => s switch
        {
            "Clubs" => BidType.Clubs,
            "Diamonds" => BidType.Diamonds,
            "Hearts" => BidType.Hearts,
            "Spades" => BidType.Spades,
            "NoTrumps" => BidType.NoTrumps,
            _ => BidType.AllTrumps,
        };
    }
}
