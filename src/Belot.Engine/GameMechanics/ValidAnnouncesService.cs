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

        public IList<Announce> GetAvailableAnnounces(CardCollection playerCards)
        {
            var combinations = new List<Announce>(2);

            // One byte per suit, bit index == card type in deck order (Seven=0 … Ace=7).
            var bits = playerCards.BitMask;
            var clubs = bits & 0xFFu;
            var diamonds = (bits >> 8) & 0xFFu;
            var hearts = (bits >> 16) & 0xFFu;
            var spades = bits >> 24;

            // Four of a kind: a type present in all four suits (sevens and eights don't count).
            var fourOfAKinds = clubs & diamonds & hearts & spades & 0b11111100u;
            while (fourOfAKinds != 0)
            {
                var type = (CardType)BitIndexOfLowestSetBit(fourOfAKinds);
                fourOfAKinds &= fourOfAKinds - 1;
                var announceType = type == CardType.Jack ? AnnounceType.FourJacks :
                                   type == CardType.Nine ? AnnounceType.FourNines : AnnounceType.FourOfAKind;
                combinations.Add(new Announce(announceType, Card.GetCard(CardSuit.Spade, type)));

                // A card may take part in only one combination, so remove the four cards
                // from the bytes used for the sequence detection below.
                var withoutType = ~(1u << (int)type);
                clubs &= withoutType;
                diamonds &= withoutType;
                hearts &= withoutType;
                spades &= withoutType;
            }

            FindSequentialAnnounces(combinations, CardSuit.Club, clubs);
            FindSequentialAnnounces(combinations, CardSuit.Diamond, diamonds);
            FindSequentialAnnounces(combinations, CardSuit.Heart, hearts);
            FindSequentialAnnounces(combinations, CardSuit.Spade, spades);
            return combinations;
        }

        public void UpdateActiveAnnounces(IList<Announce> announces)
        {
            Announce maxSameTypesAnnounce = null;
            Announce maxSameSuitAnnounce = null;
            for (var i = 0; i < announces.Count; i++)
            {
                var announce = announces[i];
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
            for (var i = 0; i < announces.Count; i++)
            {
                var announce = announces[i];
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
            for (var i = 0; i < announces.Count; i++)
            {
                var announce = announces[i];
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

        private static void FindSequentialAnnounces(ICollection<Announce> combinations, CardSuit suit, uint suitBits)
        {
            if (suitBits == 0)
            {
                return;
            }

            // Bits are in deck order, so sequences are runs of consecutive set bits. The loop
            // goes one position past the top bit so the last run is flushed too.
            var runLength = 0;
            for (var type = 0; type <= 8; type++)
            {
                if (type < 8 && ((suitBits >> type) & 1) == 1)
                {
                    runLength++;
                    continue;
                }

                switch (runLength)
                {
                    case 3:
                        combinations.Add(new Announce(AnnounceType.SequenceOf3, Card.GetCard(suit, (CardType)(type - 1))));
                        break;
                    case 4:
                        combinations.Add(new Announce(AnnounceType.SequenceOf4, Card.GetCard(suit, (CardType)(type - 1))));
                        break;
                    case 5:
                        combinations.Add(new Announce(AnnounceType.SequenceOf5, Card.GetCard(suit, (CardType)(type - 1))));
                        break;
                    case 6:
                        combinations.Add(new Announce(AnnounceType.SequenceOf6, Card.GetCard(suit, (CardType)(type - 1))));
                        break;
                    case 7:
                        combinations.Add(new Announce(AnnounceType.SequenceOf7, Card.GetCard(suit, (CardType)(type - 1))));
                        break;
                    case 8:
                        // A whole suit is declared as a quint on the top five cards plus a
                        // tierce on 9-8-7: a card may take part in only one combination, so
                        // the leftover tierce tops at the nine.
                        combinations.Add(new Announce(AnnounceType.SequenceOf8, Card.GetCard(suit, CardType.Ace)));
                        combinations.Add(new Announce(AnnounceType.SequenceOf3, Card.GetCard(suit, CardType.Nine)));
                        break;
                }

                runLength = 0;
            }
        }

        // The bit index of the lowest set bit (bits must be non-zero). The card type bytes have
        // only 8 bits, so a tiny shift loop beats a de Bruijn lookup here.
        private static int BitIndexOfLowestSetBit(uint bits)
        {
            var index = 0;
            while ((bits & 1) == 0)
            {
                bits >>= 1;
                index++;
            }

            return index;
        }
    }
}
