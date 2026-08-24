namespace Belot.Engine.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    /// <summary>
    /// Seeded random rounds through the full TricksManager + ScoreManager stack, asserting the
    /// invariants that every legal Belot round must satisfy. Fully deterministic per seed. This
    /// is the safety net for hot-path refactoring: any behavioral drift in dealing, legality,
    /// trick resolution, announce handling or scoring fails here.
    /// </summary>
    public class RoundInvariantsTests
    {
        private static readonly PlayerPosition[] Positions =
        {
            PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West,
        };

        public static IEnumerable<object[]> SeedsAndContracts =>
            from seed in Enumerable.Range(1, 30)
            from contract in new[]
            {
                BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
                BidType.NoTrumps, BidType.AllTrumps,
            }
            select new object[] { seed, contract };

        [Theory]
        [MemberData(nameof(SeedsAndContracts))]
        public void RandomRoundsPreserveEveryRoundInvariant(int seed, BidType contractType)
        {
            var firstToPlay = Positions[seed % 4];
            var declarer = Positions[(seed / 4) % 4];
            var hangingIn = (seed % 2) * 16;
            var contract = new Bid(declarer, contractType);

            var dealtHands = TestDeal.Deal(seed);
            var playerCards = dealtHands.Select(x => new CardCollection(x)).ToList();
            var recorders = Enumerable.Range(0, 4)
                .Select(i => new RecordingPlayer(new SeededRandomPlayer((seed * 17) + i)))
                .ToArray();

            var tricksManager = new TricksManager(recorders[0], recorders[1], recorders[2], recorders[3]);
            tricksManager.PlayTricks(
                1,
                firstToPlay,
                0,
                0,
                playerCards,
                new List<Bid>(),
                contract,
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            var tricks = recorders[0].Tricks;
            Assert.Equal(8, tricks.Count);

            // 1) The 32 cards end up partitioned between the two trick piles.
            Assert.Equal(32, southNorthTricks.Count + eastWestTricks.Count);
            foreach (var card in Card.AllCards)
            {
                Assert.True(
                    southNorthTricks.Contains(card) ^ eastWestTricks.Contains(card),
                    $"{card} is not in exactly one trick pile.");
            }

            // 2) Full replay: every play was legal for the hand state at that moment, every trick
            //    was collected by the team of its winner, and the winner led the next trick.
            var replayHands = dealtHands.Select(x => new CardCollection(x)).ToList();
            var validCardsService = new ValidCardsService();
            var trickWinnerService = new TrickWinnerService();
            var expectedLeader = firstToPlay;
            PlayerPosition trickWinner = firstToPlay;
            foreach (var trick in tricks)
            {
                Assert.Equal(4, trick.Count);
                Assert.Equal(expectedLeader, trick[0].Player);

                var soFar = new List<PlayCardAction>(4);
                foreach (var (player, card) in trick)
                {
                    var hand = replayHands[player.Index()];
                    var valid = validCardsService.GetValidCards(hand, contractType, soFar);
                    Assert.Contains(card, valid);
                    hand.Remove(card);
                    soFar.Add(new PlayCardAction(card) { Player = player });
                }

                trickWinner = trickWinnerService.GetWinner(contract, soFar);
                var winnerPile = trickWinner == PlayerPosition.South || trickWinner == PlayerPosition.North
                                     ? southNorthTricks
                                     : eastWestTricks;
                foreach (var (_, card) in trick)
                {
                    Assert.Contains(card, winnerPile);
                }

                expectedLeader = trickWinner;
            }

            Assert.Equal(trickWinner, lastTrickWinner);

            // 3) Announce sanity: none at all in No Trumps; every belote is a played trump K/Q.
            if (contractType == BidType.NoTrumps)
            {
                Assert.Empty(announces);
            }

            foreach (var belote in announces.Where(x => x.Type == AnnounceType.Belot))
            {
                Assert.True(belote.Card.Type == CardType.King || belote.Card.Type == CardType.Queen);
                if (contractType != BidType.AllTrumps)
                {
                    Assert.Equal(contractType.ToCardSuit(), belote.Card.Suit);
                }

                Assert.Contains(
                    tricks.SelectMany(x => x),
                    play => play.Player == belote.Player && play.Card == belote.Card);
            }

            // 4) Scoring identities.
            var score = new ScoreManager().GetScore(
                contract,
                southNorthTricks,
                eastWestTricks,
                announces,
                hangingIn,
                lastTrickWinner);

            var capot = southNorthTricks.Count == 0 || eastWestTricks.Count == 0;
            Assert.Equal(capot, score.NoTricksForOneOfTheTeams);

            var activeAnnouncePoints = announces.Where(x => x.IsActive == true).Sum(x => x.Value);
            var baseTotal = contractType == BidType.NoTrumps ? 260 : contractType == BidType.AllTrumps ? 258 : 162;
            Assert.Equal(
                baseTotal + activeAnnouncePoints + (capot ? 90 : 0),
                score.SouthNorthTotalInRoundPoints + score.EastWestTotalInRoundPoints);

            // 5) Board conservation, by outcome branch (contracts here are undoubled).
            var snTotal = score.SouthNorthTotalInRoundPoints;
            var ewTotal = score.EastWestTotalInRoundPoints;
            var declarerIsSouthNorth = declarer == PlayerPosition.South || declarer == PlayerPosition.North;
            var declarerTotal = declarerIsSouthNorth ? snTotal : ewTotal;
            var defenderTotal = declarerIsSouthNorth ? ewTotal : snTotal;
            var declarerBoard = declarerIsSouthNorth ? score.SouthNorthPoints : score.EastWestPoints;
            var defenderBoard = declarerIsSouthNorth ? score.EastWestPoints : score.SouthNorthPoints;

            if (declarerTotal < defenderTotal)
            {
                // Inside: the defenders record everything, plus what was hanging.
                Assert.Equal(0, declarerBoard);
                Assert.Equal((int)Math.Round((snTotal + ewTotal) / 10.0) + hangingIn, defenderBoard);
                Assert.Equal(0, score.HangingPoints);
            }
            else if (declarerTotal == defenderTotal)
            {
                // Level: the defenders bank their half, the declarers' half hangs.
                Assert.Equal(0, declarerBoard);
                Assert.Equal(ScoreManager.RoundPoints(contractType, defenderTotal, true), defenderBoard);
                Assert.Equal(
                    hangingIn + ScoreManager.RoundPoints(contractType, declarerTotal, false),
                    score.HangingPoints);
            }
            else
            {
                // Made: both teams round per the contract tables; hanging goes to the winner.
                Assert.Equal(
                    ScoreManager.RoundPoints(contractType, declarerTotal, true) + hangingIn,
                    declarerBoard);
                Assert.Equal(ScoreManager.RoundPoints(contractType, defenderTotal, false), defenderBoard);
                Assert.Equal(0, score.HangingPoints);
            }
        }

        private sealed class RecordingPlayer : IPlayer
        {
            private readonly IPlayer inner;

            public RecordingPlayer(IPlayer inner)
            {
                this.inner = inner;
            }

            public List<List<(PlayerPosition Player, Card Card)>> Tricks { get; } =
                new List<List<(PlayerPosition Player, Card Card)>>();

            public BidType GetBid(PlayerGetBidContext context) => this.inner.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.inner.GetAnnounces(context);

            public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.inner.PlayCard(context);

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
                // The engine reuses the trick list between tricks, so copy what we need.
                this.Tricks.Add(trickActions.Select(x => (x.Player, x.Card)).ToList());
            }

            public void EndOfRound(RoundResult roundResult)
            {
            }

            public void EndOfGame(GameResult gameResult)
            {
            }
        }
    }
}
