namespace Belot.Engine.GameMechanics
{
    /// <summary>Why a bid may not be made (see <see cref="Auction.Check"/>).</summary>
    internal enum BidCheck
    {
        Ok = 0,

        MoreThanOneFlag = 1,

        NotPermitted = 2,
    }
}
