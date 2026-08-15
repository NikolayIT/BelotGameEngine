namespace BelotV2
{
    using System.Text.Json;

    /// <summary>
    /// Dumps every recorded bid decision together with the six contract scores the original
    /// computed from, as CSV. The scoring is already exact, so the policy on top of it can be
    /// worked out from this table rather than guessed at.
    /// </summary>
    public static class BidScan
    {
        public static void Run(string path)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            Console.WriteLine("seat,holder,standing,b1,b2,b3,b4,board1,board2,seed,orig,s1,s2,s3,s4,s5,s6");
            foreach (JsonElement c in doc.RootElement.GetProperty("bidchoice").EnumerateArray())
            {
                var hand = c.GetProperty("hand").EnumerateArray()
                            .Select(e => new Card(
                                e[0].GetString() switch
                                {
                                    "C" => Suit.Clubs,
                                    "D" => Suit.Diamonds,
                                    "H" => Suit.Hearts,
                                    _ => Suit.Spades,
                                },
                                (Rank)e[1].GetInt32())).ToList();
                int[] bids = c.GetProperty("bids").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                int[] board = c.GetProperty("board").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                int holder = c.GetProperty("holder").GetInt32();
                var scores = new[]
                {
                    BidType.Clubs, BidType.Diamonds, BidType.Hearts,
                    BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
                }.Select(t => BiddingAi.ScoreContract(t, hand));

                Console.WriteLine($"{c.GetProperty("seat").GetInt32()},{holder},{bids[holder - 1]}," +
                                  $"{string.Join(",", bids)},{board[0]},{board[1]}," +
                                  $"{c.GetProperty("rngSeed").GetUInt32()},{c.GetProperty("bid").GetInt32()},{string.Join(",", scores)}");
            }
        }
    }
}
