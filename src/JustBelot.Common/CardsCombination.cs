namespace JustBelot.Common
{
    using System;

    public struct CardsCombination : IComparable, IComparable<CardsCombination>
    {
        public CardsCombination(CardsCombinationType combinationType, CardType toCardType)
            : this()
        {
            this.CombinationType = combinationType;
            this.ToCardType = toCardType;
        }

        public CardsCombination(CardsCombinationType combinationType, CardType toCardType, CardSuit cardSuit)
            : this()
        {
            this.CombinationType = combinationType;
            this.ToCardType = toCardType;
            this.CardSuit = cardSuit;
        }

        public CardsCombinationType CombinationType { get; set; }

        public CardType ToCardType { get; set; }

        public CardSuit CardSuit { get; set; }

        public static bool operator ==(CardsCombination left, CardsCombination right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardsCombination left, CardsCombination right)
        {
            return !left.Equals(right);
        }

        public bool Equals(CardsCombination other)
        {
            return this.ToCardType == other.ToCardType && this.CardSuit == other.CardSuit && this.CombinationType == other.CombinationType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)this.ToCardType;
                hashCode = (hashCode * 397) ^ (int)this.CardSuit;
                hashCode = (hashCode * 397) ^ (int)this.CombinationType;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is CardsCombination && this.Equals((CardsCombination)obj);
        }

        public int CompareTo(CardsCombination other)
        {
            if (this.Equals(other))
            {
                return 0;
            }

            return this.CombinationType.CompareTo(other.CombinationType);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return -1;
            }

            if (!(obj is CardsCombination))
            {
                return -1;
            }

            return this.CompareTo((CardsCombination)obj);
        }
    }
}
