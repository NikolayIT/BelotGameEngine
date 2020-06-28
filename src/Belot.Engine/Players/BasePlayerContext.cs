namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class BasePlayerContext
    {
        public PlayerPosition FirstToPlay { get; set; }

        public PlayerPosition MyPosition { get; set; }

        public int SouthNorthTeamPoints { get; set; }

        public int EastWestTeamPoints { get; set; }

        public CardCollection MyCards { get; }

        public IList<Bid> Bids { get; set; }
    }
}
