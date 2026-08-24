namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class ValidCardsService
    {
        private static readonly uint[] SuitMasks =
            {
                0b00000000000000000000000011111111u, // Clubs
                0b00000000000000001111111100000000u, // Diamonds
                0b00000000111111110000000000000000u, // Hearts
                0b11111111000000000000000000000000u, // Spades
            };

        // For every card: the bitmask of the cards of the same suit with a bigger trump order.
        private static readonly uint[] BiggerTrumpCardMasks = BuildBiggerTrumpCardMasks();

        public CardCollection GetValidCards(CardCollection playerCards, BidType contract, IList<PlayCardAction> currentTrickActions)
        {
            if (currentTrickActions.Count == 0 || playerCards.Count == 1)
            {
                // The player is first and can play any card or has only 1 card available
                return playerCards;
            }

            var firstCardSuit = currentTrickActions[0].Card.Suit;

            // Playing AllTrumps
            if (contract.HasFlag(BidType.AllTrumps))
            {
                return GetValidCardsForAllTrumps(playerCards, currentTrickActions, firstCardSuit);
            }

            // Playing NoTrumps
            if (contract.HasFlag(BidType.NoTrumps))
            {
                return GetValidCardsForNoTrumps(playerCards, firstCardSuit);
            }

            // Playing Clubs, Diamonds, Hearts or Spades
            var trumpSuit = contract.ToCardSuit();
            if (firstCardSuit == trumpSuit)
            {
                // Trump card played first
                return GetValidCardsForAllTrumps(playerCards, currentTrickActions, firstCardSuit);
            }

            // Playing Clubs, Diamonds, Hearts or Spades and non-trump card played first
            return GetValidCardsForTrumpWhenNonTrumpIsPlayedFirst(
                playerCards,
                trumpSuit,
                currentTrickActions,
                firstCardSuit);
        }

        // For all trumps the player should play bigger card from the same suit if available.
        // If bigger card is not available, the player should play any card of the same suit if available.
        private static CardCollection GetValidCardsForAllTrumps(
            CardCollection playerCards,
            IList<PlayCardAction> currentTrickActions,
            CardSuit firstCardSuit)
        {
            var cardsFromSuit = playerCards.BitMask & SuitMasks[(int)firstCardSuit];
            if (cardsFromSuit == 0)
            {
                // No card of the same suit available
                return playerCards;
            }

            var biggestCard = BiggestTrumpCard(currentTrickActions, firstCardSuit);
            var biggerCards = cardsFromSuit & BiggerTrumpCardMasks[biggestCard.GetHashCode()];
            if (biggerCards != 0)
            {
                // Has bigger card(s)
                return new CardCollection(biggerCards);
            }

            // Any other card from the same suit
            return FromBitMask(playerCards, cardsFromSuit);
        }

        // For no trumps the player should play card from the same suit if available, else any card is allowed.
        private static CardCollection GetValidCardsForNoTrumps(CardCollection playerCards, CardSuit firstCardSuit)
        {
            var cardsFromSuit = playerCards.BitMask & SuitMasks[(int)firstCardSuit];
            return cardsFromSuit == 0 ? playerCards : FromBitMask(playerCards, cardsFromSuit);
        }

        private static CardCollection GetValidCardsForTrumpWhenNonTrumpIsPlayedFirst(
            CardCollection playerCards,
            CardSuit trumpSuit,
            IList<PlayCardAction> currentTrickActions,
            CardSuit firstCardSuit)
        {
            var cardsFromSuit = playerCards.BitMask & SuitMasks[(int)firstCardSuit];
            if (cardsFromSuit != 0)
            {
                // If the player has the same card suit, he should play a card from the suit
                return FromBitMask(playerCards, cardsFromSuit);
            }

            var trumpCards = playerCards.BitMask & SuitMasks[(int)trumpSuit];
            if (trumpCards == 0)
            {
                // The player doesn't have any trump card or card from the played suit
                return playerCards;
            }

            // The winning card is the biggest trump if any was played, otherwise the biggest
            // card of the led suit (an off-suit discard can never win the trick).
            var biggestTrumpCard = BiggestTrumpWhenNonTrumpLed(currentTrickActions, trumpSuit);
            if (currentTrickActions.Count > 1)
            {
                // The teammate played a card.
                var biggestCard = biggestTrumpCard ?? BiggestNoTrumpCard(currentTrickActions, firstCardSuit);
                if (currentTrickActions[currentTrickActions.Count - 2].Card == biggestCard)
                {
                    // The teammate has the best card in current trick.
                    // The player is not obligatory to play any trump
                    return playerCards;
                }
            }

            // The current trick winner is the rivals of the current player
            if (biggestTrumpCard != null)
            {
                // Someone of the rivals has played trump card and is winning the trick
                var biggerTrumpCards = playerCards.BitMask & BiggerTrumpCardMasks[biggestTrumpCard.GetHashCode()];
                if (biggerTrumpCards != 0)
                {
                    // The player has bigger trump card(s) and should play one of them
                    return new CardCollection(biggerTrumpCards);
                }

                // The player hasn't any bigger trump card so he can play any card
                return playerCards;
            }

            // No one played trump card, but the player should play one of them
            return FromBitMask(playerCards, trumpCards);
        }

        // Returns the player's own collection when the filter keeps every card (no allocation).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CardCollection FromBitMask(CardCollection playerCards, uint bitMask)
        {
            return bitMask == playerCards.BitMask ? playerCards : new CardCollection(bitMask);
        }

        // The biggest card of the led suit when no trump has been played to the trick.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Card BiggestNoTrumpCard(IList<PlayCardAction> currentTrickActions, CardSuit firstCardSuit)
        {
            var bestCard = currentTrickActions[0].Card;
            if (currentTrickActions.Count > 1 && currentTrickActions[1].Card.Suit == firstCardSuit
                                              && currentTrickActions[1].Card.NoTrumpOrder > bestCard.NoTrumpOrder)
            {
                bestCard = currentTrickActions[1].Card;
            }

            if (currentTrickActions.Count > 2 && currentTrickActions[2].Card.Suit == firstCardSuit
                                              && currentTrickActions[2].Card.NoTrumpOrder > bestCard.NoTrumpOrder)
            {
                bestCard = currentTrickActions[2].Card;
            }

            return bestCard;
        }

        // The biggest trump played to a trick led by a non-trump card, or null when the trick
        // holds no trump. Unlike BiggestTrumpCard, the led card must not seed the comparison:
        // it is not a trump, so it cannot win once the trick was ruffed.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Card BiggestTrumpWhenNonTrumpLed(IList<PlayCardAction> currentTrickActions, CardSuit trumpSuit)
        {
            Card bestCard = null;
            for (var i = 0; i < currentTrickActions.Count; i++)
            {
                var card = currentTrickActions[i].Card;
                if (card.Suit == trumpSuit && (bestCard == null || card.TrumpOrder > bestCard.TrumpOrder))
                {
                    bestCard = card;
                }
            }

            return bestCard;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Card BiggestTrumpCard(IList<PlayCardAction> currentTrickActions, CardSuit firstCardSuit)
        {
            var bestCard = currentTrickActions[0].Card;
            if (currentTrickActions.Count > 1 && currentTrickActions[1].Card.Suit == firstCardSuit
                                              && currentTrickActions[1].Card.TrumpOrder > bestCard.TrumpOrder)
            {
                bestCard = currentTrickActions[1].Card;
            }

            if (currentTrickActions.Count > 2 && currentTrickActions[2].Card.Suit == firstCardSuit
                                              && currentTrickActions[2].Card.TrumpOrder > bestCard.TrumpOrder)
            {
                bestCard = currentTrickActions[2].Card;
            }

            return bestCard;
        }

        private static uint[] BuildBiggerTrumpCardMasks()
        {
            var masks = new uint[32];
            for (var hashCode = 0; hashCode < 32; hashCode++)
            {
                var card = Card.AllCards[hashCode];
                for (var otherHashCode = 0; otherHashCode < 32; otherHashCode++)
                {
                    var otherCard = Card.AllCards[otherHashCode];
                    if (otherCard.Suit == card.Suit && otherCard.TrumpOrder > card.TrumpOrder)
                    {
                        masks[hashCode] |= 1u << otherHashCode;
                    }
                }
            }

            return masks;
        }
    }
}
