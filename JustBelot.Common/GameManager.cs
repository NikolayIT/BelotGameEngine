namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;

    public class GameManager
    {
        private readonly IList<IPlayer> players;

        private int firstPlayerForTheGame;

        private DealManager dealManager;

        public GameManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer> { southPlayer, eastPlayer, northPlayer, westPlayer };

            this.GameInfo = new GameInfo(this);

            // South player
            southPlayer.StartNewGame(this.GameInfo, PlayerPosition.South);

            // East player
            eastPlayer.StartNewGame(this.GameInfo, PlayerPosition.East);

            // North player
            northPlayer.StartNewGame(this.GameInfo, PlayerPosition.North);

            // West player
            westPlayer.StartNewGame(this.GameInfo, PlayerPosition.West);
        }

        public bool IsGameOver { get; private set; }

        public int SouthNorthScore { get; private set; }

        public int EastWestScore { get; private set; }

        public int DealNumber { get; private set; }

        public GameInfo GameInfo { get; private set; }

        internal PlayerPosition this[IPlayer player]
        {
            get
            {
                if (player == this[PlayerPosition.South])
                {
                    return PlayerPosition.South;
                }

                if (player == this[PlayerPosition.East])
                {
                    return PlayerPosition.East;
                }

                if (player == this[PlayerPosition.North])
                {
                    return PlayerPosition.North;
                }

                if (player == this[PlayerPosition.West])
                {
                    return PlayerPosition.West;
                }

                throw new ArgumentException("Player not found in the game!");
            }
        }

        internal IPlayer this[PlayerPosition position]
        {
            get
            {
                switch (position)
                {
                    case PlayerPosition.West:
                        return this.players[3];
                    case PlayerPosition.East:
                        return this.players[1];
                    case PlayerPosition.South:
                        return this.players[0];
                    case PlayerPosition.North:
                        return this.players[2];
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal IPlayer this[int playerIndex]
        {
            get
            {
                return this.players[playerIndex % 4];
            }
        }

        public void StartNewGame()
        {
            this.IsGameOver = false;
            this.SouthNorthScore = 0;
            this.EastWestScore = 0;
            this.DealNumber = 0;
            this.firstPlayerForTheGame = RandomProvider.Instance.Next(0, 4);

            while (!this.IsGameOver)
            {
                this.StartNewDeal();
            }
        }

        internal IPlayer GetTeamMate(IPlayer player)
        {
            if (player == this[PlayerPosition.South])
            {
                return this[PlayerPosition.North];
            }

            if (player == this[PlayerPosition.North])
            {
                return this[PlayerPosition.South];
            }

            if (player == this[PlayerPosition.East])
            {
                return this[PlayerPosition.West];
            }

            if (player == this[PlayerPosition.West])
            {
                return this[PlayerPosition.East];
            }

            return null;
        }

        internal IPlayer GetNextPlayer(IPlayer player)
        {
            if (player == this[PlayerPosition.South])
            {
                return this[PlayerPosition.East];
            }

            if (player == this[PlayerPosition.East])
            {
                return this[PlayerPosition.North];
            }

            if (player == this[PlayerPosition.North])
            {
                return this[PlayerPosition.West];
            }

            if (player == this[PlayerPosition.West])
            {
                return this[PlayerPosition.South];
            }

            return null;
        }

        internal IPlayer GetFirstPlayerForTheDeal()
        {
            var firstPlayerForTheDeal = (this.DealNumber - this.firstPlayerForTheGame + 4) % 4;
            return this[firstPlayerForTheDeal];
        }
        
        private void StartNewDeal()
        {
            this.DealNumber++;

            var dealInfo = new DealInfo { FirstPlayerPosition = this[this.GetFirstPlayerForTheDeal()] };

            foreach (var player in this.players)
            {
                player.StartNewDeal(dealInfo);
            }

            this.dealManager = new DealManager(this);

            var dealResult = this.dealManager.PlayDeal();

            this.SouthNorthScore += dealResult.SouthNorthPoints;
            this.EastWestScore += dealResult.EastWestPoints;

            foreach (var player in this.players)
            {
                player.EndOfDeal(dealResult);
            }

            if (dealResult.NoTricksForOneOfTheTeams)
            {
                // The game should continue when one of the teams has no tricks
                return;
            }

            if (this.SouthNorthScore >= 151 || this.EastWestScore >= 151)
            {
                this.IsGameOver = true;
            }
        }

        // TODO: Give players access to previous contracts or inform them for the contracts
        // TODO: Give players access to the cards that are already played or inform them
        // TODO: Give players access to settings file?? AI players may use self-learning techniques and may need a place to store their variables
        // TODO: SECURITY: Never expose other players cards (the AI players may use this security hole for cheating)
    }
}
