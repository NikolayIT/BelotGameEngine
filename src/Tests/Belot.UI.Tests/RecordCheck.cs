namespace Belot.UI.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    // Rebuilds every deal of a match from what the table was told (the events, in order) and
    // checks it against the engine's record of the match: the same auction, contract,
    // declarations, cards, belotes, tricks and scores, and nothing else.
    internal static class RecordCheck
    {
        public static void EventsMatchTheRecord(IReadOnlyList<object> events, BelotMatchRecord record)
        {
            var deals = SplitIntoDeals(events);
            Assert.Equal(record.Rounds.Count, deals.Count);
            int southNorth = 0, eastWest = 0;
            for (var i = 0; i < deals.Count; i++)
            {
                var deal = deals[i];
                var round = record.Rounds[i];
                var end = (RoundEndInfo)deal[^1];

                CheckAuction(deal, round);
                CheckDeclarations(deal, round);
                CheckCards(deal, round);

                Assert.Equal(round.RoundNumber, end.RoundNumber);
                Assert.Equal(round.Contract.Type, end.Contract);
                if (round.Contract.Type != BidType.Pass)
                {
                    Assert.Equal(round.Contract.Player, end.Declarer);
                }

                var result = round.Result;
                Assert.Equal(result.SouthNorthTotalInRoundPoints, end.SouthNorthInDeal);
                Assert.Equal(result.EastWestTotalInRoundPoints, end.EastWestInDeal);
                Assert.Equal(result.SouthNorthPoints, end.SouthNorthAwarded);
                Assert.Equal(result.EastWestPoints, end.EastWestAwarded);
                Assert.Equal(result.HangingPoints, end.HangingAfter);
                Assert.Equal(result.NoTricksForOneOfTheTeams, end.IsCapot);
                Assert.Equal(round.Tricks.Count(t => Seats.IsUs(t.Winner)), end.SouthNorthTricks);
                Assert.Equal(round.Tricks.Count(t => !Seats.IsUs(t.Winner)), end.EastWestTricks);
                Assert.Equal(round.Announces.Count, end.Combinations.Count);
                Assert.Equal(
                    round.Announces.Where(a => a.IsScored == true && Seats.IsUs(a.Player)).Sum(a => a.Value),
                    end.SouthNorthCombinations);
                Assert.Equal(
                    round.Announces.Where(a => a.IsScored == true && !Seats.IsUs(a.Player)).Sum(a => a.Value),
                    end.EastWestCombinations);
                Assert.Equal(ExpectedOutcome(end), end.Outcome);

                // The game points run up deal by deal to the match's final score.
                southNorth += end.SouthNorthAwarded;
                eastWest += end.EastWestAwarded;
                Assert.Equal(southNorth, end.SouthNorthGamePoints);
                Assert.Equal(eastWest, end.EastWestGamePoints);
                Assert.Equal(i == deals.Count - 1, end.IsGameOver);
            }

            Assert.Equal(record.SouthNorthPoints, southNorth);
            Assert.Equal(record.EastWestPoints, eastWest);
        }

        public static RoundOutcome ExpectedOutcome(RoundEndInfo end)
        {
            if (end.Contract == BidType.Pass)
            {
                return RoundOutcome.PassedOut;
            }

            var declarers = Seats.IsUs(end.Declarer) ? end.SouthNorthInDeal : end.EastWestInDeal;
            var defenders = Seats.IsUs(end.Declarer) ? end.EastWestInDeal : end.SouthNorthInDeal;
            return declarers > defenders ? RoundOutcome.Made : declarers < defenders ? RoundOutcome.Inside : RoundOutcome.Hanging;
        }

        // The events of each deal, each list ending with its RoundEndInfo.
        private static List<List<object>> SplitIntoDeals(IReadOnlyList<object> events)
        {
            var deals = new List<List<object>>();
            var current = new List<object>();
            foreach (var item in events)
            {
                current.Add(item);
                if (item is RoundEndInfo)
                {
                    deals.Add(current);
                    current = new List<object>();
                }
            }

            Assert.DoesNotContain(current, x => x is GameEvent);
            return deals;
        }

        private static void CheckAuction(List<object> deal, BelotRoundRecord round)
        {
            var bids = deal.OfType<BidInfo>().ToList();
            Assert.Equal(round.Bids.Select(b => (b.Player, b.Type)), bids.Select(b => (b.Seat, b.Bid)));
            Assert.Equal(round.Contract.Type, bids[^1].ContractType);

            var contract = deal.OfType<ContractInfo>().ToList();
            if (round.Contract.Type == BidType.Pass)
            {
                Assert.Empty(contract);
                Assert.Empty(deal.OfType<CardInfo>());
                return;
            }

            var settled = Assert.Single(contract);
            Assert.Equal(round.Contract.Player, settled.Declarer);
            Assert.Equal(round.Contract.Type, settled.Contract);
            Assert.Equal(8, settled.MyHand.Count);
            Assert.Equal(new[] { 8, 8, 8, 8 }, settled.CardCounts);

            // The contract comes after the last bid and before the first card.
            var at = deal.IndexOf(settled);
            Assert.True(at > deal.IndexOf(bids[^1]));
            Assert.DoesNotContain(deal.Take(at), x => x is CardInfo || x is AnnounceInfo);
        }

        private static void CheckDeclarations(List<object> deal, BelotRoundRecord round)
        {
            var declared = deal.OfType<AnnounceInfo>().SelectMany(a => a.Combinations).ToList();
            Assert.All(deal.OfType<AnnounceInfo>(), a => Assert.All(a.Combinations, c => Assert.Equal(a.Seat, c.Seat)));
            Assert.Equal(
                round.Announces.Where(a => a.Type != AnnounceType.Belot).Select(a => (a.Player, a.Type)),
                declared.Select(c => (c.Seat, c.Type)));

            // The person's own combinations show their cards; nobody else's do before the deal ends.
            Assert.All(declared, c => Assert.Equal(c.Seat == Seats.Person, c.Card != null));

            // Declarations happen in the first trick only.
            foreach (var announce in deal.OfType<AnnounceInfo>())
            {
                var cardsBefore = deal.Take(deal.IndexOf(announce)).OfType<CardInfo>().Count();
                Assert.InRange(cardsBefore, 0, 3);
            }

            Assert.Equal(
                round.Announces.Where(a => a.Type == AnnounceType.Belot).Select(a => (a.Player, a.Card)).OrderBy(x => x.Card.GetHashCode()),
                deal.OfType<CardInfo>().Where(c => c.Belote).Select(c => (c.Seat, c.Card)).OrderBy(x => x.Card.GetHashCode()));
        }

        private static void CheckCards(List<object> deal, BelotRoundRecord round)
        {
            var cards = deal.OfType<CardInfo>().ToList();
            var expected = round.Tricks.SelectMany(t => t.Cards).ToList();
            Assert.Equal(expected.Select(c => (c.Player, c.Card, c.Belote)), cards.Select(c => (c.Seat, c.Card, c.Belote)));
            for (var k = 0; k < cards.Count; k++)
            {
                Assert.Equal((k / 4) + 1, cards[k].TrickNumber);
                Assert.Equal(k % 4, cards[k].IndexInTrick);
            }

            var tricks = deal.OfType<TrickInfo>().ToList();
            Assert.Equal(round.Tricks.Count, tricks.Count);
            for (var t = 0; t < tricks.Count; t++)
            {
                Assert.Equal(t + 1, tricks[t].TrickNumber);
                Assert.Equal(round.Tricks[t].Winner, tricks[t].Winner);
                Assert.Equal(t == 7, tricks[t].IsLastOfDeal);
                Assert.Equal(round.Tricks[t].Cards.Select(c => (c.Player, c.Card)), tricks[t].Cards.Select(c => (c.Seat, c.Card)));

                // Each trick leaves the table right after its fourth card.
                var fourth = cards[(t * 4) + 3];
                Assert.Equal(deal.IndexOf(fourth) + 1, deal.IndexOf(tricks[t]));
            }
        }
    }
}
