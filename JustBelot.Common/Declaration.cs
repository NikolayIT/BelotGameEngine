namespace JustBelot.Common
{
    using System;

    // TODO: Rename to Combination
    public struct Declaration : IComparable, IComparable<Declaration>
    {
        public Declaration(DeclarationType declarationType, CardType toCardType)
            : this()
        {
            this.DeclarationType = declarationType;
            this.ToCardType = toCardType;
        }

        public Declaration(DeclarationType declarationType, CardType toCardType, CardSuit cardSuit)
            : this()
        {
            this.DeclarationType = declarationType;
            this.ToCardType = toCardType;
            this.CardSuit = cardSuit;
        }

        public DeclarationType DeclarationType { get; set; }

        public CardType ToCardType { get; set; }

        public CardSuit CardSuit { get; set; }

        public static bool operator ==(Declaration left, Declaration right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Declaration left, Declaration right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Declaration other)
        {
            return this.ToCardType == other.ToCardType && this.CardSuit == other.CardSuit && this.DeclarationType == other.DeclarationType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)this.ToCardType;
                hashCode = (hashCode * 397) ^ (int)this.CardSuit;
                hashCode = (hashCode * 397) ^ (int)this.DeclarationType;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is Declaration && this.Equals((Declaration)obj);
        }

        public int CompareTo(Declaration other)
        {
            if (this.Equals(other))
            {
                return 0;
            }

            return this.DeclarationType.CompareTo(other.DeclarationType);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return -1;
            }

            if (!(obj is Declaration))
            {
                return -1;
            }

            return this.CompareTo((Declaration)obj);
        }
    }
}
