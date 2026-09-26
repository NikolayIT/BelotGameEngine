namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// Plays the eight tricks of a deal with <see cref="IPlayer"/>s: a loop over the step-by-step
    /// <see cref="TrickPlay"/>, which holds the rules.
    /// </summary>
    public class TricksManager
    {
        private readonly IPlayer[] players;

        public TricksManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new[] { southPlayer, eastPlayer, northPlayer, westPlayer };
        }

        public void PlayTricks(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            IReadOnlyList<CardCollection> playerCards,
            IList<Bid> bids,
            Bid currentContract,
            out List<Announce> announces,
            out CardCollection southNorthTricks,
            out CardCollection eastWestTricks,
            out PlayerPosition lastTrickWinner)
        {
            var tricks = new TrickPlay(
                this.players,
                roundNumber,
                firstToPlay,
                southNorthPoints,
                eastWestPoints,
                playerCards,
                bids,
                currentContract);
            while (!tricks.IsFinished)
            {
                if (tricks.Decision == BelotDecision.Announce)
                {
                    PlayerDriver.Announce(this.players, tricks);
                }
                else
                {
                    PlayerDriver.PlayCard(this.players, tricks);
                }
            }

            announces = tricks.Announces;
            southNorthTricks = tricks.SouthNorthTricks;
            eastWestTricks = tricks.EastWestTricks;
            lastTrickWinner = tricks.LastTrickWinner;
        }
    }
}
