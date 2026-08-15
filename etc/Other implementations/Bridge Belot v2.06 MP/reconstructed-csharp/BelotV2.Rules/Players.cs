namespace BelotV2
{
    /// <summary>A bid made by a seat during the auction.</summary>
    public readonly record struct BidAction(int Seat, BidType Bid);

    public sealed class BidContext
    {
        public required int Seat { get; init; }

        public required IReadOnlyList<Card> Hand { get; init; } // first 5 cards during the auction

        public required IReadOnlyList<BidAction> History { get; init; }

        /// <summary>Highest contract-declaring bid so far (Clubs..AllTrumps), or null if none.</summary>
        public BidType? HighestContract { get; init; }

        /// <summary>Seat that owns the current highest contract, or -1.</summary>
        public int ContractSeat { get; init; } = -1;

        public bool CurrentlyDoubled { get; init; }

        /// <summary>
        /// The match score of the two teams, [south-north, east-west]. The original bids more
        /// cautiously once a team is close to 151, so it needs this.
        /// </summary>
        public IReadOnlyList<int>? Board { get; init; }
    }

    public sealed class PlayContext
    {
        public required int Seat { get; init; }

        public required IReadOnlyList<Card> Hand { get; init; }

        public required Contract Contract { get; init; }

        public required IReadOnlyList<Play> CurrentTrick { get; init; }

        /// <summary>Every card played so far this round (all completed tricks, in order).</summary>
        public required IReadOnlyList<Play> PlayedHistory { get; init; }

        public required IReadOnlyList<Card> Legal { get; init; }

        /// <summary>
        /// The suit each seat named during the auction, indexed by seat 0..3, or null when it is
        /// not tracked. The original AI keeps this and reads it back as "the suit my partner asked
        /// for"; see <c>PlayMemory</c>.
        /// </summary>
        public IReadOnlyList<Suit?>? BidSuits { get; init; }

        /// <summary>
        /// The seat played by a human, or -1. It is the only seat whose discards the original
        /// reads as a signal, because that bookkeeping lives in the click handler rather than in
        /// the AI's own routine.
        /// </summary>
        public int HumanSeat { get; init; } = -1;
    }

    public interface IPlayer
    {
        string Name { get; }

        bool IsHuman => false;

        /// <summary>Return a legal bid. May be Pass, a higher contract, or Double/Redouble.</summary>
        BidType GetBid(BidContext context);

        /// <summary>Card to play; must be one of context.Legal.</summary>
        Card PlayCard(PlayContext context);
    }
}
