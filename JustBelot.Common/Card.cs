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

        public static string TypeToString(CardType cardType)
        {
            string cardTypeAsString = null;
            switch (cardType)
            {
                case CardType.Ace:
                    cardTypeAsString = "A";
                    break;
                case CardType.King:
                    cardTypeAsString = "K";
                    break;
                case CardType.Queen:
                    cardTypeAsString = "Q";
                    break;
                case CardType.Jack:
                    cardTypeAsString = "J";
                    break;
                case CardType.Ten:
                    cardTypeAsString = "10";
                    break;
                case CardType.Nine:
                    cardTypeAsString = "9";
                    break;
                case CardType.Eight:
                    cardTypeAsString = "8";
                    break;
                case CardType.Seven:
                    cardTypeAsString = "7";
                    break;
            }
            return cardTypeAsString;
        }

        public static string SuitToString(CardSuit cardSuit)
        {
            string cardSuitAsString = null;
            switch (cardSuit)
            {
                case CardSuit.Spades:
                    cardSuitAsString = "\u2660";
                    break;
                case CardSuit.Hearts:
                    cardSuitAsString = "\u2665";
                    break;
                case CardSuit.Diamonds:
                    cardSuitAsString = "\u2666";
                    break;
                case CardSuit.Clubs:
                    cardSuitAsString = "\u2663";
                    break;
            }
            return cardSuitAsString;
        }

        public int GetValue(ContractType contract)
        {
            if (contract == ContractType.AllTrumps
                || (contract == ContractType.Clubs && this.Suit == CardSuit.Clubs)
                || (contract == ContractType.Diamonds && this.Suit == CardSuit.Diamonds)
                || (contract == ContractType.Hearts && this.Suit == CardSuit.Hearts)
                || (contract == ContractType.Spades && this.Suit == CardSuit.Spades))
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
            var type = TypeToString(this.Type);
            var color = SuitToString(this.Suit);
            return string.Format("{0}{1}", type, color);
        }
    }
}
