namespace Belot.UI.Game
{
    /// <summary>How a deal ended for the declaring team.</summary>
    public enum RoundOutcome
    {
        /// <summary>Everybody passed: no contract, the deal is dealt again.</summary>
        PassedOut = 0,

        /// <summary>The declaring team scored more than the other team.</summary>
        Made = 1,

        /// <summary>The declaring team scored less: everything went to the other team.</summary>
        Inside = 2,

        /// <summary>Both teams scored the same: the declaring team's points hang.</summary>
        Hanging = 3,
    }
}
