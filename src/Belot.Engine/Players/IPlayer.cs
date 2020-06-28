namespace Belot.Engine.Players
{
    public interface IPlayer
    {
        void Move(PlayerMoveGameContext gameContext);
    }

    public struct PlayerMoveGameContext
    {
    }
}
