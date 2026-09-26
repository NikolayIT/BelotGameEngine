namespace Belot.UI.Game
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>The auction ended in a contract and everybody got three more cards.</summary>
    public sealed record ContractInfo(PlayerPosition Declarer, BidType Contract, IReadOnlyList<Card> MyHand, IReadOnlyList<int> CardCounts) : GameEvent;
}
