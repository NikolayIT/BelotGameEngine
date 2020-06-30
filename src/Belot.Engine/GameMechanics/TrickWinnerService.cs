namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrickWinnerService
    {
        public PlayerPosition GetWinner(BidType contract, IList<PlayCardAction> trickActions)
        {
            var firstCard = trickActions[0].Card;
            var bestAction = trickActions[0];
            if (contract.HasFlag(BidType.AllTrumps))
            {
                for (var i = 1; i < trickActions.Count; i++)
                {
                    if (trickActions[i].Card.Suit == firstCard.Suit
                        && trickActions[i].Card.TrumpOrder > bestAction.Card.TrumpOrder)
                    {
                        bestAction = trickActions[i];
                    }
                }
            }
            else if (contract.HasFlag(BidType.NoTrumps))
            {
                for (var i = 1; i < trickActions.Count; i++)
                {
                    if (trickActions[i].Card.Suit == firstCard.Suit
                        && trickActions[i].Card.NoTrumpOrder > bestAction.Card.NoTrumpOrder)
                    {
                        bestAction = trickActions[i];
                    }
                }
            }
            else
            {
                var trumpSuit = contract.ToCardSuit();
                if (trickActions.Any(x => x.Card.Suit == trumpSuit))
                {
                    // Trump in the trick cards
                    for (var i = 1; i < trickActions.Count; i++)
                    {
                        if (trickActions[i].Card.Suit == trumpSuit
                            && trickActions[i].Card.TrumpOrder > bestAction.Card.TrumpOrder)
                        {
                            bestAction = trickActions[i];
                        }
                    }
                }
                else
                {
                    // No trick in the cards
                    for (var i = 1; i < trickActions.Count; i++)
                    {
                        if (trickActions[i].Card.Suit == firstCard.Suit
                            && trickActions[i].Card.NoTrumpOrder > bestAction.Card.NoTrumpOrder)
                        {
                            bestAction = trickActions[i];
                        }
                    }
                }
            }

            return bestAction.Player;
        }
    }
}
