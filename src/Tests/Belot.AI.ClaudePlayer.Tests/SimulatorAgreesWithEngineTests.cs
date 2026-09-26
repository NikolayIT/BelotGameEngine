namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;

    /// <summary>
    /// The search's simulator must play by exactly the engine's rules: the same legal cards at
    /// every decision, the same belotes, and the same score, over thousands of random deals in
    /// every contract (doubled and redoubled too).
    /// </summary>
    public class SimulatorAgreesWithEngineTests
    {
        private static readonly BidType[] Contracts =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps,
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void RandomDealsPlayAndScoreTheSame(int contractIndex)
        {
            for (var seed = 0; seed < 1500; seed++)
            {
                var random = new Random((contractIndex * 100_000) + seed);
                var doubling = seed % 3 == 0 ? BidType.Double : seed % 7 == 0 ? BidType.ReDouble : BidType.Pass;
                var contract = new Bid(Deal.Seats[random.Next(4)], Contracts[contractIndex] | doubling);
                var hanging = seed % 5 == 0 ? random.Next(1, 40) : 0;
                var first = Deal.Seats[random.Next(4)];
                var players = Enumerable.Range(0, 4).Select(_ => new TestPlayer(random)).ToArray();

                // One in four players keeps some belotes to himself.
                foreach (var card in Card.AllCards.Where(_ => random.Next(4) == 0))
                {
                    players[random.Next(4)].WithoutBelote.Add(card);
                }

                var result = Deal.Play(Deal.RandomHands(random), contract, first, players, hanging);

                AssertSimulatorAgrees(result, contract, first, players, hanging);
            }
        }

        private static void AssertSimulatorAgrees(DealResult result, Bid contract, PlayerPosition first, TestPlayer[] players, int hanging)
        {
            var simulator = new BelotSimulator();
            simulator.SetContract(contract.Type, contract.Player.Index(), hanging);
            var state = default(SimState);
            for (var seat = 0; seat < 4; seat++)
            {
                state.Hands[seat] = result.OriginalHands[seat];
            }

            state.Turn = first.Index();
            var offered = new Dictionary<int, uint>();
            foreach (var player in players)
            {
                foreach (var pair in player.Offered)
                {
                    offered.Add(pair.Key, pair.Value);
                }
            }

            var actions = players.First(x => x.RoundActions != null).RoundActions;
            Assert.Equal(32, actions.Count);
            for (var i = 0; i < 32; i++)
            {
                var action = actions[i];
                Assert.Equal(action.Player.Index(), state.Turn);
                var legal = simulator.LegalMoves(in state);
                Assert.Equal(offered.TryGetValue(i, out var engineLegal) ? engineLegal : 1u << action.Card.GetHashCode(), legal);

                // The simulator claims every belote it may, so a withheld one is played as if forced.
                var card = action.Card.GetHashCode();
                var before = state.SouthNorthPoints + state.EastWestPoints;
                simulator.Play(ref state, card, players[action.Player.Index()].WithoutBelote.Contains(action.Card) ? 1u << card : legal);
                var expected = action.Belote ? 20 : 0;
                if (i % 4 == 3)
                {
                    expected += actions.Skip(i - 3).Take(4).Sum(x => x.Card.GetValue(contract.Type));
                }

                Assert.Equal(expected, state.SouthNorthPoints + state.EastWestPoints - before);
            }

            var announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
            var count = 0;
            if (!contract.Type.HasFlag(BidType.NoTrumps))
            {
                for (var seat = 0; seat < 4; seat++)
                {
                    count = AnnounceScorer.AddDeclaredCombinations(result.OriginalHands[seat], seat, announces, count);
                }
            }

            Assert.Equal(result.Announces.Count(x => x.Type != AnnounceType.Belot), count);
            AnnounceScorer.GetPoints(announces, count, out var southNorthAnnounces, out var eastWestAnnounces);
            simulator.Score(in state, southNorthAnnounces, eastWestAnnounces, out var southNorth, out var eastWest, out var hangingPoints);

            Assert.Equal(result.Score.SouthNorthPoints, southNorth);
            Assert.Equal(result.Score.EastWestPoints, eastWest);
            Assert.Equal(result.Score.HangingPoints, hangingPoints);
        }
    }
}
