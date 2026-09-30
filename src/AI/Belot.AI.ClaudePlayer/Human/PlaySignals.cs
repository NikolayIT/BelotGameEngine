namespace Belot.AI.ClaudePlayer.Human
{
    using System.Collections.Generic;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Players;

    /// <summary>
    /// What the play so far said by convention, per seat (South, East, North, West): the cards
    /// each seat threw away (neither followed suit nor trumped) while an opponent held the trick,
    /// which by the Belot convention mark a suit it has nothing in and does not want led; the
    /// cards it gave to its partner's trick; and the suits it has led.
    /// </summary>
    internal struct PlaySignals
    {
        /// <summary>Cards thrown away while an opponent held the trick (the "don't lead it" discards).</summary>
        public SeatMasks Discards;

        /// <summary>Cards thrown away while the partner held the trick.</summary>
        public SeatMasks Smears;

        /// <summary>The suits (bits) each seat has led.</summary>
        public SeatMasks LedSuits;

        public static PlaySignals Read(IEnumerable<PlayCardAction> actions, int kind)
        {
            var signals = default(PlaySignals);
            var trickCards = 0;
            var led = 0;
            var winnerSeat = 0;
            var winnerCard = 0;
            foreach (var action in actions)
            {
                var seat = action.Player.Index();
                var card = action.Card.GetHashCode();
                var suit = card >> 3;
                if (trickCards == 0)
                {
                    led = suit;
                    winnerSeat = seat;
                    winnerCard = card;
                    signals.LedSuits[seat] |= 1u << suit;
                }
                else
                {
                    if (suit != led && !(kind < SimTables.NoTrumps && suit == kind))
                    {
                        if (((winnerSeat ^ seat) & 1) == 0)
                        {
                            signals.Smears[seat] |= 1u << card;
                        }
                        else
                        {
                            signals.Discards[seat] |= 1u << card;
                        }
                    }

                    var row = ((kind * 4) + led) * 32;
                    if (SimTables.Strengths[row + card] > SimTables.Strengths[row + winnerCard])
                    {
                        winnerSeat = seat;
                        winnerCard = card;
                    }
                }

                trickCards = (trickCards + 1) & 3;
            }

            return signals;
        }

        /// <summary>The suits (bits) of the cards.</summary>
        public static int SuitsOf(uint cards)
        {
            var suits = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                if ((cards & SimTables.SuitMasks[suit]) != 0)
                {
                    suits |= 1 << suit;
                }
            }

            return suits;
        }

        /// <summary>The suits (bits) the seat has marked, by discarding them, as ones it has nothing in.</summary>
        public readonly int WeakSuits(int seat) => SuitsOf(this.Discards[seat]);
    }
}
