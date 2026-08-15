namespace BelotV2
{
    public sealed class RoundResult
    {
        public int Team0Raw { get; set; }   // South+North raw points (cards+announces+last10, ×2 for NT)

        public int Team1Raw { get; set; }   // East+West

        public int Team0Board { get; set; } // points added to the match board (tens)

        public int Team1Board { get; set; }

        public int HangingPoints { get; set; }

        public bool CapotForOneTeam { get; set; }

        public bool Inside { get; set; }    // contract team failed ("вътре")
    }

    /// <summary>
    /// Round scoring, diffed against the exe's own routine (FUN_0047AC00 @0x47AC00) over 2,500
    /// scored rounds. tools/score_probe.py runs that routine under emulation and reads the two
    /// figures it adds to the match board, so the rules below are what the game does rather than
    /// what the game of Belot generally does — the two differ in four places:
    ///
    ///   * a double, or the declaring side going inside, collapses the round into one award to
    ///     whichever side scored more, nudged to an even number of tens;
    ///   * a made contract rounds only the leader, the other side taking the remainder, so the
    ///     two always add up to the round;
    ///   * a side that took anything never rounds away to nothing;
    ///   * level points hang rather than being banked.
    ///
    /// Which of two competing declarations scores is recovered too, and is stranger than the
    /// rulebook version — see Announces.Compare. Match target = 151, confirmed as 0x97 in
    /// choosegame.
    /// </summary>
    public static class Scoring
    {
        public const int MatchTarget = 151; // 0x97, confirmed in choosegame (151 - teamScore)

        public static RoundResult Score(
            Contract contract,
            IReadOnlyList<Card> team0Cards,
            IReadOnlyList<Card> team1Cards,
            IReadOnlyList<Announce> activeAnnounces,
            int hangingPoints,
            int lastTrickWinnerSeat)
        {
            var r = new RoundResult();

            int t0 = Rules.PointsOf(team0Cards, contract);
            int t1 = Rules.PointsOf(team1Cards, contract);

            if (Seats.TeamOf(lastTrickWinnerSeat) == 0)
            {
                t0 += 10;
            }
            else
            {
                t1 += 10;
            }

            // No-trumps doubles what the cards are worth — but only the cards. Declarations are
            // added afterwards at face value, so a terca is 20 in no-trumps as in anything else.
            if (contract.Category == ContractCategory.NoTrumps)
            {
                t0 *= 2;
                t1 *= 2;
            }

            foreach (Announce a in activeAnnounces)
            {
                if (Seats.TeamOf(a.Seat) == 0)
                {
                    t0 += a.Value;
                }
                else
                {
                    t1 += a.Value;
                }
            }

            // Capot: a team that won no tricks gives +90 to the other.
            if (team0Cards.Count == 0)
            {
                t1 += 90;
                r.CapotForOneTeam = true;
            }

            if (team1Cards.Count == 0)
            {
                t0 += 90;
                r.CapotForOneTeam = true;
            }

            r.Team0Raw = t0;
            r.Team1Raw = t1;

            int declaringTeam = contract.DeclaringTeam;
            BidType type = contract.Type;

            // "Inside": the declaring side scored strictly less than the other. A tie is NOT
            // inside — the declarer holds on level points (0x47BBF5, `jge` skips the flag).
            r.Inside = declaringTeam == 0 ? t0 < t1 : t1 < t0;
            int multiplier = contract.Redoubled ? 4 : contract.Doubled ? 2 : 1;

            // Level points: neither side banks the round. The declaring side's half hangs to the
            // next deal, and the other side takes its own half — except under a double, where
            // the whole (multiplied) pot hangs and nobody scores at all.
            if (t0 == t1)
            {
                int half = RoundPoints(type, t0, true);
                if (multiplier > 1)
                {
                    r.HangingPoints = hangingPoints + RoundToTens((t0 + t1) * multiplier);
                }
                else if (declaringTeam == 0)
                {
                    r.Team1Board = half;
                    r.HangingPoints = hangingPoints + half;
                }
                else
                {
                    r.Team0Board = half;
                    r.HangingPoints = hangingPoints + half;
                }

                return r;
            }

            // A doubled contract, or the declarer going inside, collapses the round into a single
            // award (0x47BC1C): the whole pot times the doubling coefficient, to whichever side
            // scored more. When it was doubled the result is nudged to an even number of tens —
            // up in all-trumps, down in a suit contract, left alone in no-trumps.
            if (multiplier > 1 || r.Inside)
            {
                int winner = t1 >= t0 ? 1 : 0;
                int v = RoundToTens((t0 + t1) * multiplier);
                if (multiplier > 1 && (v & 1) != 0)
                {
                    if (type == BidType.AllTrumps)
                    {
                        v += 1;
                    }
                    else if (Bids.CategoryOf(type) == ContractCategory.Suit)
                    {
                        v -= 1;
                    }
                }

                if (winner == 0)
                {
                    r.Team0Board = v + hangingPoints;
                }
                else
                {
                    r.Team1Board = v + hangingPoints;
                }

                return r;
            }

            {
                // Made contract: the side with more points has its tens rounded, and the other
                // side takes whatever is left of the round's total, so the two always add up to
                // the round. Hanging points follow the leader, as they do on the doubled path.
                int total = RoundToTens(t0 + t1);
                int lead = Math.Max(t0, t1);
                int trail = Math.Min(t0, t1);
                int leadBoard = RoundPoints(type, lead, true);
                int trailBoard = total - leadBoard;

                // A side that took anything at all is never rounded away to nothing: with two
                // points against a hundred and sixty it still banks one, out of the leader's
                // share.
                if (trailBoard <= 0 && trail > 0)
                {
                    trailBoard = 1;
                    leadBoard -= 1;
                }

                if (t0 >= t1)
                {
                    r.Team0Board = leadBoard + hangingPoints;
                    r.Team1Board = trailBoard;
                }
                else
                {
                    r.Team1Board = leadBoard + hangingPoints;
                    r.Team0Board = trailBoard;
                }

                return r;
            }
        }

        // Board tens with contract-specific tie thresholds (see ScoreManager RE cross-check):
        // all-trumps rounds at 4/5, suit at 6/7 (so both teams' rounded halves sum to the
        // total tens); the winner keeps the exact-boundary remainder low. No-trumps rounds
        // to nearest ten.
        internal static int RoundPoints(BidType type, int points, bool winner)
        {
            if (type == BidType.AllTrumps)
            {
                int m = points % 10;
                if (m > 4)
                {
                    return (points / 10) + 1;
                }

                if (m == 4)
                {
                    return winner ? points / 10 : (points / 10) + 1;
                }

                return points / 10;
            }

            if (type == BidType.NoTrumps)
            {
                return RoundToTens(points);
            }

            int mm = points % 10;
            if (mm > 6)
            {
                return (points / 10) + 1;
            }

            if (mm == 6)
            {
                return winner ? points / 10 : (points / 10) + 1;
            }

            return points / 10;
        }

        private static int RoundToTens(int points) => (int)Math.Round(points / 10.0, MidpointRounding.AwayFromZero);
    }
}
