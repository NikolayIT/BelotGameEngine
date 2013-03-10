namespace JustBelot.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HandTests_FindAvailableCardsCombinations
    {
        [TestMethod]
        public void FourJacks()
        {
            var hand = new Hand
                           {
                               new Card().Jack().OfClubs(),
                               new Card().Jack().OfDiamonds(),
                               new Card().Jack().OfHearts(),
                               new Card().Jack().OfSpades(),
                               
                               new Card().Seven().OfSpades(),
                               new Card().Ace().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Eight().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfJacks, CardType.Jack)));
        }

        [TestMethod]
        public void FourNines()
        {
            var hand = new Hand
                           {
                               new Card().Nine().OfClubs(),
                               new Card().Nine().OfDiamonds(),
                               new Card().Nine().OfHearts(),
                               new Card().Nine().OfSpades(),
                               
                               new Card().Seven().OfSpades(),
                               new Card().Ace().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Eight().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfNines, CardType.Nine)));
        }

        [TestMethod]
        public void FourOfAKingAces()
        {
            var hand = new Hand
                           {
                               new Card().Ace().OfClubs(),
                               new Card().Ace().OfDiamonds(),
                               new Card().Ace().OfHearts(),
                               new Card().Ace().OfSpades(),
                               
                               new Card().Seven().OfSpades(),
                               new Card().Eight().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Eight().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfAKind, CardType.Ace)));
        }

        [TestMethod]
        public void NoFourOfAKingSevens()
        {
            var hand = new Hand
                           {
                               new Card().Seven().OfClubs(),
                               new Card().Seven().OfDiamonds(),
                               new Card().Seven().OfHearts(),
                               new Card().Seven().OfSpades(),
                               
                               new Card().Ace().OfSpades(),
                               new Card().Eight().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Eight().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(0, combinations.Count);
            Assert.IsFalse(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfAKind, CardType.Seven)));
        }

        [TestMethod]
        public void NoFourOfAKingEights()
        {
            var hand = new Hand
                           {
                               new Card().Eight().OfClubs(),
                               new Card().Eight().OfDiamonds(),
                               new Card().Eight().OfHearts(),
                               new Card().Eight().OfSpades(),
                               
                               new Card().Ace().OfSpades(),
                               new Card().Seven().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Seven().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(0, combinations.Count);
            Assert.IsFalse(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfAKind, CardType.Eight)));
        }

        [TestMethod]
        public void TierceFromSevenToNine()
        {
            var hand = new Hand
                           {
                               new Card().Eight().OfClubs(),
                               new Card().Nine().OfClubs(),
                               new Card().Seven().OfClubs(),
                               
                               new Card().Ace().OfClubs(),
                               new Card().Ace().OfDiamonds(),
                               new Card().Seven().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().Seven().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Tierce, CardType.Nine, CardSuit.Clubs)));
        }

        [TestMethod]
        public void QuartFromJackToAce()
        {
            var hand = new Hand
                           {
                               new Card().Ace().OfSpades(),
                               new Card().Ace().OfClubs(),
                               new Card().Seven().OfClubs(),
                               new Card().King().OfHearts(),

                               new Card().Jack().OfDiamonds(),
                               new Card().Queen().OfDiamonds(),
                               new Card().Ace().OfDiamonds(),
                               new Card().King().OfDiamonds(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quart, CardType.Ace, CardSuit.Diamonds)));
        }

        [TestMethod]
        public void QuintFromNineToKing()
        {
            var hand = new Hand
                           {
                               new Card().Nine().OfHearts(),
                               new Card().Ace().OfClubs(),
                               new Card().Ten().OfHearts(),
                               new Card().Ace().OfClubs(),
                               new Card().Jack().OfHearts(),
                               new Card().King().OfHearts(),
                               new Card().Ace().OfSpades(),
                               new Card().Queen().OfHearts(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quint, CardType.King, CardSuit.Hearts)));
        }

        [TestMethod]
        public void QuintFromSevenToAce()
        {
            var hand = new Hand
                           {
                               new Card().Eight().OfSpades(),
                               new Card().King().OfSpades(),
                               new Card().Jack().OfSpades(),
                               new Card().Ace().OfSpades(),
                               new Card().Seven().OfSpades(),
                               new Card().Queen().OfSpades(),
                               new Card().Ten().OfSpades(),
                               new Card().Nine().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(1, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quint, CardType.Ace, CardSuit.Spades)));
        }

        [TestMethod]
        public void TwoTiercesFromTheSameSuit()
        {
            var hand = new Hand
                           {
                               new Card().Eight().OfSpades(),
                               new Card().Nine().OfSpades(),
                               new Card().Ten().OfSpades(),
                               new Card().Ten().OfHearts(),
                               new Card().Nine().OfClubs(),
                               new Card().Queen().OfSpades(),
                               new Card().King().OfSpades(),
                               new Card().Ace().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(2, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Tierce, CardType.Ace, CardSuit.Spades)));
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Tierce, CardType.Ten, CardSuit.Spades)));
        }

        [TestMethod]
        public void TwoQuartsFromDifferentSuits()
        {
            var hand = new Hand
                           {
                               new Card().Seven().OfClubs(),
                               new Card().Eight().OfClubs(),
                               new Card().Eight().OfSpades(),
                               new Card().Nine().OfSpades(),
                               new Card().Nine().OfClubs(),
                               new Card().Ten().OfClubs(),
                               new Card().Ten().OfSpades(),
                               new Card().Jack().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(2, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quart, CardType.Ten, CardSuit.Clubs)));
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quart, CardType.Jack, CardSuit.Spades)));
        }

        [TestMethod]
        public void NoCombinationsAvailable()
        {
            var hand = new Hand
                           {
                               new Card().Seven().OfClubs(),
                               new Card().Eight().OfClubs(),
                               new Card().Nine().OfSpades(),
                               new Card().Ten().OfSpades(),
                               new Card().Ten().OfClubs(),
                               new Card().Ten().OfHearts(),
                               new Card().Jack().OfDiamonds(),
                               new Card().Ace().OfClubs(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(0, combinations.Count);
        }

        [TestMethod]
        public void FourOfAKindAndQuart()
        {
            var hand = new Hand
                           {
                               new Card().King().OfClubs(),
                               new Card().King().OfDiamonds(),
                               new Card().Queen().OfDiamonds(),
                               new Card().Jack().OfDiamonds(),
                               new Card().Ten().OfDiamonds(),
                               new Card().Nine().OfDiamonds(),
                               new Card().King().OfHearts(),
                               new Card().King().OfSpades(),
                           };

            var combinations = hand.FindAvailableCardsCombinations().ToList();

            Assert.AreEqual(2, combinations.Count);
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.FourOfAKind, CardType.King)));
            Assert.IsTrue(combinations.Contains(new CardsCombination(CardsCombinationType.Quart, CardType.Queen, CardSuit.Diamonds)));
        }
    }
}
