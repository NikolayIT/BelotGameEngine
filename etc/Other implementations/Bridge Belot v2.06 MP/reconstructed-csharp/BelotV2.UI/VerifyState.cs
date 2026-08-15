using System.Text.Json;

namespace BelotV2
{
    /// <summary>
    /// Stage-by-stage validation of <see cref="OriginalPlayAi"/> against the real routine's own
    /// stack frame, captured under emulation. Comparing intermediate state (candidate list,
    /// sure/middle/loser buckets, trump analysis) localises a divergence to the exact stage that
    /// produced it, instead of only reporting a wrong final card.
    /// </summary>
    public static class VerifyState
    {
        /// <summary>Print the per-candidate lookahead trace for the first cases of a contract.</summary>
        public static void Trace(string path, string contract, int limit)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            int shown = 0;
            foreach (JsonElement c in doc.RootElement.GetProperty("playai").EnumerateArray())
            {
                if (c.GetProperty("contract").GetString() != contract || !c.TryGetProperty("internals", out _))
                {
                    continue;
                }

                OriginalPlayAi ai = Build(c);
                ai.Analyse();
                Console.WriteLine($"--- {contract} seat={c.GetProperty("seat").GetInt32()} " +
                    $"hand=[{string.Join(",", c.GetProperty("hand").EnumerateArray().Select(x => x[0].GetString() + x[1].GetInt32()))}]");
                foreach (var t in ai.Trace)
                {
                    Console.WriteLine($"    cand#{t.Cand} simSuit={"CDSH"[t.SimSuit]} voidCap={t.VoidCap} " +
                                      $"threat={(t.Threat ? 1 : 0)} wins=[{t.Wins[1]}, {t.Wins[2]}, {t.Wins[3]}, {t.Wins[4]}]");
                }

                if (++shown >= limit)
                {
                    break;
                }
            }
        }

