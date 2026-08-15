using BelotV2;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "play";

switch (mode)
{
    case "selftest":
        SelfTest.Run();
        break;
    case "sim":
        Simulation.Run(args.Length > 1 && int.TryParse(args[1], out int n) ? n : 200);
        break;
    case "vsrandom":
        Simulation.RunVsRandom(args.Length > 1 && int.TryParse(args[1], out int nr) ? nr : 200);
        break;
    case "oracle":
        Oracle.Run(args);
        break;
    case "verify":
        Environment.ExitCode = Verify.Run(args.Length > 1 ? args[1] : "golden.json");
        break;
    case "bidscan":
        BidScan.Run(args.Length > 1 ? args[1] : "golden_bids.json");
        break;
    case "verifystate":
        Environment.ExitCode = VerifyState.Run(args.Length > 1 ? args[1] : "golden_play.json");
        break;
    case "tracestate":
        VerifyState.Trace(args[1], args.Length > 2 ? args[2] : "Diamonds",
                          args.Length > 3 && int.TryParse(args[3], out int tl) ? tl : 3);
        break;
    case "compare":
        Compare.Run(args);
        break;
    default:
        PlayInteractive();
        break;
}

static void PlayInteractive()
{
    var rng = new DelphiRandom((uint)Environment.TickCount);
    var players = new IPlayer[]
    {
        new ConsoleHumanPlayer("You (South)"),
        new AiPlayer("Lutien (East)", rng),
        new AiPlayer("Aragorn (North)", rng),
        new AiPlayer("Galadriel (West)", rng),
    };

    Console.WriteLine("=== Bridge Belot v2.06 (reconstructed) — you are South ===");
    var game = new Game(players, rng, Console.WriteLine) { HumanSeat = 0 };
    int firstSeat = rng.Next(4);
    int winner = game.PlayMatch(firstSeat);
    Console.WriteLine();
    Console.WriteLine($"Final score  S+N {game.Board[0]} : {game.Board[1]} E+W");
    Console.WriteLine(winner == 0 ? "Your team (South+North) wins!" : "Opponents (East+West) win.");
}

static class SelfTest
{
    public static void Run()
    {
        var clubs = new Contract(BidType.Clubs, 0, false, false);
        var noTrumps = new Contract(BidType.NoTrumps, 0, false, false);
        var allTrumps = new Contract(BidType.AllTrumps, 0, false, false);

        Check("all-trumps J>A", Rules.WinnerSeat(new List<Play>
        {
            new(0, new Card(Suit.Hearts, Rank.Ace)),
            new(1, new Card(Suit.Hearts, Rank.Jack)),
            new(2, new Card(Suit.Hearts, Rank.Nine)),
            new(3, new Card(Suit.Spades, Rank.Ace)),
        }, allTrumps), 1);

        Check("clubs ruff wins", Rules.WinnerSeat(new List<Play>
        {
            new(0, new Card(Suit.Hearts, Rank.Ace)),
            new(1, new Card(Suit.Clubs, Rank.Seven)),
            new(2, new Card(Suit.Hearts, Rank.Ten)),
            new(3, new Card(Suit.Hearts, Rank.King)),
        }, clubs), 1);

        Check("no-trumps A>10", Rules.WinnerSeat(new List<Play>
        {
            new(0, new Card(Suit.Hearts, Rank.Ten)),
            new(1, new Card(Suit.Hearts, Rank.Ace)),
            new(2, new Card(Suit.Diamonds, Rank.Ace)),
            new(3, new Card(Suit.Hearts, Rank.Seven)),
        }, noTrumps), 1);

        Check("J trump=20", Rules.PointValue(new Card(Suit.Clubs, Rank.Jack), clubs), 20);
        Check("9 trump=14", Rules.PointValue(new Card(Suit.Clubs, Rank.Nine), clubs), 14);

        // Announces: four jacks = 200, terca = 20.
        var hand = new List<Card>
        {
            new(Suit.Clubs, Rank.Jack), new(Suit.Diamonds, Rank.Jack),
            new(Suit.Hearts, Rank.Jack), new(Suit.Spades, Rank.Jack),
            new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Eight),
            new(Suit.Clubs, Rank.Nine), new(Suit.Diamonds, Rank.Ace),
        };
        var anns = Announces.Detect(hand, 0, allTrumps);
        Check("four jacks=200", anns.Where(a => a.Kind == AnnounceKind.Careta).Sum(a => a.Value), 200);

