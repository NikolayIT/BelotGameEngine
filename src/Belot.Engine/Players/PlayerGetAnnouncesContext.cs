namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    public class PlayerGetAnnouncesContext : BasePlayerContext
    {
        public IEnumerable<Announce> Announces { get; set; }

        public IEnumerable<PlayCardAction> CurrentTrickActions { get; set; }

        /// <summary>
        /// Gets or sets the combinations the player may declare, carres first. A sequence that
        /// shares a card with a carre follows it as the alternative: of the declarations sharing
        /// a card only the first one counts (see ValidAnnouncesService.HaveCommonCards), so
        /// returning this list as it is declares the carre.
        /// </summary>
        public IList<Announce> AvailableAnnounces { get; set; }
    }
}
