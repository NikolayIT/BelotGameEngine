namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;

    /// <summary>
    /// The heuristic player's card play: the rules of the belot.bg academy and of the other sources
    /// in HEURISTIC_PLAYER.md, over what <see cref="CardMemory"/> remembers. Leading, the declarers
    /// draw trumps from the top while the opponents may hold any (the declarer's partner leads a
    /// trump at once, never the nine into the jack), then cash the side masters the opponents
    /// cannot ruff; the defenders cash their aces before they are ruffed, lead trumps only with
    /// four, and lead a singleton to ruff it. A suit the partner asked for (its jack in all trumps,
    /// its ace otherwise, given to this player's trick) is led back at once, and the suits the
    /// partner bid or led are preferred, the ones it threw away avoided. Following, the player
    /// gives points to a trick the partner surely takes (its strongest card of a suit it does not
    /// need, never a trump, never half a belote), covers the partner's card when the opponent after
    /// it may beat it, takes a trick with the cheapest card no opponent after it can beat, in no
    /// trumps keeps its aces and tens off early tricks with few points, and otherwise loses the
    /// cheapest card: from the suit it has nothing in, which tells the partner not to lead it.
    /// </summary>
    internal sealed class HeuristicCardPlay
    {
        private const int NoTrumps = SimTables.NoTrumps;
        private const int AllTrumps = SimTables.AllTrumps;
        private const int Nine = CardMemory.Nine;
        private const int Ten = CardMemory.Ten;
        private const int Jack = CardMemory.Jack;
        private const int Ace = CardMemory.Ace;

        private readonly HeuristicSettings settings;
        private CardMemory m;
        private string rule;

        public HeuristicCardPlay(HeuristicSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>Gets the rule that chose the last card (for the trainer's diagnostics).</summary>
        public string Rule => this.rule;

        /// <summary>The card to play, one of the memory's legal cards.</summary>
        public int Choose(CardMemory memory)
        {
            this.m = memory;
            var legal = memory.Legal;
            if ((legal & (legal - 1)) == 0)
            {
                this.rule = "forced";
                return BitOperations.TrailingZeroCount(legal);
            }

            var card = memory.TrickCards == 0 ? this.Lead() : this.Follow();
            if (card >= 0 && (legal & (1u << card)) != 0)
            {
                return card;
            }

            this.rule += "/fallback";
            return this.Cheapest(legal);
        }

        /// <summary>
        /// What keeping the card is worth for later tricks, in card points: a master wins its own
        /// points and the cards it draws later, as far as it can still be cashed (a plain master
        /// can be ruffed); trumps keep the control of a suit contract; a guarded second card takes
        /// over once the master falls; half a belote scores only while the pair stays together.
        /// </summary>
        public double KeepValue(int card)
        {
            var m = this.m;
            var suit = card >> 3;
            var outstanding = m.Outstanding(suit);
            var trump = m.IsTrump(card);
            var value = trump ? 8.0 + (2 * m.Order(card)) : 0.0;
            if (outstanding != 0)
            {
                var mine = m.Mine(suit);
                var higher = CardMemory.Count(m.Higher(card) & m.Unseen);
                if (higher == 0)
                {
                    var cash = m.Kind < NoTrumps && !trump ? .8 * (1 - m.ChanceOpponentRuffs(suit)) : .8;
                    value += (m.Points(card) + 6) * cash;
                }
                else if (higher == 1 && CardMemory.Count(mine) >= 2 && m.Points(card) >= 10)
                {
                    // A guarded ten (a nine by trump order): it takes over once the master falls.
                    value += .4 * (m.Points(card) + 6);
                }
                else if (higher == 1)
                {
                    // Second to one card: the partner may hold it, or it may fall elsewhere.
                    value += this.settings.UnguardedSecondKeep * (m.Points(card) + 6);
                }
                else if (CardMemory.Count(mine) == 2 && m.Points(card) < 10 && (mine & ~(1u << card) & this.GuardedHonours(suit)) != 0)
                {
                    // The small card that guards it.
                    value += 2;
                }
            }

            if (m.IsBeloteCard(card))
            {
                value += 12;
            }

            return value;
        }

        /// <summary>
        /// The points a card gains or loses through the belote: declared now (a little: the pair
        /// would declare it later too), or lost for good when half the pair is thrown away.
        /// </summary>
        public double BeloteValue(int card) =>
            this.m.DeclaresBelote(card) ? 3 : this.m.IsBeloteCard(card) && !this.Follows(card) ? -20 : 0;

        private static uint Bit(int suit, int type) => CardMemory.Bit(suit, type);

        private int Lead()
        {
            var call = this.CallLead();
            if (call >= 0)
            {
                this.rule = "lead:call";
                return call;
            }

            var sure = this.settings.CashExhausted ? this.ExhaustedLead() : -1;
            if (sure >= 0)
            {
                this.rule = "lead:exhausted";
                return sure;
            }

            if (this.m.Kind == AllTrumps)
            {
                return this.LeadAllTrumps();
            }

            if (this.m.Kind == NoTrumps)
            {
                return this.LeadNoTrumps();
            }

            return this.m.Ours ? this.LeadTrumpsOurs() : this.LeadTrumpsTheirs();
        }

        // The partner gave its jack (all trumps) or ace to this player's trick: it holds the next
        // card of that suit and asks for it ("the clearest signal in Belot"). Lead it back.
        private int CallLead()
        {
            var m = this.m;
            var calls = m.PartnerCalls();
            for (var suit = 0; suit < 4 && calls != 0; suit++)
            {
                var cards = m.Mine(suit);
                if ((calls & (1 << suit)) == 0 || cards == 0 || m.ChanceOpponentRuffs(suit) > this.settings.RuffRiskChance)
                {
                    continue;
                }

                // In a suit contract the declarers draw the opponents' trumps first (the academy).
                if (m.Kind < NoTrumps && m.Ours && m.OpponentTrumps() != 0 && this.HoldsMasterTrump())
                {
                    continue;
                }

                return this.Lowest(cards);
            }

            return -1;
        }

        private int LeadTrumpsOurs()
        {
            var m = this.m;
            var trumps = m.Hand & m.Trumps;
            var opponentTrumps = m.OpponentTrumps();
            if (trumps != 0 && opponentTrumps != 0)
            {
                var top = this.Highest(trumps);
                var mine = CardMemory.Count(trumps);
                var theirs = CardMemory.Count(opponentTrumps);
                if (this.settings.DrawByControl)
                {
                    // The control (the academy): more trumps than the opponents can hold. Draw from
                    // the top, the jack before the nine, so only lower trumps can fall; once the
                    // highest is gone a small trump still pulls theirs.
                    var bothMayHold = m.MayHaveTrumps(m.Left) && m.MayHaveTrumps(m.Right);
                    if (m.IsMaster(top) && (mine >= this.settings.DrawMasterLength || theirs <= mine + this.settings.DrawMasterExcess
                                            || this.SecondMaster(top)
                                            || (bothMayHold && m.TricksPlayed >= this.settings.DrawLoneMasterTrick)))
                    {
                        this.rule = "lead:draw-master";
                        return top;
                    }

                    if (!m.IsMaster(top) && mine >= 2 && m.Declarer == m.Me && (mine >= this.settings.DrawWithoutMasterLength || mine >= theirs))
                    {
                        this.rule = "lead:draw-long";
                        return this.Lowest(trumps);
                    }
                }

                if (m.IsMaster(top) && !this.settings.DrawByControl)
                {
                    // Draw from the top, the jack before the nine: only lower trumps can fall, and
                    // the side suits become safe. The last trump stays while the opponents may hold
                    // two or more: it is the control.
                    if (CardMemory.Count(trumps) >= 2 || CardMemory.Count(opponentTrumps) <= 1)
                    {
                        this.rule = "lead:draw-master";
                        return top;
                    }
                }
                else if (m.Declarer == m.Partner && this.settings.PartnerLeadsTrump)
                {
                    // The declarer's partner leads a trump at the first chance: the highest if it
                    // is the master, never the nine while the jack is out (the declarer would have
                    // to beat it with the jack).
                    if (m.IsMaster(top))
                    {
                        this.rule = "lead:partner-trump";
                        return top;
                    }

                    var choice = trumps;
                    var nine = Bit(m.Kind, Nine);
                    if ((choice & nine) != 0 && (m.Unseen & Bit(m.Kind, Jack)) != 0 && choice != nine)
                    {
                        choice &= ~nine;
                    }

                    this.rule = "lead:partner-trump";
                    return this.Lowest(choice);
                }
                else if (!this.settings.DrawByControl && m.Declarer == m.Me && CardMemory.Count(trumps) >= this.settings.DrawWithoutMasterLength)
                {
                    // Long trumps without the highest: a small trump drives it out.
                    this.rule = "lead:draw-long";
                    return this.Lowest(trumps);
                }
            }

            var master = this.SafeMaster();
            if (master >= 0)
            {
                return master;
            }

            var partnerSuit = this.PartnerSuitLead();
            return partnerSuit >= 0 ? partnerSuit : this.LeadDefault();
        }

        private int LeadTrumpsTheirs()
        {
            var m = this.m;

            // When in doubt, the aces: before the declarers draw trumps and ruff them.
            var master = this.SafeMaster();
            if (master >= 0)
            {
                return master;
            }

            var partnerSuit = this.PartnerSuitLead();
            if (partnerSuit >= 0)
            {
                return partnerSuit;
            }

            // The declared suit only with four trumps: it cuts the declarers' ruffs.
            var trumps = m.Hand & m.Trumps;
            if (CardMemory.Count(trumps) >= this.settings.DefenderTrumpLeadLength && m.OpponentTrumps() != 0)
            {
                var top = this.Highest(trumps);
                this.rule = "lead:defender-trump";
                return m.IsMaster(top) ? top : this.Lowest(trumps);
            }

            // A short suit: the next round of it can be ruffed.
            if (this.settings.DefenderSingletonLead && trumps != 0)
            {
                var singleton = this.SingletonLead();
                if (singleton >= 0)
                {
                    return singleton;
                }
            }

            return this.LeadDefault();
        }

        private int LeadAllTrumps()
        {
            // The masters, the jack first (the partner gives its nine under it). Early on a lone
            // master stays as a stopper and a way back in (the sources: avoid premature cashing).
            var master = this.SafeMaster(this.m.TricksPlayed < this.settings.AllTrumpsPatienceTricks);
            if (master >= 0)
            {
                return master;
            }

            var partnerSuit = this.PartnerSuitLead();
            if (partnerSuit >= 0)
            {
                return partnerSuit;
            }

            var forcing = this.settings.ForcingLeads ? this.ForcingLead() : -1;
            if (forcing >= 0)
            {
                this.rule = "lead:forcing";
                return forcing;
            }

            return this.LeadDefault();
        }

        // All trumps: in a suit headed by the opponents' masters, the lowest card of this player's
        // top block (no unseen card between it and the top). Everybody must beat it if they can,
        // so the masters' holder has to give one up and the block's other cards climb.
        private int ForcingLead()
        {
            var m = this.m;
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if (cards == 0 || m.Outstanding(suit) == 0 || (m.WeakSuits(m.Partner) & (1 << suit)) != 0)
                {
                    continue;
                }

                var top = this.Highest(cards);
                var above = m.Higher(top) & m.Unseen;
                if (above == 0)
                {
                    continue;
                }

                var card = top;
                var block = 1;
                for (var rest = cards & ~(1u << top); rest != 0; rest &= rest - 1)
                {
                    var other = BitOperations.TrailingZeroCount(rest);
                    if ((m.Higher(other) & m.Unseen) == above)
                    {
                        block++;
                        if (m.Order(other) < m.Order(card))
                        {
                            card = other;
                        }
                    }
                }

                var score = (2 * (block - 1)) + CardMemory.Count(cards) - (1.5 * CardMemory.Count(above)) - (m.Points(card) * .1);
                if (block < 2 && CardMemory.Count(cards) < 2)
                {
                    continue;
                }

                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        private int LeadNoTrumps()
        {
            var m = this.m;
            if (this.settings.NoTrumpsLeadMasters && m.TricksPlayed >= this.settings.NoTrumpsCashTrick)
            {
                var master = this.SafeMaster();
                if (master >= 0)
                {
                    return master;
                }
            }

            // Cash a suit only with two masters in it (an ace and ten): the second takes over.
            // Otherwise the aces and tens wait for the last tricks (the academy), and a low card
            // from the longest suit lets the opponents take the tricks with few points.
            var best = -1;
            var bestLength = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if (m.Outstanding(suit) == 0 || CardMemory.Count(cards) < 2)
                {
                    continue;
                }

                var top = this.Highest(cards);
                var second = this.Highest(cards & ~(1u << top));
                if (m.IsMaster(top) && (m.Higher(second) & m.Unseen) == 0 && CardMemory.Count(cards) > bestLength)
                {
                    best = top;
                    bestLength = CardMemory.Count(cards);
                }
            }

            if (best >= 0)
            {
                this.rule = "lead:nt-cash";
                return best;
            }

            var partnerSuit = this.PartnerSuitLead();
            if (partnerSuit >= 0)
            {
                return partnerSuit;
            }

            var sequence = this.settings.NoTrumpsSequenceLead ? this.SequenceLead() : -1;
            if (sequence >= 0)
            {
                this.rule = "lead:sequence";
                return sequence;
            }

            return this.LeadDefault();
        }

        // The top of two touching honours below the outstanding master (the king from king-queen
        // with the ace out, the queen from queen-jack): it drives the master out, and the other
        // honour takes over. No suit that holds this player's own master.
        private int SequenceLead()
        {
            var m = this.m;
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if (CardMemory.Count(cards) < 2 || m.Outstanding(suit) == 0 || (m.WeakSuits(m.Partner) & (1 << suit)) != 0)
                {
                    continue;
                }

                var top = this.Highest(cards);
                var second = this.Highest(cards & ~(1u << top));
                if (m.IsMaster(top) || m.Order(top) - m.Order(second) != 1 || m.Points(top) < 3)
                {
                    continue;
                }

                var score = m.Points(top) + CardMemory.Count(cards);
                if (score > bestScore)
                {
                    best = top;
                    bestScore = score;
                }
            }

            return best;
        }

        // The cards of a suit nobody else holds cannot lose when led (nobody can follow or, with
        // the opponents out of trumps, ruff): the most valuable first. The last trumps stay: they
        // ruff the opponents' suits.
        private int ExhaustedLead()
        {
            var m = this.m;
            var opponentsRuff = m.Kind < NoTrumps && m.OpponentTrumps() != 0;
            var best = -1;
            var bestPoints = 1;
            for (var rest = m.Hand & ~m.Trumps; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                if (m.Outstanding(suit) != 0 || opponentsRuff)
                {
                    continue;
                }

                var points = m.Points(card);
                if (points > bestPoints)
                {
                    best = card;
                    bestPoints = points;
                }
            }

            return best;
        }

        // A master of a plain suit (any suit in no trumps and all trumps) that the opponents are
        // unlikely to ruff and that draws cards of the suit, the most valuable first. A patient
        // player cashes only a master with the next card behind it, from length or in the
        // partner's suit.
        private int SafeMaster(bool patient = false)
        {
            var m = this.m;
            var best = -1;
            var bestScore = double.NegativeInfinity;
            var partnerSuits = m.SuitBids(m.Partner) | m.LedSuits(m.Partner) | m.PartnerCalls();
            for (var rest = m.Hand & ~m.Trumps; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                if (!m.IsMaster(card) || m.Outstanding(suit) == 0)
                {
                    continue;
                }

                if (patient && (card & 7) == Jack && CardMemory.Count(m.Mine(suit)) < 3 && (partnerSuits & (1 << suit)) == 0
                    && !this.SecondMaster(card))
                {
                    continue;
                }

                var ruff = m.ChanceOpponentRuffs(suit);
                if (ruff > this.settings.RuffRiskChance)
                {
                    continue;
                }

                var score = m.Points(card) - (ruff * 30) + (CardMemory.Count(m.Mine(suit)) * .5);
                if ((m.WeakSuits(m.Partner) & (1 << suit)) != 0)
                {
                    score += 1;
                }

                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            this.rule = "lead:master";
            return best;
        }

        // The suit the partner bid or led (not one it threw away): its master if this player has
        // it, otherwise its lowest card for the partner's honours.
        private int PartnerSuitLead()
        {
            var m = this.m;
            var suits = (m.SuitBids(m.Partner) | m.LedSuits(m.Partner)) & ~m.WeakSuits(m.Partner);
            if (m.Kind < NoTrumps)
            {
                suits &= ~(1 << m.Kind);
            }

            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if ((suits & (1 << suit)) == 0 || cards == 0 || m.Outstanding(suit) == 0)
                {
                    continue;
                }

                var ruff = m.ChanceOpponentRuffs(suit);
                if (ruff > this.settings.RuffRiskChance)
                {
                    continue;
                }

                var top = this.Highest(cards);
                var card = m.IsMaster(top) ? top : this.Lowest(cards);
                var score = ((m.SuitBids(m.Partner) & (1 << suit)) != 0 ? 2 : 0) - m.Points(card) - (ruff * 20);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            this.rule = "lead:partner-suit";
            return best;
        }

        private int SingletonLead()
        {
            var m = this.m;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if (suit == m.Kind || CardMemory.Count(cards) != 1 || CardMemory.Count(m.Outstanding(suit)) < 3)
                {
                    continue;
                }

                var card = BitOperations.TrailingZeroCount(cards);
                if (m.Points(card) < 10 && (m.WeakSuits(m.Partner) & (1 << suit)) == 0)
                {
                    this.rule = "lead:singleton";
                    return card;
                }
            }

            return -1;
        }

        // Nothing better to lead: the cheapest card of the suit that costs least to give up.
        private int LeadDefault()
        {
            var m = this.m;
            var partnerWeak = m.WeakSuits(m.Partner);
            var partnerInterest = m.SuitBids(m.Partner) | m.LedSuits(m.Partner);
            var opponentBids = m.SuitBids(m.Left) | m.SuitBids(m.Right);
            var plainOnly = m.Kind < NoTrumps && (m.Hand & ~m.Trumps) != 0;
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = m.Mine(suit);
                if (cards == 0 || (plainOnly && suit == m.Kind))
                {
                    continue;
                }

                var card = this.Lowest(cards);
                var score = -m.Points(card) - this.KeepValue(card);
                if ((partnerWeak & (1 << suit)) != 0)
                {
                    score -= 4;
                }

                if ((partnerInterest & (1 << suit)) != 0)
                {
                    score += 3;
                }

                if ((opponentBids & (1 << suit)) != 0)
                {
                    score -= 2;
                }

                if (m.Kind < NoTrumps && suit != m.Kind)
                {
                    score -= 12 * m.ChanceOpponentRuffs(suit);
                }

                if (m.Kind >= NoTrumps)
                {
                    score += CardMemory.Count(cards) * (m.Kind == NoTrumps ? this.settings.NoTrumpsLengthLead : this.settings.AllTrumpsLengthLead);
                }

                if (m.Kind == AllTrumps && cards == Bit(suit, Nine) && (m.Unseen & Bit(suit, Jack)) != 0)
                {
                    // Never a bare nine into the jack.
                    score -= 6;
                }

                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            this.rule = "lead:default";
            return best >= 0 ? best : this.Cheapest(m.Legal);
        }

        private int Follow()
        {
            var m = this.m;
            var legal = m.Legal;
            var row = ((m.Kind * 4) + m.LedSuit) * 32;
            var winning = legal & SimTables.BeatMasks[row + m.WinnerCard];
            var last = m.TrickCards == 3;

            if (winning != 0 && (legal & ~winning) == 0)
            {
                // Every legal card beats the trick so far (all trumps, trumps led, a ruff): the
                // trick is this player's to keep or lose.
                this.rule = "follow:forced";
                return this.Forced(legal, last);
            }

            var unblock = this.settings.UnblockPartner ? this.Unblock(legal) : -1;
            if (unblock >= 0)
            {
                this.rule = "follow:unblock";
                return unblock;
            }

            if (this.settings.FollowByValue)
            {
                this.rule = "follow:value";
                return this.FollowByValue(legal, winning, last);
            }

            if (m.PartnerWinning)
            {
                if (last || !m.OpponentMayBeat(m.WinnerCard))
                {
                    this.rule = "follow:smear";
                    return this.Smear(legal);
                }

                var chance = m.ChanceOpponentBeats(m.WinnerCard);
                var cover = winning != 0 ? this.SureWinners(winning) : 0;
                if (cover != 0 && chance >= this.settings.CoverChance)
                {
                    this.rule = "follow:cover";
                    return this.CheapestWinner(cover);
                }

                this.rule = "follow:risky";
                return this.Risky(legal, chance);
            }

            if (winning != 0)
            {
                var sure = last ? winning : this.SureWinners(winning);
                if (sure != 0)
                {
                    var card = this.CheapestWinner(sure);
                    if (this.WorthTaking(card))
                    {
                        this.rule = "follow:take-sure";
                        return card;
                    }
                }
                else
                {
                    var card = this.LikelyWinner(winning);
                    if (card >= 0 && m.ChanceOpponentBeats(card) <= this.settings.TakeRiskChance && this.WorthTaking(card))
                    {
                        this.rule = "follow:take-likely";
                        return card;
                    }
                }
            }

            this.rule = (legal & SimTables.SuitMasks[m.LedSuit]) != 0 ? "follow:lose" : "follow:discard";
            return this.LoseCheaply(legal);
        }

        // The partner led the master of its suit (the jack in all trumps, the ace in no trumps):
        // give it the next card (the nine, the ten) when another card of the suit stays, so the
        // suit runs from the partner's hand and the points go to its trick.
        private int Unblock(uint legal)
        {
            var m = this.m;
            if (m.Kind < NoTrumps || m.TrickCards == 0 || m.WinnerSeat != m.Partner)
            {
                return -1;
            }

            var led = m.LedSuit;
            var top = m.Kind == AllTrumps ? Jack : Ace;
            var next = m.Kind == AllTrumps ? Nine : Ten;
            if (!this.LedBy(m.Partner) || m.WinnerCard != CardMemory.CardOf(led, top))
            {
                return -1;
            }

            var mine = m.Mine(led);
            var nextBit = Bit(led, next);
            return (legal & nextBit) != 0 && CardMemory.Count(mine) >= 2 ? CardMemory.CardOf(led, next) : -1;
        }

        // Whether the seat led the current trick.
        private bool LedBy(int seat) => ((this.m.Me - this.m.TrickCards + 4) & 3) == seat;

        // Every card by what it does to this trick and costs later: the chance the team takes the
        // trick with it (a card that beats the trick so far against the opponents still to play;
        // otherwise the card holding it, or the partner still to play), times the trick's points
        // taken or given, minus what keeping the card is worth; the belote and the discard
        // convention (nothing in that suit, never the partner's) as with the rules.
        private int FollowByValue(uint legal, uint winning, bool last)
        {
            var m = this.m;
            var following = (legal & SimTables.SuitMasks[m.LedSuit]) != 0;
            var holds = m.PartnerWinning ? (last ? 1 : 1 - m.ChanceOpponentBeats(m.WinnerCard))
                : m.TrickCards == 1 ? .8 * m.ChanceBeat(m.Partner, m.WinnerCard) : 0;
            var rest = (3 - m.TrickCards) * this.settings.FollowRestPoints;
            var partnerWants = m.PartnerCalls() | m.LedSuits(m.Partner) | m.SuitBids(m.Partner);
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var cards = legal; cards != 0; cards &= cards - 1)
            {
                var card = BitOperations.TrailingZeroCount(cards);
                var chance = (winning & (1u << card)) != 0 ? (last ? 1 : 1 - m.ChanceOpponentBeats(card)) : holds;
                var stake = m.TrickPoints + m.Points(card) + rest;
                var score = (((2 * chance) - 1) * stake) - this.KeepValue(card) + this.BeloteValue(card) - (m.Order(card) * .01);
                if (!following && !m.IsTrump(card))
                {
                    var suit = card >> 3;
                    score += !this.HasValue(suit) ? 2 : 0;
                    score -= (partnerWants & (1 << suit)) != 0 ? 3 : 0;
                    score += m.Kind < NoTrumps && CardMemory.Count(m.Mine(suit)) == 1 && (m.Hand & m.Trumps) != 0 ? 1.5 : 0;
                }

                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // Every legal card beats the trick so far: a card no opponent after can beat, keeping the
        // most valuable ones, or else the cheapest.
        private int Forced(uint legal, bool last)
        {
            var m = this.m;
            if (this.settings.ForcedByValue)
            {
                // The trick's points (and a few from the cards still to come) go to whoever keeps
                // it: a card that may be beaten risks them, a high one costs what it is worth kept.
                var choice = -1;
                var choiceScore = double.NegativeInfinity;
                for (var rest = legal; rest != 0; rest &= rest - 1)
                {
                    var card = BitOperations.TrailingZeroCount(rest);
                    var lose = last ? 0 : m.ChanceOpponentBeats(card);
                    var stake = m.TrickPoints + m.Points(card) + (last ? 0 : 4);
                    var score = ((1 - (2 * lose)) * stake) - this.KeepValue(card) + this.BeloteValue(card) - (m.Order(card) * .01);
                    if (score > choiceScore)
                    {
                        choice = card;
                        choiceScore = score;
                    }
                }

                return choice;
            }

            var sure = last ? legal : this.SureWinners(legal);
            if (sure == 0)
            {
                var likely = this.LikelyWinner(legal);
                return likely >= 0 && m.ChanceOpponentBeats(likely) <= this.settings.TakeRiskChance ? likely : this.Cheapest(legal);
            }

            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var rest = sure; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = (m.PartnerWinning ? m.Points(card) : 0) - this.KeepValue(card) + this.BeloteValue(card)
                            - (m.Order(card) * .01);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // The partner surely takes the trick: give it points, keeping what is worth more later.
        private int Smear(uint legal)
        {
            var m = this.m;
            var following = (legal & SimTables.SuitMasks[m.LedSuit]) != 0;
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var keep = this.KeepValue(card);
                if (!following && this.settings.CallSignals && this.IsCall(card))
                {
                    // The jack (all trumps) or ace with the next card behind it: its points now,
                    // the next card takes over, and the partner reads the call.
                    keep = -2;
                }

                var score = m.Points(card) - (this.settings.SmearKeepFactor * keep) + this.BeloteValue(card) - (m.Order(card) * .01);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // The partner holds the trick but may lose it: points only as far as the chance allows.
        private int Risky(uint legal, double chance)
        {
            var m = this.m;
            if ((legal & SimTables.SuitMasks[m.LedSuit]) == 0 && chance > .5)
            {
                return this.Discard(legal);
            }

            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = ((1 - (2 * chance)) * m.Points(card)) - this.KeepValue(card) + this.BeloteValue(card) - (m.Order(card) * .01);

                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // The trick is lost (or not worth taking): the card that costs least.
        private int LoseCheaply(uint legal)
        {
            var m = this.m;
            if ((legal & SimTables.SuitMasks[m.LedSuit]) == 0)
            {
                return this.Discard(legal);
            }

            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = -m.Points(card) - this.KeepValue(card) + this.BeloteValue(card) - (m.Order(card) * .01);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // Neither following nor ruffing: throw from the suit with nothing in it, the signal the
        // partner reads, and never from its suit or half a belote.
        private int Discard(uint legal)
        {
            var m = this.m;
            var plain = legal & ~m.Trumps;
            var pool = plain != 0 ? plain : legal;
            var partnerWants = m.PartnerCalls() | m.LedSuits(m.Partner) | m.SuitBids(m.Partner);
            var best = -1;
            var bestScore = double.NegativeInfinity;
            for (var rest = pool; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suit = card >> 3;
                var mine = m.Mine(suit);
                var score = -m.Points(card) - this.KeepValue(card) - (m.Order(card) * .01);
                if (!this.HasValue(suit))
                {
                    score += 2;
                }

                if ((partnerWants & (1 << suit)) != 0)
                {
                    score -= 3;
                }

                if (m.Kind < NoTrumps && CardMemory.Count(mine) == 1 && (m.Hand & m.Trumps) != 0)
                {
                    score += 1.5;
                }

                score += this.BeloteValue(card);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        private bool WorthTaking(int card)
        {
            var m = this.m;
            if (m.TricksPlayed == 7 || m.Kind != NoTrumps)
            {
                return true;
            }

            // No trumps: an ace or ten does not take a trick with few points early (the academy),
            // unless the partner led the suit: then it unblocks it (the blog).
            var type = card & 7;
            if (this.settings.UnblockTakes && this.LedBy(m.Partner) && CardMemory.Count(m.Mine(card >> 3)) <= 2)
            {
                return true;
            }

            return !((type == Ace || type == Ten) && m.TricksPlayed < this.settings.NoTrumpsSaveTricks
                     && m.TrickPoints < this.settings.NoTrumpsSavePoints && !this.LastOfSuit(card));
        }

        // Whether the card follows the suit led (a lead counts as following).
        private bool Follows(int card) => this.m.TrickCards == 0 || (card >> 3) == this.m.LedSuit;

        // Whether the card is this player's last of its suit with others still out (it cannot wait).
        private bool LastOfSuit(int card) => this.m.Mine(card >> 3) == 1u << card;

        // The cards no opponent still to play can beat.
        private uint SureWinners(uint cards)
        {
            var sure = 0u;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (!this.m.OpponentMayBeat(card))
                {
                    sure |= 1u << card;
                }
            }

            return sure;
        }

        // Of cards that take the trick, the one worth least kept.
        private int CheapestWinner(uint cards)
        {
            var best = -1;
            var bestScore = double.MaxValue;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = this.KeepValue(card) + (this.m.Order(card) * .01) - this.BeloteValue(card);
                if (score < bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        // Of cards that take the trick so far, the one least likely to be beaten.
        private int LikelyWinner(uint cards)
        {
            var best = -1;
            var bestScore = double.MaxValue;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = this.m.ChanceOpponentBeats(card) + (this.KeepValue(card) * .001);
                if (score < bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        private int Cheapest(uint cards)
        {
            var m = this.m;
            var best = -1;
            var bestScore = double.MaxValue;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = m.Points(card) + this.KeepValue(card) + (m.Order(card) * .01);
                if (score < bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        private int Highest(uint cards)
        {
            var best = -1;
            var bestOrder = -1;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var order = this.m.Order(card);
                if (order > bestOrder)
                {
                    best = card;
                    bestOrder = order;
                }
            }

            return best;
        }

        private int Lowest(uint cards)
        {
            var best = -1;
            var bestOrder = int.MaxValue;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var order = this.m.Order(card);
                if (order < bestOrder)
                {
                    best = card;
                    bestOrder = order;
                }
            }

            return best;
        }

        // Whether the next card of the suit below this master is this player's too (the jack and nine).
        private bool SecondMaster(int card)
        {
            var m = this.m;
            var below = m.Mine(card >> 3) & ~(1u << card);
            return below != 0 && (m.Higher(this.Highest(below)) & m.Unseen) == 0;
        }

        private bool HoldsMasterTrump()
        {
            var trumps = this.m.Hand & this.m.Trumps;
            return trumps != 0 && this.m.IsMaster(this.Highest(trumps));
        }

        // Whether giving the card asks for its suit: the jack in all trumps, the ace otherwise, with
        // the next card of the suit behind it (the nine, the ten).
        private bool IsCall(int card)
        {
            var m = this.m;
            var suit = card >> 3;
            var type = card & 7;
            if (m.IsTrump(card) || m.Outstanding(suit) == 0)
            {
                return false;
            }

            if (m.Kind == AllTrumps)
            {
                return type == Jack && (m.Hand & Bit(suit, Nine)) != 0;
            }

            return type == Ace && (m.Hand & Bit(suit, Ten)) != 0;
        }

        // Whether the suit holds something worth keeping: a master, or a guarded honour.
        private bool HasValue(int suit)
        {
            var m = this.m;
            var mine = m.Mine(suit);
            for (var rest = mine; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (m.IsMaster(card) && m.Outstanding(suit) != 0)
                {
                    return true;
                }
            }

            return (mine & this.GuardedHonours(suit)) != 0 && CardMemory.Count(mine) >= 2;
        }

        // The honours of the suit by its order: the ace and ten, by trump order the jack and nine.
        private uint GuardedHonours(int suit) =>
            this.m.TrumpOrdered(suit) ? Bit(suit, Jack) | Bit(suit, Nine) : Bit(suit, Ace) | Bit(suit, Ten);
    }
}
