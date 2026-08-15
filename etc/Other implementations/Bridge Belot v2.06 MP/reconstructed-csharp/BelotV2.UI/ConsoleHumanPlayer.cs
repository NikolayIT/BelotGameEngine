namespace BelotV2
{
    /// <summary>A human playing at the console (South, seat 0), mirroring the original's you-vs-3-AI mode.</summary>
    public sealed class ConsoleHumanPlayer : IPlayer
    {
        public ConsoleHumanPlayer(string name) => this.Name = name;

        public string Name { get; }

        public bool IsHuman => true;

        public BidType GetBid(BidContext ctx)
        {
            Console.WriteLine();
            Console.WriteLine($"Your hand: {FormatHand(ctx.Hand)}");
            if (ctx.HighestContract is BidType hc)
            {
                Console.WriteLine($"Highest bid so far: {Bids.NameBg(hc)}");
            }

            var options = new List<BidType> { BidType.Pass };
            foreach (BidType t in new[]
            {
                BidType.Clubs, BidType.Diamonds, BidType.Hearts,
                BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
            })
            {
                if (ctx.HighestContract is null || (int)t > (int)ctx.HighestContract)
                {
                    options.Add(t);
                }
            }

            if (ctx.HighestContract is not null && ctx.ContractSeat >= 0
                && !Seats.SameTeam(ctx.Seat, ctx.ContractSeat) && !ctx.CurrentlyDoubled)
            {
                options.Add(BidType.Double);
            }

            for (int i = 0; i < options.Count; i++)
            {
                Console.Write($"[{i}] {Bids.NameBg(options[i])}  ");
            }

            Console.WriteLine();
            return options[ReadIndex("Your bid", options.Count)];
        }

        public Card PlayCard(PlayContext ctx)
        {
            // The engine auto-plays when only one card is legal (as the original does).
            if (ctx.Legal.Count == 1)
            {
                return ctx.Legal[0];
            }

            Console.WriteLine();
            if (ctx.CurrentTrick.Count > 0)
            {
                Console.WriteLine("Trick: " + string.Join("  ", ctx.CurrentTrick.Select(
                    p => $"{Seats.NamesEn[p.Seat]}:{p.Card}")));
            }
            else
            {
                Console.WriteLine("You lead.");
            }

            Console.WriteLine($"Contract: {Bids.NameBg(ctx.Contract.Type)}");
            for (int i = 0; i < ctx.Legal.Count; i++)
            {
                Console.Write($"[{i}] {ctx.Legal[i]}  ");
            }

            Console.WriteLine();
            return ctx.Legal[ReadIndex("Play card", ctx.Legal.Count)];
        }

        private static string FormatHand(IReadOnlyList<Card> hand)
        {
            var sorted = new List<Card>(hand);
            Game.SortHand(sorted);
            return string.Join(" ", sorted);
        }

        private static int ReadIndex(string prompt, int count)
        {
            while (true)
            {
                Console.Write($"{prompt} [0-{count - 1}]: ");
                string? line = Console.ReadLine();
                if (int.TryParse(line, out int idx) && idx >= 0 && idx < count)
                {
                    return idx;
                }

                Console.WriteLine("Invalid choice.");
            }
        }
    }
}
