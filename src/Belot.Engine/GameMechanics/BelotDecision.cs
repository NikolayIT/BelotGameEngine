namespace Belot.Engine.GameMechanics
{
    /// <summary>What the seat to move has to decide.</summary>
    public enum BelotDecision
    {
        /// <summary>Nobody has to decide anything: the match has not started or is over.</summary>
        None = 0,

        /// <summary>A bid (what IPlayer.GetBid answers).</summary>
        Bid = 1,

        /// <summary>
        /// Which combinations to declare, in the first trick, before the seat plays its card
        /// (what IPlayer.GetAnnounces answers). Only a seat that has combinations is asked.
        /// </summary>
        Announce = 2,

        /// <summary>A card, and whether to claim a belote with it (what IPlayer.PlayCard answers).</summary>
        PlayCard = 3,
    }
}
