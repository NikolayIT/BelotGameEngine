namespace Belot.Engine.Players
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    public class PlayerGetAnnouncesContext : BasePlayerContext
    {
        public IList<AnnounceType> AvailableAnnounces { get; set; }
    }
}