        public static int Run(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"vector file not found: {path}");
                return 2;
            }

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("playai", out JsonElement cases))
            {
                Console.Error.WriteLine("no 'playai' section");
                return 2;
            }

            var stats = new Dictionary<string, (int total, int bad)>();
            var branchFail = new Dictionary<string, int>();
            var branchHits = new Dictionary<string, int>();
            string lastBranch = "?";
            OriginalPlayAi freshAi = null!;
            int n = 0, shown = 0, memShown = 0;
            foreach (JsonElement c in cases.EnumerateArray())
            {
                if (!c.TryGetProperty("internals", out JsonElement st))
                {
                    continue;
                }

                n++;
                OriginalPlayAi ai = Build(c);
                ai.Analyse();

                // The AI's per-round memory, as the original had it at this point. Check that the
                // port rebuilds the same block from the play history, then feed the original's
                // own copy in so a wrong byte cannot also skew the decision comparison.
                byte[]? mem = null;
                if (st.TryGetProperty("mem", out JsonElement memEl))
                {
                    mem = memEl.EnumerateArray().Select(x => (byte)x.GetInt32()).ToArray();
                    byte[] got = ReplayMemory(c);
                    int firstBad = -1;
                    for (int i = 0; i < mem.Length && firstBad < 0; i++)
                    {
                        firstBad = got[i] == mem[i] ? -1 : i;
                    }

                    Check("memory", firstBad < 0 ? 0 : PlayMemory.Base + firstBad, 0);
                    if (firstBad >= 0 && memShown++ < 5)
                    {
                        Console.WriteLine($"    memory 0x{PlayMemory.Base + firstBad:X}: " +
                                          $"mine={got[firstBad]} orig={mem[firstBad]}");
                    }

                    Array.Copy(mem, ai.Mem, mem.Length);
                }

                void Check(string name, int mine, int orig)
                {
                    var agg = stats.GetValueOrDefault(name);
                    bool ok = mine == orig;
                    stats[name] = (agg.total + 1, agg.bad + (ok ? 0 : 1));
                    if (!ok && name == "DECISION")
                    {
                        branchFail[lastBranch] = branchFail.GetValueOrDefault(lastBranch) + 1;
                    }

                    if (!ok && name == "DECISION" && shown < 12)
                    {
                        shown++;
                        string L(string tag, int[] a, int cnt) => $"{tag}[{string.Join(",", Enumerable.Range(1, cnt).Select(i => a[i]))}]";
                        Console.WriteLine($"    branch={lastBranch} ledSuit={freshAi.LedSuit} trump={freshAi.Trump48} " +
                            $"oppTrump={freshAi.AnyOppTrump} long={freshAi.LongSide} lowTrumpMixed={freshAi.LowestTrumpInMixed} " +
                            $"seed={(st.TryGetProperty("rngSeed", out JsonElement sd) ? sd.GetUInt32() : 0)} " +
                            $"draws={freshAi.RngDraws}");
                        Console.WriteLine("      " + L("sure", freshAi.SureList, freshAi.SureCount) + " " +
                            L("mid", freshAi.MiddleList, freshAi.MiddleCount) + " " +
                            L("lose", freshAi.LoserList, freshAi.LoserCount) + " " +
                            L("L1", freshAi.AlwaysWinsTrump, freshAi.AlwaysWinsTrumpCount) + " " +
                            L("L2", freshAi.AlwaysWinsPartner, freshAi.AlwaysWinsPartnerCount) + " " +
                            L("L3", freshAi.AlwaysWins, freshAi.AlwaysWinsCount) + " " +
                            L("L4mix", freshAi.Mixed, freshAi.MixedCount) + " " +
                            L("L5", freshAi.NeverWins, freshAi.NeverWinsCount));
                        Console.WriteLine($"  {name,-9} mine={mine} orig={orig}  " +
                                          $"{c.GetProperty("contract").GetString()} seat={c.GetProperty("seat").GetInt32()} " +
                                          $"played={c.GetProperty("trick").GetArrayLength()} " +
                                          $"hand=[{string.Join(",", c.GetProperty("hand").EnumerateArray().Select(x => x[0].GetString() + x[1].GetInt32()))}]");
                    }
                }

                Check("candCount", ai.CandCount, st.GetProperty("candCount").GetInt32());
                Check("playersLeft", ai.PlayersLeft, st.GetProperty("playersLeft").GetInt32());
                Check("trump", ai.Trump48, st.GetProperty("trump").GetInt32());
                Check("longSide", ai.LongSide, st.GetProperty("longSide").GetInt32());
                Check("anyOppTrump", ai.AnyOppTrump ? 1 : 0, st.GetProperty("anyOppTrump").GetInt32());
                // iStack_70 is only assigned inside the `contract < 5` arm, so in no-trumps and
                // all-trumps the original leaves the previous call's value on the stack. It is
                // never read there (that needs cStack_85, which those contracts never set), so
                // the port zeroes it and the comparison only makes sense for suit contracts.
                if (ai.Contract < 5)
                {
                    Check("trumpRanksOut", ai.TrumpRanksOut, st.GetProperty("trumpRanksOut").GetInt32());
                }
                Check("sureCount", ai.SureCount, st.GetProperty("sureCount").GetInt32());
                Check("middleCount", ai.MiddleCount, st.GetProperty("middleCount").GetInt32());
                Check("loserCount", ai.LoserCount, st.GetProperty("loserCount").GetInt32());

                int[] buckets = st.GetProperty("buckets").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                CheckList("loserList", ai.LoserList, ai.LoserCount, buckets, 0, Check);
                CheckList("middleList", ai.MiddleList, ai.MiddleCount, buckets, 8, Check);
                CheckList("sureList", ai.SureList, ai.SureCount, buckets, 0x10, Check);

                // the five lookahead lists
                int[] sc = st.GetProperty("suitCount").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                for (int p = 1; p <= 4; p++)
                {
                    Check($"wins[{p}]", ai.LastWins[p], sc[p + 6]);
                }

                // final decision (the transcribed tree)
                freshAi = Build(c);
                if (mem != null)
                {
                    Array.Copy(mem, freshAi.Mem, mem.Length);
                }

                if (st.TryGetProperty("rngSeed", out JsonElement seedEl))
                {
                    freshAi.Rng = new DelphiRandom(seedEl.GetUInt32());
                }

                int dec = freshAi.Decide();
                lastBranch = freshAi.LastBranch;
                branchHits[lastBranch] = branchHits.GetValueOrDefault(lastBranch) + 1;
                Check("DECISION", dec, c.GetProperty("chosen").GetInt32());

                int[] lists = st.GetProperty("lists").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                CheckList("L1", ai.AlwaysWinsTrump, ai.AlwaysWinsTrumpCount, lists, 0, Check);
                CheckList("L2", ai.AlwaysWinsPartner, ai.AlwaysWinsPartnerCount, lists, 8, Check);
                CheckList("L3", ai.AlwaysWins, ai.AlwaysWinsCount, lists, 0x10, Check);
                CheckList("L4", ai.Mixed, ai.MixedCount, lists, 0x18, Check);
                CheckList("L5", ai.NeverWins, ai.NeverWinsCount, lists, 0x20, Check);

                Check("l1Count", ai.AlwaysWinsTrumpCount, st.GetProperty("l1Count").GetInt32());
                Check("l2Count", ai.AlwaysWinsPartnerCount, st.GetProperty("l2Count").GetInt32());
                Check("l3Count", ai.AlwaysWinsCount, st.GetProperty("l3Count").GetInt32());
                Check("l4Count", ai.MixedCount, st.GetProperty("l4Count").GetInt32());
                Check("l5Count", ai.NeverWinsCount, st.GetProperty("l5Count").GetInt32());
            }

            Console.WriteLine("failures by branch: " + string.Join(", ", branchFail.OrderByDescending(k => k.Value).Select(k => $"{k.Key}={k.Value}")));
            Console.WriteLine($"state-checked {n} positions");

            // A branch nobody exercised is transcribed but unverified, which is worth saying out
            // loud: the totals above would read the same either way.
            var everTagged = new List<string>
            {
                "feed-partner", "two-sure-endgame", "lead-partner-memD", "lead-with-trumps",
                "lead-cash", "L1-random", "no-mixed", "L2-lead", "L2-cheap", "L2-loser-top",
                "L3-random-lead", "L3-pick", "mx-opptrump|mixed", "mx-memC|mixed", "mx-memB|mixed",
                "mx-follow|mixed", "mx-suitscore|mixed",
            };
            Console.WriteLine("decision-tree branch coverage:");
            foreach (string b in everTagged)
            {
                int hits = branchHits.GetValueOrDefault(b);
                Console.WriteLine($"    {b,-22} {hits,6}{(hits == 0 ? "   <-- NEVER EXERCISED" : string.Empty)}");
            }

            foreach (var kv in branchHits.Where(k => !everTagged.Contains(k.Key)))
            {
                Console.WriteLine($"    {kv.Key,-22} {kv.Value,6}   (untracked tag)");
            }
            foreach (var kv in stats.OrderByDescending(k => k.Value.bad))
            {
                Console.WriteLine($"    {kv.Key,-14} {kv.Value.total - kv.Value.bad}/{kv.Value.total} " +
                                  $"({100.0 * (kv.Value.total - kv.Value.bad) / kv.Value.total:F1}%)");
            }

            return stats.Values.Sum(v => v.bad) == 0 ? 0 : 1;
        }

        private static void CheckList(string name, int[] mine, int count, int[] buckets, int baseOff,
                                      Action<string, int, int> check)
        {
            for (int i = 1; i <= count && i <= 8; i++)
            {
                check($"{name}[{i}]", mine[i], buckets[baseOff + i]);
            }
        }

        /// <summary>
        /// Rebuild the AI's per-round memory the way the port does in real play, so it can be
        /// compared with the block the original actually had. Seat 1 is the human in the game the
        /// vectors were generated from, and is the only seat whose discards can signal.
        /// </summary>
        private static byte[] ReplayMemory(JsonElement c)
        {
            var plays = new List<(int, int, int)>();
            foreach (JsonElement h in c.GetProperty("history").EnumerateArray())
            {
                plays.Add((h[0].GetInt32(), SuitId(h[1].GetString()!), h[2].GetInt32()));
            }

            foreach (JsonElement t in c.GetProperty("trick").EnumerateArray())
            {
                plays.Add((t[0].GetInt32(), SuitId(t[1].GetString()!), t[2].GetInt32()));
            }

            int[]? bidSuits = null;
            if (c.TryGetProperty("bidSuits", out JsonElement bs))
            {
                bidSuits = new int[5];
                int p = 1;
                foreach (JsonElement e in bs.EnumerateArray())
                {
                    bidSuits[p++] = e.GetInt32();
                }
            }

            int level = c.TryGetProperty("level", out JsonElement lv) ? lv.GetInt32() : 1;
            return PlayMemory.Replay(
                ContractId(c.GetProperty("contract").GetString()!), plays, bidSuits,
                humanSeat: 1, cardsDealt: 8, level: level);
        }

        private static OriginalPlayAi Build(JsonElement c)
        {
            var ai = new OriginalPlayAi
            {
                Contract = ContractId(c.GetProperty("contract").GetString()!),
                Me = c.GetProperty("seat").GetInt32(),
            };
            ai.Trump = ai.Contract <= 4 ? TrumpSuitOf(ai.Contract) : OriginalPlayAi.NoSuit;
            if (c.TryGetProperty("declarer", out JsonElement dec))
            {
                ai.Declarer = dec.GetInt32();
            }

            var hand = c.GetProperty("hand").EnumerateArray()
                        .Select(x => (suit: SuitId(x[0].GetString()!), rank: x[1].GetInt32())).ToList();
            for (int i = 0; i < hand.Count; i++)
            {
                ai.SlotSuit[i] = hand[i].suit;
                ai.SlotRank[i] = hand[i].rank;
                ai.SlotPresent[i] = true;
            }

            var trick = new List<(int player, int suit, int rank)>();
            foreach (JsonElement t in c.GetProperty("trick").EnumerateArray())
            {
                trick.Add((t[0].GetInt32(), SuitId(t[1].GetString()!), t[2].GetInt32()));
            }

            for (int i = 0; i < trick.Count; i++)
            {
                ai.TableSuit[i] = trick[i].suit;
                ai.TableRank[i] = trick[i].rank;
                ai.TableOwner[i] = trick[i].player;
            }

            ai.LedSuit = trick.Count > 0 ? trick[0].suit : OriginalPlayAi.NoSuit;

            // "possible" matrix: anything not yet seen, minus suits a player has shown void in
            var seen = new HashSet<(int, int)>(hand.Select(h => (h.suit, h.rank)));
            foreach (JsonElement h in c.GetProperty("history").EnumerateArray())
            {
                seen.Add((SuitId(h[1].GetString()!), h[2].GetInt32()));
            }

            foreach (var t in trick)
            {
                seen.Add((t.suit, t.rank));
            }

            var voids = new HashSet<int>[5];
            for (int p = 1; p <= 4; p++)
            {
                voids[p] = new HashSet<int>();
            }

            var all = new List<(int player, int suit, int rank)>();
            foreach (JsonElement h in c.GetProperty("history").EnumerateArray())
            {
                all.Add((h[0].GetInt32(), SuitId(h[1].GetString()!), h[2].GetInt32()));
            }

            for (int i = 0; i + 3 < all.Count; i += 4)
            {
                int led = all[i].suit;
                for (int k = 0; k < 4; k++)
                {
                    if (all[i + k].suit != led)
                    {
                        voids[all[i + k].player].Add(led);
                    }
                }
            }

            if (trick.Count > 0)
            {
                foreach (var t in trick)
                {
                    if (t.suit != trick[0].suit)
                    {
                        voids[t.player].Add(trick[0].suit);
                    }
                }
            }

            for (int p = 1; p <= 4; p++)
            {
                for (int s = 0; s < 4; s++)
                {
                    for (int r = 7; r <= 14; r++)
                    {
                        ai.Possible[p, s, r - 7] = !seen.Contains((s, r)) && !voids[p].Contains(s);
                    }
                }
            }

            foreach (var h in hand)
            {
                ai.Possible[ai.Me, h.suit, h.rank - 7] = true;
            }

            // legality comes from the engine's own (100%-verified) rules
            var handCards = hand.Select(h => new Card(SuitEnum(h.suit), (Rank)h.rank)).ToList();
            var trickPlays = trick.Select(t => new Play(t.player - 1, new Card(SuitEnum(t.suit), (Rank)t.rank))).ToList();
            var contract = new Contract(BidTypeOf(ai.Contract), 0, false, false);
            var legal = Rules.ValidCards(handCards, trickPlays, contract, ai.Me - 1);
            for (int i = 0; i < handCards.Count; i++)
            {
                ai.SlotLegal[i] = legal.Contains(handCards[i]);
            }

            return ai;
        }

        internal static int SuitId(string s) => s switch
        {
            "C" => 0, "D" => 1, "S" => 2, "H" => 3, _ => throw new ArgumentException(s),
        };

        internal static Suit SuitEnum(int origSuit) => origSuit switch
        {
            0 => Suit.Clubs, 1 => Suit.Diamonds, 2 => Suit.Spades, _ => Suit.Hearts,
        };

        internal static int ContractId(string s) => s switch
        {
            "Clubs" => 1, "Diamonds" => 2, "Hearts" => 3, "Spades" => 4,
            "NoTrumps" => 5, "AllTrumps" => 6, _ => throw new ArgumentException(s),
        };

        internal static BidType BidTypeOf(int id) => (BidType)id;

        internal static int TrumpSuitOf(int contract) => contract switch
        {
            1 => 0, 2 => 1, 3 => 3, 4 => 2, _ => OriginalPlayAi.NoSuit,
        };
    }
}
