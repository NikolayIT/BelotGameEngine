namespace JustBelot.Common
{
    using System;
    using System.ComponentModel;

    public struct Card
    {
        public Card(CardType type, CardSuit suit)
            : this()
        {
            this.Type = type;
            this.Suit = suit;
        }

        public CardType Type { get; set; }

        public CardSuit Suit { get; set; }

        public static Card operator ++(Card card)
        {
            var newCard = new Card(card.Type, card.Suit);

            if (newCard.Suit != CardSuit.Spades)
            {
                newCard.Suit++;
            }
            else
            {
                if (newCard.Type == CardType.Ace)
                {
                    newCard.Type = CardType.Seven;
                    newCard.Suit = CardSuit.Clubs;
                }
                else
                {
                    newCard.Type++;
                    newCard.Suit = CardSuit.Clubs;
                }
            }

            return newCard;
        }

        public int GetValue(BidType contract)
        {
            if (contract == BidType.Pass || contract == BidType.Double || contract == BidType.ReDouble)
            {
                throw new InvalidEnumArgumentException("contract", (int)contract, typeof(BidType));
            }

            if (contract == BidType.AllTrumps
                || (contract == BidType.Clubs && this.Suit == CardSuit.Clubs)
                || (contract == BidType.Diamonds && this.Suit == CardSuit.Diamonds)
                || (contract == BidType.Hearts && this.Suit == CardSuit.Hearts)
                || (contract == BidType.Spades && this.Suit == CardSuit.Spades))
            {
                if (this.Type == CardType.Seven)
                {
                    return 0;
                }

                if (this.Type == CardType.Eight)
                {
                    return 0;
                }

                if (this.Type == CardType.Nine)
                {
                    return 14;
                }

                if (this.Type == CardType.Ten)
                {
                    return 10;
                }

                if (this.Type == CardType.Jack)
                {
                    return 20;
                }

                if (this.Type == CardType.Queen)
                {
                    return 3;
                }

                if (this.Type == CardType.King)
                {
                    return 4;
                }

                if (this.Type == CardType.Ace)
                {
                    return 11;
                }
            }
            else
            {
                // Non-trump card
                if (this.Type == CardType.Seven)
                {
                    return 0;
                }

                if (this.Type == CardType.Eight)
                {
                    return 0;
                }

                if (this.Type == CardType.Nine)
                {
                    return 0;
                }

                if (this.Type == CardType.Ten)
                {
                    return 10;
                }

                if (this.Type == CardType.Jack)
                {
                    return 2;
                }

                if (this.Type == CardType.Queen)
                {
                    return 3;
                }

                if (this.Type == CardType.King)
                {
                    return 4;
                }

                if (this.Type == CardType.Ace)
                {
                    return 11;
                }
            }

            throw new Exception("Unable to determine card value!");
        }

        public override int GetHashCode()
        {
            return ((int)this.Type * 8) + (int)this.Suit;
        }

        public override string ToString()
        {
            var type = CardsHelper.CardTypeToString(this.Type);
            var color = CardsHelper.CardSuitToString(this.Suit);
            return string.Format("{0}{1}", type, color);
        }
    }
}
