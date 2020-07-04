namespace Belot.Engine.Tests.Cards
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;

    using Xunit;

    public class CardCollectionTests
    {
        [Fact]
        public void IsReadOnlyShouldReturnFalse()
        {
            Assert.False(new CardCollection().IsReadOnly);
        }

        [Fact]
        public void CountShouldReturn0WhenCardCollectionIsInitialized()
        {
            Assert.Empty(new CardCollection());
        }

        [Fact]
        public void CountShouldReturn1WhenOneCardIsAdded()
        {
            var collection = new CardCollection { Card.GetCard(CardSuit.Club, CardType.Ace) };
            Assert.Single(collection);
        }

        [Fact]
        public void CountShouldReturn1WhenOneCardIsAddedAndThenRemoved()
        {
            var collection = new CardCollection();
            var card = Card.GetCard(CardSuit.Club, CardType.Ace);
            collection.Add(card);
            collection.Remove(card);
            Assert.Empty(collection);
        }

        [Fact]
        public void CountShouldReturnCorrectValueAfterFewCardAdds()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Heart, CardType.King),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Spade, CardType.Jack),
                                     Card.GetCard(CardSuit.Spade, CardType.Nine),
                                 };

            Assert.Equal(6, collection.Count);
        }

        [Fact]
        public void AnyShouldReturnCorrectValueAfterFewCardAdds()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Heart, CardType.King),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Spade, CardType.Jack),
                                     Card.GetCard(CardSuit.Spade, CardType.Nine),
                                 };

            Assert.True(collection.Any(x => x.Suit == CardSuit.Club && x.Type == CardType.Ace));
            Assert.True(collection.Any(x => x.Suit == CardSuit.Heart && x.Type == CardType.Ten));
            Assert.True(collection.Any(x => x.Suit == CardSuit.Heart && x.Type == CardType.King));
            Assert.True(collection.Any(x => x.Suit == CardSuit.Diamond && x.Type == CardType.Queen));
            Assert.True(collection.Any(x => x.Suit == CardSuit.Spade && x.Type == CardType.Jack));
            Assert.True(collection.Any(x => x.Suit == CardSuit.Spade && x.Type == CardType.Nine));
            Assert.False(collection.Any(x => x.Suit == CardSuit.Club && x.Type == CardType.Nine));
            Assert.False(collection.Any(x => x.Suit == CardSuit.Heart && x.Type == CardType.Queen));
            Assert.False(collection.Any(x => x.Suit == CardSuit.Diamond && x.Type == CardType.King));
            Assert.False(collection.Any(x => x.Suit == CardSuit.Spade && x.Type == CardType.Ace));
        }

        [Fact]
        public void HasAnyOfSuitShouldReturnCorrectValueAfterFewCardAdds()
        {
            var collection = new CardCollection(uint.MaxValue);
            var suits = new List<CardSuit> { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade };
            foreach (var cardSuit in suits)
            {
                Assert.True(collection.HasAnyOfSuit(cardSuit));
            }

            foreach (var cardSuit in suits)
            {
                foreach (var card in collection)
                {
                    if (card.Suit == cardSuit)
                    {
                        collection.Remove(card);
                    }
                }

                Assert.False(collection.HasAnyOfSuit(cardSuit));
            }

            foreach (var cardSuit in suits)
            {
                collection.Add(Card.GetCard(cardSuit, CardType.Ace));
                Assert.True(collection.HasAnyOfSuit(cardSuit));
            }
        }

        [Fact]
        public void CountShouldReturnCorrectValueAfterAddingAllCards()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                }
            }

            Assert.Equal(32, collection.Count);
        }

        [Fact]
        public void ContainsShouldReturnTrueForAllCardsAfterAddingThem()
        {
            var collection = new CardCollection();
            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    collection.Add(card);
                    Assert.Contains(card, collection);
                }
            }

            foreach (CardSuit cardSuitValue in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType cardTypeValue in Enum.GetValues(typeof(CardType)))
                {
                    var card = Card.GetCard(cardSuitValue, cardTypeValue);
                    Assert.Contains(card, collection);
                }
            }
        }

        [Fact]
        public void ClearShouldReturn0Cards()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                     Card.GetCard(CardSuit.Heart, CardType.Jack),
                                     Card.GetCard(CardSuit.Spade, CardType.Nine),
                                 };
            collection.Clear();
            Assert.Empty(collection);
            Assert.Empty(collection.ToList());
        }

        [Fact]
        public void RemoveNonExistingCardsShouldNotRemoveThem()
        {
            var collection = new CardCollection { Card.GetCard(CardSuit.Spade, CardType.Ace) };
            collection.Remove(Card.GetCard(CardSuit.Club, CardType.Ace));
            Assert.Single(collection);
        }

        [Fact]
        public void RemoveShouldWorkProperly()
        {
            var card1 = Card.GetCard(CardSuit.Club, CardType.Ace); // 7
            var card2 = Card.GetCard(CardSuit.Spade, CardType.King); // 30
            var collection = new CardCollection { card1, card2 };
            collection.Remove(card1);
            collection.Remove(card2);
            Assert.Empty(collection);
        }

        [Fact]
        public void EnumerableGetEnumeratorShouldReturnNonNullEnumeratorWhichWorksCorrectly()
        {
            var card = Card.GetCard(CardSuit.Spade, CardType.King); // 30
            IEnumerable collection = new CardCollection { card };
            var enumerator = collection.GetEnumerator();
            Assert.NotNull(enumerator);
            enumerator.MoveNext();
            Assert.Equal(card, enumerator.Current);
        }

        [Fact]
        public void GetEnumeratorShouldReturnAllElementsInCollection()
        {
            var cards = new List<Card>
                            {
                                Card.GetCard(CardSuit.Club, CardType.Ace),
                                Card.GetCard(CardSuit.Spade, CardType.Ace),
                                Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                Card.GetCard(CardSuit.Heart, CardType.Jack),
                                Card.GetCard(CardSuit.Club, CardType.Nine),
                                Card.GetCard(CardSuit.Spade, CardType.Nine),
                            };

            var collection = new CardCollection();
            foreach (var card in cards)
            {
                collection.Add(card);
            }

            foreach (var card in collection)
            {
                Assert.True(cards.Contains(card), $"Card {card} not found in collection!");
            }

            // Second enumeration
            var count = 0;
            foreach (var card in collection)
            {
                Assert.True(cards.Contains(card), $"Card {card} not found in collection!");
                count++;
            }

            Assert.Equal(cards.Count, count);
        }

        [Fact]
        public void CopyToShouldWorkProperly()
        {
            var card1 = Card.GetCard(CardSuit.Club, CardType.Ace); // 7
            var card2 = Card.GetCard(CardSuit.Spade, CardType.King); // 30
            var collection = new CardCollection { card1, card2 };
            var array = new Card[2];
            collection.CopyTo(array, 0);
            Assert.Contains(card1, array);
            Assert.Contains(card2, array);
        }

        [Fact]
        //// [Timeout(100)]
        public void GetEnumeratorShouldWorkProperlyInNestedLoops()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Spade, CardType.King),
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Club, CardType.Jack),
                                     Card.GetCard(CardSuit.Heart, CardType.Nine),
                                 };
            foreach (var firstCard in collection)
            {
                Assert.NotNull(firstCard);
                var found = collection.Any(x => x.Equals(Card.GetCard(CardSuit.Diamond, CardType.Queen)));
                Assert.True(found);
            }
        }

        [Fact]
        public void CloneShouldReturnExactSameCollectionOfCards()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace),
                                     Card.GetCard(CardSuit.Spade, CardType.King),
                                     Card.GetCard(CardSuit.Heart, CardType.Ten),
                                     Card.GetCard(CardSuit.Diamond, CardType.Queen),
                                     Card.GetCard(CardSuit.Club, CardType.Jack),
                                     Card.GetCard(CardSuit.Heart, CardType.Nine),
                                 };
            var clonedCollection = collection.Clone() as CardCollection;
            Assert.NotNull(clonedCollection);
            Assert.Equal(collection.Count, clonedCollection.Count);
            foreach (var card in clonedCollection)
            {
                Assert.Contains(card, collection);
            }
        }

        [Fact]
        public void InternalEnumeratorResetMethodShouldAllowNewEnumerating()
        {
            var collection = new CardCollection
                                 {
                                     Card.GetCard(CardSuit.Club, CardType.Ace), // 7
                                     Card.GetCard(CardSuit.Spade, CardType.King), // 30
                                 };
            int count;
            using (var enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                }

                enumerator.Reset();
                count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }
            }

            Assert.Equal(collection.Count, count);
        }
    }
}
