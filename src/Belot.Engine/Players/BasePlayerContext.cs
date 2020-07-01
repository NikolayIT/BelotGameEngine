namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    public class BasePlayerContext
    {
        public int RoundNumber { get; set; }

        public PlayerPosition FirstToPlayInTheRound { get; set; }

        public PlayerPosition MyPosition { get; set; }

        public int SouthNorthTeamPoints { get; set; }

        public int EastWestTeamPoints { get; set; }

        public IEnumerable<Card> MyCards { get; set; }

        public IEnumerable<Bid> Bids { get; set; }

        public Bid CurrentContract { get; set; }
    }
}
