namespace JustBelot.Common.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CardsHelperTests
    {
        [TestMethod]
        public void TheFullDeckOfCardsHasFourSuitedCombinationOfKingAndQueens()
        {
            var cards = CardsHelper.GetFullCardDeck();
            var combinations = CardsHelper.NumberOfQueenAndKingCombinations(cards);
            Assert.AreEqual(4, combinations);
        }
    }
}
