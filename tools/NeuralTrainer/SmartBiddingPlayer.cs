namespace Belot.NeuralTrainer
{
    using System.Collections.Generic;

    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>A player whose bids are SmartPlayer's: to judge another player's card play alone.</summary>
    internal sealed class SmartBiddingPlayer : IPlayer
    {
        private readonly IPlayer cards;
        private readonly SmartPlayer bidding = new SmartPlayer();

        public SmartBiddingPlayer(IPlayer cards)
        {
            this.cards = cards;
        }

        public BidType GetBid(PlayerGetBidContext context) => this.bidding.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.cards.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.cards.PlayCard(context);

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.cards.EndOfTrick(trickActions);

        public void EndOfRound(RoundResult roundResult) => this.cards.EndOfRound(roundResult);

        public void EndOfGame(GameResult gameResult) => this.cards.EndOfGame(gameResult);
    }
}
