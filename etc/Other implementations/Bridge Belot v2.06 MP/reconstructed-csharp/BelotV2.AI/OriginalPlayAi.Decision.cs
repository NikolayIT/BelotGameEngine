namespace BelotV2
{
    /// <summary>
    /// The decision tree of the original card-play routine: given the analysis and the five
    /// lookahead lists, which card actually gets played.
    ///
    /// Structure follows the original, which keys almost everything on how many players are
    /// still to act after me (<see cref="PlayersLeft"/>): 3 = I am leading, 2 = second,
    /// 1 = third, 0 = last.
    ///
    /// Two things about this tree are easy to get wrong and were only found by diffing against
    /// the binary:
    ///
    ///   * It reads the five lookahead lists <em>after</em> they have been sorted weakest-first
    ///     (see <c>Lookahead</c>), so "the first entry" always means the weakest card, never the
    ///     first candidate found.
    ///   * Several branches break ties with <c>Random</c>. Those are only reproducible against
    ///     the original if the RNG starts from the same seed, which is why the golden vectors
    ///     record it.
    /// </summary>
    public sealed partial class OriginalPlayAi
    {
        /// <summary>Which branch of the tree produced the last decision (diagnostics only).</summary>
        public string LastBranch = "?";

        /// <summary>
        /// Set by the arms that store their answer and jump straight to the common tail, skipping
        /// the "do not open with a trump" adjustment that the rest of the mixed branch goes
        /// through. Leading the partner's own suit is deliberate even when that suit is trump.
        /// </summary>
        private bool exitsDirectly;

        /// <summary>Runs the whole routine and returns the chosen hand slot, or -1.</summary>
        public int Decide()
        {
            Analyse();
            if (this.CandCount == 0)
            {
                return -1;
            }

            int chosen = ChooseCard();
            // The original leaves its answer in a variable it initialised to -1, and every path
            // through the tree writes to it before the common exit, so this fallback does not
            // fire on any position measured against the binary. It is kept because a caller that
            // hands the routine a state it could never reach in a real game (an empty candidate
            // list, say) should still get a legal card rather than -1.
            if (chosen < 0)
            {
                chosen = this.Candidates[1];
            }

            return PostAdjust(chosen);
        }

        private int WinnerCardSuit => this.WinnerSlot >= 0 ? this.TableSuit[this.WinnerSlot] : NoSuit;

        private int WinnerCardRank => this.WinnerSlot >= 0 ? this.TableRank[this.WinnerSlot] : 0;

        private int ChooseCard()
        {
            // ---- third to play: feed a partner who is winning with an unbeatable card ----
            if (this.PlayersLeft == 1 && FeedWinningPartner(out int fed))
            {
                this.LastBranch = "feed-partner";
                return fed;
            }

            return General();
        }

        /// <summary>
        /// With one opponent still to act and the partner holding the trick with a card nothing
        /// can beat, throw them the fattest card of that suit: the Nine under a Jack in
        /// all-trumps, or the Ten (else the King) under an Ace in no-trumps.
        /// </summary>
        private bool FeedWinningPartner(out int slot)
        {
            slot = -1;
            if (this.SuitCount[this.LongSide] > 3)
            {
                return false;
            }

            if (PartnerOf[this.Me] != this.WinnerPlayer || CardsInHand() < 7)
            {
                return false;
            }

            int suit = this.WinnerCardSuit;
            if (this.MemC[this.Me] == suit || this.MemB[this.Me] == suit)
            {
                return false;
            }

            int rank = this.WinnerCardRank;
            int found;
            if (this.Contract == 6 && rank == 11 && (found = FindCard(9, suit)) != -1)
            {
                slot = found;
                return true;
            }

            if (this.Contract == 6 && rank == 11 && (found = FindCard(14, suit)) != -1)
            {
                slot = found;
                return true;
            }

            if (this.Contract == 5 && rank == 14 && (found = FindCard(10, suit)) != -1)
            {
                slot = found;
                return true;
            }

            if (this.Contract == 5 && rank == 14 && (found = FindCard(13, suit)) != -1)
            {
                slot = found;
                return true;
            }

            return false;
        }

        // Everything that is not the "feed the partner" special case.
        private int General()
        {
            int pick;

            // Down to two cards, both certain winners, void in the led suit and no opponent
            // trump left: keep the one whose suit the others are less likely to hold.
            if (TwoSureCardsEndgame(out pick))
            {
                this.LastBranch = "two-sure-endgame";
                return pick;
            }

            if (this.PlayersLeft == 3)
            {
                // Leading: if the partner is remembered as having a suit, lead its lowest.
                int partnerSuit = this.MemD[PartnerOf[this.Me]];
                if (partnerSuit != NoSuit && this.SuitCount[partnerSuit] > 0)
                {
                    this.LastBranch = "lead-partner-memD";
                    return LowestOfSuit(partnerSuit);
                }
            }

            if (this.PlayersLeft == 3 && this.AnyOppTrump && LeadWithTrumps(out pick))
            {
                this.LastBranch = "lead-with-trumps";
                return pick;
            }

            // Leading with no opponent trump left, early in the hand: cash a certain winner from
            // a suit we are long in (or where we hold more than one certainty).
            if (this.PlayersLeft == 3 && !this.AnyOppTrump && CashWinnerWhileLeading(out pick))
            {
                this.LastBranch = "lead-cash";
                return pick;
            }

            // ---- main dispatch on the lookahead lists ----
            if (this.AlwaysWinsCount < 1 && this.AlwaysWinsPartnerCount < 1)
            {
                bool keepTrumpWinner = this.AlwaysWinsTrumpCount >= 1
                                       && this.PlayersLeft <= 2
                                       && this.LedSuit == this.Trump48;
                if (keepTrumpWinner)
                {
                    // a trump that always wins, and trumps were led: pick one at random
                    this.LastBranch = "L1-random";
                    return this.AlwaysWinsTrump[this.Draw(this.AlwaysWinsTrumpCount) + 1];
                }

                if (this.MixedCount < 1)
                {
                    this.LastBranch = "no-mixed";
                    return NothingMixed();
                }

                this.exitsDirectly = false;
                if (MixedBranch(out pick))
                {
                    this.LastBranch += "|mixed";
                    return this.exitsDirectly ? pick : AvoidLeadingTrump(pick);
                }

                this.LastBranch = "UNHANDLED-mixed";
                return -1;
            }

            // ---- there are candidates that always win ----
            if (this.AlwaysWinsCount < 1)
            {
                // only "partner takes it" winners
                if (this.PlayersLeft == 3)
                {
                    int first = this.AlwaysWinsPartner[1];
                    this.LastBranch = this.SlotSuit[first] != this.Trump48 ? "L2-lead" : "UNHANDLED-L2";
                    return this.SlotSuit[first] != this.Trump48 ? first : -1;
                }

                if (this.LoserCount < 1)
                {
                    this.LastBranch = "L2-cheap";
                    return this.MiddleCount < 1 ? this.SureList[1] : this.MiddleList[1];
                }

                this.LastBranch = "L2-loser-top";
                return this.LoserList[this.LoserCount];      // the strongest loser
            }

            if (this.PlayersLeft == 3)
            {
                this.LastBranch = "L3-random-lead";
                return this.AlwaysWins[this.Draw(this.AlwaysWinsCount) + 1];
            }

            this.LastBranch = "L3-pick";
            return this.AnyOppTrump
                ? this.AlwaysWins[this.AlwaysWinsCount]      // strongest, trumps still out
                : this.AlwaysWins[1];                        // weakest is enough
        }

        /// <summary>
        /// Two adjustments the routine makes to whatever the tree picked:
        /// don't spend a trump when a cheap discard exists, and don't throw away the fat Ten
        /// (no-trumps) or Nine (all-trumps) if a weaker card of a long suit would do.
        /// </summary>
        /// <summary>
        /// Two overrides the routine applies to whatever the tree picked, after the fact
        /// (0x473BC2 onwards). They are not part of the tree and cannot be folded into it: they
        /// second-guess every branch alike.
        ///
        ///   1. Do not spend a trump when following if a side card would do — unless the only
        ///      side card available is an Ace and the partner is not already winning.
        ///   2. If nothing at all is going our way and the pick is a fat card (the Ten in
        ///      no-trumps, the Nine in all-trumps), throw a cheaper loser instead.
        /// </summary>
        private int PostAdjust(int pick)
        {
            if (pick < 0)
            {
                return pick;
            }

            if (this.Contract < 5 && this.SlotSuit[pick] == this.Trump48 && this.PlayersLeft < 3)
            {
                int best = 10, found = -1;
                for (int i = 1; i <= this.CandCount; i++)
                {
                    int s = this.Candidates[i];
                    if (this.SlotSuit[s] == this.Trump48)
                    {
                        continue;
                    }

                    int v = NoTrumpOrderTable[this.SlotRank[s] - 7];
                    if (v < best)
                    {
                        best = v;
                        found = s;
                    }
                }

                // ...but do not throw an Ace away just to save a trump
                bool aceGuard = this.AlwaysWinsPartnerCount < 1 && found >= 0
                                && this.SlotRank[found] == 14;
                if (found >= 0 && !aceGuard)
                {
                    pick = found;
                }
            }

            bool fatCard = (this.Contract == 5 && this.SlotRank[pick] == 10)
                           || (this.Contract == 6 && this.SlotRank[pick] == 9);
            if (this.AlwaysWinsCount == 0 && this.AlwaysWinsPartnerCount == 0
                && this.MixedCount == 0 && fatCard)
            {
                for (int i = 1; i <= this.NeverWinsCount; i++)
                {
                    int s = this.NeverWins[i];
                    bool alsoMiddle = false;
                    for (int j = 1; j <= this.MiddleCount; j++)
                    {
                        if (this.MiddleList[j] == s)
                        {
                            alsoMiddle = true;
                        }
                    }

                    if (!alsoMiddle || this.SuitCount[this.SlotSuit[s]] <= 2)
                    {
                        continue;
                    }

                    if (SortStrength(s) < SortStrength(pick))
                    {
                        return s;
                    }
                }
            }

            return pick;
        }

        /// <summary>No mixed candidates: take the cheapest thing available, and if that would
        /// mean leading a trump, look for any non-trump instead.</summary>
        private int NothingMixed()
        {
            if (this.NeverWinsCount < 1)
            {
                return this.AlwaysWinsTrump[1];
            }

            int pick = this.LoserCount >= 1
                ? this.LoserList[1]
                : (this.MiddleCount >= 1 ? this.MiddleList[1] : this.SureList[1]);

            if (this.PlayersLeft == 3 && this.SlotSuit[pick] == this.Trump48)
            {
                for (int i = 1; i <= this.MiddleCount; i++)
                {
                    if (this.SlotSuit[this.MiddleList[i]] != this.Trump48)
                    {
                        return this.MiddleList[i];
                    }
                }

                for (int i = 1; i <= this.LoserCount; i++)
                {
                    if (this.SlotSuit[this.LoserList[i]] != this.Trump48)
                    {
                        return this.LoserList[i];
                    }
                }

                for (int i = 1; i <= this.SureCount; i++)
                {
                    if (this.SlotSuit[this.SureList[i]] != this.Trump48)
                    {
                        return this.SureList[i];
                    }
                }
            }

            return pick;
        }

        private bool MixedBranch(out int slot)
        {
            slot = -1;
            if (this.AnyOppTrump)
            {
                this.LastBranch = "mx-opptrump";
                return MixedWhileOpponentsHoldTrumps(out slot);
            }

            int partner = PartnerOf[this.Me];
            if (this.PlayersLeft == 3)
            {
                // lead the partner's remembered suits, weakest card first
                int suitC = this.MemC[partner];
                if (suitC != this.Trump48 && suitC != NoSuit)
                {
                    int low = LowestOfSuit(suitC);
                    if (low >= 0)
                    {
                        this.LastBranch = "mx-memC";
                        this.exitsDirectly = true;
                        slot = low;
                        return true;
                    }
                }

                int suitB = this.MemB[partner];
                if (suitB != NoSuit)
                {
                    int low = LowestOfSuit(suitB);
                    if (low >= 0)
                    {
                        this.LastBranch = "mx-memB";
                        this.exitsDirectly = true;
                        slot = low;
                        return true;
                    }
                }
            }

            if (this.PlayersLeft < 3)
            {
                // Not leading: among the mixed candidates prefer one that is also a loser (and
                // failing that, one that is also a middle card), taking the one from the longest
                // suit — spend a card from where you are richest.
                this.LastBranch = "mx-follow";
                slot = PickMixedFrom(this.LoserList, this.LoserCount);
                if (slot < 0)
                {
                    slot = PickMixedFrom(this.MiddleList, this.MiddleCount);
                }

                if (slot < 0)
                {
                    slot = this.Mixed[1];
                }

                return true;
            }

            this.LastBranch = "mx-suitscore";
            return LeadBySuitScore(out slot);
        }

        /// <summary>
        /// Leading with mixed candidates: score every suit and lead the weakest card of the best
        /// one. The score rewards suits the player has already established (via the per-player
        /// memory of who bid or showed what) and penalises a suit by hand-size × length when it
        /// holds a "middle" card — i.e. prefer to open where you are long and expendable. Ties
        /// are broken with the game's RNG.
        /// </summary>
        private bool LeadBySuitScore(out int slot)
        {
            slot = -1;

            // TeamOf is {2,1,2,1}, so `team` and `team + 2` index this player's two OPPONENTS in
            // the per-player memory rows — that is how the routine reaches them.
            int team = TeamOf[this.Me];
            int partner = PartnerOf[this.Me];

            // Score every suit for how bad it would be to open, then open the least bad one that
            // we actually hold a mixed candidate in. The weights are the routine's own; the
            // sign convention is what makes them readable: everything that says "the opponents
            // want this suit" adds, everything that says "the opponents are out of it"
            // subtracts, and the minimum wins.
            var score = new int[4];
            for (int s = 0; s < 4; s++)
            {
                // A high score means "do not lead this suit".
                if (this.MemAt(0x48BEC7 + team) == s || this.MemAt(0x48BEC9 + team) == s)
                {
                    score[s] += 200;      // an opponent signalled it
                }

                if (this.MemAt(0x48BEBF + team) == s || this.MemAt(0x48BEC1 + team) == s)
                {
                    score[s] += 15;       // an opponent bid it
                }

                if (this.MemAt(0x48BEC3 + team) == s || this.MemAt(0x48BEC5 + team) == s)
                {
                    score[s] += 10;       // an opponent opened it
                }

                // The partner has been throwing this suit away, so leading it hands the trick to
                // the opponents. Note the weights are not monotone — two discards is the
                // strongest signal, three slightly less — and that is what the binary does.
                switch (DiscardCount(partner, s))
                {
                    case 1: score[s] += 10; break;
                    case 2: score[s] += 20; break;
                    case 3: score[s] += 15; break;
                }

                int oppA = DiscardCount(team, s);
                int oppB = DiscardCount(team + 2, s);
                if (oppA >= 2 && oppB >= 2)
                {
                    score[s] -= 20;       // both opponents are long gone in it
                }
                else if (oppA > 0 && oppB > 0)
                {
                    score[s] -= 4;
                }

                // Holding a middling card in a suit we are long in makes it a good suit to
                // establish, and the longer the better — hence the product rather than a flat
                // bonus. This is the only term that scales, so in a quiet auction it is usually
                // the one that decides.
                if (this.middleInSuit[s])
                {
                    score[s] -= CardsInHand() * this.SuitCount[s];
                }

                // The routine also adds a constant here (+100, or +15) when it is short of cards
                // in a no-trump contract and its partner is short too. It does not depend on the
                // suit, so it shifts every score equally and can never change which suit wins.
            }

            int best = 1000, count = 0;
            var picks = new int[9];
            for (int s = 0; s < 4; s++)
            {
                if (score[s] > best)
                {
                    continue;
                }

                bool hasMixed = false;
                for (int i = 1; i <= this.MixedCount; i++)
                {
                    if (this.SlotSuit[this.Mixed[i]] == s)
                    {
                        hasMixed = true;
                    }
                }

                if (!hasMixed)
                {
                    continue;
                }

                count = score[s] < best ? 1 : count + 1;
                if (count < picks.Length)
                {
                    picks[count] = LowestOfSuit(s);
                }

                best = score[s];
            }

            if (count == 0)
            {
                return false;
            }

            slot = picks[this.Draw(count) + 1];
            return slot >= 0;
        }

        /// <summary>
        /// Mixed candidates while the opponents still hold trumps: cash an Ace of the led suit,
        /// unload from a long led suit, otherwise play a non-trump mixed card from a suit as long
        /// as the longest side suit.
        /// </summary>
        private bool MixedWhileOpponentsHoldTrumps(out int slot)
        {
            slot = -1;
            if (this.PlayersLeft < 3 && this.LedSuit != this.Trump48)
            {
                for (int i = 1; i <= this.MixedCount; i++)
                {
                    int s = this.Mixed[i];
                    if (this.SlotSuit[s] == this.LedSuit && this.SlotRank[s] == 14)
                    {
                        slot = s;
                        return true;
                    }
                }

                if (this.SuitCount[this.LedSuit] > 2)
                {
                    for (int i = 1; i <= this.MixedCount; i++)
                    {
                        if (this.SlotSuit[this.Mixed[i]] == this.LedSuit)
                        {
                            slot = LowestOfSuit(this.LedSuit);
                            return true;
                        }
                    }
                }
            }

            for (int i = 1; i <= this.MixedCount; i++)
            {
                int s = this.Mixed[i];
                if (this.SlotSuit[s] != this.Trump48
                    && this.SuitCount[this.SlotSuit[s]] == this.SuitCount[this.LongSide])
                {
                    slot = s;
                    return true;
                }
            }

            slot = this.SlotSuit[this.Mixed[1]] == this.Trump48
                ? this.LowestTrumpInMixed
                : this.Mixed[1];
            return slot >= 0;
        }

        /// <summary>When leading, never lay down a trump if a non-trump is available: the routine
        /// re-picks from middle, then loser, then sure.</summary>
        private int AvoidLeadingTrump(int pick)
        {
            if (this.PlayersLeft != 3 || pick < 0 || this.SlotSuit[pick] != this.Trump48)
            {
                return pick;
            }

            for (int i = 1; i <= this.MiddleCount; i++)
            {
                if (this.SlotSuit[this.MiddleList[i]] != this.Trump48)
                {
                    return this.MiddleList[i];
                }
            }

            for (int i = 1; i <= this.LoserCount; i++)
            {
                if (this.SlotSuit[this.LoserList[i]] != this.Trump48)
                {
                    return this.LoserList[i];
                }
            }

            for (int i = 1; i <= this.SureCount; i++)
            {
                if (this.SlotSuit[this.SureList[i]] != this.Trump48)
                {
                    return this.SureList[i];
                }
            }

            return pick;
        }

        // The mixed candidate that also appears in `other`, from the longest suit.
        private int PickMixedFrom(int[] other, int otherCount)
        {
            int best = 0, found = -1;
            for (int i = 1; i <= this.MixedCount; i++)
            {
                for (int j = 1; j <= otherCount; j++)
                {
                    if (this.Mixed[i] != other[j])
                    {
                        continue;
                    }

                    int len = this.SuitCount[this.SlotSuit[this.Mixed[i]]];
                    if (best < len)
                    {
                        best = len;
                        found = this.Mixed[i];
                    }
                }
            }

            return found;
        }

        private bool TwoSureCardsEndgame(out int slot)
        {
            slot = -1;
            if (this.AnyOppTrump || CardsInHand() != 2 || this.PlayersLeft >= 3 || this.SureCount != 2)
            {
                return false;
            }

            if (this.LedSuit == NoSuit || this.SuitCount[this.LedSuit] != 0)
            {
                return false;
            }

            int first = this.SureList[1], second = this.SureList[2];
            if (this.SlotRank[first] != this.SlotRank[second])
            {
                return false;
            }

            if (this.Trump48 != NoSuit && this.SuitCount[this.Trump48] != 0)
            {
                return false;
            }

            // How many of the two suits might still be out there. Note the routine filters on the
            // PREVIOUS candidate's win tally here — stale state, reproduced as-is.
            int firstSuitOut = 0, secondSuitOut = 0;
            for (int p = 1; p <= 4; p++)
            {
                if (this.LastWins[p] == 0)
                {
                    continue;
                }

                for (int r = 7; r <= 14; r++)
                {
                    if (this.Possible[p, this.SlotSuit[first], r - 7])
                    {
                        firstSuitOut++;
                    }

                    if (this.Possible[p, this.SlotSuit[second], r - 7])
                    {
                        secondSuitOut++;
                    }
                }
            }

            if (secondSuitOut < firstSuitOut)
            {
                slot = first;
            }
            else if (firstSuitOut < secondSuitOut)
            {
                slot = second;
            }
            else
            {
                slot = this.SureList[this.Draw(2) + 1];
            }

            return true;
        }

        /// <summary>
        /// Leading early with the opponents out of trumps: run down the certain winners from the
        /// strongest, taking one whose suit we are long in, hold more than one certainty in, or
        /// have already established.
        /// </summary>
        private bool CashWinnerWhileLeading(out int slot)
        {
            slot = -1;

            // Only early in the hand: 7 cards left, or 6 in no-trumps. Later the routine stops
            // cashing and switches to the endgame branches instead.
            int minHand = this.Contract == 5 ? 6 : 7;
            if (CardsInHand() < minHand)
            {
                return false;
            }

            // Strongest certain winner first. It is only worth cashing if the suit is one we can
            // keep coming back to: already broached and we still hold two, or we are simply long
            // in it, or we hold more than one certainty there.
            for (int i = this.AlwaysWinsCount; i >= 1; i--)
            {
                int s = this.AlwaysWins[i];
                int suit = this.SlotSuit[s];
                bool take = (this.MemF[suit] == 1 && this.SuitCount[suit] >= 2)
                            || this.SuitCount[suit] > 3
                            || this.SureBySuit[suit] > 1;
                if (take)
                {
                    slot = s;
                    return true;
                }
            }

            int partner = PartnerOf[this.Me];
            if (this.MemB[partner] != NoSuit)
            {
                int low = LowestOfSuit(this.MemB[partner]);
                if (low >= 0)
                {
                    slot = low;
                    return true;
                }
            }

            if (this.MemC[partner] != this.Trump48 && this.MemC[partner] != NoSuit)
            {
                int low = LowestOfSuit(this.MemC[partner]);
                if (low >= 0)
                {
                    slot = low;
                    return true;
                }
            }

            // Open the longest side suit from the bottom, provided it is genuinely long (or the
            // contract is all-trumps) and we hold a mixed card there that is also a middle card.
            for (int i = 1; i <= this.MixedCount; i++)
            {
                for (int j = 1; j <= this.MiddleCount; j++)
                {
                    if (this.Mixed[i] != this.MiddleList[j])
                    {
                        continue;
                    }

                    int suit = this.SlotSuit[this.Mixed[i]];
                    if (suit == this.Trump48
                        || this.SuitCount[suit] != this.SuitCount[this.LongSide])
                    {
                        continue;
                    }

                    if (this.SuitCount[this.LongSide] > 2 || this.Contract == 6)
                    {
                        slot = LowestOfSuit(suit);
                        return slot >= 0;
                    }
                }
            }

            // Failing all that, open with a middle card (else a loser) from a side suit the
            // opponents have not shown any interest in and that our partner has not been seen
            // discarding.
            for (int i = 1; i <= this.MiddleCount; i++)
            {
                if (SafeSuitToOpen(this.MiddleList[i], false))
                {
                    slot = this.MiddleList[i];
                    return true;
                }
            }

            for (int i = 1; i <= this.LoserCount; i++)
            {
                if (SafeSuitToOpen(this.LoserList[i], true))
                {
                    slot = this.LoserList[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is this card's suit clean to open — not trump, not one the opposing team has bid or
        /// shown, and not one our partner has already been seen throwing away?
        /// </summary>
        /// <summary>
        /// "Is this a quiet suit to open?" — one the opponents have shown no interest in and the
        /// partner has not been discarding.
        ///
        /// The address arithmetic looks strange because the original reads the per-seat memory
        /// arrays at two bases each, <c>base[team]</c> and <c>base[team + 2]</c>, which lands on
        /// the two opposing seats (see <see cref="TeamOf"/>). So each pair of tests below is
        /// really "did either opponent bid / open / signal this suit".
        /// </summary>
        /// <param name="loserVariant">
        /// The routine runs this test twice, over middle cards and then over losers, and the two
        /// passes differ in exactly one condition: opening into a suit an opponent bid is only
        /// disqualifying for a loser in all-trumps.
        /// </param>
        private bool SafeSuitToOpen(int card, bool loserVariant)
        {
            int suit = this.SlotSuit[card];
            if (suit == this.Trump48)
            {
                return false;
            }

            int team = TeamOf[this.Me];
            if (suit == this.MemAt(0x48BEC3 + team) || suit == this.MemAt(0x48BEC5 + team)
                || suit == this.MemAt(0x48BEBF + team))
            {
                return false;
            }

            // the loser variant only rejects this one outside all-trumps
            bool bec1 = suit == this.MemAt(0x48BEC1 + team);
            if (loserVariant ? (bec1 && this.Contract == 6) : bec1)
            {
                return false;
            }

            if (suit == this.MemAt(0x48BEC7 + team) || suit == this.MemAt(0x48BEC9 + team))
            {
                return false;
            }

            return DiscardCount(PartnerOf[this.Me], suit) == 0;
        }

        /// <summary>
        /// How often a seat has thrown a card of that suit away rather than following, from the
        /// 4x4 grid of int32s at 0x48BED4. Kept as raw addressing because the base the original
        /// computes from is 0x48BEC4, one player-row below the grid — reading it as
        /// <c>0x48BED4 + (player - 1) * 16</c> gives the same answer only for players 1..4, and
        /// getting the size of this grid wrong is not a loud failure: it silently feeds the
        /// suit-scoring heuristic stale bytes.
        /// </summary>
        private int DiscardCount(int player, int suit)
        {
            int address = 0x48BEC4 + (suit * 4) + (player * 0x10);
            int index = address - MemBase;
            return this.Mem[index] | (this.Mem[index + 1] << 8)
                   | (this.Mem[index + 2] << 16) | (this.Mem[index + 3] << 24);
        }

        /// <summary>Leading while opponents still hold trumps: draw or duck them.</summary>
        private bool LeadWithTrumps(out int slot)
        {
            slot = -1;
            int trump = this.Trump48;
            if (this.SureBySuit[trump] > 0)
            {
                bool holdBack = this.SuitCount[trump] < this.TrumpRanksOut
                                && this.SuitCount[trump] < 3
                                && this.SureBySuit[trump] < 2;
                if (!holdBack)
                {
                    slot = HighestOfSuit(trump);      // draw trumps with the best one
                    return true;
                }
            }

            // Duck a small trump: the declaring side does it once the suit is established, and
            // anybody does it while holding more trumps than are still out. Note the first test
            // falls through to the second when it fails.
            bool declaring = this.Me == this.Declarer || PartnerOf[this.Me] == this.Declarer;
            bool playLowTrump = (declaring && this.MemF[trump] == 0 && this.MemBE == 1)
                                || this.TrumpRanksOut < this.SuitCount[trump];
            if (playLowTrump)
            {
                int low = LowestOfSuit(trump);
                if (low >= 0)
                {
                    slot = low;
                    return true;
                }
            }

            // Otherwise cash a side Ace that is certain to hold and is also a live candidate.
            for (int i = 1; i <= this.SureCount; i++)
            {
                int s = this.SureList[i];
                if (this.SlotSuit[s] == trump || this.SlotRank[s] != 14)
                {
                    continue;
                }

                for (int j = 1; j <= this.MixedCount; j++)
                {
                    if (this.Mixed[j] == s)
                    {
                        slot = s;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
