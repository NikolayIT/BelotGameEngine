namespace Belot.Engine.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.Engine.Tests.FakeObjects;

    using Xunit;

    /// <summary>
    /// Scripted rounds for the announce and belote flow of TricksManager, plus a check of the
    /// context every player receives when asked for a card.
    /// </summary>
    public class TricksManagerScenarioTests
    {
        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void TheBeloteIsClaimedWithTheFirstOfTheTwoCardsOrNotAtAll(bool claimOnTheKing, int expectedBelotes)
        {
            // West leads K♠ then Q♠ in a Spades contract. The belote belongs to the first of
            // the two cards: declined on the king, it cannot be claimed with the queen, since
            // West no longer holds the king by then.
            var (players, hands) = SpadesDeal();
            var west = players[3];
            if (!claimOnTheKing)
            {
                west.CardsPlayedWithoutBelote.Add(C(CardSuit.Spade, CardType.King));
            }

            var announces = Play(players, hands, BidType.Spades);

            Assert.Equal(expectedBelotes, announces.Count(x => x.Type == AnnounceType.Belot));
            Assert.All(
                announces.Where(x => x.Type == AnnounceType.Belot),
                x =>
                {
                    Assert.Equal(PlayerPosition.West, x.Player);
                    Assert.Equal(C(CardSuit.Spade, CardType.King), x.Card);
                    Assert.True(x.IsActive);
                });
        }

        [Fact]
        public void OnlyHeldCombinationsAreRegisteredAndEachOnlyOnce()
        {
            // West holds 9♠-A♠ (a six-card run). Everything else West declares is either a
            // repeat of that run or a combination West does not hold, and must be dropped;
            // South may not smuggle a belote in through the combinations either.
            var (players, hands) = SpadesDeal();
            players[3].AnnouncesToDeclare = new List<Announce>
            {
                new Announce(AnnounceType.SequenceOf6, C(CardSuit.Spade, CardType.Ace)),
                new Announce(AnnounceType.SequenceOf6, C(CardSuit.Spade, CardType.Ace)),
                new Announce(AnnounceType.FourJacks, C(CardSuit.Spade, CardType.Jack)),
                new Announce(AnnounceType.SequenceOf3, C(CardSuit.Spade, CardType.Ace)),
                new Announce(AnnounceType.SequenceOf6, C(CardSuit.Spade, CardType.King)),
            };
            players[0].AnnouncesToDeclare = new List<Announce>
            {
                new Announce(AnnounceType.Belot, C(CardSuit.Heart, CardType.King)),
            };
            players[3].CardsPlayedWithoutBelote.Add(C(CardSuit.Spade, CardType.King));

            var announces = Play(players, hands, BidType.Spades);

            var registered = Assert.Single(announces);
            Assert.Equal(AnnounceType.SequenceOf6, registered.Type);
            Assert.Equal(C(CardSuit.Spade, CardType.Ace), registered.Card);
            Assert.Equal(PlayerPosition.West, registered.Player);
            Assert.True(registered.IsActive);
        }

        [Fact]
        public void CombinationsAreAskedForOnceEachInTheFirstTrickOnly()
        {
            // Every hand of this deal holds a sequence, so every player is asked exactly once.
            var (players, hands) = SpadesDeal();

            Play(players, hands, BidType.Spades);

            Assert.All(players, x => Assert.Equal(1, x.AnnounceAsksCount));
        }

        [Theory]
        [InlineData(1, BidType.Clubs)]
        [InlineData(2, BidType.Hearts)]
        [InlineData(3, BidType.NoTrumps)]
        [InlineData(4, BidType.AllTrumps)]
        [InlineData(5, BidType.Spades | BidType.Double)]
        [InlineData(6, BidType.AllTrumps | BidType.ReDouble)]
        public void EveryCardDecisionGetsAConsistentContext(int seed, BidType contractType)
        {
            var contract = new Bid(PlayerPosition.East, contractType);
            var dealtHands = TestDeal.Deal(seed);
            var checkers = Enumerable.Range(0, 4)
                .Select(i => new ContextCheckingPlayer(new SeededRandomPlayer(seed + (i * 101)), contract))
                .ToArray();

            var tricksManager = new TricksManager(checkers[0], checkers[1], checkers[2], checkers[3]);
            tricksManager.PlayTricks(
                7,
                PlayerPosition.North,
                30,
                40,
                dealtHands.Select(x => new CardCollection(x)).ToList(),
                new List<Bid>(),
                contract,
                out var announces,
                out _,
                out _,
                out _);

            Assert.All(checkers, x => Assert.True(x.Asks > 0));
            Assert.All(checkers, x => Assert.All(x.AnnounceLists, list => Assert.Same(announces, list)));
        }

        private static List<Announce> Play(ScriptedPlayer[] players, List<CardCollection> hands, BidType contractType)
        {
            var tricksManager = new TricksManager(players[0], players[1], players[2], players[3]);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.West,
                0,
                0,
                hands,
                new List<Bid>(),
                new Bid(PlayerPosition.West, contractType),
                out var announces,
                out _,
                out _,
                out _);
            Assert.All(players, x => Assert.Equal(8, x.EndOfTrickCalls));
            return announces;
        }

        // West leads and wins tricks 1-6 with its spades (K♠, Q♠, J♠, 9♠, A♠, 10♠), then leads
        // 9♥; South wins that with K♥ and the last trick with A♥. East and North only discard.
        private static (ScriptedPlayer[] Players, List<CardCollection> Hands) SpadesDeal()
        {
            var south = new ScriptedPlayer(
                C(CardSuit.Spade, CardType.Seven),
                C(CardSuit.Heart, CardType.Seven),
                C(CardSuit.Heart, CardType.Eight),
                C(CardSuit.Heart, CardType.Jack),
                C(CardSuit.Heart, CardType.Queen),
                C(CardSuit.Heart, CardType.King));
            var west = new ScriptedPlayer(
                C(CardSuit.Spade, CardType.King),
                C(CardSuit.Spade, CardType.Queen),
                C(CardSuit.Spade, CardType.Jack),
                C(CardSuit.Spade, CardType.Nine),
                C(CardSuit.Spade, CardType.Ace),
                C(CardSuit.Spade, CardType.Ten),
                C(CardSuit.Heart, CardType.Nine));

            var hands = new List<CardCollection>
            {
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.Seven), C(CardSuit.Spade, CardType.Eight),
                    C(CardSuit.Heart, CardType.Seven), C(CardSuit.Heart, CardType.Eight),
                    C(CardSuit.Heart, CardType.Jack), C(CardSuit.Heart, CardType.Queen),
                    C(CardSuit.Heart, CardType.King), C(CardSuit.Heart, CardType.Ace),
                },
                AllOfSuit(CardSuit.Club),
                AllOfSuit(CardSuit.Diamond),
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.Nine), C(CardSuit.Spade, CardType.Ten),
                    C(CardSuit.Spade, CardType.Jack), C(CardSuit.Spade, CardType.Queen),
                    C(CardSuit.Spade, CardType.King), C(CardSuit.Spade, CardType.Ace),
                    C(CardSuit.Heart, CardType.Nine), C(CardSuit.Heart, CardType.Ten),
                },
            };

            return (new[] { south, SuitedPlayer(CardSuit.Club), SuitedPlayer(CardSuit.Diamond), west }, hands);
        }

        private static ScriptedPlayer SuitedPlayer(CardSuit suit) =>
            new ScriptedPlayer(
                C(suit, CardType.Seven),
                C(suit, CardType.Eight),
                C(suit, CardType.Nine),
                C(suit, CardType.Ten),
                C(suit, CardType.Jack),
                C(suit, CardType.Queen),
                C(suit, CardType.King));

        private static CardCollection AllOfSuit(CardSuit suit)
        {
            var cards = new CardCollection();
            foreach (var type in Card.AllTypes)
            {
                cards.Add(Card.GetCard(suit, type));
            }

            return cards;
        }

        private static Card C(CardSuit suit, CardType type) => Card.GetCard(suit, type);

        /// <summary>
        /// Wraps a player and checks, on every card decision, that the context describes the
        /// round exactly: the seat, the trick number, the cards of the trick so far in seat
        /// order, the round history, the hand and the legal cards for it.
        /// </summary>
        private sealed class ContextCheckingPlayer : IPlayer
        {
            private readonly IPlayer inner;
            private readonly Bid contract;
            private readonly List<PlayCardAction> finishedTricks = new List<PlayCardAction>();
            private readonly ValidCardsService validCardsService = new ValidCardsService();

            public ContextCheckingPlayer(IPlayer inner, Bid contract)
            {
                this.inner = inner;
                this.contract = contract;
            }

            public int Asks { get; private set; }

            public List<IEnumerable<Announce>> AnnounceLists { get; } = new List<IEnumerable<Announce>>();

            public BidType GetBid(PlayerGetBidContext context) => this.inner.GetBid(context);

            public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
            {
                Assert.Equal(8, context.MyCards.Count);
                Assert.Same(this.contract, context.CurrentContract);
                this.AnnounceLists.Add(context.Announces);
                return this.inner.GetAnnounces(context);
            }

            public PlayCardAction PlayCard(PlayerPlayCardContext context)
            {
                this.Asks++;
                var trickNumber = (this.finishedTricks.Count / 4) + 1;
                Assert.Equal(7, context.RoundNumber);
                Assert.Equal(PlayerPosition.North, context.FirstToPlayInTheRound);
                Assert.Equal(30, context.SouthNorthPoints);
                Assert.Equal(40, context.EastWestPoints);
                Assert.Same(this.contract, context.CurrentContract);
                Assert.Equal(trickNumber, context.CurrentTrickNumber);
                Assert.Equal(9 - trickNumber, context.MyCards.Count);
                this.AnnounceLists.Add(context.Announces);

                // The trick so far is played in seat order and ends right before this seat.
                var trick = context.CurrentTrickActions;
                for (var i = 0; i < trick.Count; i++)
                {
                    Assert.Equal(trickNumber, trick[i].TrickNumber);
                    Assert.Equal(trick[i].Player.Next(), i + 1 < trick.Count ? trick[i + 1].Player : context.MyPosition);
                    Assert.DoesNotContain(trick[i].Card, context.MyCards);
                }

                // The round history is every finished trick followed by the current one.
                Assert.Equal(this.finishedTricks.Select(x => x.Card).Concat(trick.Select(x => x.Card)), context.RoundActions.Select(x => x.Card));

                var expected = this.validCardsService.GetValidCards(context.MyCards, this.contract.Type, trick);
                Assert.Equal(expected.OrderBy(x => x.GetHashCode()), context.AvailableCardsToPlay.OrderBy(x => x.GetHashCode()));
                Assert.True(context.AvailableCardsToPlay.Count > 1, "A single legal card must be played without asking.");

                return this.inner.PlayCard(context);
            }

            public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
            {
                this.finishedTricks.AddRange(trickActions);
                this.inner.EndOfTrick(trickActions);
            }

            public void EndOfRound(RoundResult roundResult) => this.inner.EndOfRound(roundResult);

            public void EndOfGame(GameResult gameResult) => this.inner.EndOfGame(gameResult);
        }
    }
}
