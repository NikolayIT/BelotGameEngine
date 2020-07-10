namespace Belot.Engine.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    public class TricksManager
    {
        private readonly IList<IPlayer> players;

        private readonly TrickWinnerService trickWinnerService;

        private readonly ValidCardsService validCardsService;

        private readonly ValidAnnouncesService validAnnouncesService;

        public TricksManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer>(4) { southPlayer, eastPlayer, northPlayer, westPlayer };
            this.trickWinnerService = new TrickWinnerService();
            this.validCardsService = new ValidCardsService();
            this.validAnnouncesService = new ValidAnnouncesService();
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
            announces = new List<Announce>(12);
            southNorthTricks = new CardCollection();
            eastWestTricks = new CardCollection();
            lastTrickWinner = firstToPlay;
            var actions = new List<PlayCardAction>(8 * 4);
            var trickActions = new List<PlayCardAction>(4);

            var announceContext = new PlayerGetAnnouncesContext
            {
                RoundNumber = roundNumber,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                FirstToPlayInTheRound = firstToPlay,
                Bids = bids,
                CurrentContract = currentContract,
                CurrentTrickActions = trickActions,
                Announces = announces,
            };
            var playContext = new PlayerPlayCardContext
            {
                RoundNumber = roundNumber,
                EastWestPoints = eastWestPoints,
                SouthNorthPoints = southNorthPoints,
                FirstToPlayInTheRound = firstToPlay,
                CurrentContract = currentContract,
                Bids = bids,
                Announces = announces,
                RoundActions = actions,
                CurrentTrickActions = trickActions,
            };

            var currentPlayer = firstToPlay;
            for (var trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                trickActions.Clear();
                if (trickNumber == 2)
                {
                    this.validAnnouncesService.UpdateAnnouncesToBeScored(announces);
                }

                playContext.CurrentTrickNumber = trickNumber;
                for (var i = 0; i < 4; i++)
                {
                    // Announces
                    if (trickNumber == 1 && !currentContract.Type.HasFlag(BidType.NoTrumps))
                    {
                        // Prepare GetAnnounces context
                        var availableAnnounces =
                            this.validAnnouncesService.GetAvailableAnnounces(playerCards[currentPlayer.Index()]);
                        if (availableAnnounces.Count > 0)
                        {
                            announceContext.MyPosition = currentPlayer;
                            announceContext.MyCards = playerCards[currentPlayer.Index()];
                            announceContext.AvailableAnnounces = availableAnnounces;

                            // Execute GetAnnounces
                            var playerAnnounces = this.players[currentPlayer.Index()].GetAnnounces(announceContext)
                                .ToList();

                            // Validate
                            foreach (var playerAnnounce in playerAnnounces)
                            {
                                var availableAnnounce = availableAnnounces.FirstOrDefault(
                                    x => x.Type == playerAnnounce.Type && x.Card == playerAnnounce.Card);
                                if (availableAnnounce == null)
                                {
                                    // Invalid announce
                                    continue;
                                }

                                availableAnnounces.Remove(availableAnnounce);

                                playerAnnounce.Player = currentPlayer;
                                announces.Add(playerAnnounce);
                            }
                        }
                    }

                    // Prepare PlayCard context
                    var availableCards = this.validCardsService.GetValidCards(
                        playerCards[currentPlayer.Index()],
                        currentContract.Type,
                        trickActions);
                    PlayCardAction action;
                    if (availableCards.Count == 1)
                    {
                        action = new PlayCardAction(availableCards.FirstOrDefault(), false);
                    }
                    else
                    {
                        playContext.MyPosition = currentPlayer;
                        playContext.MyCards = playerCards[currentPlayer.Index()];
                        playContext.AvailableCardsToPlay = availableCards;

                        // Execute PlayCard
                        action = this.players[currentPlayer.Index()].PlayCard(playContext);

                        // Validate
                        if (!availableCards.Contains(action.Card))
                        {
                            throw new BelotGameException($"Invalid card played from {currentPlayer} player.");
                        }

                        // Belote
                        if (action.Belote)
                        {
                            if (this.validAnnouncesService.IsBeloteAllowed(
                                playerCards[currentPlayer.Index()],
                                currentContract.Type,
                                trickActions,
                                action.Card))
                            {
                                announces.Add(new Announce(AnnounceType.Belot, action.Card) { Player = currentPlayer });
                            }
                            else
                            {
                                action.Belote = false;
                            }
                        }
                    }

                    // Update information after the action
                    playerCards[currentPlayer.Index()].Remove(action.Card);
                    action.Player = currentPlayer;
                    action.TrickNumber = trickNumber;
                    actions.Add(action);
                    trickActions.Add(action);

                    // Next player
                    currentPlayer = currentPlayer.Next();
                }

                // The player that wins the trick plays first
                var winner = this.trickWinnerService.GetWinner(currentContract, trickActions);
                if (winner == PlayerPosition.South || winner == PlayerPosition.North)
                {
                    foreach (var trickAction in trickActions)
                    {
                        southNorthTricks.Add(trickAction.Card);
                    }
                }
                else if (winner == PlayerPosition.East || winner == PlayerPosition.West)
                {
                    foreach (var trickAction in trickActions)
                    {
                        eastWestTricks.Add(trickAction.Card);
                    }
                }

                if (trickNumber == 8)
                {
                    lastTrickWinner = winner;
                }

                currentPlayer = winner;

                foreach (var player in this.players)
                {
                    player.EndOfTrick(trickActions);
                }
            }
        }
    }
}
