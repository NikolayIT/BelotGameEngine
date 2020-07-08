namespace Belot.UI.Windows
{
    using System.Collections.Generic;

    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class OpenCardsPlayerDecorator : IPlayer
    {
        private readonly IPlayer realPlayer;

        public OpenCardsPlayerDecorator(IPlayer realPlayer)
        {
            this.realPlayer = realPlayer;
            this.Cards = new CardCollection();
        }

        public CardCollection Cards { get; set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.Cards = context.MyCards;
            return this.realPlayer.GetBid(context);
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            this.Cards = context.MyCards;
            return this.realPlayer.GetAnnounces(context);
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.Cards = context.MyCards;
            return this.realPlayer.PlayCard(context);
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
            this.realPlayer.EndOfTrick(trickActions);
        }

        public void EndOfRound(RoundResult roundResult)
        {
            this.realPlayer.EndOfRound(roundResult);
        }

        public void EndOfGame(GameResult gameResult)
        {
            this.realPlayer.EndOfGame(gameResult);
        }
    }
}
