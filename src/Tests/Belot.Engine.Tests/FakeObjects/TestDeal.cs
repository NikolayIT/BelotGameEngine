namespace Belot.Engine.Tests.FakeObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;

    /// <summary>
    /// Deterministic dealing for integration and invariant tests: a seeded Fisher-Yates shuffle
    /// over the 32 cards, split into four 8-card hands. The engine's own Deck is intentionally
    /// not used here - it shuffles through the non-seedable ThreadSafeRandom.
    /// </summary>
    public static class TestDeal
    {
        public static List<CardCollection> Deal(int seed)
        {
            var random = new Random(seed);
            var indexes = Enumerable.Range(0, 32).ToArray();
            for (var n = indexes.Length - 1; n > 0; n--)
            {
                var k = random.Next(n + 1);
                (indexes[n], indexes[k]) = (indexes[k], indexes[n]);
            }

            var hands = new List<CardCollection>(4);
            for (var player = 0; player < 4; player++)
            {
                var hand = new CardCollection();
                for (var i = 0; i < 8; i++)
                {
                    hand.Add(Card.AllCards[indexes[(player * 8) + i]]);
                }

                hands.Add(hand);
            }

            return hands;
        }
    }
}
