namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// What a careful player remembers and concludes at a card decision (belot.bg academy, "How
    /// to follow the cards"): every card played and by whom, the suits each player has shown out
    /// of and the higher cards a player has shown not to hold (<see cref="RoundKnowledge"/>), the
    /// trumps still out, which cards are masters, what the bids said and the partner's signals
    /// (<see cref="PlaySignals"/>). The chance that a seat holds an unseen card treats every
    /// placement the play allows alike, except that a seat that bid a suit is taken to hold its
    /// jack and nine more often (<see cref="BidHonourWeight"/>).
    /// </summary>
    internal sealed class CardMemory
    {
        /// <summary>The card types (CardType) as numbers, the low three bits of a card.</summary>
        public const int Seven = 0;

        public const int Eight = 1;

        public const int Nine = 2;

        public const int Ten = 3;

        public const int Jack = 4;

        public const int Queen = 5;

        public const int King = 6;

        public const int Ace = 7;

        private readonly RoundKnowledge knowledge = new RoundKnowledge();
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly uint[] possible = new uint[4];
        private readonly int[] needs = new int[4];
        private readonly int[] suitBids = new int[4];
        private readonly double[] weights = new double[4 * 32];
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private PlaySignals signals;
        private int ourPoints;
        private int theirPoints;

        /// <summary>Gets or sets how much more likely a seat that bid a suit is to hold its jack and nine (1: no more).</summary>
        public double BidHonourWeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets how likely a seat is to hold an honour (the ace and ten; by trump order the
        /// jack and nine) of a suit it threw away while its opponents held the trick, relative to
        /// any other card (1: the discards say nothing; the convention: "what you discard, you don't have").
        /// </summary>
        public double DiscardHonourWeight { get; set; } = 1;

        public BelotSimulator Simulator => this.simulator;

        public RoundKnowledge Knowledge => this.knowledge;

        /// <summary>Gets the contract's kind (see <see cref="SimTables"/>): 0-3 a trump suit, 4 no trumps, 5 all trumps.</summary>
        public int Kind { get; private set; }

        public int Me { get; private set; }

        public int Partner => (this.Me + 2) & 3;

        /// <summary>Gets the seat on the left: it plays right after this player.</summary>
        public int Left => (this.Me + 1) & 3;

        /// <summary>Gets the seat on the right: it plays right before this player.</summary>
        public int Right => (this.Me + 3) & 3;

        public int Declarer { get; private set; }

        /// <summary>Gets a value indicating whether this player's team bid the contract.</summary>
        public bool Ours => ((this.Declarer ^ this.Me) & 1) == 0;

        /// <summary>Gets the trump suit's cards in a suit contract, nothing otherwise.</summary>
        public uint Trumps { get; private set; }

        public uint Hand { get; private set; }

        public uint Legal { get; private set; }

        /// <summary>Gets the cards the other three players hold between them.</summary>
        public uint Unseen { get; private set; }

        public uint Played => this.knowledge.Played;

        /// <summary>Gets the cards played to the current trick.</summary>
        public uint TrickMask { get; private set; }

        public int TricksPlayed => this.knowledge.Root.TricksPlayed;

        public int TrickCards => this.knowledge.Root.TrickCards;

        public int LedSuit => this.knowledge.Root.LedSuit;

        public int WinnerSeat => this.knowledge.Root.WinnerSeat;

        public int WinnerCard => this.knowledge.Root.WinnerCard;

        /// <summary>Gets the card points already in the current trick.</summary>
        public int TrickPoints => this.knowledge.Root.TrickPoints;

        /// <summary>Gets the card points (and belotes) this player's team has taken so far.</summary>
        public int OurPoints => this.ourPoints;

        public int TheirPoints => this.theirPoints;

        /// <summary>Gets the points of the declared combinations that score, for this player's team.</summary>
        public int OurAnnounces { get; private set; }

        public int TheirAnnounces { get; private set; }

        public ref readonly PlaySignals Signals => ref this.signals;

        /// <summary>Gets a value indicating whether the partner's card holds the trick so far.</summary>
        public bool PartnerWinning => this.TrickCards > 0 && ((this.WinnerSeat ^ this.Me) & 1) == 0;

        /// <summary>The card's rank in its suit in this contract (1 lowest, 8 highest).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OrderOf(int kind, int card) => HumanPreference.Order(kind, card);

        /// <summary>The suit's cards (a mask).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SuitOf(int suit) => SimTables.SuitMasks[suit];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(uint cards) => BitOperations.PopCount(cards);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CardOf(int suit, int type) => (suit * 8) + type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit(int suit, int type) => 1u << ((suit * 8) + type);

        public static uint ToMask(IEnumerable<Engine.Cards.Card> cards)
        {
            var mask = 0u;
            foreach (var card in cards)
            {
                mask |= 1u << card.GetHashCode();
            }

            return mask;
        }

        /// <summary>
        /// Rebuilds the memory from the context.
        /// </summary>
        /// <returns>False when the context does not add up (the caller then plays simply).</returns>
        public bool Build(PlayerPlayCardContext context)
        {
            var contract = context.CurrentContract;
            this.Kind = SimTables.ToKind(contract.Type);
            this.Declarer = contract.Player.Index();
            this.Me = context.MyPosition.Index();
            this.Trumps = this.Kind < SimTables.NoTrumps ? SimTables.SuitMasks[this.Kind] : 0;
            this.Hand = ToMask(context.MyCards);
            this.Legal = ToMask(context.AvailableCardsToPlay);
            this.simulator.SetContract(contract.Type, this.Declarer, context.HangingPoints);
            if (this.Legal == 0 || (this.Legal & ~this.Hand) != 0
                || !this.knowledge.Build(context, this.simulator, usePlayInference: true))
            {
                return false;
            }

            this.Unseen = ~(this.knowledge.Played | this.Hand);
            this.signals = PlaySignals.Read(context.RoundActions, this.Kind);
            this.ReadTrick(context);
            this.ReadBids(context.Bids);
            this.ReadPoints();
            this.ReadPossible();
            return true;
        }

        /// <summary>Narrows the cards to choose from (to those an endgame search found best).</summary>
        public void Restrict(uint legal)
        {
            if ((legal & this.Legal) != 0)
            {
                this.Legal &= legal;
            }
        }

        public uint Possible(int seat) => this.possible[seat];

        public uint KnownCards(int seat) => this.knowledge.Known[seat];

        public int HandCount(int seat) => this.knowledge.HandCounts[seat];

        /// <summary>The suits (bits) the seat has bid as trumps in this auction.</summary>
        public int SuitBids(int seat) => this.suitBids[seat];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Order(int card) => HumanPreference.Order(this.Kind, card);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Points(int card) => SimTables.Values[(this.Kind * 32) + card];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTrump(int card) => (this.Trumps & (1u << card)) != 0;

        /// <summary>Whether the card's suit is played by trump order (all trumps, or the trump suit).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrumpOrdered(int suit) => this.Kind == SimTables.AllTrumps || suit == this.Kind;

        /// <summary>The cards of the card's suit ranking above it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Higher(int card) => HumanPreference.HigherInSuit(this.Kind, card);

        /// <summary>Whether no card of the suit still out ranks above the card.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaster(int card) => (this.Higher(card) & this.Unseen) == 0;

        /// <summary>The cards of the suit the other three still hold.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Outstanding(int suit) => this.Unseen & SimTables.SuitMasks[suit];

        /// <summary>The cards of the suit (this player's).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Mine(int suit) => this.Hand & SimTables.SuitMasks[suit];

        /// <summary>Whether the seat surely has no card of the suit.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVoid(int seat, int suit) => (this.possible[seat] & SimTables.SuitMasks[suit]) == 0;

        /// <summary>Whether the seat may have no card of the suit, as far as the play shows.</summary>
        public bool MayBeVoid(int seat, int suit)
        {
            var suitMask = SimTables.SuitMasks[suit];
            return (this.knowledge.Known[seat] & suitMask) == 0
                   && Count(this.possible[seat] & ~suitMask) >= this.knowledge.HandCounts[seat];
        }

        /// <summary>The chance that the seat holds the card.</summary>
        public double Chance(int seat, int card)
        {
            var bit = 1u << card;
            if ((this.knowledge.Known[seat] & bit) != 0)
            {
                return 1;
            }

            if ((this.possible[seat] & bit) == 0)
            {
                return 0;
            }

            var total = 0.0;
            for (var other = 0; other < 4; other++)
            {
                if (other != this.Me && (this.possible[other] & bit) != 0 && (this.knowledge.Known[other] & bit) == 0)
                {
                    total += this.needs[other] * this.weights[(other * 32) + card];
                }
            }

            return total > 0 ? this.needs[seat] * this.weights[(seat * 32) + card] / total : 0;
        }

        /// <summary>The chance that the seat holds at least one of the cards.</summary>
        public double ChanceAny(int seat, uint cards)
        {
            cards &= this.possible[seat];
            if ((cards & this.knowledge.Known[seat]) != 0)
            {
                return 1;
            }

            var none = 1.0;
            for (var rest = cards; rest != 0; rest &= rest - 1)
            {
                none *= 1 - this.Chance(seat, BitOperations.TrailingZeroCount(rest));
            }

            return 1 - none;
        }

        /// <summary>The chance that the seat has no card of the suit.</summary>
        public double ChanceVoid(int seat, int suit)
        {
            if (!this.MayBeVoid(seat, suit))
            {
                return 0;
            }

            return 1 - this.ChanceAny(seat, this.possible[seat] & SimTables.SuitMasks[suit]);
        }

        /// <summary>
        /// Whether the seat, still to play to this trick, may beat the card if it holds the trick:
        /// with a higher card of the suit led, or (in a suit contract, the led suit plain) with a
        /// trump from a hand without the suit led.
        /// </summary>
        public bool MayBeat(int seat, int card)
        {
            var led = this.TrickCards == 0 ? card >> 3 : this.LedSuit;
            var beats = SimTables.BeatMasks[(((this.Kind * 4) + led) * 32) + card] & this.possible[seat];
            var ledMask = SimTables.SuitMasks[led];
            if ((beats & ledMask) != 0)
            {
                return true;
            }

            return this.Kind < SimTables.NoTrumps && led != this.Kind && (beats & this.Trumps) != 0 && this.MayBeVoid(seat, led);
        }

        /// <summary>The chance that the seat, still to play to this trick, beats the card (see <see cref="MayBeat"/>).</summary>
        public double ChanceBeat(int seat, int card)
        {
            var led = this.TrickCards == 0 ? card >> 3 : this.LedSuit;
            var beats = SimTables.BeatMasks[(((this.Kind * 4) + led) * 32) + card] & this.possible[seat];
            var ledMask = SimTables.SuitMasks[led];
            var chance = this.ChanceAny(seat, beats & ledMask);
            if (this.Kind < SimTables.NoTrumps && led != this.Kind && (beats & this.Trumps) != 0)
            {
                chance += this.ChanceVoid(seat, led) * this.ChanceAny(seat, beats & this.Trumps);
            }

            return Math.Min(1, chance);
        }

        /// <summary>Whether an opponent still to play to this trick may beat the card.</summary>
        public bool OpponentMayBeat(int card)
        {
            // Following, only the left-hand opponent can still play; leading, both can.
            if (this.TrickCards <= 2 && this.MayBeat(this.Left, card))
            {
                return true;
            }

            return this.TrickCards == 0 && this.MayBeat(this.Right, card);
        }

        /// <summary>The chance that an opponent still to play to this trick beats the card.</summary>
        public double ChanceOpponentBeats(int card)
        {
            var chance = this.TrickCards <= 2 ? this.ChanceBeat(this.Left, card) : 0;
            if (this.TrickCards == 0)
            {
                chance = 1 - ((1 - chance) * (1 - this.ChanceBeat(this.Right, card)));
            }

            return chance;
        }

        /// <summary>Whether the seat may still hold a trump (a suit contract).</summary>
        public bool MayHaveTrumps(int seat) => (this.possible[seat] & this.Trumps) != 0;

        /// <summary>Whether an opponent may ruff the suit: none of it, trumps left (a suit contract, plain suit).</summary>
        public bool OpponentMayRuff(int suit)
        {
            if (this.Kind >= SimTables.NoTrumps || suit == this.Kind)
            {
                return false;
            }

            return (this.MayBeVoid(this.Left, suit) && this.MayHaveTrumps(this.Left))
                   || (this.MayBeVoid(this.Right, suit) && this.MayHaveTrumps(this.Right));
        }

        /// <summary>The chance that an opponent ruffs the suit when it is led.</summary>
        public double ChanceOpponentRuffs(int suit)
        {
            if (this.Kind >= SimTables.NoTrumps || suit == this.Kind)
            {
                return 0;
            }

            var left = this.ChanceVoid(this.Left, suit) * this.ChanceAny(this.Left, this.Trumps);
            var right = this.ChanceVoid(this.Right, suit) * this.ChanceAny(this.Right, this.Trumps);
            return 1 - ((1 - left) * (1 - right));
        }

        /// <summary>The trumps the opponents may still hold between them.</summary>
        public uint OpponentTrumps() => (this.possible[this.Left] | this.possible[this.Right]) & this.Trumps;

        /// <summary>
        /// Whether playing the card now declares a belote: the queen or king with the other one in
        /// the hand, of trumps (all trumps: of the suit led, or leading), and a choice of cards.
        /// </summary>
        public bool DeclaresBelote(int card)
        {
            var type = card & 7;
            var suit = card >> 3;
            if ((type != Queen && type != King) || this.Kind == SimTables.NoTrumps || (this.Legal & (this.Legal - 1)) == 0
                || (this.Hand & (1u << (card ^ 3))) == 0)
            {
                return false;
            }

            return suit == (this.Kind == SimTables.AllTrumps ? (this.TrickCards == 0 ? suit : this.LedSuit) : this.Kind);
        }

        /// <summary>Whether the card is half of a belote still in the hand (its suit plays by trump order).</summary>
        public bool IsBeloteCard(int card)
        {
            var type = card & 7;
            return (type == Queen || type == King) && this.Kind != SimTables.NoTrumps && this.TrumpOrdered(card >> 3)
                   && (this.Hand & (1u << (card ^ 3))) != 0;
        }

        /// <summary>The suits (bits) the partner asked for by giving its jack (all trumps) or ace on this player's trick.</summary>
        public int PartnerCalls()
        {
            var smears = this.signals.Smears[this.Partner];
            var calls = 0;
            for (var rest = smears; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var type = card & 7;
                if ((this.Kind == SimTables.AllTrumps && type == Jack) || (this.Kind != SimTables.AllTrumps && type == Ace))
                {
                    calls |= 1 << (card >> 3);
                }
            }

            return calls;
        }

        /// <summary>The suits (bits) the seat threw away while the opponents held the trick: by the convention, ones it has nothing in.</summary>
        public int WeakSuits(int seat) => this.signals.WeakSuits(seat);

        /// <summary>The suits (bits) the seat has led.</summary>
        public int LedSuits(int seat) => (int)this.signals.LedSuits[seat];

        private void ReadTrick(PlayerPlayCardContext context)
        {
            this.TrickMask = 0;
            foreach (var action in context.CurrentTrickActions)
            {
                this.TrickMask |= 1u << action.Card.GetHashCode();
            }
        }

        private void ReadBids(IEnumerable<Bid> bids)
        {
            Array.Clear(this.suitBids);
            foreach (var bid in bids)
            {
                var type = bid.Type;
                if (type == BidType.Clubs || type == BidType.Diamonds || type == BidType.Hearts || type == BidType.Spades)
                {
                    this.suitBids[bid.Player.Index()] |= 1 << (int)type.ToCardSuit();
                }
            }
        }

        private void ReadPoints()
        {
            ref readonly var root = ref this.knowledge.Root;
            var southNorth = (this.Me & 1) == 0;
            this.ourPoints = southNorth ? root.SouthNorthPoints : root.EastWestPoints;
            this.theirPoints = southNorth ? root.EastWestPoints : root.SouthNorthPoints;

            // A hidden rank counts as the lowest the combination may have: the estimate only
            // decides how safe the contract looks, and GetPoints needs every rank.
            var count = this.knowledge.AnnounceCount;
            for (var i = 0; i < count; i++)
            {
                var announce = this.knowledge.Announces[i];
                if (announce.Rank < 0)
                {
                    announce.Rank = announce.Type == AnnounceType.FourOfAKind ? Ten
                        : announce.Type - AnnounceType.SequenceOf3 + 2;
                }

                this.announces[i] = announce;
            }

            AnnounceScorer.GetPoints(this.announces, count, out var southNorthAnnounces, out var eastWestAnnounces);
            this.OurAnnounces = southNorth ? southNorthAnnounces : eastWestAnnounces;
            this.TheirAnnounces = southNorth ? eastWestAnnounces : southNorthAnnounces;
        }

        private void ReadPossible()
        {
            var known = 0u;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat != this.Me)
                {
                    known |= this.knowledge.Known[seat];
                }
            }

            for (var seat = 0; seat < 4; seat++)
            {
                if (seat == this.Me)
                {
                    this.possible[seat] = 0;
                    this.needs[seat] = 0;
                    continue;
                }

                var own = this.knowledge.Known[seat];
                this.possible[seat] = (this.Unseen & ~this.knowledge.Excluded[seat] & ~(known & ~own)) | own;
                this.needs[seat] = Math.Max(0, this.knowledge.HandCounts[seat] - Count(own));
                var weak = this.signals.WeakSuits(seat);
                for (var card = 0; card < 32; card++)
                {
                    var type = card & 7;
                    var suitBit = 1 << (card >> 3);
                    var weight = (this.suitBids[seat] & suitBit) != 0 && (type == Jack || type == Nine) ? this.BidHonourWeight : 1;
                    var honour = this.TrumpOrdered(card >> 3) ? type == Jack || type == Nine : type == Ace || type == Ten;
                    if ((weak & suitBit) != 0 && honour)
                    {
                        weight *= this.DiscardHonourWeight;
                    }

                    this.weights[(seat * 32) + card] = weight;
                }
            }
        }
    }
}
