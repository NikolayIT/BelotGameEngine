namespace JustBelot.Common
{
    public struct Card
    {
        public Card(CardType type, CardColor color)
            : this()
        {
            this.Type = type;
            this.Color = color;
        }

        public CardType Type { get; set; }

        public CardColor Color { get; set; }

        public static Card operator ++(Card card)
        {
            var newCard = new Card(card.Type, card.Color);

            if (newCard.Color != CardColor.Spades)
            {
                newCard.Color++;
            }
            else
            {
                if (newCard.Type == CardType.Ace)
                {
                    newCard.Type = CardType.Seven;
                    newCard.Color = CardColor.Club;
                }
                else
                {
                    newCard.Type++;
                    newCard.Color = CardColor.Club;
                }
            }

            return newCard;
        }

        public override string ToString()
        {
            return string.Format("{0}{1}", this.Type.ToString(), this.Color.ToString());
        }
    }
}
