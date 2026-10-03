namespace Belot.AI.ClaudePlayer.Tests.Heuristic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Heuristic;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    public class LearnedBiddingTests
    {
        [Fact]
        public void TheEmbeddedModelIsTheDefaultAndDecidesEverySituationThatComesUp()
        {
            var model = BidModel.Default;

            Assert.Same(model, new HeuristicSettings().Bids);
            foreach (var name in new[] { "open-none", "partner-suit", "partner-NT", "opp-suit", "opp-NT", "opp-AT" })
            {
                Assert.True(model.Covers(Enumerable.Range(0, BidFeatures.Classes).Single(i => BidFeatures.ClassName(i) == name)), name);
            }
        }

        [Fact]
        public void TheSituationSeesTheAuctionFromTheBiddersSeat()
        {
            // South bids first: South passes, East bids hearts, North spades; West (East's partner) is to bid.
            var match = new BelotMatch(new BelotMatchOptions { Random = new Random(3), FirstToPlay = PlayerPosition.South });
            match.Start();
            Assert.Equal(BelotActResult.Ok, match.Act(PlayerPosition.South, BelotAction.Bid(BidType.Pass)));
            Assert.Equal(BelotActResult.Ok, match.Act(PlayerPosition.East, BelotAction.Bid(BidType.Hearts)));
            Assert.Equal(BelotActResult.Ok, match.Act(PlayerPosition.North, BelotAction.Bid(BidType.Spades)));
            Assert.Equal(PlayerPosition.West, match.ToMove);

            var situation = BidSituation.From(match.CreateBidContext());

            Assert.Equal(3, situation.Position);
            Assert.Equal(3, situation.Holder);
            Assert.Equal(BidType.Spades, situation.Contract);
            Assert.Equal(1 << (int)CardSuit.Heart, situation.PartnerSuits);
            Assert.Equal(1 << (int)CardSuit.Spade, situation.OpponentSuits);
            Assert.Equal(1, situation.PartnerTurns);
            Assert.Equal(2, situation.OpponentTurns);
            Assert.Equal(1, situation.OpponentBids);
            Assert.Equal(0, situation.MyTurns);
            Assert.Equal("opp-suit", BidFeatures.ClassName(BidFeatures.Class(situation)));
        }

        [Fact]
        public void AllTrumpsOverThePartnersNoTrumpsTakesThreeJacksOrTwoJacksWithTheirNines()
        {
            // The numbers: over the partner's no trumps all trumps lost with one or two jacks (two
            // with one nine beside them still -0.7 game points a deal) and won with three jacks or
            // two jacks each with its nine.
            Assert.Equal(BidType.Pass, OverPartnersNoTrumps("J♠ 9♠ J♥ 8♦ 7♣"));
            Assert.Equal(BidType.Pass, OverPartnersNoTrumps("J♠ A♠ 10♠ A♥ K♥"));
            Assert.Equal(BidType.AllTrumps, OverPartnersNoTrumps("J♠ J♥ J♦ 7♣ 8♣"));
            Assert.Equal(BidType.AllTrumps, OverPartnersNoTrumps("J♠ 9♠ J♥ 9♥ 7♣"));
        }

        [Fact]
        public void DoublesTheOpponentsOvercallOnlyWithTheJackAndATopTrumpBesideIt()
        {
            // We bid clubs, they took the contract with spades: with the spade jack, ace and a
            // third spade it doubles; with the ace and length but not the jack it does not.
            Assert.Equal(BidType.Double, AfterTheirOvercall("J♠ A♠ 7♠ 8♥ 7♦"));
            Assert.NotEqual(BidType.Double, AfterTheirOvercall("A♠ 10♠ 7♠ 8♥ 7♦"));
        }

        [Fact]
        public void EveryBidIsLegalAndNaturalInRandomAuctions()
        {
            var random = new Random(11);
            var settings = new HeuristicSettings();
            var bids = 0;
            for (var game = 0; game < 300; game++)
            {
                var match = new BelotMatch(new BelotMatchOptions { Random = new Random(game), FirstToPlay = (PlayerPosition)(1 << (game % 4)) });
                match.Start();
                while (match.Decision == BelotDecision.Bid)
                {
                    var context = match.CreateBidContext();
                    var bid = LearnedBidding.Choose(context, settings, settings.Bids);
                    Assert.True(context.AvailableBids.HasFlag(bid), $"{bid} not available");
                    Assert.True(LearnedBidding.Natural(CardMemory.ToMask(context.MyCards), bid), $"{bid} with {string.Join(" ", context.MyCards)}");

                    // Half the seats bid at random among the natural bids, so every situation comes up.
                    if (random.Next(2) == 0)
                    {
                        var options = Enum.GetValues<BidType>().Where(x => context.AvailableBids.HasFlag(x) && LearnedBidding.Natural(CardMemory.ToMask(context.MyCards), x)).ToList();
                        bid = options[random.Next(options.Count)];
                    }

                    Assert.Equal(BelotActResult.Ok, match.Act(match.ToMove, BelotAction.Bid(bid)));
                    bids++;
                }
            }

            Assert.True(bids > 1000);
        }

        [Fact]
        public void ModelsReadFromTextAndRejectWhatTheyCannotRead()
        {
            var weights = string.Join(" ", Enumerable.Repeat("0", BidFeatures.Count));
            var model = BidModel.Parse($"# a comment\nsuit open-none {weights}\ngate at opp-suit 1c\ncell double opp-NT 3 11.5\n");

            Assert.True(model.Covers(0));
            Assert.True(model.Allows(BidFeatures.AllTrumps, BidFeatures.Class(Situation("opp-suit")), 2));
            Assert.False(model.Allows(BidFeatures.AllTrumps, BidFeatures.Class(Situation("opp-suit")), 1));
            Assert.Throws<FormatException>(() => BidModel.Parse("suit nowhere 1 2 3"));
            Assert.Throws<FormatException>(() => BidModel.Parse("suit open-none 1 2 3"));
        }

        // South to bid after North's no trumps (East and West passed): what the default model bids.
        private static BidType OverPartnersNoTrumps(string cards)
        {
            var bids = new List<Bid>
            {
                new Bid(PlayerPosition.East, BidType.Pass),
                new Bid(PlayerPosition.North, BidType.NoTrumps),
                new Bid(PlayerPosition.West, BidType.Pass),
            };
            return Bid(cards, bids, new Bid(PlayerPosition.North, BidType.NoTrumps), BidType.Pass | BidType.AllTrumps);
        }

        // South bid clubs, East spades, North and West passed: South to bid.
        private static BidType AfterTheirOvercall(string cards)
        {
            var bids = new List<Bid>
            {
                new Bid(PlayerPosition.South, BidType.Clubs),
                new Bid(PlayerPosition.East, BidType.Spades),
                new Bid(PlayerPosition.North, BidType.Pass),
                new Bid(PlayerPosition.West, BidType.Pass),
            };
            return Bid(cards, bids, new Bid(PlayerPosition.East, BidType.Spades), BidType.Pass | BidType.NoTrumps | BidType.AllTrumps | BidType.Double);
        }

        private static BidType Bid(string cards, List<Bid> bids, Bid contract, BidType available)
        {
            var hand = new CardCollection();
            foreach (var card in cards.Split(' '))
            {
                hand.Add(Parse(card));
            }

            var context = new PlayerGetBidContext
            {
                MyPosition = PlayerPosition.South,
                FirstToPlayInTheRound = bids[0].Player,
                MyCards = hand,
                Bids = bids,
                CurrentContract = contract,
                AvailableBids = available,
            };
            var settings = new HeuristicSettings();
            return LearnedBidding.Choose(context, settings, settings.Bids);
        }

        private static BidSituation Situation(string name)
        {
            var situation = new BidSituation { Holder = -1 };
            if (name == "opp-suit")
            {
                situation.Add(1, BidType.Hearts);
            }

            return situation;
        }

        private static Card Parse(string text)
        {
            var suit = text[^1] switch
            {
                '♣' => CardSuit.Club,
                '♦' => CardSuit.Diamond,
                '♥' => CardSuit.Heart,
                _ => CardSuit.Spade,
            };
            var type = text[..^1] switch
            {
                "7" => CardType.Seven,
                "8" => CardType.Eight,
                "9" => CardType.Nine,
                "10" => CardType.Ten,
                "J" => CardType.Jack,
                "Q" => CardType.Queen,
                "K" => CardType.King,
                _ => CardType.Ace,
            };
            return Card.GetCard(suit, type);
        }
    }
}
