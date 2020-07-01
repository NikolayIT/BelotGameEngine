namespace Belot.Engine.Cards
{
    using System;

    using Belot.Engine.Game;

    public sealed class Card
    {
        public static readonly Card[] AllCards = new Card[32];
        private readonly int hashCode;

        static Card()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(suit, type);
                    AllCards[card.hashCode] = card;
                }
            }
        }

        private Card(CardSuit suit, CardType type)
        {
            this.Suit = suit;
            this.Type = type;
            this.hashCode = ((int)this.Suit * 8) + (int)this.Type;
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
            return AllCards[((int)suit * 8) + (int)type];
        }

        public int GetValue(BidType contract)
        {
            if (contract == BidType.Pass)
            {
                return 0;
            }

            if (contract.HasFlag(BidType.NoTrumps))
            {
                return this.Type == CardType.Seven ? 0 :
                       this.Type == CardType.Eight ? 0 :
                       this.Type == CardType.Nine ? 0 :
                       this.Type == CardType.Ten ? 10 :
                       this.Type == CardType.Jack ? 2 :
                       this.Type == CardType.Queen ? 3 :
                       this.Type == CardType.King ? 4 :
                       this.Type == CardType.Ace ? 11 : 0;
            }

            return this.Type == CardType.Seven ? 0 :
                this.Type == CardType.Eight ? 0 :
                this.Type == CardType.Nine ? 14 :
                this.Type == CardType.Ten ? 10 :
                this.Type == CardType.Jack ? 20 :
                this.Type == CardType.Queen ? 3 :
                this.Type == CardType.King ? 4 :
                this.Type == CardType.Ace ? 11 : 0;
        }

        public override bool Equals(object obj)
        {
            return obj is Card anotherCard && this.Suit == anotherCard.Suit && this.Type == anotherCard.Type;
        }

        public override int GetHashCode() => this.hashCode;

        public override string ToString() => $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";
    }
}
