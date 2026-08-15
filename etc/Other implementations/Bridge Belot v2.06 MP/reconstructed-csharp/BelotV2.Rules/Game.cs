namespace BelotV2
{
    public sealed class RoundOutcome
    {
        public Contract? Contract { get; init; } // null = all passed (redeal)

        public RoundResult? Result { get; init; }

        public int LastTrickWinner { get; init; }

        public List<Announce> ActiveAnnounces { get; init; } = new();

        /// <summary>All 32 cards played this round, in play order (empty if all passed).</summary>
        public List<Play> Plays { get; init; } = new();

        public bool AllPassed => this.Contract is null;
    }

    /// <summary>
    /// Drives a full Belot match: rounds of deal -> auction -> deal 3 -> announces -> 8 tricks
    /// -> score, until a team reaches 151 (with the capot "can't go out" exception). Rules and
    /// flow reconstructed from belot.exe (see the individual modules and README).
    /// </summary>
    public sealed class Game
    {
        private readonly IPlayer[] players;
        private readonly DelphiRandom rng;
        private readonly Action<string>? log;
        private readonly Suit?[] bidSuits = new Suit?[4];

        public Game(IPlayer[] players, DelphiRandom rng, Action<string>? log = null)
        {
            if (players.Length != 4)
            {
                throw new ArgumentException("need 4 players");
            }

            this.players = players;
            this.rng = rng;
            this.log = log;
        }

        /// <summary>
        /// The seat a person is playing, or -1 when all four are engines. The original AI reads a
        /// discarded boss card as a signal only for the seat that plays through the click handler;
        /// see <c>PlayMemory</c>.
        /// </summary>
        public int HumanSeat { get; init; } = -1;

        public int[] Board { get; } = new int[2]; // match score per team (0=S+N, 1=E+W)

        public int HangingPoints { get; private set; }

        private void Log(string s) => this.log?.Invoke(s);

        /// <summary>Play one full match; returns the winning team (0 or 1).</summary>
        public int PlayMatch(int firstSeat, Action<RoundOutcome>? onRound = null)
        {
            int roundNo = 0;
            while (true)
            {
                roundNo++;
                RoundOutcome outcome = PlayRound(firstSeat);
                onRound?.Invoke(outcome);
                if (outcome.AllPassed)
                {
                    this.Log($"Round {roundNo}: all passed, redeal.");
                    firstSeat = Seats.Next(firstSeat);
                    continue;
                }

                RoundResult res = outcome.Result!;
                this.Board[0] += res.Team0Board;
                this.Board[1] += res.Team1Board;
                this.HangingPoints = res.HangingPoints;

                this.Log($"Round {roundNo}: {Bids.NameBg(outcome.Contract!.Type)} by " +
                         $"{this.players[outcome.Contract.Declarer].Name} | " +
                         $"raw {res.Team0Raw}:{res.Team1Raw} -> board {res.Team0Board}:{res.Team1Board}" +
                         (res.Inside ? " (inside!)" : string.Empty) +
                         (res.CapotForOneTeam ? " (capot)" : string.Empty) +
                         $" | total {this.Board[0]}:{this.Board[1]}");

                int? winner = CheckWinner(res);
                if (winner is int w)
                {
                    return w;
                }

                if (roundNo > 5000)
                {
                    // Safety net (should never trigger in a real match).
                    return this.Board[0] >= this.Board[1] ? 0 : 1;
                }

                firstSeat = Seats.Next(firstSeat);
            }
        }

        private int? CheckWinner(RoundResult res)
        {
            bool t0 = this.Board[0] >= Scoring.MatchTarget;
            bool t1 = this.Board[1] >= Scoring.MatchTarget;
            if (!t0 && !t1)
            {
                return null;
            }

            // "С капо не се излиза" — you cannot go out on a capot round; one more is played.
            if (res.CapotForOneTeam)
            {
                this.Log("  Capot round — cannot go out on a capot, playing another round.");
                return null;
            }

            if (t0 && t1)
            {
                return this.Board[0] >= this.Board[1] ? 0 : 1;
            }

            return t0 ? 0 : 1;
        }

        public RoundOutcome PlayRound(int firstSeat)
        {
            // Build + shuffle + deal first five.
            Card[] deck = Deck.BuildOrdered();
            Deck.Shuffle(deck, this.rng);
            var hands = new List<Card>[4];
            for (int i = 0; i < 4; i++)
            {
                hands[i] = new List<Card>(8);
            }

            int cursor = Deck.DealFirstFive(deck, hands, firstSeat);

            // Auction using the first five cards.
            (Contract? contract, bool doubled, bool redoubled, int declarer) = RunAuction(hands, firstSeat);
            if (contract is null)
            {
                return new RoundOutcome();
            }

            // Deal the remaining three and sort hands.
            Deck.DealLastThree(deck, hands, firstSeat, cursor);
            for (int i = 0; i < 4; i++)
            {
                SortHand(hands[i]);
            }

            var finalContract = contract;

            // Sequence/careta declarations (from full 8-card hands) + belote.
            var allDeclared = new List<Announce>();
            for (int s = 0; s < 4; s++)
            {
                allDeclared.AddRange(Announces.Detect(hands[s], s, finalContract));
            }

            var active = Announces.Resolve(allDeclared);
            active.AddRange(DetectBelotes(hands, finalContract));

            // Play 8 tricks.
            var wonCards = new List<Card>[2] { new(), new() };
            var history = new List<Play>();
            int leader = firstSeat;
            int lastWinner = firstSeat;

            for (int t = 0; t < 8; t++)
            {
                var trick = new List<Play>();
                for (int i = 0; i < 4; i++)
                {
                    int seat = (leader + i) & 3;
                    var legal = Rules.ValidCards(hands[seat], trick, finalContract, seat);
                    Card chosen = this.players[seat].PlayCard(new PlayContext
                    {
                        Seat = seat,
                        Hand = hands[seat],
                        Contract = finalContract,
                        CurrentTrick = trick,
                        PlayedHistory = history,
                        Legal = legal,
                        BidSuits = this.bidSuits,
                        HumanSeat = this.HumanSeat,
                    });

                    if (!legal.Contains(chosen))
                    {
                        chosen = legal[0]; // enforce legality
                    }

                    hands[seat].Remove(chosen);
                    trick.Add(new Play(seat, chosen));
                }

                int winIdx = Rules.WinnerIndex(trick, finalContract);
                int winnerSeat = trick[winIdx].Seat;
                lastWinner = winnerSeat;
                int team = Seats.TeamOf(winnerSeat);
                foreach (Play p in trick)
                {
                    wonCards[team].Add(p.Card);
                    history.Add(p);
                }

                leader = winnerSeat;
            }

            RoundResult result = Scoring.Score(
                finalContract, wonCards[0], wonCards[1], active, this.HangingPoints, lastWinner);

            return new RoundOutcome
            {
                Contract = finalContract,
                Result = result,
                LastTrickWinner = lastWinner,
                ActiveAnnounces = active,
                Plays = history,
            };
        }

        private (Contract?, bool, bool, int) RunAuction(List<Card>[] hands, int firstSeat)
        {
            Array.Clear(this.bidSuits, 0, 4);
            BidType? highest = null;
            int contractSeat = -1;
            bool doubled = false;
            bool redoubled = false;
            var history = new List<BidAction>();
            int consecutivePasses = 0;
            int seat = firstSeat;
            int actions = 0;

            while (actions++ < 32)
            {
                BidType bid = this.players[seat].GetBid(new BidContext
                {
                    Seat = seat,
                    Hand = hands[seat],
                    History = history,
                    HighestContract = highest,
                    ContractSeat = contractSeat,
                    CurrentlyDoubled = doubled,
                    Board = this.Board,
                });

                bid = LegalizeBid(bid, highest, contractSeat, seat, doubled, redoubled);
                history.Add(new BidAction(seat, bid));

                if (bid == BidType.Pass)
                {
                    consecutivePasses++;
                }
                else if (bid == BidType.Double)
                {
                    doubled = true;
                    consecutivePasses = 0;
                }
                else if (bid == BidType.Redouble)
                {
                    redoubled = true;
                    consecutivePasses = 0;
                }
                else
                {
                    highest = bid;
                    contractSeat = seat;
                    doubled = false;
                    redoubled = false;
                    consecutivePasses = 0;
                    this.bidSuits[seat] = Bids.TrumpSuitOf(bid);
                }

                if (this.log != null && bid != BidType.Pass)
                {
                    this.Log($"  {this.players[seat].Name} bids {Bids.NameBg(bid)}");
                }

                // End conditions.
                if (highest is null && consecutivePasses >= 4)
                {
                    return (null, false, false, -1); // all passed
                }

                if (highest is not null && consecutivePasses >= 3)
                {
                    break;
                }

                seat = Seats.Next(seat);
            }

            if (highest is null)
            {
                return (null, false, false, -1);
            }

            return (new Contract(highest.Value, contractSeat, doubled, redoubled), doubled, redoubled, contractSeat);
        }

        private static BidType LegalizeBid(
            BidType bid, BidType? highest, int contractSeat, int seat, bool doubled, bool redoubled)
        {
            switch (bid)
            {
                case BidType.Pass:
                    return BidType.Pass;
                case BidType.Double:
                    // Only over an opponents' contract, not already doubled.
                    if (highest is not null && contractSeat >= 0
                        && !Seats.SameTeam(seat, contractSeat) && !doubled && !redoubled)
                    {
                        return BidType.Double;
                    }

                    return BidType.Pass;
                case BidType.Redouble:
                    if (highest is not null && contractSeat >= 0
                        && Seats.SameTeam(seat, contractSeat) && doubled && !redoubled)
                    {
                        return BidType.Redouble;
                    }

                    return BidType.Pass;
                default:
                    if (Bids.IsContract(bid) && (highest is null || (int)bid > (int)highest))
                    {
                        return bid;
                    }

                    return BidType.Pass;
            }
        }

        // Belote = K+Q of a trump suit held by one player. Suit contract: only the trump suit.
        // All-trumps: any suit (up to four). No belote in no-trumps.
        private static List<Announce> DetectBelotes(List<Card>[] hands, Contract contract)
        {
            var result = new List<Announce>();
            if (contract.Category == ContractCategory.NoTrumps)
            {
                return result;
            }

            for (int s = 0; s < 4; s++)
            {
                for (int suit = 0; suit < 4; suit++)
                {
                    if (!contract.IsTrump((Suit)suit))
                    {
                        continue;
                    }

                    bool hasK = hands[s].Contains(new Card((Suit)suit, Rank.King));
                    bool hasQ = hands[s].Contains(new Card((Suit)suit, Rank.Queen));
                    if (hasK && hasQ)
                    {
                        result.Add(new Announce(AnnounceKind.Belote, s, Rank.King, 20));
                    }
                }
            }

            return result;
        }

        public static void SortHand(List<Card> hand)
            => hand.Sort((a, b) => a.Suit != b.Suit
                ? ((int)a.Suit).CompareTo((int)b.Suit)
                : ((int)a.Rank).CompareTo((int)b.Rank));
    }
}
