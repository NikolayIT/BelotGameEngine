namespace BelotV2
{
    /// <summary>
    /// The heart of the original card-play AI: a one-trick lookahead.
    ///
    /// For every candidate card it plays the rest of the trick out in every plausible way. Each
    /// player still to act either follows the suit under consideration with some rank they might
    /// still hold, or is void (sentinel <see cref="OriginalPlayAi.Void"/>) — and a void player
    /// ruffs with their best possible trump when that would actually beat what is already down,
    /// otherwise discards. How many players may be void at once is capped by how thin the suit is.
    ///
    /// Counting who takes the trick across all those continuations gives a win tally per player.
    /// The ratio of team wins to opponent wins then files the candidate into one of five lists,
    /// which is what the decision tree actually chooses from.
    /// </summary>
    public sealed partial class OriginalPlayAi
    {
        /// <summary>Weakest trump among the mixed candidates, or -1 (uStack_74).</summary>
        public int LowestTrumpInMixed = -1;

        // The five candidate lists the lookahead produces, all living in one array in the
        // original (auStack_1D0, at offsets 0, 8, 0x10, 0x18, 0x20). A candidate lands in exactly
        // one of them, decided by the ratio score: +1000 when the opponents never take the trick,
        // -1000 when our side never does, otherwise somewhere in between.
        //
        // All five are sorted WEAKEST FIRST once the lookahead is done, so index 1 is always the
        // cheapest card in the list and index Count the dearest. The decision tree relies on that
        // ordering throughout, and it is the single detail whose absence looked most like a
        // working port: the lists were right, only their order was wrong.
        public readonly int[] AlwaysWinsTrump = new int[9];      // uStack_5C — a trump that wins
        public readonly int[] AlwaysWinsPartner = new int[9];    // uStack_64 — partner takes it
        public readonly int[] AlwaysWins = new int[9];           // uStack_60 — we take it
        public readonly int[] Mixed = new int[9];                // uStack_6C — depends how it goes
        public readonly int[] NeverWins = new int[9];            // uStack_68 — we lose it
        public int AlwaysWinsTrumpCount, AlwaysWinsPartnerCount, AlwaysWinsCount;
        public int MixedCount, NeverWinsCount;

        private readonly int[] simSuitOf = new int[5];   // abStack_1F0 — the simulated trick
        private readonly int[] simRankOf = new int[5];
        private readonly int[] wins = new int[5];        // auStack_2D8[p+6]
        /// <summary>Win tally of the LAST candidate evaluated (the frame dump exposes this).</summary>
        public readonly int[] LastWins = new int[5];

        /// <summary>Per-candidate trace (index, simulated suit, void cap, threat flag, wins).</summary>
        public readonly List<(int Cand, int SimSuit, int VoidCap, bool Threat, int[] Wins)> Trace = new();

        // Per-candidate order table: trump order for all-trumps, or when the card is itself trump.
        private int[] CandidateOrder(int slot)
            => (this.Contract == 6 || this.SlotSuit[slot] == this.Trump48)
                ? TrumpOrderTable : NoTrumpOrderTable;

        private void Lookahead()
        {
            this.AlwaysWinsTrumpCount = this.AlwaysWinsPartnerCount = this.AlwaysWinsCount = 0;
            this.MixedCount = this.NeverWinsCount = 0;
            this.Trace.Clear();

            for (int n = 1; n <= this.CandCount; n++)
            {
                int slot = this.Candidates[n];
                int[] order = CandidateOrder(slot);
                int simSuit = this.LedSuit == NoSuit ? this.SlotSuit[slot] : this.LedSuit;

                // How thinly the suit is spread decides how many players may be treated as void.
                int notHeld = 0;
                for (int r = 7; r <= 14; r++)
                {
                    bool any = false;
                    for (int p = 1; p <= 4; p++)
                    {
                        if (p != this.Me && this.Possible[p, simSuit, r - 7])
                        {
                            any = true;
                        }
                    }

                    if (!any)
                    {
                        notHeld++;
                    }
                }

                int held = 8 - notHeld;
                int inHand = CardsInHand();
                int spread = held % inHand == 0 ? held / inHand : (held / inHand) + 1;
                int voidCap = 3 - spread;

                // Lay the trick out as it stands, with my candidate added.
                for (int p = 1; p <= 4; p++)
                {
                    this.simSuitOf[p] = NoSuit;
                    this.simRankOf[p] = 2;
                    this.wins[p] = 0;
                }

                this.simSuitOf[this.Me] = this.SlotSuit[slot];
                this.simRankOf[this.Me] = this.SlotRank[slot];
                for (int t = 0; t < 4; t++)
                {
                    if (this.TableOwner[t] != 0)
                    {
                        this.simSuitOf[this.TableOwner[t]] = this.TableSuit[t];
                        this.simRankOf[this.TableOwner[t]] = this.TableRank[t];
                    }
                }

                int topTrump = 0;
                if (this.Trump48 != NoSuit)
                {
                    for (int p = 1; p <= 4; p++)
                    {
                        if (this.simSuitOf[p] == this.Trump48)
                        {
                            int st = TrumpOrderTable[this.simRankOf[p] - 7];
                            if (st > topTrump)
                            {
                                topTrump = st;
                            }
                        }
                    }
                }

                bool threatened = IsThreatened(slot, order, simSuit);
                EnumerateContinuations(simSuit, voidCap, topTrump);

                // A trump candidate that somebody still to play might beat does not count as a
                // win for me, however the enumeration turned out.
                if ((this.Contract == 6 || this.SlotSuit[slot] == this.Trump48) && threatened)
                {
                    this.wins[this.Me] = 0;
                }

                Array.Copy(this.wins, this.LastWins, 5);
                this.Trace.Add((n, simSuit, voidCap, threatened, (int[])this.wins.Clone()));
                Bucket(slot);
            }

            // Everything below is the routine's tail, and its order matters: it re-picks the
            // order table, sorts the mixed list, notes the weakest trump in it, and only then
            // sorts the other four. Sorting mixed first is what makes LowestTrumpInMixed
            // deterministic when two trumps tie on the chosen table.
            ChooseSortTable();
            SortWeakestFirst(this.Mixed, this.MixedCount);

            // weakest trump among the mixed candidates
            this.LowestTrumpInMixed = -1;
            int weakest = 10;
            for (int i = 1; i <= this.MixedCount; i++)
            {
                int s2 = this.Mixed[i];
                if (this.SlotSuit[s2] != this.Trump48)
                {
                    continue;
                }

                int st = TrumpOrderTable[this.SlotRank[s2] - 7];
                if (st < weakest)
                {
                    weakest = st;
                    this.LowestTrumpInMixed = s2;
                }
            }

            // The remaining four, in the original's order. Three use the table just chosen; the
            // trump-winners list holds nothing but trumps, so it always compares on trump order
            // regardless of what the trick would suggest.
            SortWeakestFirst(this.NeverWins, this.NeverWinsCount);
            SortWeakestFirst(this.AlwaysWinsPartner, this.AlwaysWinsPartnerCount);
            SortWeakestFirst(this.AlwaysWins, this.AlwaysWinsCount);
            SortByTrumpOrder(this.AlwaysWinsTrump, this.AlwaysWinsTrumpCount);
        }

        private void SortByTrumpOrder(int[] list, int count)
        {
            for (int i = 1; i < count; i++)
            {
                for (int j = i + 1; j <= count; j++)
                {
                    int a = list[j], b = list[i];
                    if (TrumpOrderTable[this.SlotRank[a] - 7] < TrumpOrderTable[this.SlotRank[b] - 7])
                    {
                        list[i] = a;
                        list[j] = b;
                    }
                }
            }
        }

        // Is there a rank at least as strong as my candidate that a player who has NOT yet played
        // might still hold (and that nobody who already played could have held)?
        private bool IsThreatened(int slot, int[] order, int simSuit)
        {
            bool threatened = false;
            for (int r = 7; r <= 14; r++)
            {
                if (order[this.SlotRank[slot] - 7] > order[r - 7])
                {
                    continue;
                }

                bool heldByPlayed = false;
                for (int p = 1; p <= 4; p++)
                {
                    if (p != this.Me && this.simSuitOf[p] != NoSuit && this.Possible[p, simSuit, r - 7])
                    {
                        heldByPlayed = true;
                    }
                }

                if (heldByPlayed)
                {
                    continue;
                }

                for (int p = 1; p <= 4; p++)
                {
                    if (p != this.Me && this.simSuitOf[p] == NoSuit && this.Possible[p, simSuit, r - 7])
                    {
                        threatened = true;
                    }
                }
            }

            return threatened;
        }

        private void EnumerateContinuations(int simSuit, int voidCap, int topTrump)
        {
            for (int a = 7; a <= Void; a++)
            {
                if (this.PlayersLeft >= 1 && a != Void
                    && !this.Possible[this.Remaining[0], simSuit, a - 7])
                {
                    continue;
                }

                for (int b = 7; b <= Void; b++)
                {
                    if (b == a && b != Void)
                    {
                        continue;
                    }

                    if (this.PlayersLeft >= 2 && b != Void
                        && !this.Possible[this.Remaining[1], simSuit, b - 7])
                    {
                        continue;
                    }

                    for (int c = 7; c <= Void; c++)
                    {
                        if ((c == a || c == b) && c != Void)
                        {
                            continue;
                        }

                        if (this.PlayersLeft >= 3 && c != Void
                            && !this.Possible[this.Remaining[2], simSuit, c - 7])
                        {
                            continue;
                        }

                        int voids = (a == Void ? 1 : 0) + (b == Void ? 1 : 0) + (c == Void ? 1 : 0);
                        if (voids > voidCap)
                        {
                            continue;   // too many players assumed void; try the next rank
                        }

                        if (this.PlayersLeft >= 1)
                        {
                            PlaceSimulated(this.Remaining[0], a, simSuit, topTrump);
                        }

                        if (this.PlayersLeft >= 2)
                        {
                            PlaceSimulated(this.Remaining[1], b, simSuit, topTrump);
                        }

                        if (this.PlayersLeft >= 3)
                        {
                            PlaceSimulated(this.Remaining[2], c, simSuit, topTrump);
                        }

                        CountContinuation(simSuit, voids);

                        // the routine only leaves the inner loops once a combination counted
                        if (this.PlayersLeft < 3)
                        {
                            break;
                        }
                    }

                    if (this.PlayersLeft < 2)
                    {
                        break;
                    }
                }

                if (this.PlayersLeft < 1)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Credit one simulated continuation.
        ///
        /// Note the original's own quirk, reproduced here deliberately: in a suit contract, when
        /// somebody was treated as void and the suit being simulated is not trump, then EVERY
        /// player who has a trump in the simulated trick is credited with a win — not just the
        /// highest one — and the trick-winner routine is not consulted at all. Only when nobody
        /// trumped does it fall back to working out who actually takes the trick.
        /// </summary>
        /// <summary>
        /// Credits one simulated continuation to whoever takes it.
        ///
        /// The special case is the original's, not a simplification: in a suit contract, when a
        /// side suit is led and somebody in the simulation is void, EVERY simulated trump holder
        /// is credited with the trick — the routine never asks which of them is highest. Two
        /// players can therefore both "win" the same imagined trick. It skews the tallies toward
        /// trumps, which is presumably the intent, and reproducing it is required: scoring these
        /// continuations properly changes the candidate lists and then the card played.
        /// </summary>
        private void CountContinuation(int simSuit, int voids)
        {
            if (this.Contract < 5 && voids != 0 && simSuit != this.Trump48)
            {
                bool anyTrump = false;
                for (int p = 1; p <= 4; p++)
                {
                    if (this.simSuitOf[p] == this.Trump48)
                    {
                        anyTrump = true;
                        this.wins[p]++;
                    }
                }

                if (!anyTrump)
                {
                    this.wins[SimulatedWinner(simSuit)]++;
                }
            }
            else
            {
                this.wins[SimulatedWinner(simSuit)]++;
            }
        }

        private void PlaceSimulated(int player, int rank, int simSuit, int topTrump)
        {
            if (rank == Void)
            {
                if (this.Contract < 5 && simSuit != this.Trump48 && this.HighTrumpOf[player] != 0
                    && topTrump < TrumpOrderTable[this.HighTrumpOf[player] - 7])
                {
                    this.simSuitOf[player] = this.Trump48;             // ruffs
                    this.simRankOf[player] = this.HighTrumpOf[player];
                }
                else
                {
                    this.simSuitOf[player] = NoSuit;                   // discards
                    this.simRankOf[player] = 2;
                }
            }
            else if (this.Possible[player, simSuit, rank - 7])
            {
                this.simSuitOf[player] = simSuit;
                this.simRankOf[player] = rank;
            }
        }

        /// <summary>TestCards @0x46F49C — who takes the simulated trick.</summary>
        private int SimulatedWinner(int ledSuit)
        {
            int ledWinner = -1, ledBest = 0, trumpWinner = -1, trumpBest = 0;
            for (int p = 1; p <= 4; p++)
            {
                if (this.simSuitOf[p] == NoSuit)
                {
                    continue;
                }

                int strength = this.Strength(this.simSuitOf[p], this.simRankOf[p]);
                if (this.simSuitOf[p] == ledSuit)
                {
                    // cards of the led suit compete on the led-suit ladder
                    if (strength > ledBest)
                    {
                        ledBest = strength;
                        ledWinner = p;
                    }
                }
                else if (this.simSuitOf[p] == this.Trump48 && this.Trump48 != NoSuit
                         && strength > trumpBest)
                {
                    trumpBest = strength;
                    trumpWinner = p;
                }
            }

            return trumpWinner >= 0 ? trumpWinner : (ledWinner >= 0 ? ledWinner : this.Me);
        }

        // team wins vs opponent wins -> a single score -> one of five lists
        private void Bucket(int slot)
        {
            int mine = this.wins[this.Me];
            int team = mine + this.wins[PartnerOf[this.Me]];
            int leftSeat = this.Me - 1;                  // the routine only serves seats 2..4
            int opponents = this.wins[leftSeat] + this.wins[PartnerOf[leftSeat]];

            double score;
            if (team == 0)
            {
                score = -1000.0;
            }
            else if (opponents == 0)
            {
                score = 1000.0;
            }
            else if (opponents < team)
            {
                score = ((float)team / opponents) - 1.0f;
            }
            else if (team < opponents)
            {
                score = ((float)(-opponents) / team) + 1.0f;
            }
            else
            {
                score = 0.0;
            }

            if (score == 1000.0)
            {
                if (this.SlotSuit[slot] == this.Trump48 && mine > 0)
                {
                    this.AlwaysWinsTrump[++this.AlwaysWinsTrumpCount] = slot;
                }
                else if (mine == 0)
                {
                    this.AlwaysWinsPartner[++this.AlwaysWinsPartnerCount] = slot;
                }
                else
                {
                    this.AlwaysWins[++this.AlwaysWinsCount] = slot;
                }
            }
            else if (score == -1000.0)
            {
                this.NeverWins[++this.NeverWinsCount] = slot;
            }
            else
            {
                this.Mixed[++this.MixedCount] = slot;
            }
        }
    }
}
