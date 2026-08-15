namespace BelotV2
{
    /// <summary>
    /// The 32-card deck plus shuffling and the two-phase deal.
    ///
    /// RE notes:
    ///  - Hands hold 8 cards; belot.exe deals them in two phases (dealother3 at 0x4754F8
    ///    toggles slot flags: slots 0..4 = first 5 cards, slots 5..7 = the last 3 dealt
    ///    after bidding).
    ///  - The first player of a round is chosen in wholegameinit (0x475AE4):
    ///      match start -> Random(4)+1 (any seat);
    ///      subsequent rounds -> alternates within a team (Random(2) picks between the two
    ///      seats of the team due to start), so the deal rotates fairly.
    ///  - Shuffling uses the Delphi RNG (see DelphiRandom). The avcard component's exact
    ///    shuffle is not reproduced bit-for-bit (the original re-seeds from the clock), so a
    ///    Fisher-Yates shuffle driven by the same RNG is used; this is behaviourally faithful.
    /// </summary>
    public static class Deck
    {
        public static Card[] BuildOrdered()
        {
            var deck = new Card[32];
            int k = 0;
            for (int s = 0; s < 4; s++)
            {
                for (int r = 7; r <= 14; r++)
                {
                    deck[k++] = new Card((Suit)s, (Rank)r);
                }
            }

            return deck;
        }

        /// <summary>In-place Fisher-Yates using the Delphi RNG.</summary>
        public static void Shuffle(Card[] deck, DelphiRandom rng)
        {
            for (int i = deck.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        /// <summary>
        /// Deal phase 1: 5 cards to each of the 4 hands, in seat order starting at
        /// <paramref name="firstSeat"/>. Returns the read cursor into the deck.
        /// </summary>
        public static int DealFirstFive(Card[] deck, List<Card>[] hands, int firstSeat)
            => DealCount(deck, hands, firstSeat, 0, 5);

        /// <summary>Deal phase 2: the remaining 3 cards to each hand.</summary>
        public static int DealLastThree(Card[] deck, List<Card>[] hands, int firstSeat, int cursor)
            => DealCount(deck, hands, firstSeat, cursor, 3);

        private static int DealCount(Card[] deck, List<Card>[] hands, int firstSeat, int cursor, int perHand)
        {
            for (int n = 0; n < perHand; n++)
            {
                for (int s = 0; s < Seats.Count; s++)
                {
                    int seat = (firstSeat + s) & 3;
                    hands[seat].Add(deck[cursor++]);
                }
            }

            return cursor;
        }
    }
}
