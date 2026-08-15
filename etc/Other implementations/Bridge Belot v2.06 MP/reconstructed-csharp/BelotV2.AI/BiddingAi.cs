namespace BelotV2
{
    /// <summary>
    /// Faithful port of the bidding valuation and decision in choosegame (0x4778CC).
    ///
    /// Scoring (per contract, over the first five cards) is transcribed exactly:
    ///   suit contract  = Σ trump-weights(trump cards) + Σ plain-weights(side cards)
    ///                    + 6·(trump count) − 10·max(0, 3 − trump count)
    ///                    + 3 (K+Q of trump) + 1 (J+9 of trump)  [clamped ≥ 0]
    ///   no-trumps      = Σ no-trump-weights(all cards)
    ///   all-trumps     = Σ trump-weights(all cards)
    ///   + declaration bonus added to every contract except no-trumps:
    ///       terca +4, quarte +10, quinte +20, careta +20 (+10 more for four nines).
    ///
    /// The decision on top of these scores is in BiddingAi.Decision.cs.
    /// </summary>
    public static partial class BiddingAi
    {
        // The 80-bit (x87 extended) normalisation constants, taken verbatim from the binary.
        // Both have exponent 0x3FFF, so the value is mantissa / 2^63. NOTE these are very
        // slightly BELOW 1.16 / 1.06, which changes exact .5 cases: 75 * 1.06 is 79.5 in
        // `double` (-> 80) but just under 79.5 with the real constant (-> 79). Using `double`
        // here produces genuine divergences from the original, so the scaling is done in exact
        // integer arithmetic instead.
        private const ulong SuitMantissa = 0x947AE147AE147AE1UL;     // @0x479D30 (~1.16)
        private const ulong NoTrumpMantissa = 0x87AE147AE147AE14UL;  // @0x479D3C (~1.06)

        /// <summary>raw * (mantissa / 2^63) rounded to nearest, ties-to-even (x87 `fistp`).</summary>
        private static int ScaleExact(int raw, ulong mantissa)
        {
            bool negative = raw < 0;
            ulong magnitude = (ulong)(negative ? -(long)raw : raw);
            UInt128 product = (UInt128)magnitude * mantissa;
            UInt128 quotient = product >> 63;
            UInt128 remainder = product & ((UInt128.One << 63) - 1);
            UInt128 half = UInt128.One << 62;
            if (remainder > half || (remainder == half && (quotient & 1) == 1))
            {
                quotient += 1;
            }

            int result = (int)quotient;
            return negative ? -result : result;
        }

        public static int ScoreContract(BidType type, IReadOnlyList<Card> hand)
        {
            var cat = Bids.CategoryOf(type);
            Suit? trump = Bids.TrumpSuitOf(type);
            int trumpPts = 0, sidePts = 0, trumpCount = 0;

            foreach (Card c in hand)
            {
                int ri = c.RankIndex;
                if (cat == ContractCategory.AllTrumps)
                {
                    trumpPts += AiTables.TrumpBid[ri];
                }
                else if (cat == ContractCategory.NoTrumps)
                {
                    sidePts += AiTables.NoTrumpBid[ri];
                }
                else if (c.Suit == trump)
                {
                    trumpPts += AiTables.TrumpBid[ri];
                    trumpCount++;
                }
                else
                {
                    sidePts += AiTables.PlainBid[ri];
                }
            }

            int score;
            if (cat == ContractCategory.Suit)
            {
                int raw = trumpPts + sidePts + (trumpCount * 6);
                if (trumpCount < 3)
                {
                    raw -= (3 - trumpCount) * 10;
                }

                // Normalisation recovered from disassembly: fild; fmul xword[0x479D30]=1.16; fistp.
                score = ScaleExact(raw, SuitMantissa);
                if (Has(hand, trump!.Value, Rank.King) && Has(hand, trump.Value, Rank.Queen))
                {
                    score += 3;
                }

                if (Has(hand, trump.Value, Rank.Jack) && Has(hand, trump.Value, Rank.Nine))
                {
                    score += 1;
                }

                if (trumpCount > 3)
                {
                    score += 5;
                }

                if (score < 0)
                {
                    score = 0;
                }
            }
            else if (cat == ContractCategory.NoTrumps)
            {
                // fild; fmul xword[0x479D3C]=1.06; fistp.
                score = ScaleExact(sidePts, NoTrumpMantissa);
            }
            else
            {
                // All-trumps is not scaled in the original.
                score = trumpPts;
            }

            if (cat != ContractCategory.NoTrumps)
            {
                score += DeclarationBonus(hand);
            }

            return score;
        }

        // Declaration bonus added by choosegame to non-no-trump contracts.
        private static int DeclarationBonus(IReadOnlyList<Card> hand)
        {
            int bonus = 0;
            var dummy = new Contract(BidType.AllTrumps, 0, false, false);
            foreach (Announce a in Announces.Detect(hand, 0, dummy))
            {
                bonus += a.Kind switch
                {
                    AnnounceKind.Terca => 4,
                    AnnounceKind.Quarte => 10,
                    AnnounceKind.Quinte => 20,
                    AnnounceKind.Careta => a.TopRank == Rank.Nine ? 30 : 20,
                    _ => 0,
                };
            }

            return bonus;
        }

        private static bool Has(IReadOnlyList<Card> hand, Suit s, Rank r)
        {
            foreach (Card c in hand)
            {
                if (c.Suit == s && c.Rank == r)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The engine-facing wrapper: maps <see cref="BidContext"/> onto the globals the original
        /// works from and translates its answer back into a <see cref="BidType"/>.
        /// </summary>
        public static BidType ChooseBid(BidContext ctx, DelphiRandom rng)
        {
            // Each seat's own last bid, in the binary's encoding.
            var bids = new int[5];
            foreach (BidAction a in ctx.History)
            {
                int seat1 = a.Seat + 1;
                if (a.Bid == BidType.Double)
                {
                    // A double is recorded against the contract it doubles.
                    int h = ctx.ContractSeat + 1;
                    if (h >= 1 && bids[h] != 0)
                    {
                        bids[h] += 10;
                    }
                }
                else if (a.Bid == BidType.Redouble)
                {
                    int h = ctx.ContractSeat + 1;
                    if (h >= 1 && bids[h] != 0)
                    {
                        bids[h] += 10;
                    }
                }
                else if (a.Bid != BidType.Pass)
                {
                    bids[seat1] = ContractId(a.Bid);
                }
            }

            int holder = ctx.ContractSeat >= 0 ? ctx.ContractSeat + 1 : 1;
            var board = new[] { 0, ctx.Board?[0] ?? 0, ctx.Board?[1] ?? 0 };
            int opener = ctx.History.Count > 0 ? ctx.History[0].Seat + 1 : ctx.Seat + 1;
            int bid = Choose(ctx.Hand, ctx.Seat + 1, holder, bids, board, opener, rng);

            if (bid == 0)
            {
                return BidType.Pass;
            }

            return (bid / 10) switch
            {
                0 => ContractOf(bid % 10),
                1 => BidType.Double,
                _ => BidType.Redouble,
            };
        }

        private static int ContractId(BidType t) => t switch
        {
            BidType.Clubs => 1,
            BidType.Diamonds => 2,
            BidType.Hearts => 3,
            BidType.Spades => 4,
            BidType.NoTrumps => 5,
            _ => 6,
        };
    }
}
