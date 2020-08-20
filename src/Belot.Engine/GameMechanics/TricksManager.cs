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
            var currentPlayerIndex = firstToPlay.Index();
            for (var trickNumber = 1; trickNumber <= 8; trickNumber++)
            {
                trickActions.Clear();
                if (trickNumber == 2)
                {
                    this.validAnnouncesService.UpdateActiveAnnounces(announces);
                }

                playContext.CurrentTrickNumber = trickNumber;
                for (var actionNumber = 0; actionNumber < 4; actionNumber++)
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
                            var playerAnnounces = this.players[currentPlayer.Index()].GetAnnounces(announceContext);

                            // Validate
                            for (var i = 0; i < playerAnnounces.Count; i++)
                            {
                                var playerAnnounce = playerAnnounces[i];
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
                        playerCards[currentPlayerIndex],
                        currentContract.Type,
                        trickActions);
                    PlayCardAction action;
                    if (availableCards.Count == 1)
                    {
                        // Only 1 card is available. Play it. Belot is not available in this situation.
                        action = new PlayCardAction(availableCards.FirstOrDefault(), false);
                    }
                    else
                    {
                        playContext.MyPosition = currentPlayer;
                        playContext.MyCards = playerCards[currentPlayerIndex];
                        playContext.AvailableCardsToPlay = availableCards;

                        // Execute PlayCard
                        action = this.players[currentPlayerIndex].PlayCard(playContext);

                        // Validate
                        if (!availableCards.Contains(action.Card))
                        {
                            throw new BelotGameException($"Invalid card played from {currentPlayer} player.");
                        }

                        // Belote
                        if (action.Belote)
                        {
                            if (this.validAnnouncesService.IsBeloteAllowed(
                                playerCards[currentPlayerIndex],
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
                    playerCards[currentPlayerIndex].Remove(action.Card);
                    action.Player = currentPlayer;
                    action.TrickNumber = trickNumber;
                    actions.Add(action);
                    trickActions.Add(action);

                    // Next player
                    currentPlayer = currentPlayer.Next();
                    currentPlayerIndex = currentPlayer.Index();
                }

                var winner = this.trickWinnerService.GetWinner(currentContract, trickActions);
                if (winner == PlayerPosition.South || winner == PlayerPosition.North)
                {
                    southNorthTricks.Add(trickActions[0].Card);
                    southNorthTricks.Add(trickActions[1].Card);
                    southNorthTricks.Add(trickActions[2].Card);
                    southNorthTricks.Add(trickActions[3].Card);
                }
                else if (winner == PlayerPosition.East || winner == PlayerPosition.West)
                {
                    eastWestTricks.Add(trickActions[0].Card);
                    eastWestTricks.Add(trickActions[1].Card);
                    eastWestTricks.Add(trickActions[2].Card);
                    eastWestTricks.Add(trickActions[3].Card);
                }

                if (trickNumber == 8)
                {
                    lastTrickWinner = winner;
                }

                // The player that wins the trick plays first
                currentPlayer = winner;
                currentPlayerIndex = currentPlayer.Index();

                this.players[0].EndOfTrick(trickActions);
                this.players[1].EndOfTrick(trickActions);
                this.players[2].EndOfTrick(trickActions);
                this.players[3].EndOfTrick(trickActions);
            }
        }
    }
}
