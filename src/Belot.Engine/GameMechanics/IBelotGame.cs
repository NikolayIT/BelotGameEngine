namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Players;

    public interface IBelotGame
    {
        GameResult PlayGame(PlayerPosition firstToPlay);
    }
}
