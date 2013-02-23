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

        private DealManager dealManager;

        public GameManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.players = new List<IPlayer> { southPlayer, eastPlayer, northPlayer, westPlayer };
        }

        public bool IsGameOver
        {
            get
            {
                throw new NotImplementedException();
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

        public void StartNewGame()
        {
            this.southNorthScore = 0;
            this.eastWestScore = 0;
            this.dealNumber = 0;

            while (!this.IsGameOver)
            {
                this.StartNewDeal();
            }
        }

        public void StartNewDeal()
        {
            this.dealManager = new DealManager(this, this.dealNumber);
            this.dealManager.StartNewDeal();
            this.dealNumber++;
        }
    }
}
