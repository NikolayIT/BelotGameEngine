namespace Belot.AI.ClaudePlayer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.ClaudePlayer.Tests.TestHelpers;
    using Belot.AI.DummyPlayer;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Xunit;
    using Xunit.Abstractions;

    public class ClaudePlayerIsmctsTests
    {
        private readonly ITestOutputHelper output;

        public ClaudePlayerIsmctsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void PlaysWholeGamesAgainstSmartPlayerWithoutFallingBack()
        {
            for (var game = 0; game < 3; game++)
            {
                var south = Create(game);
                var north = Create(game + 100);
                var result = new BelotGame(south, new SmartPlayer.SmartPlayer(), north, new SmartPlayer.SmartPlayer()).PlayGame();

                Assert.True(result.SouthNorthPoints >= 151 || result.EastWestPoints >= 151);
                Assert.Equal(0, south.Fallbacks);
                Assert.Equal(0, north.Fallbacks);
            }
        }

        [Fact]
        public void PlaysEverySeatAndContractAgainstRandomPlayers()
        {
            // Random bidders reach every contract, doubles included.
            var claude = Create(7);
            for (var game = 0; game < 2; game++)
            {
                new BelotGame(new RandomPlayer(), claude, new RandomPlayer(), new RandomPlayer()).PlayGame((PlayerPosition)(1 << game));
            }

            Assert.Equal(0, claude.Fallbacks);
        }

        [Fact]
        public void WithASeedAndAnIterationCapTheSearchIsDeterministic()
        {
            var first = ChooseAll(Create(3));
            var second = ChooseAll(Create(3));

            Assert.Equal(first, second);
            Assert.True(first.Count >= 3);
            Assert.DoesNotContain(-1, first);
        }

        [Fact]
        public void WithEveryTopCardItTakesEveryTrick()
        {
            // All trumps, South holds the four jacks and the four nines: a capot however it plays.
            var claude = Create(11);
            var south = new TestPlayer(new Random(1), "JC", claude);
            var result = Deal.Play(
                new[]
                {
                    Deal.Cards("JC JH JD JS 9C 9H 9D 9S"),
                    Deal.Cards("7C 8C 10C QC KC AC 7D 8D"),
                    Deal.Cards("10D QD KD AD 7H 8H 10H QH"),
                    Deal.Cards("KH AH 7S 8S 10S QS KS AS"),
                },
                new Bid(PlayerPosition.South, BidType.AllTrumps),
                PlayerPosition.South,
                new IPlayer[] { south, new TestPlayer(new Random(2)), new TestPlayer(new Random(3)), new TestPlayer(new Random(4)) });

            Assert.True(result.Score.NoTricksForOneOfTheTeams);
            Assert.True(result.Score.SouthNorthPoints > 0);
        }

        [Fact]
        public void SearchesThousandsOfDealsInItsBudget()
        {
            // Not a benchmark (the machine may be busy): it only catches a search gone badly slow,
            // and prints the counts (dotnet test --logger "console;verbosity=detailed").
            var claude = new ClaudePlayerIsmcts { Rng = new Random(1), TimeLimitMilliseconds = 50 };
            var iterations = new List<int>();
            var random = new Random(2);
            foreach (var contract in new[] { BidType.Hearts, BidType.NoTrumps, BidType.AllTrumps, BidType.Clubs })
            {
                var players = new IPlayer[] { new RecordingSearch(claude, iterations), new TestPlayer(random), new TestPlayer(random), new TestPlayer(random) };
                Deal.Play(Deal.RandomHands(random), new Bid(PlayerPosition.East, contract), PlayerPosition.South, players);
            }

            this.output.WriteLine($"Iterations per 50 ms decision: {string.Join(", ", iterations)}; average {iterations.Average():0}");
            Assert.True(iterations.Average() > 500);
        }

        private static ClaudePlayerIsmcts Create(int seed) =>
            new ClaudePlayerIsmcts
            {
                Rng = new Random(seed),
                MaxIterations = 300,
                TimeLimitMilliseconds = 60_000,
            };

        // The cards the player chooses through a fixed deal against seeded random opponents.
        private static List<int> ChooseAll(ClaudePlayerIsmcts claude)
        {
            var chosen = new List<int>();
            var random = new Random(9);
            var hands = Deal.RandomHands(random);
            var south = new TestPlayer(random, inner: claude)
            {
                OnDecision = context => chosen.Add(claude.Search(context)),
            };
            Deal.Play(
                hands,
                new Bid(PlayerPosition.East, BidType.Hearts),
                PlayerPosition.South,
                new IPlayer[] { south, new TestPlayer(random), new TestPlayer(random), new TestPlayer(random) });
            return chosen;
        }

        // Plays as the player and keeps the iteration count of every searched decision.
        private sealed class RecordingSearch : IPlayer
        {
            private readonly ClaudePlayerIsmcts claude;
            private readonly List<int> iterations;

            public RecordingSearch(ClaudePlayerIsmcts claude, List<int> iterations)
            {
                this.claude = claude;
                this.iterations = iterations;
            }

            public BidType GetBid(PlayerGetBidContext context) => this.claude.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.claude.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context)
            {
                var action = this.claude.PlayCard(context);
                if (context.AvailableCardsToPlay.Count > 1)
                {
                    this.iterations.Add(this.claude.LastIterations);
                }

                return action;
            }

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.claude.EndOfTrick(trickActions);

            public void EndOfRound(RoundResult roundResult) => this.claude.EndOfRound(roundResult);

            public void EndOfGame(GameResult gameResult) => this.claude.EndOfGame(gameResult);
        }
    }
}
