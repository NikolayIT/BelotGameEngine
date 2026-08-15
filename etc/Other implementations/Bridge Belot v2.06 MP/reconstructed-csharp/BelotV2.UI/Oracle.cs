namespace BelotV2
{
    /// <summary>
    /// Deterministic decision dump — the reconstructed AI acting as a reference oracle.
    /// Every scenario is reproducible from a seed, and the output is line-oriented and stable so
    /// another Belot implementation can be run over the same inputs and diffed.
    ///
    ///   oracle bids [N] [seed]   dumps N hands with all six contract scores + the opening bid
    ///   oracle game [N] [seed]   dumps N full deals: hands, auction, every trick, and result
    ///
    /// The dump prints each player's exact 8-card hand, so a comparison engine can feed the same
    /// hands in without having to replicate the shuffle.
    /// </summary>
    public static class Oracle
    {
        public static void Run(string[] args)
        {
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "game";
            int n = args.Length > 2 && int.TryParse(args[2], out int a) ? a : 10;
            uint seed = args.Length > 3 && uint.TryParse(args[3], out uint s) ? s : 1u;

            if (sub == "bids")
            {
                DumpBids(n, seed);
            }
            else
            {
                DumpGames(n, seed);
            }
        }

        private static void DumpBids(int n, uint seed)
        {
            var rng = new DelphiRandom(seed);
            var order = new[]
            {
                BidType.Clubs, BidType.Diamonds, BidType.Hearts,
                BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
            };
            Console.WriteLine($"# bids  seed={seed}  n={n}  (score per contract, then opening bid)");
            for (int i = 0; i < n; i++)
            {
                var deck = Deck.BuildOrdered();
                Deck.Shuffle(deck, rng);
                var hand = new List<Card>();
                for (int k = 0; k < 5; k++)
                {
                    hand.Add(deck[k]); // first five, as at bid time
                }

                Game.SortHand(hand);
                var ctx = new BidContext
                {
                    Seat = 0, Hand = hand, History = Array.Empty<BidAction>(), HighestContract = null,
                };
                string scores = string.Join(" ", order.Select(
                    t => $"{Bids.NameBg(t)}={BiddingAi.ScoreContract(t, hand)}"));
                Console.WriteLine($"hand=[{string.Join(",", hand)}]  {scores}  -> {Bids.NameBg(BiddingAi.ChooseBid(ctx, new DelphiRandom(1)))}");
            }
        }

        private static void DumpGames(int n, uint seed)
        {
            var rng = new DelphiRandom(seed);
            Console.WriteLine($"# game  seed={seed}  n={n}");
            for (int i = 0; i < n; i++)
            {
                var players = new IPlayer[]
                {
                    new AiPlayer("S", rng), new AiPlayer("E", rng),
                    new AiPlayer("N", rng), new AiPlayer("W", rng),
                };
                int firstSeat = rng.Next(4);
                var recorder = new RecordingGame(players, rng);
                recorder.PlayOneContractedRound(firstSeat, i);
            }
        }

        // Plays deals from firstSeat until one is actually contracted, tracing every decision.
        private sealed class RecordingGame
        {
            private readonly IPlayer[] players;
            private readonly Game game;

            public RecordingGame(IPlayer[] players, DelphiRandom rng)
            {
                this.players = players;
                this.game = new Game(players, rng, null);
            }

            public void PlayOneContractedRound(int firstSeat, int index)
            {
                RoundOutcome outcome;
                int seat = firstSeat;
                do
                {
                    outcome = this.game.PlayRound(seat);
                    seat = Seats.Next(seat);
                }
                while (outcome.AllPassed);

                var c = outcome.Contract!;
                var r = outcome.Result!;
                Console.WriteLine($"--- deal {index}  first={Seats.NamesEn[firstSeat]}  " +
                                  $"contract={Bids.NameBg(c.Type)} by {Seats.NamesEn[c.Declarer]}" +
                                  (c.Doubled ? " X" : string.Empty) + (c.Redoubled ? "XX" : string.Empty) + " ---");
                foreach (Announce an in outcome.ActiveAnnounces)
                {
                    Console.WriteLine($"    announce {an.Kind} {Seats.NamesEn[an.Seat]} {an.Value}");
                }

                // Trick-by-trick play trace (4 plays each, in play order).
                for (int t = 0; t * 4 < outcome.Plays.Count; t++)
                {
                    var trick = outcome.Plays.GetRange(t * 4, 4);
                    int w = Rules.WinnerSeat(trick, c);
                    string line = string.Join(" ", trick.Select(p => $"{Seats.NamesEn[p.Seat][0]}:{p.Card}"));
                    Console.WriteLine($"    T{t + 1}: {line}  -> {Seats.NamesEn[w]}");
                }

                Console.WriteLine($"    raw S+N={r.Team0Raw} E+W={r.Team1Raw}  board S+N={r.Team0Board} E+W={r.Team1Board}" +
                                  (r.Inside ? " INSIDE" : string.Empty) + (r.CapotForOneTeam ? " CAPOT" : string.Empty));
            }
        }
    }
}
