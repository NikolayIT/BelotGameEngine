namespace Belot.Engine.Players
{
    public enum BelotActionType
    {
        /// <summary>A bid: the answer to a bid decision.</summary>
        Bid = 1,

        /// <summary>The combinations to declare: the answer to an announce decision.</summary>
        Announce = 2,

        /// <summary>A card: the answer to a card decision.</summary>
        PlayCard = 3,
    }
}
