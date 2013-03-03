namespace JustBelot.Common
{
    /// <summary>
    /// Game manager wrapper
    /// </summary>
    public class GameInfo
    {
        private readonly GameManager gameManager;

        internal GameInfo(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        public delegate void PlayerBidHandler(BidEventArgs eventArgs);

        public delegate void CardPlayedHandled(CardPlayedEventArgs eventArgs);

        public event PlayerBidHandler PlayerBid;

        public event CardPlayedHandled CardPlayed;

        public int SouthNorthScore
        {
            get
            {
                return this.gameManager.SouthNorthScore;
            }
        }

        public int EastWestScore
        {
            get
            {
                return this.gameManager.EastWestScore;
            }
        }

        public int DealNumber
        {
            get
            {
                return this.gameManager.DealNumber;
            }
        }

        public PlayerInfo this[PlayerPosition position]
        {
            get
            {
                return new PlayerInfo(this.gameManager[position]);
            }
        }

        public PlayerInfo this[int playerIndex]
        {
            get
            {
                return new PlayerInfo(this.gameManager[playerIndex]);
            }
        }

        internal void InformForBid(BidEventArgs eventArgs)
        {
            if (this.PlayerBid != null)
            {
                this.PlayerBid(eventArgs);
            }
        }

        internal void InformForPlayedCard(CardPlayedEventArgs eventArgs)
        {
            if (this.CardPlayed != null)
            {
                this.CardPlayed(eventArgs);
            }
        }
    }
}
