namespace Belot.NeuralTrainer
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>Times another player's card decisions (those with a choice).</summary>
    internal sealed class TimedPlayer : IPlayer
    {
        private readonly IPlayer player;

        public TimedPlayer(IPlayer player)
        {
            this.player = player;
        }

        public long Decisions { get; private set; }

        public long Ticks { get; private set; }

        public List<long> DecisionTicks { get; } = new List<long>();

        public BidType GetBid(PlayerGetBidContext context) => this.player.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.player.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var start = Stopwatch.GetTimestamp();
            var action = this.player.PlayCard(context);
            var elapsed = Stopwatch.GetTimestamp() - start;
            this.Ticks += elapsed;
            this.DecisionTicks.Add(elapsed);
            this.Decisions++;
            return action;
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.player.EndOfTrick(trickActions);

        public void EndOfRound(RoundResult roundResult) => this.player.EndOfRound(roundResult);

        public void EndOfGame(GameResult gameResult) => this.player.EndOfGame(gameResult);
    }
}
