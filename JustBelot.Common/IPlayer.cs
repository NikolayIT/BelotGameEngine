﻿using System.Collections.Generic;
namespace JustBelot.Common
{
    /// <summary>
    /// Interface that must be implemented by every player (human or AI)
    /// Method calls:
    /// 0. If an empty constructor is available it is called first
    /// 1. StartNewGame is called when the game begins. Player position and game info are passed as arguments.
    /// 2. StartNewDeal is called for each card deal
    /// 3. AddCard is called 8 times (3+2+3) for each card deal. Card is passed as an argument.
    /// 4. AskForBid is called for each player until a contract is agreed
    /// 5. If the contract is not "no trumps", before the first card is played, the player is asked for declarations (e.g. four of a kind and a tierce, etc.)
    /// 6. PlayCard is called 8 times until all cards are played
    /// </summary>
    public interface IPlayer
    {
        string Name { get; }

        void StartNewGame(GameInfo gameInfo, PlayerPosition position);

        void StartNewDeal(DealInfo dealInfo);

        void AddCards(IEnumerable<Card> cards);

        BidType AskForBid(Contract currentContract, IList<BidType> allowedBids, IList<BidType> previousBids);

        IEnumerable<Declaration> AskForDeclarations();

        PlayAction PlayCard();
    }
}
