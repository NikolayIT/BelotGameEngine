namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Game;

    /// <summary>
    /// The card play of one deal over <see cref="SimState"/>: the legal cards, playing a card
    /// (the trick winner and the belote), the scoring, and a fast greedy policy for rollouts. The
    /// rules mirror ValidCardsService, TrickWinnerService, ValidAnnouncesService.IsBeloteAllowed
    /// and ScoreManager (the tests check them against the engine), on card masks only.
    /// </summary>
    internal sealed class BelotSimulator
    {
        private int kind;
        private int valueRow;
        private int declarerTeam;
        private int coefficient;
        private int hangingPoints;

        public int Kind => this.kind;

        public int Coefficient => this.coefficient;

        public int HangingPoints => this.hangingPoints;

        public void SetContract(BidType contract, int declarerSeat, int hangingPoints)
        {
            this.kind = SimTables.ToKind(contract);
            this.valueRow = this.kind * 32;
            this.declarerTeam = declarerSeat & 1;
            this.coefficient = contract.HasFlag(BidType.ReDouble) ? 4 : contract.HasFlag(BidType.Double) ? 2 : 1;
            this.hangingPoints = hangingPoints;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Value(int card) => SimTables.Values[this.valueRow + card];

        public uint LegalMoves(in SimState state)
        {
            var hand = state.Hands[state.Turn];
            if (state.TrickCards == 0)
            {
                return hand;
            }

            var led = state.LedSuit;
            var suitCards = hand & SimTables.SuitMasks[led];
            if (this.kind == SimTables.AllTrumps || led == this.kind)
            {
                // Follow suit, and beat the best card of the suit when possible.
                if (suitCards == 0)
                {
                    return hand;
                }

                var higher = suitCards & SimTables.HigherTrumps[state.WinnerCard];
                return higher != 0 ? higher : suitCards;
            }

            if (suitCards != 0 || this.kind == SimTables.NoTrumps)
            {
                return suitCards != 0 ? suitCards : hand;
            }

            // A suit contract with a plain suit led and none of it in the hand.
            var trumps = hand & SimTables.SuitMasks[this.kind];
            if (trumps == 0 || (state.TrickCards >= 2 && state.WinnerSeat == ((state.Turn + 2) & 3)))
            {
                // No trumps, or the partner holds the trick: anything goes.
                return hand;
            }

            if (state.WinnerCard >> 3 == this.kind)
            {
                // An opponent has trumped: overtrump if possible, otherwise anything goes.
                var higher = trumps & SimTables.HigherTrumps[state.WinnerCard];
                return higher != 0 ? higher : hand;
            }

            return trumps;
        }

        /// <summary>
        /// Plays the card. <paramref name="legal"/> is the mover's legal set: a belote is
        /// declared when the queen or king is played with the other one still in the hand, the
        /// way every bot does, but never with a forced card, which the engine plays by itself.
        /// </summary>
        public void Play(ref SimState state, int card, uint legal)
        {
            var seat = state.Turn;
            state.Hands[seat] &= ~(1u << card);
            var suit = card >> 3;
            var value = SimTables.Values[this.valueRow + card];
            if (state.TrickCards == 0)
            {
                state.LedSuit = suit;
                state.WinnerSeat = seat;
                state.WinnerCard = card;
                state.TrickPoints = value;
            }
            else
            {
                var row = ((this.kind * 4) + state.LedSuit) * 32;
                if (SimTables.Strengths[row + card] > SimTables.Strengths[row + state.WinnerCard])
                {
                    state.WinnerSeat = seat;
                    state.WinnerCard = card;
                }

                state.TrickPoints += value;
            }

            var type = card & 7;
            if ((type == SimTables.Queen || type == SimTables.King)
                && this.kind != SimTables.NoTrumps
                && (legal & (legal - 1)) != 0
                && (state.Hands[seat] & (1u << (card ^ 3))) != 0
                && suit == (this.kind == SimTables.AllTrumps ? state.LedSuit : this.kind))
            {
                // card ^ 3 swaps the queen (5) and the king (6) of the suit.
                if ((seat & 1) == 0)
                {
                    state.SouthNorthPoints += 20;
                }
                else
                {
                    state.EastWestPoints += 20;
                }
            }

            if (++state.TrickCards < 4)
            {
                state.Turn = (seat + 1) & 3;
                return;
            }

            var winnerTeam = state.WinnerSeat & 1;
            if (winnerTeam == 0)
            {
                state.SouthNorthPoints += state.TrickPoints;
                state.SouthNorthTricks++;
            }
            else
            {
                state.EastWestPoints += state.TrickPoints;
                state.EastWestTricks++;
            }

            state.TricksPlayed++;
            state.LastTrickTeam = winnerTeam;
            state.TrickCards = 0;
            state.Turn = state.WinnerSeat;
        }

        /// <summary>
        /// Scores a finished deal like ScoreManager. The announces are the active combinations'
        /// points (the belotes are already in the state). The hanging points carried into the
        /// deal go to its winner.
        /// </summary>
        public void Score(
            in SimState state,
            int southNorthAnnounces,
            int eastWestAnnounces,
            out int southNorthPoints,
            out int eastWestPoints,
            out int hangingPoints)
        {
            var southNorth = state.SouthNorthPoints + southNorthAnnounces;
            var eastWest = state.EastWestPoints + eastWestAnnounces;
            if (state.LastTrickTeam == 0)
            {
                southNorth += 10;
            }
            else
            {
                eastWest += 10;
            }

            if (this.kind == SimTables.NoTrumps)
            {
                southNorth *= 2;
                eastWest *= 2;
            }

            if (state.SouthNorthTricks == 0)
            {
                eastWest += 90;
            }

            if (state.EastWestTricks == 0)
            {
                southNorth += 90;
            }

            southNorthPoints = 0;
            eastWestPoints = 0;
            hangingPoints = 0;
            var carried = this.hangingPoints;
            if (this.coefficient > 1)
            {
                var all = (this.RoundPoints(southNorth + eastWest, true) * this.coefficient) + carried;
                if (southNorth > eastWest)
                {
                    southNorthPoints = all;
                }
                else if (eastWest > southNorth)
                {
                    eastWestPoints = all;
                }
                else
                {
                    hangingPoints = all;
                }
            }
            else if (this.declarerTeam == 0 && southNorth < eastWest)
            {
                // Inside: everything goes to the defenders.
                eastWestPoints = RoundHalfToEven(southNorth + eastWest) + carried;
            }
            else if (this.declarerTeam == 0 && southNorth == eastWest)
            {
                eastWestPoints = this.RoundPoints(eastWest, true);
                hangingPoints = carried + this.RoundPoints(southNorth, false);
            }
            else if (this.declarerTeam == 1 && eastWest < southNorth)
            {
                southNorthPoints = RoundHalfToEven(southNorth + eastWest) + carried;
            }
            else if (this.declarerTeam == 1 && southNorth == eastWest)
            {
                southNorthPoints = this.RoundPoints(southNorth, true);
                hangingPoints = carried + this.RoundPoints(eastWest, false);
            }
            else
            {
                southNorthPoints = this.RoundPoints(southNorth, southNorth > eastWest);
                eastWestPoints = this.RoundPoints(eastWest, eastWest > southNorth);
                if (southNorth > eastWest)
                {
                    southNorthPoints += carried;
                }
                else if (eastWest > southNorth)
                {
                    eastWestPoints += carried;
                }
            }
        }

        /// <summary>
        /// A greedy perfect-information policy for the rollouts: lead a card nobody can beat
        /// (the most valuable one) or else the cheapest; give points to a partner who keeps the
        /// trick; otherwise take the trick with the cheapest card no later opponent can beat, or
        /// play the cheapest card.
        /// </summary>
        public int ChooseRolloutMove(in SimState state, uint legal)
        {
            if ((legal & (legal - 1)) == 0)
            {
                return BitOperations.TrailingZeroCount(legal);
            }

            var seat = state.Turn;
            if (state.TrickCards == 0)
            {
                return this.ChooseLead(in state, legal, seat);
            }

            var row = ((this.kind * 4) + state.LedSuit) * 32;
            if (((state.WinnerSeat ^ seat) & 1) == 0 && !this.CanBeBeatenLater(in state, seat, state.WinnerCard, row))
            {
                return this.MostValuableToGive(legal);
            }

            var best = -1;
            var bestCost = int.MaxValue;
            for (var rest = legal & SimTables.BeatMasks[row + state.WinnerCard]; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var cost = this.Cost(card);
                if (cost < bestCost && !this.CanBeBeatenLater(in state, seat, card, row))
                {
                    best = card;
                    bestCost = cost;
                }
            }

            return best >= 0 ? best : this.Cheapest(legal);
        }

        // ScoreManager's rounding of the deal's points to game points.
        internal int RoundPoints(int points, bool winner)
        {
            if (this.kind == SimTables.NoTrumps)
            {
                return RoundHalfToEven(points);
            }

            // All trumps rounds 4 by the winner down, suits round 6 by the winner down.
            var threshold = this.kind == SimTables.AllTrumps ? 4 : 6;
            var remainder = points % 10;
            if (remainder > threshold || (remainder == threshold && !winner))
            {
                return (points / 10) + 1;
            }

            return points / 10;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundHalfToEven(int points) => (int)Math.Round(points / 10.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanBeat(uint hand, uint ledSuitMask, uint beat)
        {
            // A player with the led suit must follow it; without it any card may go.
            var follow = hand & ledSuitMask;
            return ((follow != 0 ? follow : hand) & beat) != 0;
        }

        private int ChooseLead(in SimState state, uint legal, int seat)
        {
            var left = state.Hands[(seat + 1) & 3];
            var right = state.Hands[(seat + 3) & 3];
            var best = -1;
            var bestValue = -1;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var suitMask = SimTables.SuitMasks[card >> 3];
                var beat = SimTables.BeatMasks[(((this.kind * 4) + (card >> 3)) * 32) + card];
                var value = this.Value(card);
                if (value > bestValue && !CanBeat(left, suitMask, beat) && !CanBeat(right, suitMask, beat))
                {
                    best = card;
                    bestValue = value;
                }
            }

            return best >= 0 ? best : this.Cheapest(legal);
        }

        // Whether an opponent still to play in this trick could beat the card.
        private bool CanBeBeatenLater(in SimState state, int seat, int card, int row)
        {
            var beat = SimTables.BeatMasks[row + card];
            var suitMask = SimTables.SuitMasks[state.LedSuit];
            var toPlay = 3 - state.TrickCards;
            for (var i = 1; i <= toPlay; i += 2)
            {
                // The opponents are one and three seats on; the partner in between never counts.
                if (CanBeat(state.Hands[(seat + i) & 3], suitMask, beat))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Cost(int card) => this.Value(card) + (card >> 3 == this.kind ? 12 : 0);

        private int Cheapest(uint legal)
        {
            var best = -1;
            var bestCost = int.MaxValue;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var cost = this.Cost(card);
                if (cost < bestCost)
                {
                    best = card;
                    bestCost = cost;
                }
            }

            return best;
        }

        // The most valuable plain card, or the cheapest trump when only trumps may go.
        private int MostValuableToGive(uint legal)
        {
            var best = -1;
            var bestScore = int.MinValue;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var score = card >> 3 == this.kind ? -100 - this.Value(card) : this.Value(card);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }
    }
}
