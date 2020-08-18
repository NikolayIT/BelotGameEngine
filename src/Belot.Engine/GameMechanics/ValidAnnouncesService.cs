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

            return playerCards.Contains(
                playedCard.Type == CardType.Queen
                    ? Card.GetCard(playedCard.Suit, CardType.King)
                    : Card.GetCard(playedCard.Suit, CardType.Queen));
        }

        public ICollection<Announce> GetAvailableAnnounces(CardCollection playerCards)
        {
            var cards = new CardCollection(playerCards);
            var combinations = new List<Announce>(2);
            FindFourOfAKindAnnounces(cards, combinations);
            FindSequentialAnnounces(cards, combinations);
            return combinations;
        }

        public void UpdateActiveAnnounces(ICollection<Announce> announces)
        {
            Announce maxSameTypesAnnounce = null;
            Announce maxSameSuitAnnounce = null;
            foreach (var announce in announces)
            {
                if (announce.Type == AnnounceType.Belot)
                {
                }
                else if (announce.Type == AnnounceType.FourJacks || announce.Type == AnnounceType.FourNines
                                                                 || announce.Type == AnnounceType.FourOfAKind)
                {
                    if (announce.CompareTo(maxSameTypesAnnounce) > 0)
                    {
                        maxSameTypesAnnounce = announce;
                    }
                }
                else
                {
                    // Sequence
                    if (announce.CompareTo(maxSameSuitAnnounce) > 0)
                    {
                        maxSameSuitAnnounce = announce;
                    }
                }
            }

            // Check for same announces in different teams
            var sameMaxAnnounceInDifferentTeams = false;
            foreach (var announce in announces)
            {
                if (announce.Type == AnnounceType.SequenceOf3 || announce.Type == AnnounceType.SequenceOf4
                                                              || announce.Type == AnnounceType.SequenceOf5
                                                              || announce.Type == AnnounceType.SequenceOf6
                                                              || announce.Type == AnnounceType.SequenceOf7
                                                              || announce.Type == AnnounceType.SequenceOf8)
                {
                    if (announce.CompareTo(maxSameSuitAnnounce) == 0 && maxSameSuitAnnounce != null
                                                                     && !announce.Player.IsInSameTeamWith(maxSameSuitAnnounce.Player))
                    {
                        sameMaxAnnounceInDifferentTeams = true;
                    }
                }
            }

            // Mark announces that should be scored
            foreach (var announce in announces)
            {
                announce.IsActive = false;
                if (announce.Type == AnnounceType.Belot)
                {
                    announce.IsActive = true;
                }
                else if (announce.Type == AnnounceType.FourJacks || announce.Type == AnnounceType.FourNines
                                                                 || announce.Type == AnnounceType.FourOfAKind)
                {
                    if (announce.CompareTo(maxSameTypesAnnounce) >= 0 ||
                        (maxSameTypesAnnounce != null && announce.Player.IsInSameTeamWith(maxSameTypesAnnounce.Player)))
                    {
                        announce.IsActive = true;
                    }
                }
                else if (!sameMaxAnnounceInDifferentTeams)
                {
                    // Sequence
                    if (announce.CompareTo(maxSameSuitAnnounce) >= 0 ||
                        (maxSameSuitAnnounce != null && announce.Player.IsInSameTeamWith(maxSameSuitAnnounce.Player)))
                    {
                        announce.IsActive = true;
                    }
                }
            }
        }

        private static void FindFourOfAKindAnnounces(CardCollection cards, ICollection<Announce> combinations)
        {
            // Group by type
            var countOfCardTypes = new int[8];
            foreach (var card in cards)
            {
                countOfCardTypes[(int)card.Type]++;
            }

            // Check each type
            for (var i = 0; i < 8; i++)
            {
                var cardType = (CardType)i;
                if (countOfCardTypes[i] != 4 || cardType == CardType.Seven || cardType == CardType.Eight)
                {
                    continue;
                }

                switch (cardType)
                {
                    case CardType.Jack:
                        combinations.Add(
                            new Announce(AnnounceType.FourJacks, Card.GetCard(CardSuit.Spade, cardType)));
                        break;
                    case CardType.Nine:
                        combinations.Add(
                            new Announce(AnnounceType.FourNines, Card.GetCard(CardSuit.Spade, cardType)));
                        break;
                    case CardType.Ace:
                    case CardType.King:
                    case CardType.Queen:
                    case CardType.Ten:
                        combinations.Add(
                            new Announce(AnnounceType.FourOfAKind, Card.GetCard(CardSuit.Spade, cardType)));
                        break;
                }

                // Remove these cards from the available combination cards
                foreach (var card in cards)
                {
                    if (card.Type == cardType)
                    {
                        cards.Remove(card);
                    }
                }
            }
        }

        private static void FindSequentialAnnounces(CardCollection cards, ICollection<Announce> combinations)
        {
            // Group by suit
            var cardsBySuit = new[] { new List<Card>(4), new List<Card>(4), new List<Card>(4), new List<Card>(4) };
            foreach (var card in cards)
            {
                cardsBySuit[(int)card.Suit].Add(card);
            }

            // Check each suit
            for (var suitIndex = 0; suitIndex < 4; suitIndex++)
            {
                var suitedCards = cardsBySuit[suitIndex];
                if (suitedCards.Count < 3)
                {
                    continue;
                }

                suitedCards.Sort((card, card1) => card.Type.CompareTo(card1.Type));
                var previousCardValue = (int)suitedCards[0].Type;
                var count = 1;
                for (var i = 1; i < suitedCards.Count; i++)
                {
                    if ((int)suitedCards[i].Type == previousCardValue + 1)
                    {
                        count++;
                    }
                    else
                    {
                        switch (count)
                        {
                            case 3:
                                combinations.Add(new Announce(AnnounceType.SequenceOf3, suitedCards[i - 1]));
                                break;
                            case 4:
                                combinations.Add(new Announce(AnnounceType.SequenceOf4, suitedCards[i - 1]));
                                break;
                            case 5:
                                combinations.Add(new Announce(AnnounceType.SequenceOf5, suitedCards[i - 1]));
                                break;
                            case 6:
                                combinations.Add(new Announce(AnnounceType.SequenceOf6, suitedCards[i - 1]));
                                break;
                            //// Cases 7 and 8 cannot happen here, they are instead handled in the code after this for loop
                        }

                        count = 1;
                    }

                    previousCardValue = (int)suitedCards[i].Type;
                }

                switch (count)
                {
                    case 3:
                        combinations.Add(new Announce(AnnounceType.SequenceOf3, suitedCards[suitedCards.Count - 1]));
                        break;
                    case 4:
                        combinations.Add(new Announce(AnnounceType.SequenceOf4, suitedCards[suitedCards.Count - 1]));
                        break;
                    case 5:
                        combinations.Add(new Announce(AnnounceType.SequenceOf5, suitedCards[suitedCards.Count - 1]));
                        break;
                    case 6:
                        combinations.Add(new Announce(AnnounceType.SequenceOf6, suitedCards[suitedCards.Count - 1]));
                        break;
                    case 7:
                        combinations.Add(new Announce(AnnounceType.SequenceOf7, suitedCards[suitedCards.Count - 1]));
                        break;
                    case 8:
                        combinations.Add(new Announce(AnnounceType.SequenceOf8, suitedCards[suitedCards.Count - 1]));
                        combinations.Add(new Announce(AnnounceType.SequenceOf3, suitedCards[suitedCards.Count - 1]));
                        break;
                }
            }
        }
    }
}