        // Total card points: a suit contract has 152 card points in the deck.
        int total = 0;
        foreach (Suit s in Enum.GetValues<Suit>())
        {
            foreach (Rank r in Enum.GetValues<Rank>())
            {
                total += Rules.PointValue(new Card(s, r), clubs);
            }
        }

        Check("suit deck points=152", total, 152);

        // All-trumps deck points = 248; no-trumps = 120.
        int at = 0, nt = 0;
        foreach (Suit s in Enum.GetValues<Suit>())
        {
            foreach (Rank r in Enum.GetValues<Rank>())
            {
                at += Rules.PointValue(new Card(s, r), allTrumps);
                nt += Rules.PointValue(new Card(s, r), noTrumps);
            }
        }

        Check("all-trumps deck points=248", at, 248);
        Check("no-trumps deck points=120", nt, 120);

        Console.WriteLine("All self-tests passed.");
    }

    private static void Check(string label, int actual, int expected)
    {
        if (actual != expected)
        {
            Console.Error.WriteLine($"FAIL {label}: got {actual}, expected {expected}");
            Environment.Exit(1);
        }

        Console.WriteLine($"  ok  {label}");
    }
}

static class Simulation
{
    public static void Run(int matches)
    {
        var rng = new DelphiRandom(12345);
        int[] wins = new int[2];
        int totalRounds = 0;
        var contractCounts = new Dictionary<BidType, int>();
        int allPassDeals = 0;

        for (int m = 0; m < matches; m++)
        {
            var players = new IPlayer[]
            {
                new AiPlayer("S", rng), new AiPlayer("E", rng),
                new AiPlayer("N", rng), new AiPlayer("W", rng),
            };
            var game = new Game(players, rng);
            int winner = game.PlayMatch(rng.Next(4), outcome =>
            {
                if (outcome.AllPassed)
                {
                    allPassDeals++;
                }
                else
                {
                    totalRounds++;
                    contractCounts[outcome.Contract!.Type] =
                        contractCounts.GetValueOrDefault(outcome.Contract.Type) + 1;
                }
            });
            wins[winner]++;
        }

        Console.WriteLine($"Simulated {matches} matches (4 AI players).");
        Console.WriteLine($"  Team wins: S+N {wins[0]}  vs  E+W {wins[1]}");
        Console.WriteLine($"  Avg rounds/match: {(double)totalRounds / matches:F1}");
        Console.WriteLine($"  All-pass redeals: {allPassDeals}");
        Console.WriteLine("  Contract distribution:");
        foreach (var kv in contractCounts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"    {Bids.NameBg(kv.Key),-12} {kv.Value}");
        }
    }

    // Sanity check: reconstructed AI (seats 0 & 2) vs random baseline (seats 1 & 3).
    public static void RunVsRandom(int matches)
    {
        var rng = new DelphiRandom(999);
        int aiWins = 0;
        for (int m = 0; m < matches; m++)
        {
            var players = new IPlayer[]
            {
                new AiPlayer("AI-S", rng), new RandomPlayer("Rnd-E", rng),
                new AiPlayer("AI-N", rng), new RandomPlayer("Rnd-W", rng),
            };
            var game = new Game(players, rng);
            if (game.PlayMatch(rng.Next(4)) == 0)
            {
                aiWins++;
            }
        }

        Console.WriteLine($"Reconstructed AI vs Random baseline over {matches} matches:");
        Console.WriteLine($"  AI team (S+N) won {aiWins}/{matches} = {100.0 * aiWins / matches:F1}%");
    }
}
