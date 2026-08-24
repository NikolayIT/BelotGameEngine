namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;
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

            // Sum all active announce points
            for (var i = 0; i < announces.Count; i++)
            {
                var announce = announces[i];
                if (announce.IsActive != true)
                {
                    continue;
                }

                if (announce.Player == PlayerPosition.South || announce.Player == PlayerPosition.North)
                {
                    result.SouthNorthTotalInRoundPoints += announce.Value;
                }
                else if (announce.Player == PlayerPosition.East || announce.Player == PlayerPosition.West)
                {
                    result.EastWestTotalInRoundPoints += announce.Value;
                }
            }

            // Sum all south-north points
            result.SouthNorthTotalInRoundPoints += SumCardValues(southNorthTricks, contract.Type);
            if (lastTrickWinner == PlayerPosition.South || lastTrickWinner == PlayerPosition.North)
            {
                // Last 10
                result.SouthNorthTotalInRoundPoints += 10;
            }

            // Sum all east-west points
            result.EastWestTotalInRoundPoints += SumCardValues(eastWestTricks, contract.Type);
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
                // The multiplier applies to everything, including the 90 for taking all the
                // tricks: "all bonuses are doubled including the bonus for getting all the hands".
                var coefficient = contract.Type.HasFlag(BidType.ReDouble) ? 4 : 2;
                var allPoints = result.SouthNorthTotalInRoundPoints + result.EastWestTotalInRoundPoints;
                var allPointsRounded = RoundPoints(contract.Type, allPoints, true);
                if (result.SouthNorthTotalInRoundPoints > result.EastWestTotalInRoundPoints)
                {
                    result.SouthNorthPoints += (allPointsRounded * coefficient) + hangingPoints;
                }
                else if (result.EastWestTotalInRoundPoints > result.SouthNorthTotalInRoundPoints)
                {
                    result.EastWestPoints += (allPointsRounded * coefficient) + hangingPoints;
                }
                else if (result.SouthNorthTotalInRoundPoints == result.EastWestTotalInRoundPoints)
                {
                    result.HangingPoints = (allPointsRounded * coefficient) + hangingPoints;
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
                return RoundPoints(points);
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

        private static int SumCardValues(CardCollection cards, BidType contractType)
        {
            var sum = 0;
            foreach (var card in cards)
            {
                sum += card.GetValue(contractType);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundPoints(int points) => (int)Math.Round(points / 10.0);
    }
}
