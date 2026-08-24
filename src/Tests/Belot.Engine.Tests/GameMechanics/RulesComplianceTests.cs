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
    /// Executable specification of Belot rules the engine currently violates. Each test asserts
    /// the behavior required by the game rules (see etc/Rules.md and etc/belot-rules.hit.bg/Rules.md,
    /// cross-checked against the reconstructed 2001 belot.exe and SharpBelot reference engines),
    /// so each test FAILS until the corresponding engine defect is fixed.
    /// </summary>
    public class RulesComplianceTests
    {
        // Rule: the belote (K+Q of trumps) is declared "during the play" when the first of the two
        // cards is played — in ANY trick, not just the first one (etc/Rules.md "The 'Belot' bonus";
        // hit.bg §Премии). The engine only activates announces once, before trick 2, so a belote
        // declared from trick 2 onwards keeps IsActive == null and is silently never scored.
        [Fact]
        public void BeloteAnnouncedAfterTheFirstTrickMustBeScored()
        {
            // Spades contract, declared by South. Hands are suit-partitioned so play is forced
            // into a fully deterministic script; West declares belote by leading K♠ in trick 3.
            var south = new ScriptedPlayer(
                C(CardSuit.Heart, CardType.Seven),
                C(CardSuit.Spade, CardType.Ace),
                C(CardSuit.Spade, CardType.Jack),
                C(CardSuit.Spade, CardType.Ten),
                C(CardSuit.Spade, CardType.Nine),
                C(CardSuit.Spade, CardType.Eight));
            var east = new ScriptedPlayer(
                C(CardSuit.Club, CardType.Seven),
                C(CardSuit.Club, CardType.Eight),
                C(CardSuit.Club, CardType.Nine),
                C(CardSuit.Club, CardType.Ten),
                C(CardSuit.Club, CardType.Jack),
                C(CardSuit.Club, CardType.Queen),
                C(CardSuit.Club, CardType.King));
            var north = new ScriptedPlayer(
                C(CardSuit.Diamond, CardType.Seven),
                C(CardSuit.Diamond, CardType.Eight),
                C(CardSuit.Diamond, CardType.Nine),
                C(CardSuit.Diamond, CardType.Ten),
                C(CardSuit.Diamond, CardType.Jack),
                C(CardSuit.Diamond, CardType.Queen),
                C(CardSuit.Diamond, CardType.King));
            var west = new ScriptedPlayer(
                C(CardSuit.Heart, CardType.Ace),
                C(CardSuit.Heart, CardType.Nine),
                C(CardSuit.Spade, CardType.King),
                C(CardSuit.Heart, CardType.Ten),
                C(CardSuit.Heart, CardType.Jack),
                C(CardSuit.Heart, CardType.Queen));

            var playerCards = new List<CardCollection>
            {
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.Seven), C(CardSuit.Spade, CardType.Eight),
                    C(CardSuit.Spade, CardType.Nine), C(CardSuit.Spade, CardType.Ten),
                    C(CardSuit.Spade, CardType.Jack), C(CardSuit.Spade, CardType.Ace),
                    C(CardSuit.Heart, CardType.Seven), C(CardSuit.Heart, CardType.Eight),
                },
                AllOfSuit(CardSuit.Club),
                AllOfSuit(CardSuit.Diamond),
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.King), C(CardSuit.Spade, CardType.Queen),
                    C(CardSuit.Heart, CardType.Nine), C(CardSuit.Heart, CardType.Ten),
                    C(CardSuit.Heart, CardType.Jack), C(CardSuit.Heart, CardType.Queen),
                    C(CardSuit.Heart, CardType.King), C(CardSuit.Heart, CardType.Ace),
                },
            };

            var contract = new Bid(PlayerPosition.South, BidType.Spades);
            var tricksManager = new TricksManager(south, east, north, west);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                new List<Bid>(),
                contract,
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);

            // The belote is registered in trick 3...
            Assert.Contains(
                announces,
                x => x.Type == AnnounceType.Belot && x.Player == PlayerPosition.West);

            var score = new ScoreManager().GetScore(
                contract,
                southNorthTricks,
                eastWestTricks,
                announces,
                0,
                lastTrickWinner);

            // ...so East-West must be credited its 20 points: 11 from cards + 20 belote = 31.
            Assert.Equal(151, score.SouthNorthTotalInRoundPoints);
            Assert.Equal(31, score.EastWestTotalInRoundPoints);
        }

        // Rule: a player may declare all combinations they hold (etc/Rules.md "Bonuses"). The
        // engine removes matched items from availableAnnounces while iterating the player's
        // returned list, and every shipped player (SmartPlayer, DummyPlayer, RandomPlayer)
        // returns context.AvailableAnnounces itself — the same list instance — so every second
        // announce is silently skipped.
        [Fact]
        public void AllAnnouncesMustBeRegisteredWhenThePlayerReturnsTheAvailableAnnouncesList()
        {
            var south = new ScriptedPlayer(
                C(CardSuit.Heart, CardType.Seven),
                C(CardSuit.Spade, CardType.Ace),
                C(CardSuit.Spade, CardType.Jack),
                C(CardSuit.Spade, CardType.Ten),
                C(CardSuit.Spade, CardType.Nine),
                C(CardSuit.Spade, CardType.Eight));
            var east = new ScriptedPlayer(
                C(CardSuit.Club, CardType.Seven),
                C(CardSuit.Club, CardType.Eight),
                C(CardSuit.Club, CardType.Nine),
                C(CardSuit.Club, CardType.Ten),
                C(CardSuit.Club, CardType.Jack),
                C(CardSuit.Club, CardType.Queen),
                C(CardSuit.Club, CardType.King));
            var north = new ScriptedPlayer(
                C(CardSuit.Diamond, CardType.Seven),
                C(CardSuit.Diamond, CardType.Eight),
                C(CardSuit.Diamond, CardType.Nine),
                C(CardSuit.Diamond, CardType.Ten),
                C(CardSuit.Diamond, CardType.Jack),
                C(CardSuit.Diamond, CardType.Queen),
                C(CardSuit.Diamond, CardType.King))
            {
                // Holds the whole diamond suit => two available announces. Returns the
                // AvailableAnnounces list itself, like all shipped players do.
                ReturnAvailableAnnounces = true,
            };
            var west = new ScriptedPlayer(
                C(CardSuit.Heart, CardType.Ace),
                C(CardSuit.Heart, CardType.Nine),
                C(CardSuit.Spade, CardType.King),
                C(CardSuit.Heart, CardType.Ten),
                C(CardSuit.Heart, CardType.Jack),
                C(CardSuit.Heart, CardType.Queen));

            var playerCards = new List<CardCollection>
            {
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.Seven), C(CardSuit.Spade, CardType.Eight),
                    C(CardSuit.Spade, CardType.Nine), C(CardSuit.Spade, CardType.Ten),
                    C(CardSuit.Spade, CardType.Jack), C(CardSuit.Spade, CardType.Ace),
                    C(CardSuit.Heart, CardType.Seven), C(CardSuit.Heart, CardType.Eight),
                },
                AllOfSuit(CardSuit.Club),
                AllOfSuit(CardSuit.Diamond),
                new CardCollection
                {
                    C(CardSuit.Spade, CardType.King), C(CardSuit.Spade, CardType.Queen),
                    C(CardSuit.Heart, CardType.Nine), C(CardSuit.Heart, CardType.Ten),
                    C(CardSuit.Heart, CardType.Jack), C(CardSuit.Heart, CardType.Queen),
                    C(CardSuit.Heart, CardType.King), C(CardSuit.Heart, CardType.Ace),
                },
            };

            var tricksManager = new TricksManager(south, east, north, west);
            tricksManager.PlayTricks(
                1,
                PlayerPosition.South,
                0,
                0,
                playerCards,
                new List<Bid>(),
                new Bid(PlayerPosition.South, BidType.Spades),
                out var announces,
                out _,
                out _,
                out _);

            // North announced everything available (the whole-suit run: sequence of 8 + tierce).
            Assert.Equal(
                2,
                announces.Count(x => x.Player == PlayerPosition.North && x.Type != AnnounceType.Belot));
        }

        // Rule (etc/Rules.md §3.1; hit.bg §3.1): when void in the led suit you must trump only
        // if the trick is currently held by an OPPONENT. The engine decides "who holds the trick"
        // by taking the highest no-trump-order card of ANY suit, so an off-suit discard is
        // mistaken for the winning card. Here the partner led 10♥ and is winning (A♦ is a
        // discard), so the player must be free to play any card.
        [Fact]
        public void PartnerWinningWithTheLedSuitMustNotForceTrumping()
        {
            var validCardsService = new ValidCardsService();
            var trickActions = new List<PlayCardAction>
            {
                new PlayCardAction(C(CardSuit.Heart, CardType.Ten)), // partner leads, currently winning
                new PlayCardAction(C(CardSuit.Diamond, CardType.Ace)), // opponent discards
            };
            var hand = new CardCollection
            {
                C(CardSuit.Spade, CardType.Seven),
                C(CardSuit.Club, CardType.Seven),
            };

            var validCards = validCardsService.GetValidCards(hand, BidType.Spades, trickActions);

            Assert.Equal(hand, validCards);
        }

        // The mirror case: the opponent is winning with the led suit (8♥ over 7♥), the partner's
        // A♦ is just a discard, so the void player MUST trump — yet the engine treats the
        // partner's high discard as the winning card and allows any card.
        [Fact]
        public void OpponentWinningMustForceTrumpingEvenWhenPartnerDiscardedHigh()
        {
            var validCardsService = new ValidCardsService();
            var trickActions = new List<PlayCardAction>
            {
                new PlayCardAction(C(CardSuit.Heart, CardType.Seven)), // opponent leads
                new PlayCardAction(C(CardSuit.Diamond, CardType.Ace)), // partner discards
                new PlayCardAction(C(CardSuit.Heart, CardType.Eight)), // opponent, currently winning
            };
            var hand = new CardCollection
            {
                C(CardSuit.Spade, CardType.Seven),
                C(CardSuit.Club, CardType.Seven),
            };

            var validCards = validCardsService.GetValidCards(hand, BidType.Spades, trickActions);

            Assert.Equal(
                new CardCollection { C(CardSuit.Spade, CardType.Seven) },
                validCards);
        }

        // A low ruff by an opponent must not free the void player from overtrumping. The winner
        // detection seeded "the biggest trump" with the led card and compared trump orders across
        // suits, so a high led card (a nine or a jack) masked the opponent's ruff entirely.
        [Fact]
        public void LowRuffByAnOpponentMustStillForceOvertrumping()
        {
            var validCardsService = new ValidCardsService();
            var trickActions = new List<PlayCardAction>
            {
                new PlayCardAction(C(CardSuit.Diamond, CardType.Jack)), // partner leads J♦
                new PlayCardAction(C(CardSuit.Heart, CardType.Eight)), // opponent ruffs low, winning
            };
            var hand = new CardCollection
            {
                C(CardSuit.Heart, CardType.Ten),
                C(CardSuit.Club, CardType.Seven),
            };

            var validCards = validCardsService.GetValidCards(hand, BidType.Hearts, trickActions);

            Assert.Equal(new CardCollection { C(CardSuit.Heart, CardType.Ten) }, validCards);
        }

        // The same defect in the over-ruff branch: the obligation must be measured against the
        // highest trump actually played, not against the (non-trump) led card's trump order.
        [Fact]
        public void OverruffObligationMustCompareAgainstTheHighestTrumpInTheTrick()
        {
            var validCardsService = new ValidCardsService();
            var trickActions = new List<PlayCardAction>
            {
                new PlayCardAction(C(CardSuit.Diamond, CardType.Jack)), // opponent leads J♦
                new PlayCardAction(C(CardSuit.Heart, CardType.Seven)), // partner ruffs low
                new PlayCardAction(C(CardSuit.Heart, CardType.Eight)), // opponent over-ruffs, winning
            };
            var hand = new CardCollection
            {
                C(CardSuit.Heart, CardType.Ten),
                C(CardSuit.Club, CardType.Seven),
            };

            var validCards = validCardsService.GetValidCards(hand, BidType.Hearts, trickActions);

            Assert.Equal(new CardCollection { C(CardSuit.Heart, CardType.Ten) }, validCards);
        }

        // Rule (etc/Rules.md "Scoring"; hit.bg §Запис): under a double/redouble the winning team
        // records ALL points multiplied — "all bonuses are doubled including the bonus for
        // getting all the hands". The engine instead sets the coefficient to 1 whenever a team
        // is capot, so a doubled valat scores no more (and via a rounding inconsistency can even
        // score less) than an undoubled one.
        [Theory]
        [InlineData(BidType.Hearts | BidType.Double, 50)] // (152 cards + 10 + 90) = 252 -> 25 x2
        [InlineData(BidType.Hearts | BidType.ReDouble, 100)] // 252 -> 25 x4
        [InlineData(BidType.AllTrumps | BidType.Double, 70)] // (248 + 10 + 90) = 348 -> 35 x2
        [InlineData(BidType.NoTrumps | BidType.Double, 70)] // (120 + 10) x2 + 90 = 350 -> 35 x2
        public void DoublingMustStillApplyWhenOneTeamIsCapot(BidType bidType, int expectedWinnerPoints)
        {
            var southNorthTricks = new CardCollection();
            foreach (var card in Card.AllCards)
            {
                southNorthTricks.Add(card);
            }

            var score = new ScoreManager().GetScore(
                new Bid(PlayerPosition.East, bidType),
                southNorthTricks,
                new CardCollection(),
                new List<Announce>(),
                0,
                PlayerPosition.South);

            Assert.True(score.NoTricksForOneOfTheTeams);
            Assert.Equal(expectedWinnerPoints, score.SouthNorthPoints);
            Assert.Equal(0, score.EastWestPoints);
        }

        // Rule (etc/Rules.md "Scoring"; hit.bg §Запис): hanging points stay on the table until a
        // team wins a subsequent deal. An all-pass deal has no winner, so it must not consume
        // them — but RoundManager returns a fresh RoundResult with HangingPoints = 0.
        [Fact]
        public void HangingPointsMustSurviveAnAllPassRound()
        {
            var roundManager = new RoundManager(
                new ScriptedPlayer(),
                new ScriptedPlayer(),
                new ScriptedPlayer(),
                new ScriptedPlayer());

            var result = roundManager.PlayRound(1, PlayerPosition.South, 0, 0, 16);

            Assert.Equal(BidType.Pass, result.Contract.Type);
            Assert.Equal(16, result.HangingPoints);
        }

        // Rule (etc/Rules.md "Bonuses": quads compare "in trump suit order"; hit.bg §Премии:
        // by the card's point value in trumps, table 1; confirmed against the 2001 belot.exe):
        // among the 100-point quads the order is A > 10 > K > Q, so four tens beat four kings
        // and four queens. The engine compares by deck order (A > K > Q > 10) instead.
        [Theory]
        [InlineData(CardType.Ten, CardType.King)]
        [InlineData(CardType.Ten, CardType.Queen)]
        [InlineData(CardType.Ace, CardType.Ten)] // already correct, pinned for completeness
        public void FourOfAKindRankingMustFollowTrumpCardValues(CardType stronger, CardType weaker)
        {
            var strongerAnnounce = new Announce(AnnounceType.FourOfAKind, C(CardSuit.Spade, stronger));
            var weakerAnnounce = new Announce(AnnounceType.FourOfAKind, C(CardSuit.Spade, weaker));

            Assert.True(strongerAnnounce.CompareTo(weakerAnnounce) > 0);
            Assert.True(weakerAnnounce.CompareTo(strongerAnnounce) < 0);
        }

        // The same defect at the round level: East-West's four tens must beat South-North's four
        // kings, so only East-West's quad may be scored.
        [Fact]
        public void FourTensMustWinTheQuadContestAgainstFourKings()
        {
            var announces = new List<Announce>
            {
                new Announce(AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.Ten)) { Player = PlayerPosition.West },
                new Announce(AnnounceType.FourOfAKind, C(CardSuit.Spade, CardType.King)) { Player = PlayerPosition.South },
            };

            new ValidAnnouncesService().UpdateActiveAnnounces(announces);

            Assert.True(announces[0].IsActive);
            Assert.False(announces[1].IsActive);
        }

        // An 8-card suit is declared as a quint (top 5 cards) plus a tierce of the remaining
        // 9-8-7 (hit.bg §Премии: a card may take part in only one combination). The engine
        // labels the extra tierce with the ACE as its top card, which both reuses a card of the
        // quint and makes the tierce unbeatable in the sequence contest.
        [Fact]
        public void EightCardSuitMustOfferTheLeftoverTierceToTheNine()
        {
            var combinations = new ValidAnnouncesService().GetAvailableAnnounces(AllOfSuit(CardSuit.Spade));

            Assert.Contains(
                combinations,
                x => x.Type == AnnounceType.SequenceOf3 && x.Card == C(CardSuit.Spade, CardType.Nine));
        }

        private static Card C(CardSuit suit, CardType type) => Card.GetCard(suit, type);

        private static CardCollection AllOfSuit(CardSuit suit)
        {
            var cards = new CardCollection();
            foreach (var type in Card.AllTypes)
            {
                cards.Add(Card.GetCard(suit, type));
            }

            return cards;
        }
    }
}
