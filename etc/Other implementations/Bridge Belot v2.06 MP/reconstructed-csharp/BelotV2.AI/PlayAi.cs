namespace BelotV2
{
    /// <summary>
    /// Card-play AI — a faithful structural reconstruction of player2BeforePlay (0x46F5C0).
    ///
    /// The original is a ~4500-instruction routine; a byte-exact transcription is not
    /// verifiable, but its architecture is reproduced here exactly as decompiled:
    ///   1. Build the legal-candidate list with the game's own move predicate
    ///      (FUN_004767F8 == <see cref="Rules.ValidCards"/>).
    ///   2. Infer, per opponent, which cards they might still hold — a card is possible for a
    ///      seat unless it has been seen or the seat has shown void in that suit (matrix
    ///      auStack_26c / auStack_265 in the original).
    ///   3. Classify each candidate by comparing the number of opponents who could still beat
    ///      it against the number of the player's own higher cards, into three buckets:
    ///      sure-winners (no opponent can beat), middle, and losers (auStack_120 buckets,
    ///      counters uStack_78 / uStack_7c / uStack_80).
    ///   4. Select from the buckets by seat-in-trick, contract and who currently holds the
    ///      trick (cash sure winners, schmier points onto a winning partner, win cheaply, else
    ///      discard the lowest-value loser).
    ///
    /// Card strength/beat comparisons use the recovered order tables via <see cref="Rules"/>,
    /// which are equivalent to the binary's runtime strength cache at 0x48BAED. The exact
    /// per-branch card choice of step 4 across all situations is reconstructed, not transcribed.
    /// </summary>
    public static class PlayAi
    {
        private enum Bucket
        {
            SureWinner,
            Middle,
            Loser,
        }

        public static Card Play(PlayContext ctx, DelphiRandom rng)
        {
            var legal = ctx.Legal;
            if (legal.Count == 1)
            {
                return legal[0];
            }

            var voids = InferVoids(ctx);
            var seen = SeenCards(ctx);

            // Classify each legal candidate.
            var sure = new List<Card>();
            var middle = new List<Card>();
            var losers = new List<Card>();
            foreach (Card c in legal)
            {
                switch (Classify(c, ctx, voids, seen))
                {
                    case Bucket.SureWinner: sure.Add(c); break;
                    case Bucket.Middle: middle.Add(c); break;
                    default: losers.Add(c); break;
                }
            }

            return ctx.CurrentTrick.Count == 0
                ? ChooseLead(ctx, sure, middle, losers)
                : ChooseFollow(ctx, sure, middle, losers);
        }

        // ---- classification --------------------------------------------------------------

        private static Bucket Classify(
            Card c, PlayContext ctx, IReadOnlyList<HashSet<Suit>> voids, HashSet<Card> seen)
        {
            int beaters = OpponentsWhoCanBeat(c, ctx, voids, seen);
            int myHigher = MyHigherCards(c, ctx);
            if (beaters == 0)
            {
                return Bucket.SureWinner;
            }

            return myHigher < beaters ? Bucket.Loser : Bucket.Middle;
        }

        // Number of opponents who could still hold a card that beats `c` if the player commits
        // to `c` now (whether leading or following the current trick).
        private static int OpponentsWhoCanBeat(
            Card c, PlayContext ctx, IReadOnlyList<HashSet<Suit>> voids, HashSet<Card> seen)
        {
            var trick = ctx.CurrentTrick;
            int count = 0;
            for (int seat = 0; seat < Seats.Count; seat++)
            {
                if (Seats.SameTeam(seat, ctx.Seat))
                {
                    continue; // opponents only
                }

                if (AlreadyPlayed(trick, seat))
                {
                    continue; // can no longer affect the trick
                }

                if (OpponentCanBeat(seat, c, ctx, voids, seen))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool OpponentCanBeat(
            int seat, Card mine, PlayContext ctx,
            IReadOnlyList<HashSet<Suit>> voids, HashSet<Card> seen)
        {
            var contract = ctx.Contract;
            var baseTrick = new List<Play>(ctx.CurrentTrick) { new(ctx.Seat, mine) };

            for (int s = 0; s < 4; s++)
            {
                for (int r = 7; r <= 14; r++)
                {
                    var u = new Card((Suit)s, (Rank)r);
                    if (seen.Contains(u) || voids[seat].Contains(u.Suit))
                    {
                        continue; // opponent cannot hold this card
                    }

                    // Would this card beat mine if the opponent played it into the trick?
                    var t = new List<Play>(baseTrick) { new(seat, u) };
                    if (Rules.WinnerIndex(t, contract) == t.Count - 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Count of the player's own cards of the same suit that outrank `c` (matches the
        // original's same-suit higher-card tally used in the classification).
        private static int MyHigherCards(Card c, PlayContext ctx)
        {
            bool trump = ctx.Contract.IsTrump(c.Suit);
            int cStr = trump ? Cards.TrumpStrength(c) : Cards.PlainStrength(c);
            int higher = 0;
            foreach (Card h in ctx.Hand)
            {
                if (h.Equals(c) || h.Suit != c.Suit)
                {
                    continue;
                }

                int hStr = trump ? Cards.TrumpStrength(h) : Cards.PlainStrength(h);
                if (hStr > cStr)
                {
                    higher++;
                }
            }

            return higher;
        }

        // ---- selection -------------------------------------------------------------------

        private static Card ChooseLead(
            PlayContext ctx, List<Card> sure, List<Card> middle, List<Card> losers)
        {
            // Cash a guaranteed trick if we have one (highest points among sure winners).
            if (sure.Count > 0)
            {
                return HighestPoints(sure, ctx.Contract);
            }

            // Otherwise lead the WEAKEST card of the longest side suit — note "weakest" is by
            // trick strength, not points, so a Queen is a lower lead than a Ten. Verified
            // against the original's leads.
            var contract = ctx.Contract;
            var bySuit = new Dictionary<Suit, List<Card>>();
            foreach (Card c in ctx.Legal)
            {
                if (contract.Category == ContractCategory.Suit && contract.IsTrump(c.Suit))
                {
                    continue;   // keep trumps back when leading
                }

                if (!bySuit.TryGetValue(c.Suit, out var l))
                {
                    bySuit[c.Suit] = l = new List<Card>();
                }

                l.Add(c);
            }

            if (bySuit.Count == 0)
            {
                return LowestStrength(ctx.Legal, contract);
            }

            int longest = bySuit.Max(kv => kv.Value.Count);
            var candidates = new List<Card>();
            foreach (var kv in bySuit)
            {
                if (kv.Value.Count == longest)
                {
                    candidates.AddRange(kv.Value);
                }
            }

            return LowestStrength(candidates, contract);
        }

        // Can any opponent still to play beat the card the partner is currently winning with?
        private static bool PartnerCardIsSafe(PlayContext ctx, IReadOnlyList<Play> trick, int winnerIdx)
        {
            var seen = SeenCards(ctx);
            var voids = InferVoids(ctx);
            var baseTrick = new List<Play>(trick);
            for (int seat = 0; seat < Seats.Count; seat++)
            {
                if (Seats.SameTeam(seat, ctx.Seat) || AlreadyPlayed(trick, seat) || seat == ctx.Seat)
                {
                    continue;
                }

                for (int s = 0; s < 4; s++)
                {
                    for (int r = 7; r <= 14; r++)
                    {
                        var u = new Card((Suit)s, (Rank)r);
                        if (seen.Contains(u) || voids[seat].Contains(u.Suit))
                        {
                            continue;
                        }

                        var t = new List<Play>(baseTrick) { new(seat, u) };
                        if (Rules.WinnerIndex(t, ctx.Contract) == t.Count - 1)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static Card LowestStrength(IReadOnlyList<Card> pool, Contract contract)
        {
            Card best = pool[0];
            int bestStrength = int.MaxValue;
            foreach (Card c in pool)
            {
                int s = contract.IsTrump(c.Suit) ? Cards.TrumpStrength(c) : Cards.PlainStrength(c);
                if (s < bestStrength)
                {
                    bestStrength = s;
                    best = c;
                }
            }

            return best;
        }

        private static Card ChooseFollow(
            PlayContext ctx, List<Card> sure, List<Card> middle, List<Card> losers)
        {
            var trick = ctx.CurrentTrick;
            var contract = ctx.Contract;
            int winnerIdx = Rules.WinnerIndex(trick, contract);
            bool partnerWinning = Seats.SameTeam(ctx.Seat, trick[winnerIdx].Seat);

            // Which legal cards actually take the trick right now?
            var takers = new List<Card>();
            foreach (Card c in ctx.Legal)
            {
                var t = new List<Play>(trick) { new(ctx.Seat, c) };
                if (Rules.WinnerIndex(t, contract) == t.Count - 1)
                {
                    takers.Add(c);
                }
            }

            if (partnerWinning)
            {
                // Take over a partner only when their card is NOT already safe — the original
                // overtakes a Queen with the Ace, but never ruffs a partner's winning Ace.
                bool partnerSafe = PartnerCardIsSafe(ctx, trick, winnerIdx);
                var safeTakers = new List<Card>();
                if (!partnerSafe)
                {
                    foreach (Card c in takers)
                    {
                        if (sure.Contains(c))
                        {
                            safeTakers.Add(c);
                        }
                    }
                }

                if (safeTakers.Count > 0)
                {
                    return LowestStrength(safeTakers, contract);
                }

                // Otherwise schmier points onto partner's trick, but keep cards that are still
                // winners of their suit (the original feeds a Ten before an Ace).
                var nonTakers = new List<Card>();
                foreach (Card c in ctx.Legal)
                {
                    if (!takers.Contains(c))
                    {
                        nonTakers.Add(c);
                    }
                }

                var pool = nonTakers.Count > 0 ? nonTakers : ctx.Legal;
                var keepers = new List<Card>();
                foreach (Card c in pool)
                {
                    if (!sure.Contains(c))
                    {
                        keepers.Add(c);
                    }
                }

                return HighestPoints(keepers.Count > 0 ? keepers : pool, contract);
            }

            // Opponent currently winning.
            if (takers.Count > 0)
            {
                // Prefer a taker that is also a sure winner (won't be over-taken by a later
                // opponent); otherwise the cheapest taker.
                var safeTakers = new List<Card>();
                foreach (Card c in takers)
                {
                    if (sure.Contains(c))
                    {
                        safeTakers.Add(c);
                    }
                }

                bool isLast = trick.Count == 3;
                var pool = safeTakers.Count > 0 ? safeTakers : (isLast ? takers : takers);
                return LowestPoints(pool, contract);
            }

            // Can't win: discard the lowest-value loser (keep guards/middle cards).
            var discardPool = losers.Count > 0 ? losers : (middle.Count > 0 ? middle : ctx.Legal);
            return LowestPoints(discardPool, contract);
        }

        // ---- inference helpers -----------------------------------------------------------

        // Suits each seat is known to be void in, from completed tricks: a seat that did not
        // follow the led suit is void in it.
        private static List<HashSet<Suit>> InferVoids(PlayContext ctx)
        {
            var voids = new List<HashSet<Suit>>();
            for (int i = 0; i < 4; i++)
            {
                voids.Add(new HashSet<Suit>());
            }

            // Reconstruct tricks from the flat history (groups of 4 in play order).
            var hist = ctx.PlayedHistory;
            for (int i = 0; i + 3 < hist.Count; i += 4)
            {
                Suit led = hist[i].Card.Suit;
                for (int k = 0; k < 4; k++)
                {
                    Play p = hist[i + k];
                    if (p.Card.Suit != led)
                    {
                        voids[p.Seat].Add(led);
                    }
                }
            }

            // Current (incomplete) trick too.
            if (ctx.CurrentTrick.Count > 0)
            {
                Suit led = ctx.CurrentTrick[0].Card.Suit;
                foreach (Play p in ctx.CurrentTrick)
                {
                    if (p.Card.Suit != led)
                    {
                        voids[p.Seat].Add(led);
                    }
                }
            }

            return voids;
        }

        private static HashSet<Card> SeenCards(PlayContext ctx)
        {
            var seen = new HashSet<Card>();
            foreach (Card c in ctx.Hand)
            {
                seen.Add(c);
            }

            foreach (Play p in ctx.PlayedHistory)
            {
                seen.Add(p.Card);
            }

            foreach (Play p in ctx.CurrentTrick)
            {
                seen.Add(p.Card);
            }

            return seen;
        }

        private static bool AlreadyPlayed(IReadOnlyList<Play> trick, int seat)
        {
            foreach (Play p in trick)
            {
                if (p.Seat == seat)
                {
                    return true;
                }
            }

            return false;
        }

        private static Card HighestPoints(IReadOnlyList<Card> pool, Contract contract)
        {
            Card best = pool[0];
            int bestV = -1;
            foreach (Card c in pool)
            {
                int v = Rules.PointValue(c, contract);
                if (v > bestV)
                {
                    bestV = v;
                    best = c;
                }
            }

            return best;
        }

        private static Card LowestPoints(IReadOnlyList<Card> pool, Contract contract)
        {
            Card best = pool[0];
            int bestKey = int.MaxValue;
            foreach (Card c in pool)
            {
                int v = Rules.PointValue(c, contract);
                int strength = contract.IsTrump(c.Suit) ? Cards.TrumpStrength(c) : Cards.PlainStrength(c);
                int key = (v * 10) + strength;
                if (key < bestKey)
                {
                    bestKey = key;
                    best = c;
                }
            }

            return best;
        }
    }
}
