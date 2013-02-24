using System.Collections.Generic;
namespace JustBelot.Common
{
    /// <summary>
    /// Interface that must be implemented by every player (human or AI)
    /// Method calls:
    /// 1. StartNewGame is called when the game begins. Player position is passed as an argument.
    /// 2. StartNewDeal is called for each card deal
    /// 3. AddCard is called 8 times (3+2+3) for each card deal. Card is passed as an argument.
    /// 4. AskForBid is called for each player until a contract is agreed
    /// 5. Before the first card is played, the player is asked for declarations (e.g. four of a kind and a Tierce)
    /// 6. PlayCard is called 8 times until all cards are played
    /// </summary>
    public interface IPlayer
    {
        string Name { get; }

        GameInfo Game { set; }

        void StartNewGame(PlayerPosition position);

        void StartNewDeal();

        void AddCard(Card card);

        BidType AskForBid();

        IEnumerable<Declaration> AskForDeclarations();

        PlayAction PlayCard();
    }
}
