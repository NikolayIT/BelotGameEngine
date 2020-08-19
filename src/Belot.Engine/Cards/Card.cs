namespace Belot.Engine.Cards
{
    using System.Runtime.CompilerServices;

    public sealed class Card
    {
        public static readonly Card[] AllCards = new Card[32];
        public static readonly CardSuit[] AllSuits = { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade, };
        public static readonly CardType[] AllTypes =
            {
                CardType.Seven, CardType.Eight, CardType.Nine, CardType.Ten,
                CardType.Jack, CardType.Queen, CardType.King, CardType.Ace,
            };

        private static readonly int[] TrumpOrders = { 1, 2, 7, 5, 8, 3, 4, 6 };
        private static readonly int[] NoTrumpOrders = { 1, 2, 3, 7, 4, 5, 6, 8 };

        private readonly int hashCode;

        static Card()
        {
            foreach (var suit in AllSuits)
            {
                foreach (var type in AllTypes)
                {
                    var card = new Card(suit, type);
                    AllCards[card.hashCode] = card;
                }
            }
        }

        private Card(CardSuit suit, CardType type)
        {
            this.hashCode = ((int)suit * 8) + (int)type;
            this.Suit = suit;
            this.Type = type;
            this.TrumpOrder = TrumpOrders[(int)this.Type];
            this.NoTrumpOrder = NoTrumpOrders[(int)this.Type];
        }

        public CardSuit Suit { get; }

        public CardType Type { get; }

        public int TrumpOrder { get; }

        public int NoTrumpOrder { get; }

        public static bool operator ==(Card left, Card right) => left?.hashCode == right?.hashCode;

        public static bool operator !=(Card left, Card right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Card GetCard(CardSuit suit, CardType type) => AllCards[((int)suit * 8) + (int)type];

        public override bool Equals(object obj) => obj is Card anotherCard && this.hashCode == anotherCard.hashCode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => this.hashCode;

        public override string ToString() => $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";
    }
}
