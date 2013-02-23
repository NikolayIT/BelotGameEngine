namespace JustBelot.Common
{
    using System;
    using System.Collections.Generic;

    public class GameManager
    {
        private readonly List<IPlayer> players;

        private int southNorthScore;

        private int eastWestScore;

        private int dealNumber;

        private int firstPlayerForTheGame;

        private DealManager dealManager;

        public GameManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer> { southPlayer, eastPlayer, northPlayer, westPlayer };

            // South player
            southPlayer.Game = this;
            southPlayer.Position = PlayerPosition.South;

            // East player
            eastPlayer.Game = this;
            eastPlayer.Position = PlayerPosition.East;

            // North player
            northPlayer.Game = this;
            northPlayer.Position = PlayerPosition.North;

            // West player
            westPlayer.Game = this;
            westPlayer.Position = PlayerPosition.West;
        }

        public bool IsGameOver
        {
            get
            {
                return false; // (SouthNorthScore >= 151 || EastWestScore >= 151) && Last game is not "valat"
            }
        }

        public int SouthNorthScore
        {
            get
            {
                return this.southNorthScore;
            }
        }

        public int EastWestScore
        {
            get
            {
                return this.eastWestScore;
            }
        }

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
            this.southNorthScore = 0;
            this.eastWestScore = 0;
            this.dealNumber = 0;
            this.firstPlayerForTheGame = RandomProvider.Next(0, 4);

            while (!this.IsGameOver)
            {
                this.StartNewDeal();
            }
        }

        public void StartNewDeal()
        {
            this.dealNumber++;
            this.dealManager = new DealManager(this);
            this.dealManager.StartNewDeal();
        }
    }
}
