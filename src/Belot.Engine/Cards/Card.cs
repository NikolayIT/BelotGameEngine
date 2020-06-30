namespace Belot.Engine.Cards
{
    using System;

    using Belot.Engine.Game;

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

        public int TrumpOrder =>
            this.Type switch
                {
                    CardType.Seven => 1,
                    CardType.Eight => 2,
                    CardType.Queen => 3,
                    CardType.King => 4,
                    CardType.Ten => 5,
                    CardType.Ace => 6,
                    CardType.Nine => 7,
                    CardType.Jack => 8,
                    _ => 0,
                };

        public int NoTrumpOrder =>
            this.Type switch
                {
                    CardType.Seven => 1,
                    CardType.Eight => 2,
                    CardType.Nine => 3,
                    CardType.Jack => 4,
                    CardType.Queen => 5,
                    CardType.King => 6,
                    CardType.Ten => 7,
                    CardType.Ace => 8,
                    _ => 0,
                };

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

        public int GetValue(BidType contract)
        {
            if (contract == BidType.Pass)
            {
                return 0;
            }

            if (contract.HasFlag(BidType.NoTrumps))
            {
                return this.Type switch
                    {
                        CardType.Seven => 0,
                        CardType.Eight => 0,
                        CardType.Nine => 0,
                        CardType.Ten => 10,
                        CardType.Jack => 2,
                        CardType.Queen => 3,
                        CardType.King => 4,
                        CardType.Ace => 11,
                        _ => 0,
                    };
            }

            return this.Type switch
                {
                    CardType.Seven => 0,
                    CardType.Eight => 0,
                    CardType.Nine => 14,
                    CardType.Ten => 10,
                    CardType.Jack => 20,
                    CardType.Queen => 3,
                    CardType.King => 4,
                    CardType.Ace => 11,
                    _ => 0,
                };
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
