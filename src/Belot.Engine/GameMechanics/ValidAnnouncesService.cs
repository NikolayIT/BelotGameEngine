namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ValidAnnouncesService
    {
        public bool IsBeloteAllowed(CardCollection playerCards, BidType contract, IList<PlayCardAction> currentTrickActions, Card playedCard)
        {
            if (playedCard.Type != CardType.Queen && playedCard.Type != CardType.King)
            {
                return false;
            }

            if (contract.HasFlag(BidType.NoTrumps))
            {
                return false;
            }

            if (contract.HasFlag(BidType.AllTrumps))
            {
                if (currentTrickActions.Count > 0 && currentTrickActions[0].Card.Suit != playedCard.Suit)
                {
                    // Belote is only allowed when playing card from the same suit as the first card played
                    return false;
                }
            }
            else
            {
                // Clubs, Diamonds, Hearts or Spades
                if (playedCard.Suit != contract.ToCardSuit())
                {
                    // Belote is only allowed when playing card from the trump suit
                    return false;
                }
            }

            if (playedCard.Type == CardType.Queen)
            {
                return playerCards.Contains(Card.GetCard(playedCard.Suit, CardType.King));
            }

            if (playedCard.Type == CardType.King)
            {
                return playerCards.Contains(Card.GetCard(playedCard.Suit, CardType.Queen));
            }

            return false;
        }
    }
}
