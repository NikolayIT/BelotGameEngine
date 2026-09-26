namespace Belot.AI.ClaudePlayer.Tests.TestHelpers
{
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;

    public class DealResult
    {
        public DealResult(uint[] originalHands, IList<Announce> announces, RoundResult score)
        {
            this.OriginalHands = originalHands;
            this.Announces = announces;
            this.Score = score;
        }

        public uint[] OriginalHands { get; }

        public IList<Announce> Announces { get; }

        public RoundResult Score { get; }
    }
}
