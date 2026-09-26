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

        public int SouthNorthPoints { get; set; }

        public int EastWestPoints { get; set; }

        /// <summary>
        /// Gets or sets the points hanging from earlier deals: they go to the team that wins the
        /// next played deal (see <see cref="GameMechanics.RoundResult.HangingPoints"/>).
        /// </summary>
        public int HangingPoints { get; set; }

        public CardCollection MyCards { get; set; }

        public IEnumerable<Bid> Bids { get; set; }

        public Bid CurrentContract { get; set; }
    }
}
