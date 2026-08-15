namespace BelotV2
{
    /// <summary>
    /// Suit index 0..3, exactly as stored at TAvSingleCard+0x20 in belot.exe.
    /// The concrete suit-name mapping is confirmed by the deck-building RE
    /// (see Deck.cs) and matches the Bulgarian bid order Clubs&lt;Diamonds&lt;Hearts&lt;Spades.
    /// </summary>
    public enum Suit : byte
    {
        Clubs = 0,     // Спатия
        Diamonds = 1,  // Каро
        Hearts = 2,    // Купа
        Spades = 3,    // Пика
    }

    /// <summary>
    /// Rank stored as 7..14 at TAvSingleCard+0x2c (7,8,9,10, J=11,Q=12,K=13,A=14).
    /// </summary>
    public enum Rank : byte
    {
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14,
    }

    /// <summary>An immutable 32-card-deck card: (suit, rank).</summary>
    public readonly struct Card : IEquatable<Card>
    {
        public Card(Suit suit, Rank rank)
        {
            this.Suit = suit;
            this.Rank = rank;
        }

        public Suit Suit { get; }

        public Rank Rank { get; }

        /// <summary>Index 0..7 into the strength/point tables (rank - 7).</summary>
        public int RankIndex => (int)this.Rank - 7;

        public bool Equals(Card other) => this.Suit == other.Suit && this.Rank == other.Rank;

        public override bool Equals(object? obj) => obj is Card c && this.Equals(c);

        public override int GetHashCode() => ((int)this.Suit * 8) + this.RankIndex;

        public override string ToString() => Cards.RankGlyph(this.Rank) + Cards.SuitGlyph(this.Suit);
    }

    /// <summary>
    /// Card strength orderings and point values, recovered verbatim from the
    /// DATA-section tables in belot.exe (indexed by rank-7):
    ///   trump order   @0x489D7C = [1,2,7,5,8,3,4,6]  -> J &gt; 9 &gt; A &gt; 10 &gt; K &gt; Q &gt; 8 &gt; 7
    ///   no-trump order@0x489D84 = [1,2,3,7,4,5,6,8]  -> A &gt; 10 &gt; K &gt; Q &gt; J &gt; 9 &gt; 8 &gt; 7
    ///   trump points  @0x489D9C = [0,0,14,10,20,3,4,11]
    ///   no-trump pts  @0x489DA4 = [0,0,0,10,2,3,4,11]
    /// </summary>
    public static class Cards
    {
        // index by (rank - 7): 7,8,9,10,J,Q,K,A
        public static readonly int[] TrumpOrder = { 1, 2, 7, 5, 8, 3, 4, 6 };
        public static readonly int[] NoTrumpOrder = { 1, 2, 3, 7, 4, 5, 6, 8 };
        public static readonly int[] TrumpPoints = { 0, 0, 14, 10, 20, 3, 4, 11 };
        public static readonly int[] NoTrumpPoints = { 0, 0, 0, 10, 2, 3, 4, 11 };

        /// <summary>Strength of a card when it IS trump (higher wins).</summary>
        public static int TrumpStrength(Card c) => TrumpOrder[c.RankIndex];

        /// <summary>Strength of a card when it is NOT trump (higher wins).</summary>
        public static int PlainStrength(Card c) => NoTrumpOrder[c.RankIndex];

        public static int TrumpPointValue(Card c) => TrumpPoints[c.RankIndex];

        public static int PlainPointValue(Card c) => NoTrumpPoints[c.RankIndex];

        public static string SuitGlyph(Suit s) => s switch
        {
            Suit.Clubs => "♣",     // ♣
            Suit.Diamonds => "♦",  // ♦
            Suit.Hearts => "♥",    // ♥
            Suit.Spades => "♠",    // ♠
            _ => "?",
        };

        public static string SuitNameBg(Suit s) => s switch
        {
            Suit.Clubs => "Спатия",
            Suit.Diamonds => "Каро",
            Suit.Hearts => "Купа",
            Suit.Spades => "Пика",
            _ => "?",
        };

        public static string RankGlyph(Rank r) => r switch
        {
            Rank.Seven => "7",
            Rank.Eight => "8",
            Rank.Nine => "9",
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => "?",
        };
    }
}
