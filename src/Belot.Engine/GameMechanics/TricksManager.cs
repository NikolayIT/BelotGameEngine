namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TricksManager
    {
        private readonly IList<IPlayer> players;

        public TricksManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
        }

        public void PlayTricks(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthTeamPoints,
            int eastWestTeamPoints,
            IReadOnlyList<CardCollection> playerCards,
            IList<Bid> bids,
            BidType currentContract)
        {
            var announces = new List<Announce>(8);
            var actions = new List<PlayCardAction>(8 * 4);

            var playContext = new PlayerPlayCardContext
            {
                RoundNumber = roundNumber,
                EastWestTeamPoints = eastWestTeamPoints,
                SouthNorthTeamPoints = southNorthTeamPoints,
                FirstToPlayInTheRound = firstToPlay,
                CurrentContract = currentContract,
                Bids = bids,
                Announces = announces,
                PreviousActions = actions,
            };

            for (byte trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                var currentPlayer = firstToPlay;
                for (var i = 0; i < 4; i++)
                {
                    // Announces
                    if (trickNumber == 1)
                    {
                        var playerAnnounces = this.players[currentPlayer.Index()].GetAnnounces(
                            new PlayerGetAnnouncesContext
                            {
                                RoundNumber = roundNumber,
                                EastWestTeamPoints = eastWestTeamPoints,
                                SouthNorthTeamPoints = southNorthTeamPoints,
                                MyPosition = currentPlayer,
                                MyCards = playerCards[currentPlayer.Index()],
                                FirstToPlayInTheRound = firstToPlay,
                                Bids = bids,
                                CurrentContract = currentContract,
                                PreviousActions = actions,
                                Announces = announces,
                                AvailableAnnounces = new List<AnnounceType>(), // TODO: Implement
                            });

                        //// TODO: Validate
                        foreach (var playerAnnounce in playerAnnounces)
                        {
                            announces.Add(new Announce(currentPlayer, playerAnnounce));
                        }
                    }

                    //// TODO: Play cards
                    playContext.MyPosition = currentPlayer;
                    playContext.MyCards = playerCards[currentPlayer.Index()];
                    playContext.AvailableCardsToPlay = playerCards[currentPlayer.Index()];
                    var action = this.players[currentPlayer.Index()].PlayCard(playContext);

                    action.Player = currentPlayer;
                    action.TrickNumber = trickNumber;
                    actions.Add(action);

                    currentPlayer = currentPlayer.Next();
                }

                // TODO: Change current player
            }
        }
    }
}
