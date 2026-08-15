namespace BelotV2
{
    public enum AnnounceKind
    {
        Terca,   // 3-card sequence  = 20
        Quarte,  // 4-card sequence  = 50
        Quinte,  // 5+-card sequence = 100
        Careta,  // four of a kind
        Belote,  // K+Q of trump, declared in play = 20
    }

    public readonly record struct Announce(AnnounceKind Kind, int Seat, Rank TopRank, int Value)
    {
        // Comparison rank between competing declarations (higher wins the whole comparison).
        // Sequences: quinte(3) > quarte(2) > terca(1); a careta outranks any sequence (4);
        // ties broken by top rank. Belote never competes (always scores for its holder).
        public int Tier => this.Kind switch
        {
            AnnounceKind.Terca => 1,
            AnnounceKind.Quarte => 2,
            AnnounceKind.Quinte => 3,
            AnnounceKind.Careta => 4,
            _ => 0,
        };
    }

    /// <summary>
    /// Announcement (declaration) detection and valuation.
    ///
    /// Detection reproduces FUN_00479ee8 (RE):
    ///  - Four-of-a-kind ("careta") only for ranks 9,10,J,Q,K,A (never 7/8).
    ///  - Sequences are runs of consecutive ranks in one suit over the sorted hand;
    ///    length 3 = terca, 4 = quarte, 5..8 = quinte. Cards belonging to a careta are
    ///    excluded from sequence scanning (matches the original).
    /// Careta values are binary-confirmed (table @0x489DF0 = [150,100,200,100,100,100]
    /// for ranks [9,10,J,Q,K,A]); sequence values are the standard 20/50/100.
    /// Announcements do not apply in No-trumps contracts (standard Belot).
    /// </summary>
    public static class Announces
    {
        public static int CaretaValue(Rank r) => r switch
        {
            Rank.Jack => 200,
            Rank.Nine => 150,
            Rank.Ten or Rank.Queen or Rank.King or Rank.Ace => 100,
            _ => 0,
        };

        public static int SequenceValue(int length) => length switch
        {
            3 => 20,
            4 => 50,
            >= 5 => 100,
            _ => 0,
        };

        /// <summary>All sequence/careta declarations in a hand (no Belote; none in no-trumps).</summary>
        public static List<Announce> Detect(IReadOnlyList<Card> hand, int seat, Contract contract)
        {
            var result = new List<Announce>();
            if (contract.Category == ContractCategory.NoTrumps)
            {
                return result; // no declarations in no-trumps
            }

            // Rank counts (index by rank-7) for careta detection.
            var counts = new int[8];
            foreach (Card c in hand)
            {
                counts[c.RankIndex]++;
            }

            var inCareta = new HashSet<Rank>();
            for (int ri = 2; ri < 8; ri++) // ranks 9..A
            {
                if (counts[ri] == 4)
                {
                    var r = (Rank)(ri + 7);
                    inCareta.Add(r);
                    result.Add(new Announce(AnnounceKind.Careta, seat, r, CaretaValue(r)));
                }
            }

            // Sequences per suit over sorted ranks, skipping cards used in a careta.
            for (int s = 0; s < 4; s++)
            {
                var ranks = new List<int>();
                foreach (Card c in hand)
                {
                    if ((int)c.Suit == s && !inCareta.Contains(c.Rank))
                    {
                        ranks.Add((int)c.Rank);
                    }
                }

                ranks.Sort();
                int runStart = 0;
                for (int i = 1; i <= ranks.Count; i++)
                {
                    bool broke = i == ranks.Count || ranks[i] != ranks[i - 1] + 1;
                    if (broke)
                    {
                        int len = i - runStart;
                        if (len >= 3)
                        {
                            var top = (Rank)ranks[i - 1];
                            AnnounceKind kind = len == 3 ? AnnounceKind.Terca
                                : len == 4 ? AnnounceKind.Quarte : AnnounceKind.Quinte;
                            result.Add(new Announce(kind, seat, top, SequenceValue(len)));
                        }

                        runStart = i;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolve competing declarations between the two teams. Only the team holding the
        /// single strongest declaration scores its sequence/careta announcements; an exact
        /// tie cancels them for both teams. Belote (declared during play) always scores.
        /// Returns the announcements that actually count.
        /// </summary>
        public static List<Announce> Resolve(IEnumerable<Announce> all)
        {
            var list = all.ToList();
            var competing = list.Where(a => a.Kind != AnnounceKind.Belote).ToList();
            var kept = list.Where(a => a.Kind == AnnounceKind.Belote).ToList();
            if (competing.Count == 0)
            {
                return kept;
            }

            var best0 = competing.Where(a => Seats.TeamOf(a.Seat) == 0).ToList();
            var best1 = competing.Where(a => Seats.TeamOf(a.Seat) == 1).ToList();
            if (best0.Count == 0)
            {
                kept.AddRange(best1);
                return kept;
            }

            if (best1.Count == 0)
            {
                kept.AddRange(best0);
                return kept;
            }

            Announce b0 = best0.Aggregate((x, y) => Compare(x, y) >= 0 ? x : y);
            Announce b1 = best1.Aggregate((x, y) => Compare(x, y) >= 0 ? x : y);
            int cmp = Compare(b0, b1);
            if (cmp > 0)
            {
                kept.AddRange(best0);
            }
            else if (cmp < 0)
            {
                kept.AddRange(best1);
            }

            // Exact tie: neither team's competing announcements score.
            return kept;
        }

        // Higher = stronger. Careta beats sequence; within a kind, higher top rank wins.
        private static int Compare(Announce a, Announce b)
        {
            if (a.Tier != b.Tier)
            {
                return a.Tier.CompareTo(b.Tier);
            }

            return ((int)a.TopRank).CompareTo((int)b.TopRank);
        }
    }
}
