namespace Belot.Engine.Players
{
    using System;

    /*  N
     * W E
     *  S
     */
    [Flags]
    public enum PlayerPosition : byte
    {
        South = 1 << 0,
        East = 1 << 1,
        North = 1 << 2,
        West = 1 << 3,
        SouthNorthTeam = South | North,
        EastWestTeam = East | West,
    }
}
