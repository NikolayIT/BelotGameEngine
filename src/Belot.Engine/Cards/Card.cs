namespace Belot.Engine.Cards
{
    using System;

    using Belot.Engine.Game;

    public sealed class Card
    {
        public static readonly Card[] AllCards = new Card[32];
        public static readonly CardSuit[] AllSuits = { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade, };

        private static readonly int[] TrumpOrders = { 1, 2, 7, 5, 8, 3, 4, 6 };
        private static readonly int[] NoTrumpOrders = { 1, 2, 3, 7, 4, 5, 6, 8 };
        private static readonly int[] NoTrumpValues = { 0, 0, 0, 10, 2, 3, 4, 11 };
        private static readonly int[] TrumpValues = { 0, 0, 14, 10, 20, 3, 4, 11 };

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

        public static Card GetCard(CardSuit suit, CardType type) => AllCards[((int)suit * 8) + (int)type];

        public override bool Equals(object obj) => obj is Card anotherCard && this.hashCode == anotherCard.hashCode;

        public override int GetHashCode() => this.hashCode;

        public override string ToString() => $"{this.Type.ToFriendlyString()}{this.Suit.ToFriendlyString()}";

        public int GetValue(BidType contract) =>
            contract == BidType.Pass ? 0 :
            contract.HasFlag(BidType.AllTrumps) ? TrumpValues[(int)this.Type] :
            contract.HasFlag(BidType.NoTrumps) ? NoTrumpValues[(int)this.Type] :
            contract.ToCardSuit() == this.Suit ? TrumpValues[(int)this.Type] : NoTrumpValues[(int)this.Type];
    }
}
