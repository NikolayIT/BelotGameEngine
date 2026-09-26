namespace Belot.AI.ClaudePlayer.Tests.TestHelpers
{
    using System;
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays one deal's tricks through the engine's TricksManager with the given hands and
    /// contract, so that tests see the exact contexts the engine gives its players.
    /// </summary>
    public static class Deal
    {
        public static readonly PlayerPosition[] Seats =
        {
            PlayerPosition.South, PlayerPosition.East, PlayerPosition.North, PlayerPosition.West,
        };

        private static readonly string[] TypeCodes = { "7", "8", "9", "10", "J", "Q", "K", "A" };

        /// <summary>Parses "10C AS 7H QD" (types 7 8 9 10 J Q K A, suits C D H S).</summary>
        public static CardCollection Cards(string codes)
        {
            var cards = new CardCollection();
            foreach (var code in codes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                cards.Add(Card(code));
            }

            return cards;
        }

        public static Card Card(string code)
        {
            var type = code.Substring(0, code.Length - 1) switch
                {
                    "7" => CardType.Seven,
                    "8" => CardType.Eight,
                    "9" => CardType.Nine,
                    "10" => CardType.Ten,
                    "J" => CardType.Jack,
                    "Q" => CardType.Queen,
                    "K" => CardType.King,
                    "A" => CardType.Ace,
                    _ => throw new ArgumentException(code),
                };
            var suit = code[code.Length - 1] switch
                {
                    'C' => CardSuit.Club,
                    'D' => CardSuit.Diamond,
                    'H' => CardSuit.Heart,
                    'S' => CardSuit.Spade,
                    _ => throw new ArgumentException(code),
                };
            return Engine.Cards.Card.GetCard(suit, type);
        }

        /// <summary>The card in the notation of <see cref="Cards"/>.</summary>
        public static string Code(Card card) => TypeCodes[(int)card.Type] + "CDHS"[(int)card.Suit];

        public static uint Mask(IEnumerable<Card> cards)
        {
            var mask = 0u;
            foreach (var card in cards)
            {
                mask |= 1u << card.GetHashCode();
            }

            return mask;
        }

        /// <summary>Four random hands of eight cards.</summary>
        public static CardCollection[] RandomHands(Random random)
        {
            var cards = new List<Card>(Engine.Cards.Card.AllCards);
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            var hands = new CardCollection[4];
            for (var seat = 0; seat < 4; seat++)
            {
                hands[seat] = new CardCollection();
                for (var i = 0; i < 8; i++)
                {
                    hands[seat].Add(cards[(seat * 8) + i]);
                }
            }

            return hands;
        }

        /// <summary>
        /// Plays the eight tricks. The hands are played from (the engine removes the cards as
        /// they go), so a player's callback sees every hand as it is at that moment.
        /// </summary>
        public static DealResult Play(IList<CardCollection> hands, Bid contract, PlayerPosition firstToPlay, IPlayer[] players, int hangingPoints = 0)
        {
            var original = new uint[4];
            for (var seat = 0; seat < 4; seat++)
            {
                original[seat] = Mask(hands[seat]);
            }

            var tricksManager = new TricksManager(players[0], players[1], players[2], players[3]);
            tricksManager.PlayTricks(
                1,
                firstToPlay,
                0,
                0,
                new List<CardCollection>(hands),
                new List<Bid> { contract },
                contract,
                out var announces,
                out var southNorthTricks,
                out var eastWestTricks,
                out var lastTrickWinner);
            var score = new ScoreManager().GetScore(contract, southNorthTricks, eastWestTricks, announces, hangingPoints, lastTrickWinner);
            return new DealResult(original, announces, score);
        }
    }
}
