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

        /// <summary>
        /// The combinations the player may declare: the carres, then the sequences of the cards
        /// outside them, then - when a carre card also belongs to a sequence - that sequence as
        /// the alternative. A card may take part in only one combination and the player chooses
        /// (hit.bg §Премии), so of the declarations sharing a card only the first one counts:
        /// declaring the whole list keeps the carre.
        /// </summary>
        /// <param name="playerCards">The player's hand.</param>
        /// <returns>The combinations, carres first.</returns>
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
            var remaining = fourOfAKinds;
            while (remaining != 0)
            {
                var type = (CardType)BitIndexOfLowestSetBit(remaining);
                remaining &= remaining - 1;
                var announceType = type == CardType.Jack ? AnnounceType.FourJacks :
                                   type == CardType.Nine ? AnnounceType.FourNines : AnnounceType.FourOfAKind;
                combinations.Add(new Announce(announceType, Card.GetCard(CardSuit.Spade, type)));
            }

            // The sequences of the cards the carres leave free.
            var free = ~fourOfAKinds;
            FindSequentialAnnounces(combinations, CardSuit.Club, clubs & free, 0);
            FindSequentialAnnounces(combinations, CardSuit.Diamond, diamonds & free, 0);
            FindSequentialAnnounces(combinations, CardSuit.Heart, hearts & free, 0);
            FindSequentialAnnounces(combinations, CardSuit.Spade, spades & free, 0);

            // The sequences through a carre card, which the player may declare instead of it.
            if (fourOfAKinds != 0)
            {
                FindSequentialAnnounces(combinations, CardSuit.Club, clubs, fourOfAKinds);
                FindSequentialAnnounces(combinations, CardSuit.Diamond, diamonds, fourOfAKinds);
                FindSequentialAnnounces(combinations, CardSuit.Heart, hearts, fourOfAKinds);
                FindSequentialAnnounces(combinations, CardSuit.Spade, spades, fourOfAKinds);
            }

            return combinations;
        }

        /// <summary>
        /// Whether two combinations use a common card, in which case only one of them may be
        /// declared. The belote never excludes anything: its king and queen may also be part of
        /// a sequence or a carre.
        /// </summary>
        /// <param name="first">A combination.</param>
        /// <param name="second">Another combination.</param>
        /// <returns>True when a card belongs to both.</returns>
        public bool HaveCommonCards(Announce first, Announce second) =>
            (GetCardsBitMask(first) & GetCardsBitMask(second)) != 0;

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

        // The cards of a combination as a CardCollection bitmask (none for the belote).
        internal static uint GetCardsBitMask(Announce announce)
        {
            var type = (int)announce.Card.Type;
            switch (announce.Type)
            {
                case AnnounceType.Belot:
                    return 0;
                case AnnounceType.FourOfAKind:
                case AnnounceType.FourNines:
                case AnnounceType.FourJacks:
                    return 0x01010101u << type;
                default:
                    // SequenceOf3 … SequenceOf8, identified by the top card.
                    var length = announce.Type - AnnounceType.SequenceOf3 + 3;
                    var lowest = type - length + 1;
                    if (lowest < 0)
                    {
                        throw new BelotGameException($"Invalid announce {announce.Type} to {announce.Card}.");
                    }

                    return ((1u << length) - 1) << (lowest + ((int)announce.Card.Suit * 8));
            }
        }

        // Adds the runs of 3+ cards in the suit; with requiredTypes set, only the runs that
        // contain one of those card types. Five or more cards in a row are one quint (100),
        // the whole suit included.
        private static void FindSequentialAnnounces(ICollection<Announce> combinations, CardSuit suit, uint suitBits, uint requiredTypes)
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

                var runTypes = ((1u << runLength) - 1) << (type - runLength);
                if (runLength >= 3 && (requiredTypes == 0 || (runTypes & requiredTypes) != 0))
                {
                    var announceType = (AnnounceType)((int)AnnounceType.SequenceOf3 + runLength - 3);
                    combinations.Add(new Announce(announceType, Card.GetCard(suit, (CardType)(type - 1))));
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
