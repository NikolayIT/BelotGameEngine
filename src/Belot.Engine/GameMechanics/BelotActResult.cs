namespace Belot.Engine.GameMechanics
{
    /// <summary>The outcome of <see cref="BelotMatch.Act"/> (and of <see cref="BelotMatch.Validate"/>).</summary>
    public enum BelotActResult
    {
        /// <summary>The action was applied (or would be).</summary>
        Ok = 0,

        /// <summary>
        /// The action is not legal for this player now: a bid that is not open (or has more than
        /// one flag), a card that may not be played, an action of the wrong kind for the pending
        /// decision, or null. Nothing changed.
        /// </summary>
        InvalidAction = 1,

        /// <summary>Another player must decide now. Nothing changed.</summary>
        NotYourTurn = 2,

        /// <summary>The match is over. Nothing changed.</summary>
        MatchFinished = 3,
    }
}
