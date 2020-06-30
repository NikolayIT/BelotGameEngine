namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            int hangingPoints)
        {
            var result = new RoundResult(contract);

            // Sum all south-north points
            result.SouthNorthTotalInRoundPoints = announces
                .Where(x => x.PlayerPosition == PlayerPosition.South || x.PlayerPosition == PlayerPosition.North)
                .Sum(x => x.Value);
            result.SouthNorthTotalInRoundPoints += southNorthTricks.Sum(x => x.GetValue(contract.Type));

            // Sum all east-west points
            result.EastWestTotalInRoundPoints = announces
                .Where(x => x.PlayerPosition == PlayerPosition.East || x.PlayerPosition == PlayerPosition.West)
                .Sum(x => x.Value);
            result.EastWestTotalInRoundPoints += eastWestTricks.Sum(x => x.GetValue(contract.Type));

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
            if ((contract.Type.HasFlag(BidType.Double) || contract.Type.HasFlag(BidType.ReDouble))
                && !result.NoTricksForOneOfTheTeams) //// When no tricks - double and re-double doesn't take place
            {
                var coefficient = contract.Type.HasFlag(BidType.ReDouble) ? 4 : 2;
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
                result.EastWestPoints += RoundPoints(contract.CleanBidType, result.EastWestTotalInRoundPoints, false);

                // "Hanging" points are added to current hanging points
                result.HangingPoints = hangingPoints + RoundPoints(
                                           contract.CleanBidType,
                                           result.SouthNorthTotalInRoundPoints,
                                           true);
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
                result.SouthNorthPoints += RoundPoints(contract.CleanBidType, result.SouthNorthTotalInRoundPoints, false);

                // "Hanging" points are added to current hanging points
                result.HangingPoints = hangingPoints + RoundPoints(
                                           contract.CleanBidType,
                                           result.EastWestTotalInRoundPoints,
                                           true);
            }
            else
            {
                // Normal game
                result.SouthNorthPoints = RoundPoints(
                    contract.CleanBidType,
                    result.SouthNorthTotalInRoundPoints,
                    result.SouthNorthTotalInRoundPoints > result.EastWestTotalInRoundPoints);

                result.EastWestPoints = RoundPoints(
                    contract.CleanBidType,
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

        private static int RoundPoints(int points)
        {
            return (int)Math.Round(points / 10.0);
        }

        private static int RoundPoints(BidType bidType, int points, bool winner)
        {
            switch (bidType)
            {
                case BidType.AllTrumps:
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
                case BidType.NoTrumps:
                    return (int)Math.Round(points / 10M);
                case BidType.Clubs:
                case BidType.Diamonds:
                case BidType.Hearts:
                case BidType.Spades:
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

            return points / 10;
        }
    }
}
