namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    public class PlayerGetAnnouncesContext : BasePlayerContext
    {
        public IEnumerable<Announce> Announces { get; set; }

        public IEnumerable<PlayCardAction> CurrentTrickActions { get; set; }

        public IReadOnlyList<Announce> AvailableAnnounces { get; set; }
    }
}
