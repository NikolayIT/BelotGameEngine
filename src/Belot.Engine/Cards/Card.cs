namespace Belot.Engine.Cards
{
    using System;

    public sealed class Card
    {
        private static readonly Card[] Cards = new Card[53];
        private readonly int hashCode;

        static Card()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(suit, type);
                    Cards[card.hashCode] = card;
                }
            }
        }

        private Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
            this.hashCode = ((int)this.Suit * 13) + (int)this.Type;
        }

        public CardSuit Suit { get; }

        public CardType Type { get; }

        public static bool operator ==(Card left, Card right)
        {
            return left?.Suit == right?.Suit && left?.Type == right?.Type;
        }

        public static bool operator !=(Card left, Card right)
        {
            return !(left == right);
        }

        public static Card GetCard(CardSuit suit, CardType type)
        {
            var code = ((int)suit * 13) + (int)type;
            if (code < 0 || code > 52)
            {
                throw new IndexOutOfRangeException("Invalid suit and type given.");
            }

            return Cards[code];
        }

        public override bool Equals(object obj)
        {
            return obj is Card anotherCard && this.Suit == anotherCard.Suit && this.Type == anotherCard.Type;
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }

        public override string ToString()
        {
            return $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";
        }
    }
}
