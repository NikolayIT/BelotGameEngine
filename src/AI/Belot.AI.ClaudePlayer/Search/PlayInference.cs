namespace Belot.AI.ClaudePlayer.Search
{
    /// <summary>
    /// What a card says about the hand of the seat that plays it, from the position it is played
    /// in (see <see cref="RoundKnowledge"/> for the rules). Everybody at the table can draw these
    /// conclusions, so the search and the neural player share them.
    /// </summary>
    internal static class PlayInference
    {
        /// <summary>
        /// Adds what the card shows to the seat's cards it cannot hold and those it surely holds.
        /// </summary>
        /// <param name="state">The position before the card is played (the seat is to play).</param>
        /// <param name="seat">The seat playing.</param>
        /// <param name="card">The card.</param>
        /// <param name="belote">Whether the seat declared a belote with it.</param>
        /// <param name="kind">The contract's kind (see <see cref="SimTables"/>).</param>
        /// <param name="usePlayInference">False to learn only from the belotes.</param>
        /// <param name="excluded">The seat's cards it cannot hold.</param>
        /// <param name="known">The seat's cards it surely holds.</param>
        public static void Observe(
            in SimState state,
            int seat,
            int card,
            bool belote,
            int kind,
            bool usePlayInference,
            ref uint excluded,
            ref uint known)
        {
            var suit = card >> 3;
            var type = card & 7;
            var beloteSuit = kind == SimTables.AllTrumps ? (state.TrickCards == 0 ? suit : state.LedSuit) : kind;
            if ((type == SimTables.Queen || type == SimTables.King) && kind != SimTables.NoTrumps && suit == beloteSuit)
            {
                var other = 1u << (card ^ 3);
                if (belote)
                {
                    known |= other;
                }
                else if (usePlayInference)
                {
                    excluded |= other;
                }
            }

            if (!usePlayInference || state.TrickCards == 0)
            {
                return;
            }

            var led = state.LedSuit;
            var bit = 1u << card;
            if (kind == SimTables.AllTrumps || led == kind)
            {
                if (suit != led)
                {
                    excluded |= SimTables.SuitMasks[led];
                }
                else if ((SimTables.HigherTrumps[state.WinnerCard] & bit) == 0)
                {
                    // Followed below the best card: nothing higher of the suit.
                    excluded |= SimTables.HigherTrumps[state.WinnerCard];
                }

                return;
            }

            if (suit == led)
            {
                return;
            }

            excluded |= SimTables.SuitMasks[led];
            if (kind == SimTables.NoTrumps || (state.TrickCards >= 2 && state.WinnerSeat == ((seat + 2) & 3)))
            {
                // No trumps, or the partner held the trick: any card was allowed.
                return;
            }

            if (state.WinnerCard >> 3 == kind)
            {
                // An opponent had trumped: overtrumping was compulsory.
                if (suit != kind || (SimTables.HigherTrumps[state.WinnerCard] & bit) == 0)
                {
                    excluded |= SimTables.HigherTrumps[state.WinnerCard];
                }
            }
            else if (suit != kind)
            {
                // Trumping was compulsory.
                excluded |= SimTables.SuitMasks[kind];
            }
        }
    }
}
