namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;

    public class GameManager
    {
        private readonly List<IPlayer> players;

        private int dealNumber;

        private int firstPlayerForTheGame;

        private DealManager dealManager;

        public GameManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer> { southPlayer, eastPlayer, northPlayer, westPlayer };

            // South player
            southPlayer.Game = this;
            southPlayer.StartNewGame(PlayerPosition.South);

            // East player
            eastPlayer.Game = this;
            eastPlayer.StartNewGame(PlayerPosition.East);

            // North player
            northPlayer.Game = this;
            northPlayer.StartNewGame(PlayerPosition.North);

            // West player
            westPlayer.Game = this;
            westPlayer.StartNewGame(PlayerPosition.West);
        }

        public bool IsGameOver
        {
            get
            {
                return false; // (SouthNorthScore >= 151 || EastWestScore >= 151) && Last game is not "valat"
            }
        }

        public int SouthNorthScore { get; private set; }

        public int EastWestScore { get; private set; }

        public IPlayer this[PlayerPosition position]
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

        public IPlayer this[int playerIndex]
        {
            get
            {
                return this.players[playerIndex % 4];
            }
        }

        public IPlayer GetTeamMate(IPlayer player)
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

        public IPlayer GetNextPlayer(IPlayer player)
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

        public IPlayer GetFirstPlayerForTheDeal()
        {
            var firstPlayerForTheDeal = (this.dealNumber - this.firstPlayerForTheGame + 4) % 4;
            return this[firstPlayerForTheDeal];
        }

        public void StartNewGame()
        {
            this.SouthNorthScore = 0;
            this.EastWestScore = 0;
            this.dealNumber = 0;
            this.firstPlayerForTheGame = RandomProvider.Next(0, 4);

            while (!this.IsGameOver)
            {
                this.StartNewDeal();
            }
        }

        private void StartNewDeal()
        {
            this.dealNumber++;

            foreach (var player in this.players)
            {
                player.StartNewDeal();
            }

            this.dealManager = new DealManager(this);
            this.dealManager.PlayDeal(); // var dealResult = 
            // TODO: "С капо (валат) не се излиза
        }

        // TODO: Give players access to previous contracts or inform them for the contracts
        // TODO: Give players access to the cards that are already played or inform them
        // TODO: Give players access to settings file?? AI players may use self-learning techniques and may need a place to store their variables
        // TODO: SECURITY: Never expose other players cards (the AI players may use this security hole for cheating)
    }
}
