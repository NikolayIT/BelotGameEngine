namespace BelotArena
{
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Takes its bids from one player and its card play from another.
    ///
    /// A win rate says which AI is better; it does not say at what. Pitting SmartPlayer against a
    /// hybrid that bids like SmartPlayer but plays like the 2001 AI holds the auction constant and
    /// measures the card play alone, and the mirror hybrid measures the bidding alone. Both halves
    /// are stateless between callbacks -- SmartPlayer rebuilds what it knows from the context on
    /// every call, and so does the original -- so neither notices it is only doing half the job.
    /// </summary>
    public sealed class MixedPlayer : IPlayer
    {
        private readonly IPlayer bidder;
        private readonly IPlayer cardPlayer;

        public MixedPlayer(IPlayer bidder, IPlayer cardPlayer)
        {
            this.bidder = bidder;
            this.cardPlayer = cardPlayer;
        }

        public BidType GetBid(PlayerGetBidContext context) => this.bidder.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) =>
            this.cardPlayer.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.cardPlayer.PlayCard(context);

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
            this.bidder.EndOfTrick(trickActions);
            this.cardPlayer.EndOfTrick(trickActions);
        }

        public void EndOfRound(RoundResult roundResult)
        {
            this.bidder.EndOfRound(roundResult);
            this.cardPlayer.EndOfRound(roundResult);
        }

        public void EndOfGame(GameResult gameResult)
        {
            this.bidder.EndOfGame(gameResult);
            this.cardPlayer.EndOfGame(gameResult);
        }
    }
}
