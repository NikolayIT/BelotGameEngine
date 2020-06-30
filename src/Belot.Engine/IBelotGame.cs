namespace Belot.Engine
{
    using Belot.Engine.Players;

    public interface IBelotGame
    {
        GameResult PlayGame(PlayerPosition firstToPlay);
    }
}
