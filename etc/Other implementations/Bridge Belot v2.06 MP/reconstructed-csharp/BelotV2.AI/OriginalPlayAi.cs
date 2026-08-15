namespace BelotV2
{
    /// <summary>
    /// Transcription of the original card-play routine
    /// <c>TMainBelotForm_player2BeforePlay</c> @0x46F5C0.
    ///
    /// Written as a near-literal port of the decompiled code — same arrays, same counters, same
    /// iteration orders — and validated by comparing its intermediate state against the real
    /// routine's stack frame captured under emulation (see tools/). Every stage matches on every
    /// position tested, so this is the original AI rather than an imitation of it; the reason to
    /// keep it literal is that the next person to change it can diff it again.
    ///
    /// Everything uses the BINARY's own encodings so the index arithmetic matches:
    ///   suit    0=Clubs 1=Diamonds 2=Spades 3=Hearts (4 = none)
    ///   rank    7..14   (J=11 Q=12 K=13 A=14)
    ///   player  1..4    (1=South 2=East 3=North 4=West), partners {1,3} and {2,4}
    ///   contract 1..6 = Clubs, Diamonds, Hearts, Spades, No-trumps, All-trumps
    ///
    /// The routine is the handler for the computer seats 2..4; seat 1 is the human in the game.
    /// </summary>
    public sealed partial class OriginalPlayAi
    {
        public const int NoSuit = 4;
        public const int Void = 15;      // the "cannot follow" sentinel used by the lookahead

        // Rank strength, indexed rank-7 (7,8,9,10,J,Q,K,A). Not the enum order: in trumps the
        // Jack and the Nine jump to the top (8 and 7), which is why every comparison in the
        // routine goes through one of these tables rather than comparing ranks.
        internal static readonly int[] TrumpOrderTable = { 1, 2, 7, 5, 8, 3, 4, 6 };   // @0x489D7C
        internal static readonly int[] NoTrumpOrderTable = { 1, 2, 3, 7, 4, 5, 6, 8 }; // @0x489D84
        public static readonly int[] PartnerOf = { 0, 3, 4, 1, 2 };                    // @0x489D1B

        // ---------------- inputs ----------------
        public int Contract;
        public int Trump = NoSuit;
        public int Me;
        public int Declarer = 1;
        public int LedSuit = NoSuit;
        public readonly int[] SlotSuit = new int[8];
        public readonly int[] SlotRank = new int[8];
        public readonly bool[] SlotPresent = new bool[8];
        public readonly bool[] SlotLegal = new bool[8];
        public readonly int[] TableSuit = new int[4];
        public readonly int[] TableRank = new int[4];
        public readonly int[] TableOwner = new int[4];
        /// <summary>[player 1..4][suit 0..3][rank-7] — "this player may still hold that card".</summary>
        public readonly bool[,,] Possible = new bool[5, 4, 8];

        // The memory the game carries across the tricks of a round, at 0x48BEB8..0x48BF17. The
        // routine indexes it with *overlapping* bases (0x48BEC1[team], 0x48BEC9[team] and so on),
        // so it is stored as one flat block addressed absolutely and the named arrays below are
        // views onto it. See PlayMemory for what fills it in.
        public const int MemBase = 0x48BEB8;
        public readonly byte[] Mem = PlayMemory.NewRound();

        /// <summary>
        /// @0x489D20 — the team of a seat, but note the values: seat 1 is team 2 and seat 2 is
        /// team 1. That is deliberate. The routine uses <c>TeamOf[me]</c> and
        /// <c>TeamOf[me] + 2</c> as *player* indices into the per-seat memory arrays, and those
        /// two indices are exactly the seats of the opposing pair — {2,4} for seats 1 and 3,
        /// {1,3} for seats 2 and 4. So wherever this is used as an index, read it as "my
        /// opponents", not "my team".
        /// </summary>
        public static readonly int[] TeamOf = { 0, 2, 1, 2, 1 };

        public MemSlots MemA => new(this.Mem, 0x48BEB7 - MemBase);  // player 1..4, unused
        public MemSlots MemB => new(this.Mem, 0x48BEBF - MemBase);  // suit each player bid
        public MemSlots MemC => new(this.Mem, 0x48BEC3 - MemBase);  // suit a player opened
        public MemSlots MemD => new(this.Mem, 0x48BEC7 - MemBase);  // suit a player signalled
        public MemSlots MemE => new(this.Mem, 0x48BECB - MemBase);  // player is running short
        public MemSlots MemF => new(this.Mem, 0x48BED0 - MemBase);  // suit 0..3 has been led

        /// <summary>
        /// @0x48BEBE — the contract's doubling multiplier: 1 plain, 2 doubled, 4 redoubled
        /// (set at 0x4799B6 when the contract settles). Not a flag: leaving it 0, as a harness
        /// that never runs an auction will, disables a branch that fires on ordinary hands.
        /// </summary>
        public int MemBE
        {
            get => this.Mem[0x48BEBE - MemBase];
            set => this.Mem[0x48BEBE - MemBase] = (byte)value;
        }

        public int MemAt(int absoluteAddress) => this.Mem[absoluteAddress - MemBase];

        /// <summary>The game's own RNG — the routine breaks a few ties with Random().</summary>
        public DelphiRandom Rng = new(1);

        /// <summary>How many times the decision tree drew from <see cref="Rng"/> (diagnostics).</summary>
        public int RngDraws;

        /// <summary>Delphi Random(range), counted so a trace can be lined up with the original.</summary>
        internal int Draw(int range)
        {
            this.RngDraws++;
            return this.Rng.Next(range);
        }

        // ---------------- working state (mirrors the routine's locals) ----------------
        public int CandCount;                                  // uStack_24
        public readonly int[] Candidates = new int[9];         // auStack_210, 1-based
        public int Trump48 = NoSuit;                           // bStack_48
        public int LongSide;                                   // bStack_47
        public bool AnyOppTrump;                               // cStack_85
        public int TrumpRanksOut;                              // iStack_70
        public readonly int[] SuitCount = new int[4];          // auStack_2D8[0..3]
        public readonly int[] HighTrumpOf = new int[5];        // auStack_2B0[5..8], per player
        public readonly int[] SureList = new int[9];           // auStack_120[0x10 + n], 1-based
        public readonly int[] MiddleList = new int[9];         // auStack_120[8 + n]
        public readonly int[] LoserList = new int[9];          // auStack_120[n]
        public int SureCount, MiddleCount, LoserCount;         // uStack_78 / 7C / 80
        public readonly int[] SureBySuit = new int[4];         // aiStack_2E8
        public int PlayersLeft;                                // uStack_34 : OTHERS yet to play
        public readonly int[] Remaining = new int[3];          // auStack_2C8[0..2]
        public int WinnerPlayer;                               // owner of the winning table card
        public int WinnerSlot = -1;

        // The bucket sorts use ONE order table chosen up-front (not per-card strength):
        // trump order for all-trumps, or for a suit contract when the trump suit was led and the
        // first candidate is a trump; otherwise the no-trump order. Set at 0x4715F0.
        private int[] sortTable = NoTrumpOrderTable;

        private readonly int[] minStrengthOfSuit = new int[4];   // auStack_2F8
        private readonly int[] beatBudgetOfSuit = new int[4];    // auStack_308
        private readonly bool[] middleInSuit = new bool[4];      // uStack_84 flags

        /// <summary>Strength under the current contract: trump table iff all-trumps or the
        /// card's suit is trump. This is what the game caches at 0x48BAED.</summary>
        public int Strength(int suit, int rank)
            => (this.Contract == 6 || suit == this.Trump48)
                ? TrumpOrderTable[rank - 7] : NoTrumpOrderTable[rank - 7];

        public int CardsInHand()
        {
            int n = 0;
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i])
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>FUN_00476BAC — my highest card of a suit, or -1.</summary>
        public int HighestOfSuit(int suit)
        {
            int best = -1, bestStrength = 0;
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i] && this.SlotSuit[i] == suit)
                {
                    int s = this.Strength(suit, this.SlotRank[i]);
                    if (s > bestStrength)
                    {
                        bestStrength = s;
                        best = i;
                    }
                }
            }

            return best;
        }

        /// <summary>FUN_00476CD0 — my lowest card of a suit, or -1.</summary>
        public int LowestOfSuit(int suit)
        {
            int best = -1, bestStrength = 100;
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i] && this.SlotSuit[i] == suit)
                {
                    int s = this.Strength(suit, this.SlotRank[i]);
                    if (s < bestStrength)
                    {
                        bestStrength = s;
                        best = i;
                    }
                }
            }

            return best;
        }

        /// <summary>FUN_00479E58 — slot holding an exact (rank, suit), or -1.</summary>
        public int FindCard(int rank, int suit)
        {
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i] && this.SlotRank[i] == rank && this.SlotSuit[i] == suit)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Prepare all the derived state, exactly as the routine does before deciding.</summary>
        public void Analyse()
        {
            BuildCandidates();
            AnalyseTrumps();
            CountSuits();
            ChooseSortTable();
            Classify();
            ScanTrick();
            Lookahead();
        }

        /// <summary>
        /// Picks the single rank-order table every bucket sort will use, at 0x4715F0. Worth
        /// noting because it is chosen ONCE per decision, from the trick as a whole — it is not
        /// "use trump order for trump cards". In a suit contract a heart and a trump in the same
        /// list are therefore compared on the same table, which is only correct because of how
        /// the lists are built.
        /// </summary>
        internal void ChooseSortTable()
        {
            if (this.Contract == 6)
            {
                this.sortTable = TrumpOrderTable;
            }
            else if (this.Contract == 5)
            {
                this.sortTable = NoTrumpOrderTable;
            }
            else if (this.LedSuit == this.Trump48 && this.Trump48 != NoSuit && this.CandCount >= 1
                     && this.SlotSuit[this.Candidates[1]] == this.Trump48)
            {
                this.sortTable = TrumpOrderTable;
            }
            else
            {
                this.sortTable = NoTrumpOrderTable;
            }
        }

        internal int SortStrength(int slot) => this.sortTable[this.SlotRank[slot] - 7];

        private void BuildCandidates()
        {
            this.CandCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i] && this.SlotLegal[i])
                {
                    this.Candidates[++this.CandCount] = i;
                }
            }
        }

        // bStack_48 / cStack_85 / iStack_70 / auStack_2B0
        private void AnalyseTrumps()
        {
            this.Trump48 = NoSuit;
            this.AnyOppTrump = false;
            this.TrumpRanksOut = 0;
            Array.Clear(this.HighTrumpOf);
            if (this.Contract >= 5)
            {
                return;
            }

            this.Trump48 = this.Trump;
            for (int p = 1; p <= 4; p++)
            {
                int best = 0;
                for (int r = 7; r <= 14; r++)
                {
                    if (this.Possible[p, this.Trump48, r - 7] && TrumpOrderTable[r - 7] > best)
                    {
                        best = TrumpOrderTable[r - 7];
                        this.HighTrumpOf[p] = r;
                    }
                }

                if (p != this.Me && PartnerOf[this.Me] != p && this.HighTrumpOf[p] != 0)
                {
                    this.AnyOppTrump = true;
                }
            }

            for (int r = 7; r <= 14; r++)
            {
                bool any = false;
                for (int p = 1; p <= 4; p++)
                {
                    if (p != this.Me && this.Possible[p, this.Trump48, r - 7])
                    {
                        any = true;
                    }
                }

                if (any)
                {
                    this.TrumpRanksOut++;
                }
            }
        }

        private void CountSuits()
        {
            Array.Clear(this.SuitCount);
            for (int i = 0; i < 8; i++)
            {
                if (this.SlotPresent[i])
                {
                    this.SuitCount[this.SlotSuit[i]]++;
                }
            }

            int max = 0;
            this.LongSide = 0;
            for (int s = 0; s < 4; s++)
            {
                if (s != this.Trump48 && this.SuitCount[s] > max)
                {
                    max = this.SuitCount[s];
                    this.LongSide = s;
                }
            }
        }

        /// <summary>
        /// Split the candidates into sure / middle / loser.
        ///
        /// For a candidate C of suit S:
        ///   myLower = how many of MY cards of S are weaker than C
        ///   beaters = how many ranks of S at least as strong as C a non-partner opponent might
        ///             still hold, plus one if a card already on the table beats C
        ///   beaters == 0            -> sure winner
        ///   myLower &lt; beaters     -> loser
        ///   otherwise               -> middle
        /// Middle cards then record, per suit, the beater count of the weakest of them; that
        /// many losers of the same suit are afterwards promoted into middle (the routine reasons
        /// that those low cards are covered).
        /// </summary>
        private void Classify()
        {
            this.SureCount = this.MiddleCount = this.LoserCount = 0;
            Array.Clear(this.SureBySuit);
            Array.Clear(this.middleInSuit);
            for (int s = 0; s < 4; s++)
            {
                this.minStrengthOfSuit[s] = 10;      // auStack_2F8 is initialised to 10
                this.beatBudgetOfSuit[s] = 0;
            }

            for (int n = 1; n <= this.CandCount; n++)
            {
                int slot = this.Candidates[n];
                int suit = this.SlotSuit[slot];
                int rank = this.SlotRank[slot];
                int strength = this.Strength(suit, rank);

                int myLower = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (this.SlotPresent[i] && this.SlotSuit[i] == suit
                        && this.Strength(suit, this.SlotRank[i]) < strength)
                    {
                        myLower++;
                    }
                }

                int beaters = 0;
                for (int r = 7; r <= 14; r++)
                {
                    if (this.Strength(suit, r) < strength)
                    {
                        continue;
                    }

                    bool any = false;
                    for (int p = 1; p <= 4; p++)
                    {
                        if (p != this.Me && p != PartnerOf[this.Me] && this.Possible[p, suit, r - 7])
                        {
                            any = true;
                        }
                    }

                    if (any)
                    {
                        beaters++;
                    }
                }

                if (suit == this.LedSuit)
                {
                    bool beatenOnTable = false;
                    for (int t = 0; t < 4; t++)
                    {
                        if (this.TableOwner[t] != 0 && this.TableSuit[t] == suit
                            && strength < this.Strength(suit, this.TableRank[t]))
                        {
                            beatenOnTable = true;
                        }
                    }

                    if (beatenOnTable)
                    {
                        beaters++;
                    }
                }

                if (beaters == 0)
                {
                    this.SureList[++this.SureCount] = slot;
                    this.SureBySuit[suit]++;
                }
                else if (myLower < beaters)
                {
                    this.LoserList[++this.LoserCount] = slot;
                }
                else
                {
                    this.MiddleList[++this.MiddleCount] = slot;
                    if (strength < this.minStrengthOfSuit[suit])
                    {
                        this.minStrengthOfSuit[suit] = strength;
                        this.beatBudgetOfSuit[suit] = beaters;
                    }

                    this.middleInSuit[suit] = true;
                }
            }

            SortWeakestFirst(this.LoserList, this.LoserCount);
            PromoteCoveredLosers();
            SortWeakestFirst(this.MiddleList, this.MiddleCount);
            SortWeakestFirst(this.SureList, this.SureCount);
        }

        // Losers of a suit that already has a middle card are promoted, weakest last, while the
        // suit's beater budget lasts.
        private void PromoteCoveredLosers()
        {
            var keep = new List<int>();
            for (int i = this.LoserCount; i >= 1; i--)
            {
                int slot = this.LoserList[i];
                int suit = this.SlotSuit[slot];
                if (this.middleInSuit[suit] && this.beatBudgetOfSuit[suit] > 0)
                {
                    this.MiddleList[++this.MiddleCount] = slot;
                    this.beatBudgetOfSuit[suit]--;
                }
                else
                {
                    keep.Add(slot);
                }
            }

            keep.Reverse();
            this.LoserCount = keep.Count;
            for (int i = 0; i < keep.Count; i++)
            {
                this.LoserList[i + 1] = keep[i];
            }
        }

        internal void SortWeakestFirst(int[] list, int count)
        {
            for (int i = 1; i < count; i++)
            {
                for (int j = i + 1; j <= count; j++)
                {
                    int a = list[j], b = list[i];
                    if (SortStrength(a) < SortStrength(b))
                    {
                        list[i] = a;
                        list[j] = b;
                    }
                }
            }
        }

        // uStack_34 counts the OTHER players still to play (the routine puts its own candidate on
        // the table first): 3 = leading, 2 = second, 1 = third, 0 = last.
        private void ScanTrick()
        {
            this.PlayersLeft = 0;
            Array.Clear(this.Remaining);
            for (int p = 1; p <= 4; p++)
            {
                if (p == this.Me)
                {
                    continue;
                }

                bool played = false;
                for (int t = 0; t < 4; t++)
                {
                    if (this.TableOwner[t] == p)
                    {
                        played = true;
                    }
                }

                if (!played)
                {
                    this.Remaining[this.PlayersLeft++] = p;
                }
            }

            this.WinnerSlot = -1;
            this.WinnerPlayer = 0;
            int bestTrump = 0, bestLed = 0;
            for (int t = 0; t < 4; t++)
            {
                if (this.TableOwner[t] == 0)
                {
                    continue;
                }

                int s = this.TableSuit[t];
                int strength = this.Strength(s, this.TableRank[t]);
                if (this.Contract < 5 && s == this.Trump48)
                {
                    if (strength > bestTrump)
                    {
                        bestTrump = strength;
                        this.WinnerSlot = t;
                    }
                }
                else if (bestTrump == 0 && s == this.LedSuit && strength > bestLed)
                {
                    bestLed = strength;
                    this.WinnerSlot = t;
                }
            }

            if (this.WinnerSlot >= 0)
            {
                this.WinnerPlayer = this.TableOwner[this.WinnerSlot];
            }
        }
    }
}
