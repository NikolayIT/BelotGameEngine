namespace Belot.AI.ClaudePlayer.Search
{
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// What a player knows at a card decision, rebuilt from the context alone: the position of
    /// the deal (the root of the search), the cards each other player cannot hold and those they
    /// surely hold, and the combinations declared.
    ///
    /// Every rule of play reveals something: a player who does not follow suit has none of it; in
    /// all trumps (or when trumps are led) one who follows below the best card has nothing
    /// higher of the suit; in a suit contract one who neither follows nor trumps while an
    /// opponent holds the trick has no trumps, and one who does not overtrump an opponent's
    /// trump has no higher trump. A belote shows the other card of the pair; the queen or king
    /// played without one shows there is none (every bot declares it). Four jacks or four nines
    /// show all four cards, and a carre of hidden rank does too when only one rank is possible.
    /// </summary>
    internal sealed class RoundKnowledge
    {
        private const uint RankMask = 0x01010101u;

        private static readonly int[] PlainCarreRanks = { 3, 5, 6, 7 };

        private SimState root;

        public int Me { get; private set; }

        public uint MyHand { get; private set; }

        public uint Played { get; private set; }

        public uint[] PlayedBy { get; } = new uint[4];

        /// <summary>Gets the cards each seat cannot hold.</summary>
        public uint[] Excluded { get; } = new uint[4];

        /// <summary>Gets the cards each seat surely holds.</summary>
        public uint[] Known { get; } = new uint[4];

        public int[] HandCounts { get; } = new int[4];

        /// <summary>Gets the combinations declared so far, the searching player's with their ranks.</summary>
        public DeclaredAnnounce[] Announces { get; } = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];

        public int AnnounceCount { get; private set; }

        /// <summary>Gets the seats (as bits) still to declare their combinations in the first trick.</summary>
        public int SeatsToDeclare { get; private set; }

        /// <summary>Gets the position now, with only the searching player's hand in it.</summary>
        public ref readonly SimState Root => ref this.root;

        /// <summary>
        /// Rebuilds the knowledge. The simulator must already have the contract.
        /// </summary>
        /// <returns>False when the context is not consistent with the rules.</returns>
        public bool Build(PlayerPlayCardContext context, BelotSimulator simulator, bool usePlayInference)
        {
            var me = context.MyPosition.Index();
            this.Me = me;
            this.MyHand = ToMask(context.MyCards);
            this.Played = 0;
            this.AnnounceCount = 0;
            this.SeatsToDeclare = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                this.PlayedBy[seat] = 0;
                this.Excluded[seat] = 0;
                this.Known[seat] = 0;
                this.HandCounts[seat] = 8;
            }

            var state = default(SimState);
            state.Turn = context.FirstToPlayInTheRound.Index();
            if (context.RoundActions is IList<PlayCardAction> actions)
            {
                for (var i = 0; i < actions.Count; i++)
                {
                    if (!this.Replay(ref state, actions[i], simulator, usePlayInference))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var action in context.RoundActions)
                {
                    if (!this.Replay(ref state, action, simulator, usePlayInference))
                    {
                        return false;
                    }
                }
            }

            if (state.Turn != me || this.HandCounts[me] != BitOperations.PopCount(this.MyHand)
                                 || (this.MyHand & this.Played) != 0)
            {
                return false;
            }

            state.Hands[me] = this.MyHand;
            this.ReadAnnounces(context, ref state, simulator.Kind);
            if (state.TricksPlayed == 0 && simulator.Kind != SimTables.NoTrumps)
            {
                for (var i = 1; i <= 3 - state.TrickCards; i++)
                {
                    this.SeatsToDeclare |= 1 << ((me + i) & 3);
                }
            }

            this.CleanUp();
            this.root = state;
            return true;
        }

        private static uint ToMask(CardCollection cards)
        {
            var mask = 0u;
            foreach (var card in cards)
            {
                mask |= 1u << card.GetHashCode();
            }

            return mask;
        }

        private bool Replay(ref SimState state, PlayCardAction action, BelotSimulator simulator, bool usePlayInference)
        {
            var seat = action.Player.Index();
            var card = action.Card.GetHashCode();
            var bit = 1u << card;
            if (seat != state.Turn || (this.Played & bit) != 0)
            {
                return false;
            }

            if (seat != this.Me)
            {
                this.Infer(in state, seat, card, action.Belote, simulator.Kind, usePlayInference);
            }

            // A single legal card declares no belote: the belotes are counted from the announces.
            simulator.Play(ref state, card, bit);
            this.Played |= bit;
            this.PlayedBy[seat] |= bit;
            this.HandCounts[seat]--;
            return true;
        }

        // What the card, played by another seat in this position, says about that seat's hand.
        private void Infer(in SimState state, int seat, int card, bool belote, int kind, bool usePlayInference) =>
            PlayInference.Observe(in state, seat, card, belote, kind, usePlayInference, ref this.Excluded[seat], ref this.Known[seat]);

        private void ReadAnnounces(PlayerPlayCardContext context, ref SimState state, int kind)
        {
            var me = this.Me;
            foreach (var announce in context.Announces)
            {
                var seat = announce.Player.Index();
                if (announce.Type == AnnounceType.Belot)
                {
                    if ((seat & 1) == 0)
                    {
                        state.SouthNorthPoints += 20;
                    }
                    else
                    {
                        state.EastWestPoints += 20;
                    }

                    continue;
                }

                if (seat == me || this.AnnounceCount == this.Announces.Length)
                {
                    continue;
                }

                var rank = announce.Type == AnnounceType.FourJacks ? (int)CardType.Jack :
                           announce.Type == AnnounceType.FourNines ? (int)CardType.Nine : -1;
                if (rank >= 0)
                {
                    this.Known[seat] |= RankMask << rank;
                }

                this.Announces[this.AnnounceCount++] = new DeclaredAnnounce(seat, announce.Type, rank);
            }

            if (kind != SimTables.NoTrumps)
            {
                // The searching player declares everything it is offered, so its combinations
                // follow from its eight cards, ranks included.
                this.AnnounceCount = AnnounceScorer.AddDeclaredCombinations(
                    this.MyHand | this.PlayedBy[me], me, this.Announces, this.AnnounceCount);
            }

            for (var seat = 0; seat < 4; seat++)
            {
                if (seat != me)
                {
                    this.InferCarreRanks(seat);
                }
            }
        }

        // A carre of hidden rank is of a rank none of whose cards anybody else has had.
        private void InferCarreRanks(int seat)
        {
            var hidden = 0;
            for (var i = 0; i < this.AnnounceCount; i++)
            {
                if (this.Announces[i].Seat == seat && this.Announces[i].Rank < 0 && this.Announces[i].Type == AnnounceType.FourOfAKind)
                {
                    hidden++;
                }
            }

            if (hidden == 0)
            {
                return;
            }

            var elsewhere = this.MyHand | (this.Played & ~this.PlayedBy[seat]);
            for (var other = 0; other < 4; other++)
            {
                if (other != seat)
                {
                    elsewhere |= this.Known[other];
                }
            }

            var candidates = 0;
            var ranks = new int[PlainCarreRanks.Length];
            foreach (var rank in PlainCarreRanks)
            {
                var cards = RankMask << rank;
                if ((cards & elsewhere) == 0 && (cards & ~this.PlayedBy[seat] & this.Excluded[seat]) == 0)
                {
                    ranks[candidates++] = rank;
                }
            }

            if (candidates != hidden)
            {
                return;
            }

            var next = 0;
            for (var i = 0; i < this.AnnounceCount; i++)
            {
                if (this.Announces[i].Seat == seat && this.Announces[i].Rank < 0 && this.Announces[i].Type == AnnounceType.FourOfAKind)
                {
                    this.Announces[i].Rank = ranks[next];
                    this.Known[seat] |= RankMask << ranks[next];
                    next++;
                }
            }
        }

        // Drops what the play has used up, and anything contradictory (a human may skip a belote).
        private void CleanUp()
        {
            var me = this.Me;
            this.Known[me] = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                if (seat == me)
                {
                    continue;
                }

                this.Known[seat] &= ~this.Played & ~this.MyHand & ~this.Excluded[seat];
                for (var other = seat + 1; other < 4; other++)
                {
                    var both = this.Known[seat] & this.Known[other];
                    this.Known[seat] &= ~both;
                    this.Known[other] &= ~both;
                }
            }

            for (var seat = 0; seat < 4; seat++)
            {
                if (BitOperations.PopCount(this.Known[seat]) > this.HandCounts[seat])
                {
                    this.Known[seat] = 0;
                }

                this.Excluded[seat] |= this.Played | (seat == me ? 0 : this.MyHand);
            }
        }
    }
}
