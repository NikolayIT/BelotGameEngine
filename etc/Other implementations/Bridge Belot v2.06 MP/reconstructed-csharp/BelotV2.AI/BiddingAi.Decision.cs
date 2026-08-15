namespace BelotV2
{
    /// <summary>
    /// What the original actually bids, on top of the (exact) valuation in BiddingAi.cs.
    ///
    /// Transcribed from choosegame @0x4778CC. The routine leaves its answer in EBX and jumps to a
    /// common tail at 0x4791FD, so <see cref="Choose"/> is that function's return value. Bids are
    /// encoded the way the game encodes them, <c>level * 10 + contract</c>: contract 1..6 =
    /// Clubs, Diamonds, Hearts, Spades, No-trumps, All-trumps, level 1 = doubled, 2 = redoubled,
    /// and 0 is Pass.
    ///
    /// The state it reads lives in globals the auction maintains — see tools/bid_probe.py, which
    /// drives the real routine so this transcription can be diffed against it.
    /// </summary>
    public static partial class BiddingAi
    {
        /// <summary>Bulgarian belote's target; the routine works from `151 - board`.</summary>
        public const int Target = 151;

        /// <summary>The order the opener considers contracts in (@0x489D2A read downwards).</summary>
        private static readonly int[] OpeningLadder = { 6, 5, 3, 4, 2, 1 };

        /// <summary>The order used when a team is about to go out (@0x489D25 read upwards).</summary>
        private static readonly int[] EndgameLadder = { 1, 2, 4, 3, 5, 6 };

        /// <summary>Contract 1..6 to the binary's suit index (@0x489DAC).</summary>
        private static readonly int[] ContractSuit = { 0, 0, 1, 3, 2, 4, 4 };

        /// <summary>
        /// One call of choosegame's decision.
        /// </summary>
        /// <param name="hand">the five cards dealt so far.</param>
        /// <param name="seat">1..4.</param>
        /// <param name="holder">the seat holding the contract, 1..4 (@0x48BE94).</param>
        /// <param name="bids">each seat's own last bid, indexed 1..4; 0 = none (@0x48BE70).</param>
        /// <param name="board">the match score per team, indexed 1..2 (@0x48BFDC).</param>
        /// <param name="opener">the seat the auction started with (@0x48BF15).</param>
        /// <param name="rng">the game's RNG — the routine draws from it before deciding.</param>
        public static int Choose(
            IReadOnlyList<Card> hand,
            int seat,
            int holder,
            IReadOnlyList<int> bids,
            IReadOnlyList<int> board,
            int opener,
            DelphiRandom rng)
        {
            // The routine walks the hand in the order the cards sit in the player's card group,
            // which is the dealt hand sorted game-style. One branch below is sensitive to that
            // order, so normalise it here rather than trusting the caller.
            var sorted5 = new List<Card>(hand);
            Game.SortHand(sorted5);
            hand = sorted5;

            int[] score = new int[7];
            for (int c = 1; c <= 6; c++)
            {
                score[c] = ScoreContract(ContractOf(c), hand);
            }

            int myTeam = (seat == 2 || seat == 4) ? 2 : 1;
            int otherTeam = 3 - myTeam;
            int diff = board[myTeam] - board[otherTeam];
            var needs = new[] { 0, Math.Max(1, Target - board[1]), Math.Max(1, Target - board[2]) };
            int partner = seat + 2 > 4 ? seat - 2 : seat + 2;
            int standing = bids[holder];

            // Someone is about to go out and the match is not close: play the score, not the hand.
            int margin = rng.Next(3) + 4;
            if ((margin > needs[otherTeam] || margin > needs[myTeam]) && Math.Abs(diff) > 6)
            {
                if (standing == 0)
                {
                    foreach (int c in EndgameLadder)
                    {
                        if (score[c] >= 75)
                        {
                            return c;
                        }
                    }

                    return 0;
                }

                // Behind, with something to lose: double, unless it is the partner's contract.
                if (seat == 3 || diff >= 0 || holder == partner || standing >= 10)
                {
                    return 0;
                }

                return standing + 10;
            }

            // Less extreme version of the same idea: only flags the state, does not decide.
            int margin2 = rng.Next(5) + 16;
            bool wellBehind = false, wellAhead = false;
            if ((margin2 > needs[1] || margin2 > needs[2]) && Math.Abs(diff) > 6)
            {
                if (diff < -15)
                {
                    wellBehind = true;
                }
                else if (diff > 15)
                {
                    wellAhead = true;
                }
            }

            int[] caretas = CaretaRanks(hand);

            // Four jacks: 200 points, and only in all-trumps.
            if (caretas[0] == 11 || caretas[1] == 11)
            {
                if (standing % 10 != 6)
                {
                    return 6;
                }

                if (standing == 6 && holder != partner)
                {
                    return 16;
                }

                if (standing == 16 && holder != partner)
                {
                    return 26;
                }

                return 0;
            }

            // Any other four of a kind, while the auction is still open (@0x47828C). Note which
            // card it names: it walks the hand and takes the suit of the first card that is NOT
            // part of the four. The four score the same whatever the contract, so trumps are
            // chosen from the one card that is still free to matter — with four queens and a
            // stray heart, it bids hearts.
            if (caretas[0] != 0 && standing == 0)
            {
                foreach (Card c in hand)
                {
                    if ((int)c.Rank == caretas[0])
                    {
                        continue;
                    }

                    for (int contract = 1; contract <= 4; contract++)
                    {
                        if (OrigSuit(c.Suit) == ContractSuit[contract])
                        {
                            return contract;
                        }
                    }
                }
            }

            if (standing == 0)
            {
                // Nobody has bid: take the best contract that clears the bar, all-trumps first.
                // A lopsided match near its end raises the bar, whichever side is ahead.
                int bar = wellBehind || wellAhead ? 65 : 50;
                foreach (int c in OpeningLadder)
                {
                    if (score[c] >= bar)
                    {
                        return c;
                    }
                }

                return 0;
            }

            var jackOfSuit = new bool[4];
            foreach (Card c in hand)
            {
                if (c.Rank == Rank.Jack)
                {
                    jackOfSuit[OrigSuit(c.Suit)] = true;
                }
            }

            return Contested(score, SortContracts(score), seat, holder, bids, standing, partner,
                             opener, diff, wellBehind, wellAhead, jackOfSuit, rng);
        }

        /// <summary>
        /// Once there is a contract on the table the routine dispatches on the standing bid
        /// through a jump table (@0x4783BB). Five entries do anything; every other value passes.
        ///
        ///   standing 1..4             a suit contract     @0x47844E
        ///   standing 5                no-trumps           @0x478AE2
        ///   standing 6                all-trumps          @0x478E07
        ///   standing 11..14 / 21..24  doubled suit        @0x478EF2
        ///   standing 15 / 25          doubled no-trumps   @0x4790F4
        ///
        /// They share a vocabulary: lift the side to all-trumps, double the opponents when we
        /// rate their own contract highly, or walk the per-seat table of contracts sorted by
        /// score looking for a suit worth naming.
        /// </summary>
        private static int Contested(
            int[] score, (int Score, int Contract)[] sorted, int seat, int holder,
            IReadOnlyList<int> bids, int standing, int partner, int opener, int diff,
            bool wellBehind, bool wellAhead, bool[] jackOfSuit, DelphiRandom rng)
        {
            int current = standing % 10;
            int level = standing / 10;
            bool partnerHolds = holder == partner;

            if (level == 0)
            {
                if (current >= 1 && current <= 4)
                {
                    return OverSuit(score, sorted, seat, bids, partner, opener, standing,
                                    partnerHolds, diff, wellBehind, wellAhead, jackOfSuit);
                }

                if (current == 5)
                {
                    return OverNoTrumps(score, seat, bids, partner, opener, partnerHolds, diff,
                                        wellBehind, wellAhead);
                }

                if (current == 6)
                {
                    return OverAllTrumps(score, bids, partner, partnerHolds, diff,
                                         wellBehind, wellAhead);
                }

                return 0;
            }

            if (level == 1 || level == 2)
            {
                if (current >= 1 && current <= 4)
                {
                    return OverDoubledSuit(score, sorted, bids, partner, opener, seat, standing,
                                           partnerHolds, wellBehind, wellAhead, rng);
                }

                if (current == 5)
                {
                    return OverDoubledNoTrumps(score, seat, bids, partner, opener, partnerHolds,
                                               wellBehind, wellAhead, rng);
                }
            }

            return 0;
        }

        /// <summary>@0x47844E — somebody has named a suit.</summary>
        private static int OverSuit(
            int[] score, (int Score, int Contract)[] sorted, int seat, IReadOnlyList<int> bids,
            int partner, int opener, int standing, bool partnerHolds, int diff,
            bool wellBehind, bool wellAhead, bool[] jackOfSuit)
        {
            int opp = OriginalPlayAi.TeamOf[seat];                  // indexes the opposing pair; see TeamOf
            bool oppBid = bids[opp] != 0 || bids[opp + 2] != 0;
            bool ourAuction = opener == seat || opener == partner;

            // Straight to all-trumps when our side has done all the bidding: both of us named a
            // suit, neither went past one, and the auction started with us.
            bool bothNamedSuits = bids[seat] > 0 && bids[seat] < 5
                                  && bids[partner] > 0 && bids[partner] < 5;
            if ((!oppBid || score[6] >= 32) && bothNamedSuits && ourAuction)
            {
                return 6;
            }

            if (!partnerHolds)
            {
                return OverOpponentsSuit(score, sorted, seat, bids, partner, opener, standing,
                                         diff, wellBehind, wellAhead);
            }

            // ---- my partner holds it: the question is whether to lift them ----
            if (wellBehind || wellAhead)
            {
                return 0;
            }

            // A contested auction costs all-trumps a flat 15.
            int allTrumps = score[6] - (oppBid ? 15 : 0);
            if (allTrumps > 35)
            {
                return ourAuction ? BestSuitHoldingItsJack(sorted, bids[partner], jackOfSuit) : 6;
            }

            // Not strong enough for all-trumps. Name a suit above the partner's, but only a
            // clearly good one; otherwise leave them in it.
            return BestSuitScoring(sorted, bids[partner], 65, fallback: 0);
        }

        /// <summary>@0x478885 — the opponents hold a suit contract.</summary>
        private static int OverOpponentsSuit(
            int[] score, (int Score, int Contract)[] sorted, int seat, IReadOnlyList<int> bids,
            int partner, int opener, int standing, int diff, bool wellBehind, bool wellAhead)
        {
            int current = standing % 10;

            // Double it if we rate their contract well ourselves. The bar drops from 55 to 40
            // when we are well behind, and being comfortably ahead rules it out entirely.
            if (wellBehind)
            {
                if (score[current] >= 40)
                {
                    return standing + 10;
                }
            }
            else if (score[current] > 55 && !wellAhead && diff < 40)
            {
                return standing + 10;
            }

            int suit = BestSuitScoring(sorted, standing, 40, fallback: -1);
            if (suit >= 0)
            {
                return suit;
            }

            if (score[5] >= 40)
            {
                return 5;
            }

            int allTrumps = score[6];
            if (bids[partner] % 10 != 0)
            {
                allTrumps += 10;       // the partner has shown something
            }

            if (opener != seat && opener != partner)
            {
                allTrumps -= 15;       // and it is not even our auction
            }

            return allTrumps >= 44 ? 6 : 0;
        }

        /// <summary>@0x478AE2 — no-trumps is standing.</summary>
        private static int OverNoTrumps(
            int[] score, int seat, IReadOnlyList<int> bids, int partner, int opener,
            bool partnerHolds, int diff, bool wellBehind, bool wellAhead)
        {
            if (partnerHolds)
            {
                return 0;
            }

            int opp = OriginalPlayAi.TeamOf[seat];
            bool ourAuction = opener == seat || opener == partner;
            bool bothNamedSuits = bids[seat] > 0 && bids[seat] < 5
                                  && bids[partner] > 0 && bids[partner] < 5;
            if (score[6] >= 32 && bothNamedSuits && ourAuction)
            {
                return 6;
            }

            if (wellBehind)
            {
                return score[5] >= 40 ? 15 : 0;
            }

            if (score[5] > 55 && !wellAhead && diff < 40)
            {
                return 15;
            }

            // Otherwise weigh up all-trumps, adjusted by who has shown what.
            int v = score[6];
            if (bids[partner] % 10 != 0)
            {
                v += 10;
            }

            if (NamedASuit(bids[opp]) || NamedASuit(bids[opp + 2]))
            {
                v -= 10;               // an opponent is showing a suit of their own
            }

            if (!ourAuction && bids[seat] != 0)
            {
                v -= 7;
            }

            if (score[5] > 32)
            {
                v -= 5;                // our own no-trump hand argues against all-trumps
            }

            return v >= 40 ? 6 : 0;
        }

        /// <summary>@0x478E07 — all-trumps is standing, so nothing can outbid it.</summary>
        private static int OverAllTrumps(
            int[] score, IReadOnlyList<int> bids, int partner, bool partnerHolds, int diff,
            bool wellBehind, bool wellAhead)
        {
            if (partnerHolds)
            {
                return 0;
            }

            if (wellBehind)
            {
                return score[6] >= 40 ? 16 : 0;
            }

            int v = score[6];
            if (NamedASuit(bids[partner]))
            {
                v += 10;
            }

            return v > 55 && !wellAhead && diff < 40 ? 16 : 0;
        }

        /// <summary>@0x478EF2 — a doubled suit contract is standing.</summary>
        private static int OverDoubledSuit(
            int[] score, (int Score, int Contract)[] sorted, IReadOnlyList<int> bids,
            int partner, int opener, int seat, int standing, bool partnerHolds,
            bool wellBehind, bool wellAhead, DelphiRandom rng)
        {
            if (wellBehind || wellAhead || partnerHolds)
            {
                return 0;
            }

            // It is already doubled, so bidding on only makes sense if we do NOT rate their
            // contract — and even then the routine only does it half the time.
            if (score[standing % 10] >= 40 || rng.Next(2) != 0)
            {
                return 0;
            }

            int suit = BestSuitScoring(sorted, standing, 40, fallback: -1);
            if (suit >= 0)
            {
                return suit;
            }

            if (score[5] >= 40)
            {
                return 5;
            }

            int v = score[6];
            if (opener != seat && opener != partner)
            {
                v -= 15;
            }

            return v >= 50 ? 6 : 0;
        }

        /// <summary>@0x4790F4 — doubled no-trumps is standing.</summary>
        private static int OverDoubledNoTrumps(
            int[] score, int seat, IReadOnlyList<int> bids, int partner, int opener,
            bool partnerHolds, bool wellBehind, bool wellAhead, DelphiRandom rng)
        {
            if (wellBehind || wellAhead || partnerHolds)
            {
                return 0;
            }

            if (score[5] >= 40 || rng.Next(2) != 0)
            {
                return 0;
            }

            int v = score[6];
            if (NamedASuit(bids[partner]))
            {
                v += 10;
            }

            if (opener != seat && opener != partner)
            {
                v -= 10;
            }

            return v >= 50 ? 6 : 0;
        }

        /// <summary>Did this seat name a plain suit, rather than passing or bidding no/all-trumps?</summary>
        private static bool NamedASuit(int bid)
        {
            int c = bid % 10;
            return c != 0 && c < 5;
        }

        /// <summary>
        /// Walk the sorted table for the best suit contract that genuinely raises
        /// <paramref name="over"/> and clears <paramref name="bar"/>.
        /// </summary>
        private static int BestSuitScoring(
            (int Score, int Contract)[] sorted, int over, int bar, int fallback)
        {
            for (int i = 1; i <= 6; i++)
            {
                (int s, int c) = sorted[i];
                if (c >= 5 || LadderRank[c] <= LadderRank[over % 10] || s < bar)
                {
                    continue;
                }

                return c;
            }

            return fallback;
        }

        /// <summary>
        /// The same walk, but qualifying on holding the suit's Jack rather than on a score — the
        /// routine will name any suit whose boss trump is in its hand. Falls through to
        /// all-trumps, which is what it bids when no suit qualifies.
        /// </summary>
        private static int BestSuitHoldingItsJack(
            (int Score, int Contract)[] sorted, int over, bool[] jackOfSuit)
        {
            for (int i = 1; i <= 6; i++)
            {
                (_, int c) = sorted[i];
                if (c >= 5 || LadderRank[c] <= LadderRank[over % 10])
                {
                    continue;
                }

                if (jackOfSuit[ContractSuit[c]])
                {
                    return c;
                }
            }

            return 6;
        }

        /// <summary>
        /// Where a contract sits on the bidding ladder (@0x489D24). Hearts outrank spades here,
        /// which is the opposite of the suit-index order used everywhere else in the binary.
        /// </summary>
        private static readonly int[] LadderRank = { 0, 1, 2, 4, 3, 5, 6 };

        /// <summary>
        /// The six contracts ordered by this seat's score, best first (@0x48BDB0 + seat*48, six
        /// 8-byte records of {int score; byte contract}). Built by the scoring loop at 0x477D28
        /// with a descending selection sort that swaps only on a strict improvement, so equal
        /// scores keep the order they were filled in — contract 1..6. Reproducing that exactly
        /// matters, because the raise cases take the FIRST qualifying entry.
        /// </summary>
        private static (int Score, int Contract)[] SortContracts(int[] score)
        {
            var t = new (int Score, int Contract)[7];
            for (int c = 1; c <= 6; c++)
            {
                t[c] = (score[c], c);
            }

            for (int i = 1; i <= 5; i++)
            {
                for (int j = i + 1; j <= 6; j++)
                {
                    if (t[j].Score > t[i].Score)
                    {
                        (t[i], t[j]) = (t[j], t[i]);
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// The ranks this hand holds a scoring four of a kind in, at most two (@0x48BE74).
        ///
        /// Goes through the announce detector rather than counting four of a rank directly,
        /// because four sevens and four eights are not declarations in Belot and the routine
        /// stores nothing for them. Counting them makes the AI open a suit on a worthless hand.
        /// </summary>
        private static int[] CaretaRanks(IReadOnlyList<Card> hand)
        {
            var found = new int[2];
            int n = 0;
            var allTrumps = new Contract(BidType.AllTrumps, 0, false, false);
            foreach (Announce a in Announces.Detect(hand, 0, allTrumps))
            {
                if (a.Kind == AnnounceKind.Careta && n < 2)
                {
                    found[n++] = (int)a.TopRank;
                }
            }

            return found;
        }

        private static int OrigSuit(Suit s) => s switch
        {
            Suit.Clubs => 0,
            Suit.Diamonds => 1,
            Suit.Spades => 2,
            _ => 3,
        };

        private static BidType ContractOf(int c) => c switch
        {
            1 => BidType.Clubs,
            2 => BidType.Diamonds,
            3 => BidType.Hearts,
            4 => BidType.Spades,
            5 => BidType.NoTrumps,
            _ => BidType.AllTrumps,
        };
    }
}
