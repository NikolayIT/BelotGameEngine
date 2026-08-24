// Differential tester: plays random Belot rounds and compares, at every decision point,
// the repository engine (Belot.Engine) against the binary-verified reconstruction of the
// 2001 belot.exe (BelotV2.Rules):
//   1) the legal-card set offered to the player,
//   2) the trick winner,
//   3) per-hand announce detection (normalized; the 8-card-run representation differs by design).
using System.Reflection;
using System.Text;

using Belot.Engine.Cards;
using Belot.Engine.Game;
using Belot.Engine.GameMechanics;
using Belot.Engine.Players;

using V2 = BelotV2;

var validCardsService = new ValidCardsService();
var trickWinnerService = new TrickWinnerService();
var announcesService = new ValidAnnouncesService();
var playerProp = typeof(PlayCardAction).GetProperty("Player", BindingFlags.Public | BindingFlags.Instance)!;

PlayerPosition[] positions = { PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West };
BidType[] ourContracts = { BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
V2.BidType[] v2Contracts = { V2.BidType.Clubs, V2.BidType.Diamonds, V2.BidType.Hearts, V2.BidType.Spades, V2.BidType.NoTrumps, V2.BidType.AllTrumps };

int deals = args.Length > 0 ? int.Parse(args[0]) : 30000;
int baseSeed = args.Length > 1 ? int.Parse(args[1]) : 12345;

long turnChecks = 0, trickChecks = 0, announceChecks = 0;
long validMismatches = 0, winnerMismatches = 0, announceMismatches = 0;
int printed = 0;

V2.Card ToV2(Card c) => new((V2.Suit)(int)c.Suit, (V2.Rank)((int)c.Type + 7));
string Key(Card c) => c.ToString();

for (int deal = 0; deal < deals; deal++)
{
    // Shuffle 0..31 deterministically per deal.
    var rng = new Random(baseSeed + deal);
    int[] idx = Enumerable.Range(0, 32).ToArray();
    for (int n = 31; n > 0; n--)
    {
        int k = rng.Next(n + 1);
        (idx[n], idx[k]) = (idx[k], idx[n]);
    }

    for (int c = 0; c < 6; c++)
    {
        var ourContract = ourContracts[c];
        var v2Contract = new V2.Contract(v2Contracts[c], declarer: deal % 4, doubled: false, redoubled: false);
        var ourBid = new Bid(positions[deal % 4], ourContract);

        // Deal the same 32 cards to both models.
        var ourHands = new CardCollection[4];
        var v2Hands = new List<V2.Card>[4];
        for (int s = 0; s < 4; s++)
        {
            ourHands[s] = new CardCollection();
            v2Hands[s] = new List<V2.Card>(8);
            for (int i = 0; i < 8; i++)
            {
                var card = Card.AllCards[idx[(s * 8) + i]];
                ourHands[s].Add(card);
                v2Hands[s].Add(ToV2(card));
            }
        }

        // 3) Announce detection comparison (skip NT: not announceable; skip whole-suit hands:
        //    the engines represent the 8-card run differently by design).
        if (ourContract != BidType.NoTrumps)
        {
            for (int s = 0; s < 4; s++)
            {
                bool wholeSuit = Card.AllSuits.Any(suit => ourHands[s].GetCount(x => x.Suit == suit) == 8);
                if (wholeSuit)
                {
                    continue;
                }

                announceChecks++;
                var ourSet = ourHands[s].Count == 8
                    ? announcesService.GetAvailableAnnounces(ourHands[s]).Select(NormalizeOurs).OrderBy(x => x).ToList()
                    : new List<string>();
                var v2Set = V2.Announces.Detect(v2Hands[s], s, v2Contract).Select(NormalizeV2).OrderBy(x => x).ToList();
                if (!ourSet.SequenceEqual(v2Set))
                {
                    announceMismatches++;
                    Report($"ANNOUNCE mismatch deal={deal} seat={s} contract={ourContract}\n  hand: {HandString(ourHands[s])}\n  ours:   [{string.Join(", ", ourSet)}]\n  theirs: [{string.Join(", ", v2Set)}]");
                }
            }
        }

        // Play 8 tricks with random legal cards (chosen from OUR set), comparing at each turn.
        int leader = deal % 4;
        var playRng = new Random(baseSeed + (deal * 7) + c);
        for (int t = 0; t < 8; t++)
        {
            var ourTrick = new List<PlayCardAction>(4);
            var v2Trick = new List<V2.Play>(4);
            for (int i = 0; i < 4; i++)
            {
                int seat = (leader + i) & 3;
                turnChecks++;

                var oursValid = validCardsService.GetValidCards(ourHands[seat], ourContract, ourTrick);
                var v2Valid = V2.Rules.ValidCards(v2Hands[seat], v2Trick, v2Contract, seat);

                var ourKeys = oursValid.Select(Key).OrderBy(x => x).ToList();
                var v2Keys = v2Valid.Select(x => x.ToString()).OrderBy(x => x).ToList();
                if (!ourKeys.SequenceEqual(v2Keys))
                {
                    validMismatches++;
                    Report($"VALID-CARDS mismatch deal={deal} contract={ourContract} trick={t + 1} seat={seat}\n  hand:  {HandString(ourHands[seat])}\n  trick: {string.Join(" ", ourTrick.Select(x => x.Card.ToString()))}\n  ours:   [{string.Join(", ", ourKeys)}]\n  theirs: [{string.Join(", ", v2Keys)}]");
                }

                // Play a random card from OUR legal set.
                var chosen = oursValid.Skip(playRng.Next(oursValid.Count)).First();
                var action = new PlayCardAction(chosen, false);
                playerProp.SetValue(action, positions[seat]);
                ourTrick.Add(action);
                v2Trick.Add(new V2.Play(seat, ToV2(chosen)));
                ourHands[seat].Remove(chosen);
                v2Hands[seat].Remove(ToV2(chosen));
            }

            trickChecks++;
            int ourWinner = trickWinnerService.GetWinner(ourBid, ourTrick).Index();
            int v2Winner = V2.Rules.WinnerSeat(v2Trick, v2Contract);
            if (ourWinner != v2Winner)
            {
                winnerMismatches++;
                Report($"WINNER mismatch deal={deal} contract={ourContract} trick={t + 1}\n  trick: {string.Join(" ", ourTrick.Select(x => x.Card.ToString()))} led by seat {leader}\n  ours: seat {ourWinner}, theirs: seat {v2Winner}");
            }

            leader = v2Winner == ourWinner ? ourWinner : ourWinner;
        }
    }
}

Console.WriteLine($"deals={deals} x 6 contracts");
Console.WriteLine($"turn checks:     {turnChecks,12:n0}  valid-card mismatches: {validMismatches}");
Console.WriteLine($"trick checks:    {trickChecks,12:n0}  winner mismatches:     {winnerMismatches}");
Console.WriteLine($"announce checks: {announceChecks,12:n0}  announce mismatches:   {announceMismatches}");
Console.WriteLine(validMismatches + winnerMismatches + announceMismatches == 0 ? "ALL MATCH" : "MISMATCHES FOUND");

void Report(string message)
{
    if (printed < 12)
    {
        Console.WriteLine(message);
        printed++;
    }
}

string HandString(CardCollection hand) => string.Join(" ", hand.Select(x => x.ToString()));

// Normalizations: compare announces as (kind, top rank), suit-insensitive, quinte length-insensitive
// (the 2001 game folds 5..8-card runs into one "quinte to X"; whole-suit hands are skipped above).
string NormalizeOurs(Announce a)
{
    string s = a.ToString();
    if (s == "4 Jacks")
    {
        return "careta J";
    }

    if (s == "4 Nines")
    {
        return "careta 9";
    }

    if (s.StartsWith("4 of a kind "))
    {
        return "careta " + s["4 of a kind ".Length..] switch
        {
            "Ace" => "A",
            "King" => "K",
            "Queen" => "Q",
            "Ten" => "10",
            var other => other,
        };
    }

    int to = s.IndexOf(" to ", StringComparison.Ordinal);
    string kind = s.StartsWith("Tierce") ? "tierce" : s.StartsWith("Quarte") ? "quarte" : "quinte";
    string cardPart = s[(to + 4)..];
    string rank = cardPart[..^1]; // strip the suit glyph
    return $"{kind} {rank}";
}

string NormalizeV2(V2.Announce a)
{
    string rank = a.TopRank switch
    {
        V2.Rank.Seven => "7",
        V2.Rank.Eight => "8",
        V2.Rank.Nine => "9",
        V2.Rank.Ten => "10",
        V2.Rank.Jack => "J",
        V2.Rank.Queen => "Q",
        V2.Rank.King => "K",
        _ => "A",
    };
    return a.Kind switch
    {
        V2.AnnounceKind.Terca => $"tierce {rank}",
        V2.AnnounceKind.Quarte => $"quarte {rank}",
        V2.AnnounceKind.Quinte => $"quinte {rank}",
        V2.AnnounceKind.Careta => $"careta {rank}",
        _ => $"belote {rank}",
    };
}
