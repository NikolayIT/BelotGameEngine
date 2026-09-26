namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Game;

    /// <summary>
    /// How a finished deal went and was scored (the <see cref="RoundResult"/> the players got, as a
    /// plain model), with everything public once it is over: the auction, the tricks and the
    /// combinations with their cards. The views' <see cref="BelotSeatView.PreviousRounds"/> share
    /// these objects with the match record, so copy them before changing anything.
    /// </summary>
    public sealed class BelotRoundSummary
    {
        /// <summary>Gets or sets the contract: its declarer and type (Pass for a passed-out deal).</summary>
        public Bid Contract { get; set; }

        /// <summary>Gets or sets the game points South-North got from the deal.</summary>
        public int SouthNorthPoints { get; set; }

        /// <summary>Gets or sets the game points East-West got from the deal.</summary>
        public int EastWestPoints { get; set; }

        /// <summary>Gets or sets South-North's points in the deal (cards, last trick, combinations, capot), before rounding.</summary>
        public int SouthNorthTotalInRoundPoints { get; set; }

        /// <summary>Gets or sets East-West's points in the deal, before rounding.</summary>
        public int EastWestTotalInRoundPoints { get; set; }

        /// <summary>Gets or sets a value indicating whether a team took no trick (a capot).</summary>
        public bool NoTricksForOneOfTheTeams { get; set; }

        /// <summary>Gets or sets the points left hanging for the next played deal.</summary>
        public int HangingPoints { get; set; }

        /// <summary>Gets or sets the combinations and belotes declared, with their cards and whether they scored.</summary>
        public IReadOnlyList<BelotAnnounce> Announces { get; set; }

        /// <summary>Gets or sets the auction, in order (the passes of seats that could only pass included).</summary>
        public IReadOnlyList<Bid> Bids { get; set; }

        /// <summary>Gets or sets the eight tricks (none for a passed-out deal).</summary>
        public IReadOnlyList<BelotTrick> Tricks { get; set; }
    }
}
