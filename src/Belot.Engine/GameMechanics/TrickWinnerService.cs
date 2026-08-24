namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TrickWinnerService
    {
        public PlayerPosition GetWinner(Bid contract, IList<PlayCardAction> trickActions)
        {
            var firstCard = trickActions[0].Card;
            var bestAction = trickActions[0];
            if (contract.Type.HasFlag(BidType.AllTrumps))
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
            else if (contract.Type.HasFlag(BidType.NoTrumps))
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
                // A trump beats any non-trump; trumps race by trump order; with no trump in the
                // trick the led suit races by no-trump order. Single pass, no allocations.
                var trumpSuit = contract.Type.ToCardSuit();
                var bestIsTrump = firstCard.Suit == trumpSuit;
                for (var i = 1; i < trickActions.Count; i++)
                {
                    var card = trickActions[i].Card;
                    if (card.Suit == trumpSuit)
                    {
                        if (!bestIsTrump || card.TrumpOrder > bestAction.Card.TrumpOrder)
                        {
                            bestAction = trickActions[i];
                            bestIsTrump = true;
                        }
                    }
                    else if (!bestIsTrump && card.Suit == firstCard.Suit
                                          && card.NoTrumpOrder > bestAction.Card.NoTrumpOrder)
                    {
                        bestAction = trickActions[i];
                    }
                }
            }

            return bestAction.Player;
        }
    }
}
