namespace BelotV2
{
    /// <summary>
    /// Constant tables recovered verbatim from belot.exe, used by the AI. All are cited by
    /// address so they can be checked against the binary.
    /// </summary>
    internal static class AiTables
    {
        // Per-card bidding weights, indexed by rank-7 (7,8,9,10,J,Q,K,A).
        // In the binary these are byte tables indexed directly by the 7..14 rank, based at
        // 0x489DAD (trump), 0x489DB5 (no-trumps), 0x489DBD (plain side suit).
        public static readonly int[] TrumpBid = { 0, 0, 12, 0, 20, 0, 0, 4 };    // @0x489DB4
        public static readonly int[] NoTrumpBid = { 0, 0, 0, 12, 0, 1, 6, 17 };  // @0x489DBC
        public static readonly int[] PlainBid = { 0, 0, 0, 3, 0, 0, 0, 10 };     // @0x489DC4

        // The recovered opening priority list @0x489D25 = [1,2,4,3,5,6] (Clubs, Diamonds,
        // Spades, Hearts, No-trumps, All-trumps) and the endgame score gate @0x478094
        // (`if (0x4A < score)` -> 74) are documented in BiddingAi; the main decision path is a
        // jump-table switch the decompilation does not resolve, so the gate there is calibrated.
    }
}
