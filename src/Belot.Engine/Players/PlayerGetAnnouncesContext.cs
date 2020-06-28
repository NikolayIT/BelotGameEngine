namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class PlayerGetAnnouncesContext
    {
        public PlayerPosition FirstToPlay { get; set; }

        public PlayerPosition MyPosition { get; set; }

        public CardCollection MyCards { get; }

        public IList<Bid> Bids { get; set; }

        public IList<AnnounceType> AvailableAnnounces { get; set; }
    }
}
