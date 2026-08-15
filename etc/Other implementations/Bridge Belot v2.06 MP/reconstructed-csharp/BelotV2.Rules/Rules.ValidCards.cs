namespace BelotV2
{
    public static partial class Rules
    {
        /// <summary>
        /// The legal cards a player may play, reproducing the rules enforced by
        /// belot.exe's move predicate FUN_004767f8 (which itself uses the master
        /// strength table at 0x48BAED). Rules confirmed by RE:
        ///  - Leading: any card.
        ///  - Holding the led suit: must follow it. A raise obligation (must beat the
        ///    current winner when able) applies ONLY when the led suit is trump or the
        ///    contract is all-trumps; a plain led suit has no rank obligation, and this
        ///    raise obligation is NOT waived when your partner is winning.
        ///  - Void in the led suit: if your partner currently wins the trick you may play
        ///    anything; otherwise you must ruff if you hold trumps, and must over-ruff when
        ///    an opponent has already trumped and you are able; with no trumps, discard freely.
        /// </summary>
        public static List<Card> ValidCards(
            IReadOnlyList<Card> hand,
            IReadOnlyList<Play> trick,
            Contract contract,
            int mySeat)
        {
            // Leading: everything is legal.
            if (trick.Count == 0)
            {
                return new List<Card>(hand);
            }

            Suit led = trick[0].Card.Suit;
            int winnerIdx = WinnerIndex(trick, contract);
            Card winning = trick[winnerIdx].Card;
            bool partnerWinning = Seats.SameTeam(mySeat, trick[winnerIdx].Seat);

            var ofLed = Where(hand, c => c.Suit == led);

            if (ofLed.Count > 0)
            {
                bool ledIsTrump = contract.IsTrump(led);
                bool raiseObligation = ledIsTrump || contract.Category == ContractCategory.AllTrumps;
                if (!raiseObligation)
                {
                    return ofLed; // plain suit led: any card of it
                }

                // Following trump (or all-trumps): must overtake the current winner if able.
                // The winner is necessarily a card of the led suit here.
                int winStr = StrengthInLed(winning, contract, led);
                var higher = Where(ofLed, c => StrengthInLed(c, contract, led) > winStr);
                return higher.Count > 0 ? higher : ofLed;
            }

            // Void in the led suit.
            if (partnerWinning)
            {
                return new List<Card>(hand); // partner winning: free discard
            }

            if (contract.Category == ContractCategory.NoTrumps
                || contract.Category == ContractCategory.AllTrumps)
            {
                // No cross-suit trumping possible: any card (can't win anyway).
                return new List<Card>(hand);
            }

            // Suit contract, opponent winning, void in led suit: must ruff if holding trump.
            Suit trump = contract.TrumpSuit!.Value;
            var trumps = Where(hand, c => c.Suit == trump);
            if (trumps.Count == 0)
            {
                return new List<Card>(hand); // no trumps: discard anything
            }

            if (winning.Suit == trump)
            {
                // An opponent has already trumped: you must OVER-trump if you can, but if you
                // cannot, you are free to play anything — there is no obligation to under-trump.
                // (Verified against the original: FUN_004767F8 golden vectors.)
                int winStr = Cards.TrumpStrength(winning);
                var higher = Where(trumps, c => Cards.TrumpStrength(c) > winStr);
                return higher.Count > 0 ? higher : new List<Card>(hand);
            }

            // Opponent winning with the led (plain) suit: must ruff with any trump.
            return trumps;
        }

        // Strength of a card that belongs to the led suit, under the contract.
        private static int StrengthInLed(Card c, Contract contract, Suit led)
            => contract.IsTrump(led) ? Cards.TrumpStrength(c) : Cards.PlainStrength(c);

        private static List<Card> Where(IReadOnlyList<Card> src, Func<Card, bool> pred)
        {
            var r = new List<Card>();
            for (int i = 0; i < src.Count; i++)
            {
                if (pred(src[i]))
                {
                    r.Add(src[i]);
                }
            }

            return r;
        }
    }
}
