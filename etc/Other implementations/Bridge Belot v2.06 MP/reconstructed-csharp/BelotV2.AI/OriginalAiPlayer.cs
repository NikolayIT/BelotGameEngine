using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BelotV2
{
    /// <summary>
    /// An <see cref="IPlayer"/> whose card play is decided by the ORIGINAL belot.exe code.
    ///
    /// It talks to <c>tools/ai_server.py</c>, which runs the real `player2BeforePlay` routine
    /// under emulation, so the chosen card is exact by construction rather than by
    /// reimplementation. Bidding uses <see cref="BiddingAi"/>, whose valuation is already
    /// verified 1:1 against the binary.
    ///
    /// Use this as the reference opponent when checking another Belot engine: it is the real
    /// AI, not an approximation. It costs roughly half a second per decision, so it is meant for
    /// comparison harnesses rather than bulk simulation.
    ///
    /// The original routine is the handler for seats 2..4 (seat 1 is the human in the game and
    /// the routine range-checks on that), so ask it only for those seats.
    /// </summary>
    public sealed class OriginalAiPlayer : IPlayer, IDisposable
    {
        private static readonly string[] SuitLetters = { "C", "D", "H", "S" };

        private readonly Process process;
        private readonly StreamWriter input;
        private readonly StreamReader output;
        private readonly DelphiRandom rng = new(1);
        private bool disposed;

        public OriginalAiPlayer(string name, string serverScript, string pythonExe = "python")
        {
            this.Name = name;
            var psi = new ProcessStartInfo(pythonExe, $"\"{serverScript}\"")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(serverScript))!,
                StandardOutputEncoding = Encoding.UTF8,
            };
            this.process = Process.Start(psi) ?? throw new InvalidOperationException("cannot start ai_server");
            this.input = this.process.StandardInput;
            this.output = this.process.StandardOutput;
            this.process.StandardError.ReadLine();   // "ai_server ready"
        }

        public string Name { get; }

        /// <summary>Declarer seat (1..4) reported to the AI; affects a few of its branches.</summary>
        public int Declarer { get; set; } = 1;

        public BidType GetBid(BidContext context) => BiddingAi.ChooseBid(context, this.rng);

        public Card PlayCard(PlayContext context)
        {
            if (context.Legal.Count == 1)
            {
                return context.Legal[0];
            }

            var hand = new List<Card>(context.Hand);
            Game.SortHand(hand);

            var seen = new HashSet<Card>(hand);
            foreach (Play p in context.PlayedHistory)
            {
                seen.Add(p.Card);
            }

            foreach (Play p in context.CurrentTrick)
            {
                seen.Add(p.Card);
            }

            // suits each seat has shown void in, from completed tricks and the current one
            var voids = new List<HashSet<Suit>> { new(), new(), new(), new() };
            void ScanTrick(IReadOnlyList<Play> plays)
            {
                if (plays.Count == 0)
                {
                    return;
                }

                Suit led = plays[0].Card.Suit;
                foreach (Play p in plays)
                {
                    if (p.Card.Suit != led)
                    {
                        voids[p.Seat].Add(led);
                    }
                }
            }

            for (int i = 0; i + 3 < context.PlayedHistory.Count; i += 4)
            {
                ScanTrick(new List<Play>
                {
                    context.PlayedHistory[i], context.PlayedHistory[i + 1],
                    context.PlayedHistory[i + 2], context.PlayedHistory[i + 3],
                });
            }

            ScanTrick(context.CurrentTrick);

            var req = new
            {
                contract = ContractId(context.Contract.Type),
                me = context.Seat + 1,
                declarer = this.Declarer,
                hand = hand.Select(c => new object[] { SuitLetters[(int)c.Suit], (int)c.Rank }),
                trick = context.CurrentTrick.Select(p => new object[]
                {
                    p.Seat + 1, SuitLetters[(int)p.Card.Suit], (int)p.Card.Rank,
                }),
                played = seen.Select(c => new object[] { SuitLetters[(int)c.Suit], (int)c.Rank }),
                voids = Enumerable.Range(1, 4).ToDictionary(
                    p => p.ToString(),
                    p => voids[p - 1].Select(s => SuitLetters[(int)s]).ToArray()),

                // Give the binary the same round memory the port builds, so a comparison is of
                // the decision alone and not of two different memories. Sent as numbers: a byte[]
                // would serialise to base64.
                mem = OriginalPlayAdapter.RoundMemory(context).Select(b => (int)b).ToArray(),
            };

            this.input.WriteLine(JsonSerializer.Serialize(req));
            this.input.Flush();
            string? line = this.output.ReadLine()
                ?? throw new InvalidOperationException("ai_server closed the connection");

            using JsonDocument doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("error", out JsonElement err))
            {
                throw new InvalidOperationException($"original AI: {err.GetString()}");
            }

            Card chosen = hand[doc.RootElement.GetProperty("index").GetInt32()];
            return context.Legal.Contains(chosen) ? chosen : context.Legal[0];
        }

        private static int ContractId(BidType t) => t switch
        {
            BidType.Clubs => 1,
            BidType.Diamonds => 2,
            BidType.Hearts => 3,
            BidType.Spades => 4,
            BidType.NoTrumps => 5,
            BidType.AllTrumps => 6,
            _ => throw new ArgumentException($"not a contract: {t}"),
        };

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            try
            {
                this.input.WriteLine("quit");
                this.input.Flush();
                if (!this.process.WaitForExit(2000))
                {
                    this.process.Kill();
                }
            }
            catch (Exception)
            {
                // best effort
            }

            this.process.Dispose();
        }
    }
}
