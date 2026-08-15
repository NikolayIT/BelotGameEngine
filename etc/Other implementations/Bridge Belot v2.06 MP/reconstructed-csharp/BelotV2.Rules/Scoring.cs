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
    /// Round scoring. The card-point tables and the careta values were reverse-engineered
    /// from belot.exe; the surrounding scoring rules (last-10, no-trumps ×2, capot +90,
    /// double/redouble coefficients, the "inside"/hanging logic, and the tens rounding with
    /// its contract-specific thresholds) are the standard Bulgarian Belot rules. The exe's
    /// own scoring routine (FUN_0047AC00) is present but was too badly decompiled to transcribe
    /// line-by-line, so these follow the canonical rules (match target = 151, confirmed as
    /// 0x97 in choosegame). See README for the fidelity notes.
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

            if (Seats.TeamOf(lastTrickWinnerSeat) == 0)
            {
                t0 += 10;
            }
            else
            {
                t1 += 10;
            }

            if (contract.Category == ContractCategory.NoTrumps)
            {
                t0 *= 2;
                t1 *= 2;
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

            if (contract.Doubled || contract.Redoubled)
            {
                int coeff = contract.Redoubled ? 4 : 2;
                if (r.CapotForOneTeam)
                {
                    coeff = 1; // no tricks -> coefficient does not apply
                }

                int all = t0 + t1;
                if (t0 > t1)
                {
                    r.Team0Board = (RoundToTens(all) * coeff) + hangingPoints;
                }
                else if (t1 > t0)
                {
                    r.Team1Board = (RoundToTens(all) * coeff) + hangingPoints;
                }
                else
                {
                    r.HangingPoints = (RoundToTens(all) * coeff) + hangingPoints;
                }

                return r;
            }

            bool declWon = declaringTeam == 0 ? t0 >= t1 : t1 >= t0;

            // Contract team strictly inside -> all points to the other team.
            if (declaringTeam == 0 && t0 < t1)
            {
                r.Inside = true;
                r.Team1Board = RoundToTens(t0 + t1) + hangingPoints;
            }
            else if (declaringTeam == 1 && t1 < t0)
            {
                r.Inside = true;
                r.Team0Board = RoundToTens(t0 + t1) + hangingPoints;
            }
            else if (t0 == t1)
            {
                // Tie: the non-declaring team banks its rounded half; the declaring team's
                // half hangs to the next round.
                if (declaringTeam == 0)
                {
                    r.Team1Board = RoundPoints(type, t1, true);
                    r.HangingPoints = hangingPoints + RoundPoints(type, t0, false);
                }
                else
                {
                    r.Team0Board = RoundPoints(type, t0, true);
                    r.HangingPoints = hangingPoints + RoundPoints(type, t1, false);
                }
            }
            else
            {
                // Normal made contract.
                r.Team0Board = RoundPoints(type, t0, t0 > t1);
                r.Team1Board = RoundPoints(type, t1, t1 > t0);
                if (t0 > t1)
                {
                    r.Team0Board += hangingPoints;
                }
                else
                {
                    r.Team1Board += hangingPoints;
                }
            }

            return r;
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
