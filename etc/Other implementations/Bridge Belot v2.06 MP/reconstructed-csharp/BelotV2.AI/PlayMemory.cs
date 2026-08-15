namespace BelotV2
{
    using System.Collections.Generic;

    /// <summary>
    /// A window onto four consecutive bytes of the AI's memory block, indexed the way the routine
    /// indexes it (players 1..4, or suits 0..3).
    /// </summary>
    public readonly struct MemSlots
    {
        private readonly byte[] mem;
        private readonly int offset;

        internal MemSlots(byte[] mem, int offset)
        {
            this.mem = mem;
            this.offset = offset;
        }

        public int this[int index]
        {
            get => this.mem[this.offset + index];
            set => this.mem[this.offset + index] = (byte)value;
        }
    }

    /// <summary>
    /// The per-round memory the game keeps for the AI at 0x48BEB8..0x48BF17, and the rules that
    /// maintain it while a round is played. <see cref="OriginalPlayAi"/> reads this block; nothing
    /// inside the decision routine fills it in, so a host that wants the original's behaviour over
    /// a whole round has to feed it — <see cref="OriginalPlayAdapter"/> rebuilds it from the play
    /// history before every decision.
    ///
    /// Layout (all bytes, players indexed 1..4, suits 0..3 in the binary's own order):
    ///
    ///   0x48BEB8  memA[p]     reset to 0, never written again — dead
    ///   0x48BEBE  memBE       the contract's doubling multiplier: 1, 2 or 4
    ///   0x48BEC0  memB[p]     the suit player p bid, or 4; cleared once that suit is led
    ///   0x48BEC4  memC[p]     the suit p opened of their own accord, or 4
    ///   0x48BEC8  memD[p]     the suit p signalled by discarding its boss card, or 4
    ///   0x48BECC  memE[p]     1 once p's hand has run short
    ///   0x48BED0  memF[s]     1 once suit s has been led
    ///   0x48BED4  memG[p][s]  int32 count of how often p discarded suit s off-suit
    ///
    /// The maintenance rules are transcribed from three places in the binary:
    ///
    ///   * end of trick, 0x46EBC8 — memF, and memB/memC/memD/memE for the trick <em>winner</em>
    ///   * a card being played, 0x46EE6C — memC when leading, memG on an off-suit discard, and
    ///     memD when that discard is the boss card of the contract
    ///   * player2BeforePlay's own epilogue, 0x473E86 — the same memC and memG updates for the
    ///     card the AI just chose (it does not do the memD signal)
    /// </summary>
    public static class PlayMemory
    {
        public const int Base = 0x48BEB8;
        public const int Size = 0x60;
        public const int NoSuit = 4;

        /// <summary>
        /// The state choosegame leaves behind when a contract is settled.
        /// </summary>
        /// <param name="level">
        /// The contract's doubling multiplier — 1 plain, 2 doubled, 4 redoubled — which the game
        /// stores at 0x48BEBE (@0x4799B6). The decision tree reads it, so leaving it at 0 quietly
        /// disables a branch that fires on ordinary undoubled hands.
        /// </param>
        public static byte[] NewRound(int level = 1)
        {
            var m = new byte[Size];
            m[0x48BEBE - Base] = (byte)level;
            for (int p = 0; p < 4; p++)
            {
                m[0x48BEC0 - Base + p] = NoSuit;   // memB
                m[0x48BEC4 - Base + p] = NoSuit;   // memC
                m[0x48BEC8 - Base + p] = NoSuit;   // memD
            }

            return m;
        }

        public static int MemB(byte[] m, int player) => m[0x48BEBF - Base + player];

        public static int MemC(byte[] m, int player) => m[0x48BEC3 - Base + player];

        public static int MemD(byte[] m, int player) => m[0x48BEC7 - Base + player];

        public static int MemF(byte[] m, int suit) => m[0x48BED0 - Base + suit];

        public static int MemG(byte[] m, int player, int suit)
        {
            int i = 0x48BEC4 - Base + (player * 0x10) + (suit * 4);
            return m[i] | (m[i + 1] << 8) | (m[i + 2] << 16) | (m[i + 3] << 24);
        }

        /// <summary>Records the suit a player bid, so partners can read it back (0x48BEC0).</summary>
        public static void SetBidSuit(byte[] m, int player, int suit) =>
            m[0x48BEBF - Base + player] = (byte)suit;

        /// <summary>
        /// A card hitting the table. <paramref name="ledSuit"/> is 4 when this is the opening lead.
        /// <paramref name="signals"/> is the 0x46EE6C path, which also reads a discarded boss card
        /// as a signal; player2BeforePlay's own epilogue does everything here except that.
        /// </summary>
        public static void OnCardPlayed(
            byte[] m, int contract, int player, int suit, int rank, int ledSuit, bool signals)
        {
            if (ledSuit == NoSuit)
            {
                // Leading. Unless this is the suit our partner asked for, remember that we chose it.
                if (suit != MemB(m, OriginalPlayAi.PartnerOf[player]))
                {
                    m[0x48BEC3 - Base + player] = (byte)suit;
                }

                return;
            }

            if (suit == ledSuit)
            {
                return;
            }

            int g = 0x48BEC4 - Base + (player * 0x10) + (suit * 4);
            int count = (m[g] | (m[g + 1] << 8) | (m[g + 2] << 16) | (m[g + 3] << 24)) + 1;
            m[g] = (byte)count;
            m[g + 1] = (byte)(count >> 8);
            m[g + 2] = (byte)(count >> 16);
            m[g + 3] = (byte)(count >> 24);

            if (signals && ((contract == 6 && rank == 11) || (contract == 5 && rank == 14)))
            {
                m[0x48BEC7 - Base + player] = (byte)suit;
            }
        }

        /// <summary>
        /// End of a trick. Only the winner's entries are touched — the routine looks the winner up
        /// through the trick-winner function and works from that seat alone.
        /// </summary>
        public static void OnTrickEnd(byte[] m, int contract, int winner, int ledSuit, int cardsLeft)
        {
            m[0x48BED0 - Base + ledSuit] = 1;

            if (MemB(m, winner) == ledSuit)
            {
                m[0x48BEBF - Base + winner] = NoSuit;
            }

            if (MemC(m, winner) == ledSuit)
            {
                m[0x48BEC3 - Base + winner] = NoSuit;
            }

            m[0x48BEC7 - Base + winner] = NoSuit;

            if (cardsLeft < (contract == 5 ? 6 : 7))
            {
                m[0x48BECB - Base + winner] = 1;
            }
        }

        /// <summary>
        /// Replays a round's cards through the rules above. Everything is in the binary's own
        /// encodings: contract 1..6, players 1..4, suits 0..3 (C, D, S, H), ranks 7..14.
        /// <paramref name="humanSeat"/> is the one seat whose cards go through the 0x46EE6C path
        /// and can therefore signal; pass 0 when every seat is played by the AI.
        /// </summary>
        public static byte[] Replay(
            int contract,
            IReadOnlyList<(int Player, int Suit, int Rank)> plays,
            IReadOnlyList<int>? bidSuitByPlayer,
            int humanSeat,
            int cardsDealt = 8,
            int level = 1)
        {
            byte[] m = NewRound(level);
            if (bidSuitByPlayer != null)
            {
                for (int p = 1; p <= 4; p++)
                {
                    SetBidSuit(m, p, bidSuitByPlayer[p]);
                }
            }

            var left = new int[5];
            for (int p = 1; p <= 4; p++)
            {
                left[p] = cardsDealt;
            }

            for (int i = 0; i < plays.Count; i += 4)
            {
                int n = System.Math.Min(4, plays.Count - i);
                int ledSuit = plays[i].Suit;
                for (int k = 0; k < n; k++)
                {
                    (int player, int suit, int rank) = plays[i + k];
                    OnCardPlayed(m, contract, player, suit, rank,
                                 k == 0 ? NoSuit : ledSuit, player == humanSeat);
                    left[player]--;
                }

                if (n < 4)
                {
                    break;      // trick still in progress: its end-of-trick update has not happened
                }

                int winner = TrickWinner(contract, plays, i);
                OnTrickEnd(m, contract, winner, ledSuit, left[winner]);
            }

            return m;
        }

        private static int TrickWinner(
            int contract, IReadOnlyList<(int Player, int Suit, int Rank)> plays, int from)
        {
            int trump = contract <= 4 ? TrumpOf(contract) : NoSuit;
            int bestIndex = 0;
            for (int k = 1; k < 4; k++)
            {
                if (Beats(plays[from + k], plays[from + bestIndex], plays[from].Suit, trump, contract))
                {
                    bestIndex = k;
                }
            }

            return plays[from + bestIndex].Player;
        }

        private static bool Beats(
            (int Player, int Suit, int Rank) card,
            (int Player, int Suit, int Rank) best,
            int ledSuit,
            int trump,
            int contract)
        {
            bool cardTrump = card.Suit == trump;
            bool bestTrump = best.Suit == trump;
            if (cardTrump != bestTrump)
            {
                return cardTrump;
            }

            if (card.Suit != best.Suit)
            {
                return false;   // an off-suit discard never wins
            }

            if (!cardTrump && card.Suit != ledSuit)
            {
                return false;
            }

            int[] order = cardTrump || contract == 6
                ? OriginalPlayAi.TrumpOrderTable
                : OriginalPlayAi.NoTrumpOrderTable;
            return order[card.Rank - 7] > order[best.Rank - 7];
        }

        private static int TrumpOf(int contract) => contract switch
        {
            1 => 0,     // clubs
            2 => 1,     // diamonds
            3 => 3,     // hearts
            _ => 2,     // spades
        };
    }
}
