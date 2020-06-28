namespace Belot.Engine.Game
{
    using System;

    [Flags]
    public enum BidType : byte
    {
        Pass = 0,
        Clubs = 1, // ♣
        Diamonds = 2, // ♦
        Hearts = 3, // ♥
        Spades = 4, // ♠
        NoTrumps = 5,
        AllTrumps = 6,
        Double = 64,
        ReDouble = 128,
    }
}
