namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public interface IPlayStrategy
    {
        PlayCardAction PlayCard(PlayerPlayCardContext context, CardCollection playedCards);
    }
}
