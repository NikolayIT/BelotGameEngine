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
        // Kind ordering, consulted only after value (see Announces.Compare): quinte(3) >
        // quarte(2) > terca(1), and a careta(4) outranks any sequence it ties with.
        // Belote never competes — it always scores for its holder.
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
        /// Resolve competing declarations between the two teams. Only the team holding the single
        /// strongest declaration scores, and when it does it scores ALL of its sequences and
        /// caretas, not just the winning one; the other team scores none of its own. An exact tie
        /// cancels the contest for both. Belote (declared during play) always scores, for whoever
        /// holds it, and takes no part in the contest. Returns the announcements that count.
        ///
        /// Measured against the binary rather than assumed: see Compare for the ordering, which
        /// is not the one the printed rules suggest.
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

        // Higher = stronger, and what is compared first is the VALUE, not the kind: four nines
        // (150) outrank four queens (100) even though a nine is the lower card, and four jacks
        // (200) outrank both. Only when two declarations are worth the same does the kind decide
        // (a careta beats a sequence), and only then does the top rank break the remaining tie.
        // Comparing by kind first — the intuitive reading — awards the round to the wrong side
        // whenever two fours of a kind meet.
        private static int Compare(Announce a, Announce b)
        {
            if (a.Value != b.Value)
            {
                return a.Value.CompareTo(b.Value);
            }

            if (a.Tier != b.Tier)
            {
                return a.Tier.CompareTo(b.Tier);
            }

            // Last tie-break, and the two kinds do not use the same order.
            //
            // A sequence is ranked by its top card by RANK: a terca to the king beats a terca to
            // the ten, and a terca to the jack beats a terca to the ten, which is what the run's
            // "top" card means. Two identical sequences cancel.
            //
            // A careta is ranked by the card's STRENGTH instead — the plain trick-taking order,
            // where a ten sits above a king. Four tens (100) therefore beats four kings (100),
            // but four aces beats four tens. All twelve orderings of the four hundred-point
            // caretas were measured against the binary, both seat orders each, and the result is
            // exactly Cards.NoTrumpOrder. Nines and jacks never reach here: 150 and 200 are
            // unique values, so they are already decided above.
            if (a.Kind != AnnounceKind.Careta)
            {
                return ((int)a.TopRank).CompareTo((int)b.TopRank);
            }

            return Cards.NoTrumpOrder[(int)a.TopRank - 7].CompareTo(Cards.NoTrumpOrder[(int)b.TopRank - 7]);
        }
    }
}
