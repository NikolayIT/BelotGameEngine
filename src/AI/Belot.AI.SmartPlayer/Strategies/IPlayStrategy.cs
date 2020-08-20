namespace Belot.AI.SmartPlayer.Strategies
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public interface IPlayStrategy
    {
        PlayCardAction PlayFirst(PlayerPlayCardContext context, CardCollection playedCards);

        PlayCardAction PlaySecond(PlayerPlayCardContext context, CardCollection playedCards);

        PlayCardAction PlayThird(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner);

        PlayCardAction PlayFourth(PlayerPlayCardContext context, CardCollection playedCards, PlayerPosition trickWinner);
    }
}
