namespace BelotV2
{
    /// <summary>
    /// The bid ladder, matching ChoiseForm radio buttons rb0..rb8 in belot.exe:
    /// 0=Pass 1=Clubs 2=Diamonds 3=Hearts 4=Spades 5=NoTrumps 6=AllTrumps
    /// 7=Double(Контра) 8=Redouble(Реконтра).
    /// </summary>
    public enum BidType
    {
        Pass = 0,
        Clubs = 1,
        Diamonds = 2,
        Hearts = 3,
        Spades = 4,
        NoTrumps = 5,
        AllTrumps = 6,
        Double = 7,
        Redouble = 8,
    }

    /// <summary>Broad category that drives play + scoring.</summary>
    public enum ContractCategory
    {
        Suit,      // one suit is trump
        NoTrumps,  // no trump
        AllTrumps, // every suit is trump
    }

    public static class Bids
    {
        /// <summary>Rank of a contract-declaring bid on the ladder (Pass and Double/Redouble excluded).</summary>
        public static bool IsContract(BidType b) => b >= BidType.Clubs && b <= BidType.AllTrumps;

        public static ContractCategory CategoryOf(BidType b) => b switch
        {
            BidType.NoTrumps => ContractCategory.NoTrumps,
            BidType.AllTrumps => ContractCategory.AllTrumps,
            _ => ContractCategory.Suit,
        };

        /// <summary>Trump suit for a suit contract; null for no-trumps / all-trumps.</summary>
        public static Suit? TrumpSuitOf(BidType b) => b switch
        {
            BidType.Clubs => Suit.Clubs,
            BidType.Diamonds => Suit.Diamonds,
            BidType.Hearts => Suit.Hearts,
            BidType.Spades => Suit.Spades,
            _ => null,
        };

        public static string NameBg(BidType b) => b switch
        {
            BidType.Pass => "Пас",
            BidType.Clubs => "Спатия",
            BidType.Diamonds => "Каро",
            BidType.Hearts => "Купа",
            BidType.Spades => "Пика",
            BidType.NoTrumps => "Без коз",
            BidType.AllTrumps => "Всичко коз",
            BidType.Double => "Контра",
            BidType.Redouble => "Реконтра",
            _ => "?",
        };
    }

    /// <summary>The settled contract for a round.</summary>
    public sealed class Contract
    {
        public Contract(BidType type, int declarer, bool doubled, bool redoubled)
        {
            this.Type = type;
            this.Declarer = declarer;
            this.Doubled = doubled;
            this.Redoubled = redoubled;
        }

        public BidType Type { get; }

        /// <summary>Seat 0..3 of the player who set the contract.</summary>
        public int Declarer { get; }

        public bool Doubled { get; }

        public bool Redoubled { get; }

        public ContractCategory Category => Bids.CategoryOf(this.Type);

        public Suit? TrumpSuit => Bids.TrumpSuitOf(this.Type);

        /// <summary>Team (0 = seats 0&amp;2, 1 = seats 1&amp;3) that owns the contract.</summary>
        public int DeclaringTeam => this.Declarer & 1;

        public bool IsTrump(Suit s) => this.Category switch
        {
            ContractCategory.AllTrumps => true,
            ContractCategory.NoTrumps => false,
            _ => this.TrumpSuit == s,
        };
    }
}
