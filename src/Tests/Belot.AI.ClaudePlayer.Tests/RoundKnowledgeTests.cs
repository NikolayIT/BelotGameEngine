namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.AI.DummyPlayer;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using Xunit;

    /// <summary>
    /// What the search infers about the hidden hands must always hold (at every decision of
    /// thousands of deals the real hands satisfy it), and it must actually infer what the rules
    /// reveal (the scenarios).
    /// </summary>
    public class RoundKnowledgeTests
    {
        private static readonly BidType[] Contracts =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheRealHandsAlwaysFitTheKnowledge(bool smartPlay)
        {
            var decisions = 0;
            var excludedCards = 0L;
            for (var seed = 0; seed < 3000; seed++)
            {
                var random = new Random(seed);
                var hands = Deal.RandomHands(random);
                var contract = new Bid(Deal.Seats[random.Next(4)], Contracts[seed % 6]);
                var players = Enumerable.Range(0, 4)
                    .Select(_ => new TestPlayer(random, inner: smartPlay ? new SmartPlayer.SmartPlayer() : null))
                    .ToArray();
                foreach (var player in players)
                {
                    player.OnDecision = context =>
                    {
                        var knowledge = Build(context);
                        var me = context.MyPosition.Index();
                        for (var seat = 0; seat < 4; seat++)
                        {
                            var hand = Deal.Mask(hands[seat]);
                            Assert.Equal(BitOperations.PopCount(hand), knowledge.HandCounts[seat]);
                            if (seat == me)
                            {
                                continue;
                            }

                            Assert.Equal(0u, hand & knowledge.Excluded[seat]);
                            Assert.Equal(knowledge.Known[seat], hand & knowledge.Known[seat]);
                            excludedCards += BitOperations.PopCount(knowledge.Excluded[seat] & ~knowledge.Played & ~knowledge.MyHand);
                        }

                        decisions++;
                    };
                }

                Deal.Play(hands, contract, Deal.Seats[random.Next(4)], players);
            }

            // The play reveals a lot: on average several unseen cards per player are ruled out.
            Assert.True(decisions > 50_000);
            Assert.True(excludedCards > decisions, $"{excludedCards} exclusions in {decisions} decisions");
        }

        [Fact]
        public void NotFollowingShowsAVoidAndNotTrumpingShowsNoTrumps()
        {
            // Spades. South leads the ace of hearts; East has neither hearts nor spades and
            // discards while South holds the trick. North decides next.
            var knowledge = KnowledgeAt(
                BidType.Spades,
                PlayerPosition.North,
                2,
                new[] { "AH 7S 8S 9S JD QD KD AD", "7D 8D 9D 10D 7C 8C 9C 10C", "8H 7H 10H 10S JS JC QC KC", "9H JH QH KH QS KS AS AC" },
                new[] { "AH", "7D", "8H", "9H" });

            var east = PlayerPosition.East.Index();
            Assert.Equal(Mask("KH QH JH 9H AS KS QS 9S"), knowledge.Excluded[east] & Mask("KH QH JH 9H AS KS QS 9S"));
            Assert.Equal(0u, knowledge.Excluded[east] & Mask("8D 9D 10D 7C 8C 9C 10C JD QD KD AD AC"));
        }

        [Fact]
        public void NotOvertrumpingShowsNoHigherTrumpButAPartnersTrickShowsNothing()
        {
            // Spades. South leads the ace of hearts, East trumps with the nine, North cannot
            // overtrump (the jack is East's) and discards, West discards under his partner.
            var hands = new[]
            {
                "AH KH QH JH 10H 9H 8H 7H",
                "9S JS 7D 8D 9D 10D JD QD",
                "7S 8S 7C 9C 10C KD AD QS",
                "10S KS AS 8C JC QC KC AC",
            };
            var scripts = new[] { "AH", "9S 7D", "7C", "8C" };
            var atWest = KnowledgeAt(BidType.Spades, PlayerPosition.West, 3, hands, scripts);
            var north = PlayerPosition.North.Index();
            var east = PlayerPosition.East.Index();
            Assert.Equal(Mask("JS"), atWest.Excluded[north] & Mask("JS 8S QS 7S"));
            Assert.Equal(0u, atWest.Excluded[east] & Mask("JS"));

            // East then leads a diamond and South, last to play, decides: West showed nothing
            // about the trumps he still holds.
            var atSouth = KnowledgeAt(BidType.Spades, PlayerPosition.South, 7, hands, scripts);
            var west = PlayerPosition.West.Index();
            Assert.Equal(0u, atSouth.Excluded[west] & ~atSouth.Played & Mask("10S KS AS"));
            Assert.Equal(Mask("JS"), atSouth.Excluded[north] & Mask("JS"));
        }

        [Fact]
        public void FollowingBelowTheBestCardAndTheBeloteShowCards()
        {
            // All trumps. South leads the ten of clubs, East follows with the eight, North with the
            // king, declaring the belote, and West takes with the jack and leads the seven.
            var knowledge = KnowledgeAt(
                BidType.AllTrumps,
                PlayerPosition.South,
                5,
                new[] { "10C AC 9C 7D 8D 9D 10D JD", "8C QD KD AD 7H 8H 9H 10H", "KC QC JH QH KH AH 7S 8S", "JC 7C 9S 10S JS QS KS AS" },
                new[] { "10C", "8C", "KC", "JC 7C" });

            var east = PlayerPosition.East.Index();
            var north = PlayerPosition.North.Index();
            Assert.Equal(Mask("AC 9C JC"), knowledge.Excluded[east] & Mask("AC 9C JC"));
            Assert.Equal(Mask("AC 9C JC"), knowledge.Excluded[north] & Mask("AC 9C JC"));
            Assert.Equal(Mask("QC"), knowledge.Known[north]);

            // The belote is North-South's; the first trick (10 + 0 + 4 + 20) went to West.
            Assert.Equal(20, knowledge.Root.SouthNorthPoints);
            Assert.Equal(34, knowledge.Root.EastWestPoints);
            Assert.Equal(1, knowledge.Root.TrickCards);
        }

        [Fact]
        public void AQueenOrKingOfTrumpsWithoutTheBeloteShowsThereIsNoPair()
        {
            // Hearts. South leads the king of hearts without claiming a belote.
            var knowledge = KnowledgeAt(
                BidType.Hearts,
                PlayerPosition.East,
                1,
                new[] { "KH 7C 8C 9C 10C JC QC KC", "AH 10H 7D 8D 9D 10D JD QD", "QH 7S 8S 9S 10S JS QS KS", "JH 9H 8H 7H KD AC AD AS" },
                new[] { "KH", string.Empty, string.Empty, string.Empty });

            Assert.Equal(Mask("QH"), knowledge.Excluded[PlayerPosition.South.Index()] & Mask("QH"));
        }

        [Fact]
        public void FourJacksShowTheJacks()
        {
            var knowledge = KnowledgeAt(
                BidType.Clubs,
                PlayerPosition.East,
                1,
                new[] { "JC JD JH JS 7C 8D 9H 10S", "8C 9C AH 7D 9D 10D QD KD", "10C QC 7H 8H 7S 8S 9S QS", "KC AC AD 10H QH KH KS AS" },
                new[] { "7C", string.Empty, string.Empty, string.Empty });

            Assert.Equal(Mask("JC JD JH JS"), knowledge.Known[PlayerPosition.South.Index()]);
            Assert.Contains(knowledge.Announces.Take(knowledge.AnnounceCount), x => x.Type == AnnounceType.FourJacks && x.Rank == (int)CardType.Jack);

            // North and West have not declared yet.
            Assert.Equal((1 << PlayerPosition.North.Index()) | (1 << PlayerPosition.West.Index()), knowledge.SeatsToDeclare);
        }

        private static uint Mask(string cards) => Deal.Mask(Deal.Cards(cards));

        private static RoundKnowledge Build(PlayerPlayCardContext context)
        {
            var simulator = new BelotSimulator();
            simulator.SetContract(context.CurrentContract.Type, context.CurrentContract.Player.Index(), 0);
            var knowledge = new RoundKnowledge();
            Assert.True(knowledge.Build(context, simulator, true));
            return knowledge;
        }

        // Plays the deal (South first, each seat playing its script, then random cards) and
        // returns the given seat's knowledge at its decision after the given number of cards.
        private static RoundKnowledge KnowledgeAt(BidType contract, PlayerPosition seat, int cardsPlayed, string[] hands, string[] scripts)
        {
            RoundKnowledge result = null;
            var random = new Random(1);
            var players = Enumerable.Range(0, 4).Select(i => new TestPlayer(random, scripts[i])).ToArray();
            players[seat.Index()].OnDecision = context =>
            {
                if (((IList<PlayCardAction>)context.RoundActions).Count == cardsPlayed)
                {
                    result = Build(context);
                }
            };

            // South keeps quiet about the belote with the king of hearts.
            players[0].WithoutBelote.Add(Deal.Card("KH"));
            var cards = hands.Select(Deal.Cards).ToArray();
            Deal.Play(cards, new Bid(PlayerPosition.South, contract), PlayerPosition.South, players);
            Assert.NotNull(result);
            return result;
        }
    }
}
