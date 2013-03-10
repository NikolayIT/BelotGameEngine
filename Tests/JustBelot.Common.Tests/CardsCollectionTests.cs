namespace JustBelot.Common.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CardsCollectionTests
    {
        [TestMethod]
        public void TheFullDeckOfCardsHasFourSuitedCombinationOfKingAndQueens()
        {
            var cards = CardsCollection.FullDeckOfCards;
            var combinations = cards.NumberOfQueenAndKingCombinations();
            Assert.AreEqual(4, combinations);
        }
    }
}
