namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ValidCardsService
    {
        public CardCollection GetValidCards(CardCollection playerCards, BidType contract, IList<PlayCardAction> currentTrickActions)
        {
            if (currentTrickActions.Count == 0)
            {
                // The player is first and can play any card
                return playerCards;
            }

            var firstCardSuit = currentTrickActions[0].Card.Suit;

            // Playing AllTrumps
            if (contract == BidType.AllTrumps)
            {
                return GetValidCardsForAllTrumps(playerCards, currentTrickActions, firstCardSuit);
            }

            // Playing NoTrumps
            if (contract == BidType.NoTrumps)
            {
                return GetValidCardsForNoTrumps(playerCards, firstCardSuit);
            }

            // Playing Clubs, Diamonds, Hearts or Spades
            var trumpSuit = contract.ToCardSuit();
            if (firstCardSuit == trumpSuit)
            {
                // Trump card played first
                return GetValidCardsForTrumpWhenTrumpIsPlayedFirst(playerCards, currentTrickActions, firstCardSuit);
            }
            else
            {
                // Non-trump card played first
                return GetValidCardsForTrumpWhenNonTrumpIsPlayedFirst(
                    playerCards,
                    trumpSuit,
                    currentTrickActions,
                    firstCardSuit);
            }
        }

        // For all trumps the player should play bigger card from the same suit if available.
        // If bigger card is not available, the player should play any card of the same suit if available.
        private static CardCollection GetValidCardsForAllTrumps(
            CardCollection playerCards,
            IList<PlayCardAction> currentTrickActions,
            CardSuit firstCardSuit)
        {
            if (playerCards.Any(x => x.Suit == firstCardSuit))
            {
                var bestCard = currentTrickActions.Where(action => action.Card.Suit == firstCardSuit)
                    .OrderByDescending(x => x.Card.TrumpOrder).First().Card;

                if (playerCards.Any(card => card.Suit == firstCardSuit && card.TrumpOrder > bestCard.TrumpOrder))
                {
                    // Has bigger card(s)
                    return new CardCollection(
                        playerCards,
                        card => card.Suit == firstCardSuit && card.TrumpOrder > bestCard.TrumpOrder);
                }

                // Any other card from the same suit
                return new CardCollection(playerCards, card => card.Suit == firstCardSuit);
            }

            // No card of the same suit available
            return playerCards;
        }

        // For no trumps the player should play card from the same suit if available.
        private static CardCollection GetValidCardsForNoTrumps(CardCollection playerCards, CardSuit firstCardSuit)
        {
            if (playerCards.Any(x => x.Suit == firstCardSuit))
            {
                return new CardCollection(playerCards, x => x.Suit == firstCardSuit);
            }

            // No card of the same suit is available
            return playerCards;
        }

        private static CardCollection GetValidCardsForTrumpWhenTrumpIsPlayedFirst(
            CardCollection playerCards,
            IList<PlayCardAction> currentTrickActions,
            CardSuit firstCardSuit)
        {
            if (playerCards.Any(x => x.Suit == firstCardSuit))
            {
                var bestCard = currentTrickActions.Where(action => action.Card.Suit == firstCardSuit)
                    .OrderByDescending(x => x.Card.TrumpOrder).First().Card;

                if (playerCards.Any(card => card.Suit == firstCardSuit && card.TrumpOrder > bestCard.TrumpOrder))
                {
                    // Has bigger card(s)
                    return new CardCollection(
                        playerCards, card => card.Suit == firstCardSuit && card.TrumpOrder > bestCard.TrumpOrder);
                }

                // Any other card from the same suit
                return new CardCollection(playerCards, card => card.Suit == firstCardSuit);
            }

            // No card of the same suit available
            return playerCards;
        }

        private static CardCollection GetValidCardsForTrumpWhenNonTrumpIsPlayedFirst(
            CardCollection playerCards,
            CardSuit trumpSuit,
            IList<PlayCardAction> currentTrickActions,
            CardSuit firstCardSuit)
        {
            if (playerCards.Any(x => x.Suit == firstCardSuit))
            {
                // If the player has the same card suit, he should play a card from the suit
                return new CardCollection(playerCards, x => x.Suit == firstCardSuit);
            }

            // The player has not a card with the same suit
            if (playerCards.Any(x => x.Suit == trumpSuit))
            {
                var currentPlayerTeamIsCurrentTrickWinner = false;
                if (currentTrickActions.Count > 1)
                {
                    // The teammate played card
                    Card bestCard;
                    if (currentTrickActions.Any(x => x.Card.Suit == trumpSuit))
                    {
                        // Someone played trump
                        bestCard = currentTrickActions.Where(x => x.Card.Suit == trumpSuit)
                            .OrderByDescending(x => x.Card.TrumpOrder).First().Card;
                    }
                    else
                    {
                        // No one played trump
                        bestCard = currentTrickActions.OrderByDescending(x => x.Card.NoTrumpOrder).First().Card;
                    }

                    if (currentTrickActions[currentTrickActions.Count - 2].Card == bestCard)
                    {
                        // The teammate has the best card in current trick
                        currentPlayerTeamIsCurrentTrickWinner = true;
                    }
                }

                // The player has trump card
                if (currentPlayerTeamIsCurrentTrickWinner)
                {
                    // The current trick winner is the player or his teammate.
                    // The player is not obligatory to play any trump
                    return playerCards;
                }

                // The current trick winner is the rivals of the current player
                if (currentTrickActions.Any(x => x.Card.Suit == trumpSuit))
                {
                    // Someone of the rivals has played trump card and is winning the trick
                    var biggestTrumpCard = currentTrickActions.OrderByDescending(x => x.Card.TrumpOrder).First();
                    if (playerCards.Any(
                        x => x.Suit == trumpSuit && x.TrumpOrder > biggestTrumpCard.Card.TrumpOrder))
                    {
                        // The player has bigger trump card(s) and should play one of them
                        return new CardCollection(
                            playerCards,
                            x => x.Suit == trumpSuit && x.TrumpOrder > biggestTrumpCard.Card.TrumpOrder);
                    }

                    // The player hasn't any bigger trump card so he can play any card
                    return playerCards;
                }

                // No one played trump card, but the player should play one of them
                return new CardCollection(playerCards, x => x.Suit == trumpSuit);
            }

            // The player has not any trump card or card from the played suit
            return playerCards;
        }
    }
}
