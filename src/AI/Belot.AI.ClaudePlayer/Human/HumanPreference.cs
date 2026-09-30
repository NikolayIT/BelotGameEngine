namespace Belot.AI.ClaudePlayer.Human
{
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;

    /// <summary>
    /// How natural each legal card is to a strong human player, as a score in card points: the
    /// points the card gives or wins, what keeping it is worth (a master, a guarded card, a trump,
    /// a belote), and the conventions: give points to a partner's sure trick, throw from a suit
    /// with nothing in it, lead the partner's suit and not the one it threw away, draw trumps as
    /// the declarers and do not lead them as the defenders. The search's values decide; this only
    /// chooses among cards the search finds equally good, the way a person would.
    /// </summary>
    internal static class HumanPreference
    {
        private static readonly int[] TrumpOrders = { 1, 2, 7, 5, 8, 3, 4, 6 };
        private static readonly int[] PlainOrders = { 1, 2, 3, 7, 4, 5, 6, 8 };

        // [kind * 32 + card]: the cards of the card's own suit that rank above it in the contract.
        private static readonly uint[] Higher = new uint[6 * 32];

        static HumanPreference()
        {
            for (var kind = 0; kind < 6; kind++)
            {
                for (var card = 0; card < 32; card++)
                {
                    for (var other = card & ~7; other < (card & ~7) + 8; other++)
                    {
                        if (Order(kind, other) > Order(kind, card))
                        {
                            Higher[(kind * 32) + card] |= 1u << other;
                        }
                    }
                }
            }
        }

        /// <summary>The card's rank within its suit in the contract (1 lowest, 8 highest).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Order(int kind, int card) =>
            kind == SimTables.AllTrumps || (card >> 3) == kind ? TrumpOrders[card & 7] : PlainOrders[card & 7];

        /// <summary>The cards of the card's suit ranking above it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint HigherInSuit(int kind, int card) => Higher[(kind * 32) + card];

        /// <summary>
        /// The legal cards a person would never play: in a trick surely lost (third or fourth seat,
        /// the opponents holding it), a card of the suit that loses as well as a lower one of the
        /// same suit with no more points. Throwing the ten under an ace with the seven in hand, or
        /// the ten of trumps under the jack with the king, gives points and a later trick away.
        /// </summary>
        public static uint Dominated(in NeuralDeal deal, uint legal)
        {
            ref readonly var play = ref deal.Play;
            if (play.TrickCards < 2 || ((play.WinnerSeat ^ play.Turn) & 1) == 0)
            {
                return 0;
            }

            var kind = deal.Kind;
            var row = ((kind * 4) + play.LedSuit) * 32;
            var losing = legal & ~SimTables.BeatMasks[row + play.WinnerCard];
            var dominated = 0u;
            for (var rest = losing; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var points = SimTables.Values[(kind * 32) + card];
                for (var others = losing & SimTables.SuitMasks[card >> 3] & ~(1u << card); others != 0; others &= others - 1)
                {
                    var other = BitOperations.TrailingZeroCount(others);
                    if (SimTables.Values[(kind * 32) + other] <= points && Order(kind, other) < Order(kind, card))
                    {
                        dominated |= 1u << card;
                        break;
                    }
                }
            }

            return dominated;
        }

        /// <summary>Fills the score of every legal card of the seat to play (higher is more natural).</summary>
        public static void Score(in NeuralDeal deal, in PlaySignals signals, uint legal, float[] scores)
        {
            var view = new View(in deal, in signals, legal);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                scores[card] = deal.Play.TrickCards == 0 ? view.Lead(card) : view.Follow(card);
            }
        }

        private readonly ref struct View
        {
            private readonly ref readonly NeuralDeal deal;
            private readonly int me;
            private readonly int partner;
            private readonly uint hand;
            private readonly int kind;
            private readonly uint outside;
            private readonly uint trumps;
            private readonly bool ours;
            private readonly int partnerWeak;
            private readonly bool choice;

            public View(in NeuralDeal deal, in PlaySignals signals, uint legal)
            {
                this.choice = (legal & (legal - 1)) != 0;
                this.deal = ref deal;
                this.me = deal.Play.Turn;
                this.partner = (this.me + 2) & 3;
                this.hand = deal.Play.Hands[this.me];
                this.kind = deal.Kind;
                this.outside = ~(this.hand | deal.Played);
                this.trumps = this.kind < SimTables.NoTrumps ? SimTables.SuitMasks[this.kind] : 0;
                this.ours = ((deal.Declarer ^ this.me) & 1) == 0;
                this.partnerWeak = signals.WeakSuits(this.partner);
            }

            public float Lead(int card)
            {
                var suit = card >> 3;
                var suitMask = SimTables.SuitMasks[suit];
                var points = SimTables.Values[(this.kind * 32) + card];
                var trump = (this.trumps & (1u << card)) != 0;
                var master = (HigherInSuit(this.kind, card) & this.outside) == 0;
                var score = 0f;
                if (master && (this.outside & suitMask) != 0)
                {
                    if (trump || this.kind >= SimTables.NoTrumps || this.OpponentsOutOfTrumps())
                    {
                        score += 18 + points;
                    }
                    else
                    {
                        score += this.OpponentMayRuff(suit) ? -6 : 8 + (points * .5f);
                    }
                }
                else
                {
                    score -= points + this.Keep(card);
                }

                if (trump)
                {
                    if (this.ours)
                    {
                        score += 8;

                        // Do not lead the nine of trumps into the declaring partner's jack.
                        if ((card & 7) == 2 && this.deal.Declarer == this.partner && (this.outside & (1u << ((suit * 8) + 4))) != 0)
                        {
                            score -= 16;
                        }
                    }
                    else
                    {
                        score -= 14;
                    }
                }
                else if (this.kind < SimTables.NoTrumps && this.OpponentMayRuff(suit))
                {
                    score -= 10;
                }

                if ((this.partnerWeak & (1 << suit)) != 0)
                {
                    score -= 8;
                }

                if (!trump && (this.deal.BidsMade[this.partner] & (1 << suit)) != 0)
                {
                    score += 6;
                }

                if (this.DeclaresBelote(card, suit))
                {
                    score += 20;
                }

                return score;
            }

            public float Follow(int card)
            {
                ref readonly var play = ref this.deal.Play;
                var bit = 1u << card;
                var suit = card >> 3;
                var suitMask = SimTables.SuitMasks[suit];
                var points = SimTables.Values[(this.kind * 32) + card];
                var row = ((this.kind * 4) + play.LedSuit) * 32;
                var beats = (SimTables.BeatMasks[row + play.WinnerCard] & bit) != 0;
                var last = play.TrickCards == 3;
                var partnerHolds = ((play.WinnerSeat ^ this.me) & 1) == 0;
                float chance;
                if (beats)
                {
                    chance = last ? 1 : this.CanBeBeaten(card, row) ? .45f : .95f;
                }
                else if (partnerHolds)
                {
                    chance = last ? 1 : this.CanBeBeaten(play.WinnerCard, row) ? .5f : .92f;
                }
                else
                {
                    chance = play.TrickCards == 1 ? .35f : 0;
                }

                var stake = play.TrickPoints + points + (last ? 0 : 6);
                var score = (((2 * chance) - 1) * stake) - this.Keep(card);
                if (beats && partnerHolds)
                {
                    score -= 3;
                }

                if (this.DeclaresBelote(card, play.LedSuit))
                {
                    score += 20;
                }

                var followed = suit == play.LedSuit;
                if (!followed && (this.trumps & bit) == 0)
                {
                    // Throw from a suit with nothing in it: that is the signal the partner reads.
                    var mine = this.hand & suitMask;
                    if (!this.HasHonour(mine, suit))
                    {
                        score += 2;
                    }

                    if (this.HasBelote(suit))
                    {
                        score -= 4;
                    }

                    if (this.kind < SimTables.NoTrumps && (mine & (mine - 1)) == 0 && (this.hand & this.trumps) != 0)
                    {
                        score += 1.5f;
                    }
                }

                return score;
            }

            // What keeping the card is worth for later tricks (its own points apart).
            private float Keep(int card)
            {
                var suit = card >> 3;
                var suitMask = SimTables.SuitMasks[suit];
                var trumpSuit = this.kind == SimTables.AllTrumps || suit == this.kind;
                var higher = BitOperations.PopCount(HigherInSuit(this.kind, card) & this.outside);
                var length = BitOperations.PopCount(this.hand & suitMask);
                var order = Order(this.kind, card);
                var keep = order * .05f;
                if (higher == 0)
                {
                    keep += trumpSuit ? 28 : this.kind < SimTables.NoTrumps && this.OpponentMayRuff(suit) ? 10 : 20;
                }
                else if (higher == 1 && length >= 2)
                {
                    keep += trumpSuit ? 12 : 7;
                }
                else if (higher == 2 && length >= 3)
                {
                    keep += trumpSuit ? 6 : 3;
                }

                if ((this.trumps & (1u << card)) != 0)
                {
                    keep += 10 + order;
                }

                var type = card & 7;
                if ((type == SimTables.Queen || type == SimTables.King) && trumpSuit && this.kind != SimTables.NoTrumps
                    && (this.hand & (1u << (card ^ 3))) != 0)
                {
                    // Half the belote: it scores only if the suit is led or followed later.
                    keep += 10;
                }

                return keep;
            }

            // Whether playing the card now declares a belote (the queen or king with the other in
            // hand, of trumps or in all trumps of the suit led, as BelotSimulator.IsBelote).
            private bool DeclaresBelote(int card, int ledSuit)
            {
                var type = card & 7;
                var suit = card >> 3;
                return this.choice && (type == SimTables.Queen || type == SimTables.King) && this.kind != SimTables.NoTrumps
                    && (this.hand & (1u << (card ^ 3))) != 0
                    && suit == (this.kind == SimTables.AllTrumps ? ledSuit : this.kind);
            }

            // Whether the opponent still to play in this trick may beat the card, as far as is known.
            private bool CanBeBeaten(int card, int row)
            {
                ref readonly var play = ref this.deal.Play;
                var opponent = (this.me + 1) & 3;
                var possible = this.outside & ~this.deal.Excluded[opponent];
                var led = SimTables.SuitMasks[play.LedSuit];
                var beaters = SimTables.BeatMasks[row + card] & possible;
                if ((beaters & led) != 0)
                {
                    return true;
                }

                // A trump beats it only from an opponent with none of the suit led.
                return this.kind < SimTables.NoTrumps && play.LedSuit != this.kind && (possible & led) == 0 && (beaters & this.trumps) != 0;
            }

            private bool OpponentMayRuff(int suit)
            {
                if (this.kind >= SimTables.NoTrumps || suit == this.kind)
                {
                    return false;
                }

                var suitMask = SimTables.SuitMasks[suit];
                for (var i = 1; i <= 3; i += 2)
                {
                    var opponent = (this.me + i) & 3;
                    var possible = this.outside & ~this.deal.Excluded[opponent];
                    if ((possible & suitMask) == 0 && (possible & this.trumps) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool OpponentsOutOfTrumps()
            {
                for (var i = 1; i <= 3; i += 2)
                {
                    var opponent = (this.me + i) & 3;
                    if ((this.outside & ~this.deal.Excluded[opponent] & this.trumps) != 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            // An ace or ten in a plain suit, a jack or nine in a trump suit.
            private bool HasHonour(uint cards, int suit)
            {
                var trumpSuit = this.kind == SimTables.AllTrumps || suit == this.kind;
                var honours = trumpSuit ? (1u << ((suit * 8) + 4)) | (1u << ((suit * 8) + 2)) : (1u << ((suit * 8) + 7)) | (1u << ((suit * 8) + 3));
                return (cards & honours) != 0;
            }

            private bool HasBelote(int suit)
            {
                if (this.kind == SimTables.NoTrumps || (this.kind < SimTables.NoTrumps && suit != this.kind))
                {
                    return false;
                }

                var pair = (1u << ((suit * 8) + SimTables.Queen)) | (1u << ((suit * 8) + SimTables.King));
                return (this.hand & pair) == pair;
            }
        }
    }
}
