namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ScoreManager
    {
        public RoundResult GetScore(
            Bid contract,
            CardCollection southNorthTricks,
            CardCollection eastWestTricks,
            IList<Announce> announces,
            int hangingPoints,
            PlayerPosition lastTrickWinner)
        {
            var result = new RoundResult(contract);

            // Sum all south-north points
            result.SouthNorthTotalInRoundPoints = announces.Where(
                    x => x.IsActive == true && (x.Player == PlayerPosition.South || x.Player == PlayerPosition.North))
                .Sum(x => x.Value);
            result.SouthNorthTotalInRoundPoints += southNorthTricks.Sum(x => x.GetValue(contract.Type));
            if (lastTrickWinner == PlayerPosition.South || lastTrickWinner == PlayerPosition.North)
            {
                // Last 10
                result.SouthNorthTotalInRoundPoints += 10;
            }

            // Sum all east-west points
            result.EastWestTotalInRoundPoints = announces.Where(
                    x => x.IsActive == true && (x.Player == PlayerPosition.East || x.Player == PlayerPosition.West))
                .Sum(x => x.Value);
            result.EastWestTotalInRoundPoints += eastWestTricks.Sum(x => x.GetValue(contract.Type));
            if (lastTrickWinner == PlayerPosition.East || lastTrickWinner == PlayerPosition.West)
            {
                // Last 10
                result.EastWestTotalInRoundPoints += 10;
            }

            // Double no trump points
            if (contract.Type.HasFlag(BidType.NoTrumps))
            {
                result.SouthNorthTotalInRoundPoints *= 2;
                result.EastWestTotalInRoundPoints *= 2;
            }

            // 9 points for no tricks
            if (southNorthTricks.Count == 0)
            {
                result.EastWestTotalInRoundPoints += 90;
                result.NoTricksForOneOfTheTeams = true;
            }

            if (eastWestTricks.Count == 0)
            {
                result.SouthNorthTotalInRoundPoints += 90;
                result.NoTricksForOneOfTheTeams = true;
            }

            // Check if game is inside or hanging
            if (contract.Type.HasFlag(BidType.Double) || contract.Type.HasFlag(BidType.ReDouble))
            {
                var coefficient = contract.Type.HasFlag(BidType.ReDouble) ? 4 : 2;
                if (result.NoTricksForOneOfTheTeams)
                {
                    // When no tricks - double and re-double doesn't take place
                    coefficient = 1;
                }

                var allPoints = result.SouthNorthTotalInRoundPoints + result.EastWestTotalInRoundPoints;
                if (result.SouthNorthTotalInRoundPoints > result.EastWestTotalInRoundPoints)
                {
                    result.SouthNorthPoints += (RoundPoints(allPoints) * coefficient) + hangingPoints;
                }
                else if (result.EastWestTotalInRoundPoints > result.SouthNorthTotalInRoundPoints)
                {
                    result.EastWestPoints += (RoundPoints(allPoints) * coefficient) + hangingPoints;
                }
                else if (result.SouthNorthTotalInRoundPoints == result.EastWestTotalInRoundPoints)
                {
                    result.HangingPoints = (RoundPoints(allPoints) * coefficient) + hangingPoints;
                }
            }
            else if ((contract.Player == PlayerPosition.South || contract.Player == PlayerPosition.North) &&
                result.SouthNorthTotalInRoundPoints < result.EastWestTotalInRoundPoints)
            {
                // Inside -> all points goes to the other team
                result.EastWestPoints +=
                    RoundPoints(result.SouthNorthTotalInRoundPoints + result.EastWestTotalInRoundPoints)
                    + hangingPoints;
            }
            else if ((contract.Player == PlayerPosition.South || contract.Player == PlayerPosition.North)
                     && result.SouthNorthTotalInRoundPoints == result.EastWestTotalInRoundPoints)
            {
                // The other team gets its half of the points
                result.EastWestPoints += RoundPoints(contract.Type, result.EastWestTotalInRoundPoints, true);

                // "Hanging" points are added to current hanging points
                result.HangingPoints = hangingPoints + RoundPoints(
                                           contract.Type,
                                           result.SouthNorthTotalInRoundPoints,
                                           false);
            }
            else if ((contract.Player == PlayerPosition.East || contract.Player == PlayerPosition.West)
                && result.EastWestTotalInRoundPoints < result.SouthNorthTotalInRoundPoints)
            {
                // Inside -> all points goes to the other team
                result.SouthNorthPoints +=
                    RoundPoints(result.SouthNorthTotalInRoundPoints + result.EastWestTotalInRoundPoints)
                    + hangingPoints;
            }
            else if ((contract.Player == PlayerPosition.East || contract.Player == PlayerPosition.West)
                     && result.SouthNorthTotalInRoundPoints == result.EastWestTotalInRoundPoints)
            {
                // The other team gets its half of the points
                result.SouthNorthPoints += RoundPoints(contract.Type, result.SouthNorthTotalInRoundPoints, true);

                // "Hanging" points are added to current hanging points
                result.HangingPoints = hangingPoints + RoundPoints(
                                           contract.Type,
                                           result.EastWestTotalInRoundPoints,
                                           false);
            }
            else
            {
                // Normal game
                result.SouthNorthPoints = RoundPoints(
                    contract.Type,
                    result.SouthNorthTotalInRoundPoints,
                    result.SouthNorthTotalInRoundPoints > result.EastWestTotalInRoundPoints);

                result.EastWestPoints = RoundPoints(
                    contract.Type,
                    result.EastWestTotalInRoundPoints,
                    result.EastWestTotalInRoundPoints > result.SouthNorthTotalInRoundPoints);

                if (result.SouthNorthTotalInRoundPoints > result.EastWestTotalInRoundPoints)
                {
                    result.SouthNorthPoints += hangingPoints;
                }
                else if (result.EastWestTotalInRoundPoints > result.SouthNorthTotalInRoundPoints)
                {
                    result.EastWestPoints += hangingPoints;
                }
            }

            return result;
        }

        internal static int RoundPoints(BidType bidType, int points, bool winner)
        {
            // All trumps
            if (bidType.HasFlag(BidType.AllTrumps))
            {
                if (points % 10 > 4)
                {
                    return (points / 10) + 1;
                }

                if (points % 10 == 4)
                {
                    if (winner)
                    {
                        return points / 10;
                    }

                    return (points / 10) + 1;
                }

                return points / 10;
            }

            // No trumps
            if (bidType.HasFlag(BidType.NoTrumps))
            {
                return (int)Math.Round(points / 10M);
            }

            // Trump
            if (points % 10 > 6)
            {
                return (points / 10) + 1;
            }

            if (points % 10 == 6)
            {
                if (winner)
                {
                    return points / 10;
                }

                return (points / 10) + 1;
            }

            return points / 10;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundPoints(int points)
        {
            // TODO: Try (int)(points + 0.5d)
            return (int)Math.Round(points / 10.0);
        }
    }
}
