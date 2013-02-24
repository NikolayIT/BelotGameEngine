namespace JustBelot.Common.Tests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CardTests
    {
        [TestMethod]
        public void EachCardHasDifferentHashCode()
        {
            var cards = new Dictionary<int, Card>();

            foreach (CardSuit cardColor in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardType in Enum.GetValues(typeof(CardType)))
                {
                    var card = new Card(cardType, cardColor);
                    var hashCode = card.GetHashCode();
                    if (cards.ContainsKey(hashCode))
                    {
                        Assert.Fail("Found 2 cards with the same hash code: {0} and {1}", card, cards[hashCode]);
                    }

                    cards.Add(hashCode, card);
                }
            }
        }
    }
}
