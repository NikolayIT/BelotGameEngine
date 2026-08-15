namespace BelotV2
{
    /// <summary>
    /// Live head-to-head between an engine and the ORIGINAL AI running out of belot.exe.
    ///
    ///   compare &lt;rounds&gt; [pathToAiServer] [python]
    ///
    /// Every card decision is put to both the transcribed <see cref="OriginalPlayAi"/> and the real
    /// routine; the deals are driven by the original's choices, so the positions visited are the
    /// ones the original would actually reach. Swap in your own decision function to measure
    /// another engine against the authentic AI.
    /// </summary>
    public static class Compare
    {
        public static void Run(string[] args)
        {
            int rounds = args.Length > 1 && int.TryParse(args[1], out int r) ? r : 10;
            string script = args.Length > 2
                ? args[2]
                : Path.Combine("..", "tools", "ai_server.py");
            string python = args.Length > 3 ? args[3] : "python";

            if (!File.Exists(script))
            {
                Console.Error.WriteLine($"ai_server.py not found at {script}");
                Console.Error.WriteLine("pass its path: compare <rounds> <path-to-ai_server.py> [python]");
                return;
            }

            using var original = new OriginalAiPlayer("original", script, python);
            var rng = new DelphiRandom(4242);
            int total = 0, agree = 0;
            var byContext = new Dictionary<string, (int total, int agree)>();

            for (int round = 0; round < rounds; round++)
            {
                var contract = new Contract(
                    (BidType)(1 + rng.Next(6)), rng.Next(4), false, false);
                Card[] deck = Deck.BuildOrdered();
                Deck.Shuffle(deck, rng);
                var hands = new List<Card>[4];
                for (int i = 0; i < 4; i++)
                {
                    hands[i] = new List<Card>(deck[(i * 8)..((i + 1) * 8)]);
                    Game.SortHand(hands[i]);
                }

                original.Declarer = contract.Declarer + 1;
                int leader = rng.Next(4);
                var history = new List<Play>();

                for (int t = 0; t < 8; t++)
                {
                    var trick = new List<Play>();
                    for (int k = 0; k < 4; k++)
                    {
                        int seat = (leader + k) & 3;
                        var legal = Rules.ValidCards(hands[seat], trick, contract, seat);
                        var ctx = new PlayContext
                        {
                            Seat = seat,
                            Hand = hands[seat],
                            Contract = contract,
                            CurrentTrick = trick,
                            PlayedHistory = history,
                            Legal = legal,
                            HumanSeat = 0,   // seat 1 in the original: the human's seat
                        };

                        Card chosen;
                        if (seat == 0 || legal.Count == 1)
                        {
                            // seat 1 is the human in the original, so it has no AI decision there
                            chosen = legal[0];
                        }
                        else
                        {
                            Card fromOriginal = original.PlayCard(ctx);
                            Card fromPort = OriginalPlayAdapter.Play(ctx, rng);
                            total++;
                            string key = trick.Count == 0 ? "lead"
                                : (Seats.SameTeam(seat, trick[Rules.WinnerIndex(trick, contract)].Seat)
                                    ? $"seat{trick.Count + 1}-partner-wins"
                                    : $"seat{trick.Count + 1}-opp-wins");
                            var agg = byContext.GetValueOrDefault(key);
                            bool same = fromPort.Equals(fromOriginal);
                            byContext[key] = (agg.total + 1, agg.agree + (same ? 1 : 0));
                            if (same)
                            {
                                agree++;
                            }

                            chosen = fromOriginal;   // follow the original's line of play
                        }

                        hands[seat].Remove(chosen);
                        trick.Add(new Play(seat, chosen));
                    }

                    history.AddRange(trick);
                    leader = Rules.WinnerSeat(trick, contract);
                }

                Console.Write($"\rround {round + 1}/{rounds}  agreement {100.0 * agree / Math.Max(total, 1):F1}%   ");
            }

            Console.WriteLine();
            Console.WriteLine($"Transcribed OriginalPlayAi vs the ORIGINAL AI: {agree}/{total} " +
                              $"({100.0 * agree / Math.Max(total, 1):F1}%)");
            foreach (var kv in byContext.OrderBy(k => k.Key))
            {
                Console.WriteLine($"    {kv.Key,-22} {kv.Value.agree}/{kv.Value.total} " +
                                  $"({100.0 * kv.Value.agree / kv.Value.total:F0}%)");
            }
        }
    }
}
