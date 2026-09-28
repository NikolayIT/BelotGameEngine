namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;

    using Xunit;

    public class EquivalentEndgameCardsTests
    {
        [Theory]
        [InlineData(BidType.NoTrumps, 1u)]
        [InlineData(BidType.Hearts, 1u)]
        [InlineData(BidType.Clubs, 5u)]
        [InlineData(BidType.AllTrumps, 5u)]
        public void OnlyZeroPointRanksOfTheSameSuitAreMerged(BidType contract, uint expected)
        {
            var simulator = new BelotSimulator();
            simulator.SetContract(contract, 0, 0);
            var state = default(SimState);
            state.Hands[0] = 7u;
            Assert.Equal(expected, EndgameSearch.EquivalentChoices(in state, simulator, 7u));
            Assert.Equal(0x01010101u, EndgameSearch.EquivalentChoices(in state, simulator, 0x01010101u));
            Assert.Equal(0xF8F8F8F8u, EndgameSearch.EquivalentChoices(in state, simulator, 0xF8F8F8F8u));
        }

        [Fact]
        public void InterveningOpponentCardOrCurrentWinningCardPreventsMerging()
        {
            var simulator = new BelotSimulator();
            simulator.SetContract(BidType.NoTrumps, 0, 0);
            var state = default(SimState);
            state.Hands[0] = 5u;
            state.Hands[1] = 2u;
            Assert.Equal(5u, EndgameSearch.EquivalentChoices(in state, simulator, 5u));
            state.Hands[1] = 0;
            Assert.Equal(1u, EndgameSearch.EquivalentChoices(in state, simulator, 5u));
            state.TrickCards = 1;
            state.WinnerCard = 1;
            Assert.Equal(5u, EndgameSearch.EquivalentChoices(in state, simulator, 5u));
        }

        [Fact]
        public void PrunedFullPlayMatchesUnprunedValuesAcrossContractsDoublingAndBelotes()
        {
            var random = new Random(8631);
            var simulator = new BelotSimulator();
            var removedBranches = 0;
            for (var round = 0; round < 180; round++)
            {
                var deal = NeuralDeal.Deal(Enumerable.Range(0, 32).OrderBy(_ => random.Next()).ToArray(), round % 4, round % 20);
                deal.Bid(FeatureEncoder.BidOfIndex((round % 6) + 1));
                if ((round / 6) % 3 > 0)
                {
                    deal.Bid(BidType.Double);
                    if ((round / 6) % 3 == 2)
                    {
                        deal.Bid(BidType.ReDouble);
                    }
                }

                while (!deal.AuctionFinished)
                {
                    deal.Bid(BidType.Pass);
                }

                deal.StartPlay(simulator, new DeclaredAnnounce[AnnounceScorer.MaxAnnounces]);
                var cards = 16 + (round % 8);
                for (var play = 0; play < cards; play++)
                {
                    deal.DeclareIfFirstCard();
                    var legal = simulator.LegalMoves(in deal.Play);
                    var candidates = Enumerable.Range(0, 32).Where(card => (legal & (1u << card)) != 0).ToArray();
                    deal.PlayCard(simulator, candidates[random.Next(candidates.Length)], legal);
                }

                var team = round & 1;
                var expected = EndgameSearch.Solve(in deal.Play, simulator, team, deal.SouthNorthAnnounces, deal.EastWestAnnounces);
                var actual = PrunedFullPlay(in deal.Play, simulator, team, deal.SouthNorthAnnounces, deal.EastWestAnnounces, ref removedBranches);
                Assert.Equal(expected, actual);
            }

            Assert.True(removedBranches > 100, $"Only {removedBranches} equivalent branches were removed.");
        }

        private static int PrunedFullPlay(in SimState state, BelotSimulator simulator, int team, int southNorth, int eastWest, ref int removedBranches)
        {
            if (state.TricksPlayed == 8)
            {
                simulator.Score(in state, southNorth, eastWest, out var first, out var second, out _);
                return team == 0 ? first - second : second - first;
            }

            var maximizing = (state.Turn & 1) == team;
            var best = maximizing ? int.MinValue : int.MaxValue;
            var legal = simulator.LegalMoves(in state);
            var choices = EndgameSearch.EquivalentChoices(in state, simulator, legal);
            Assert.NotEqual(0u, choices);
            Assert.Equal(choices, choices & legal);
            removedBranches += BitOperations.PopCount(legal & ~choices);
            for (var rest = choices; rest != 0; rest &= rest - 1)
            {
                var copy = state;
                simulator.Play(ref copy, BitOperations.TrailingZeroCount(rest), legal);
                var value = PrunedFullPlay(in copy, simulator, team, southNorth, eastWest, ref removedBranches);
                best = maximizing ? Math.Max(best, value) : Math.Min(best, value);
            }

            return best;
        }
    }
}
