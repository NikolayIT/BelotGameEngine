namespace Belot.UI.Game
{
    using Belot.Engine.Players;

    /// <summary>The game is over: the winning team and the last deal.</summary>
    public sealed record GameOverInfo(PlayerPosition WinnerTeam, RoundEndInfo LastRound)
    {
        public bool WeWon => this.WinnerTeam == PlayerPosition.SouthNorthTeam;
    }
}
