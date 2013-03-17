namespace JustBelot.Tests.GameLogicTest
{
    using System;

    using JustBelot.AI.DummyPlayer;
    using JustBelot.Common;

    internal class DebugDummyPlayer : DummyPlayer
    {
        public DebugDummyPlayer(string name)
            : base(name)
        {
        }

        public override void EndOfDeal(DealResult dealResult)
        {
            Console.WriteLine("{0} - {1} (contract kept: {2}, no tricks: {3})", dealResult.SouthNorthPoints, dealResult.EastWestPoints, !dealResult.ContractNotKept, dealResult.NoTricksForOneOfTheTeams);
        }
    }
}