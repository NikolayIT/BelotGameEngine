namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TricksManager
    {
        private readonly IList<IPlayer> players;

        private readonly TrickWinnerService trickWinnerService;

        public TricksManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.trickWinnerService = new TrickWinnerService();
        }

        public void PlayTricks(
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthTeamPoints,
            int eastWestTeamPoints,
            IReadOnlyList<CardCollection> playerCards,
            IList<Bid> bids,
            Bid currentContract,
            out List<Announce> announces,
            out CardCollection southNorthTricks,
            out CardCollection eastWestTricks)
        {
            announces = new List<Announce>(12);
            southNorthTricks = new CardCollection();
            eastWestTricks = new CardCollection();
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

            var trickActions = new List<PlayCardAction>(4);
            var currentPlayer = firstToPlay;
            for (byte trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                trickActions.Clear();
                for (var i = 0; i < 4; i++)
                {
                    // Announces
                    if (trickNumber == 1)
                    {
                        var availableAnnounces = new List<Announce>(); // TODO: Implement
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
                                AvailableAnnounces = availableAnnounces,
                            });

                        //// TODO: Validate
                        foreach (var playerAnnounce in playerAnnounces)
                        {
                            playerAnnounce.PlayerPosition = currentPlayer;
                            announces.Add(playerAnnounce);
                        }
                    }

                    //// TODO: Play cards
                    playContext.MyPosition = currentPlayer;
                    playContext.MyCards = playerCards[currentPlayer.Index()];
                    playContext.AvailableCardsToPlay = playerCards[currentPlayer.Index()];

                    var action = this.players[currentPlayer.Index()].PlayCard(playContext);
                    action.Player = currentPlayer;
                    action.TrickNumber = trickNumber;
                    if (action.Belote)
                    {
                        // TODO: Validate if belot is real
                        announces.Add(
                            new Announce(AnnounceType.Belot, action.Card) { PlayerPosition = currentPlayer });
                    }

                    actions.Add(action);
                    trickActions.Add(action);

                    currentPlayer = currentPlayer.Next();
                }

                // The player that wins the trick plays first
                var winner = this.trickWinnerService.GetWinner(currentContract.Type, trickActions);
                currentPlayer = winner;
            }
        }
    }
}
