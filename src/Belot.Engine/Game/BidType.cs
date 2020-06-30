namespace Belot.Engine.Game
{
    using System;

    [Flags]
    public enum BidType : byte
    {
        Pass = 0,
        Clubs = 1 << 0, // ♣
        Diamonds = 1 << 1, // ♦
        Hearts = 1 << 2, // ♥
        Spades = 1 << 3, // ♠
        NoTrumps = 1 << 4,
        AllTrumps = 1 << 5,
        Double = 1 << 6,
        ReDouble = 1 << 7,
        All = 0b11111111,
    }
}
