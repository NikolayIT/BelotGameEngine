namespace JustBelot.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface that must be implemented by every player (human or AI)
    /// Method calls:
    /// 0. If an empty constructor is available it is called first
    /// 1. StartNewGame is called when the game begins. Player position and game info are passed as arguments.
    /// 2. StartNewDeal is called for each card deal
    /// 3. AddCards is called 2 times (5+3) for each card deal. Cards are passed as an argument.
    /// 4. AskForBid is called for each player until a contract is agreed
    /// 5. If the contract is not "no trumps", before the first card is played, the player is asked for card combinations (e.g. four of a kind and a tierce, etc.)
    /// 6. PlayCard is called 8 times until all cards are played
    /// 7. EndOfDeal is called when the deal is over
    /// </summary>
    public interface IPlayer
    {
        string Name { get; }

        void StartNewGame(GameInfo gameInfo, PlayerPosition position);

        void StartNewDeal(DealInfo dealInfo);

        void AddCards(IEnumerable<Card> cards);

        BidType AskForBid(Contract currentContract, IList<BidType> allowedBids, IList<BidType> previousBids);

        IEnumerable<CardsCombination> AskForCardsCombinations(IEnumerable<CardsCombination> allowedCombinations);

        PlayAction PlayCard(IList<Card> allowedCards, IList<Card> currentTrickCards);

        void EndOfDeal(DealResult dealResult);
    }
}
