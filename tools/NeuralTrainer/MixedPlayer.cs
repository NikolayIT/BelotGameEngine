namespace Belot.NeuralTrainer
{
    using System.Collections.Generic;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>One player's bids with another's card play: to judge either part alone.</summary>
    internal sealed class MixedPlayer : IPlayer
    {
        private readonly IPlayer bidding;
        private readonly IPlayer cards;

        public MixedPlayer(IPlayer bidding, IPlayer cards)
        {
            this.bidding = bidding;
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
