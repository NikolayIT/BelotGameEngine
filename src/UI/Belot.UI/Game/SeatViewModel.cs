namespace Belot.UI.Game
{
    using System.Collections.ObjectModel;

    using Belot.Engine.Players;

    /// <summary>One seat at the table as the screen shows it.</summary>
    public sealed class SeatViewModel : ObservableObject
    {
        private string name = string.Empty;

        private int cardCount;

        private string bubbleText = string.Empty;

        private CardSlot? playedCard;

        private bool isToMove;

        private bool isDealer;

        private bool isDeclarer;

        private string declaredText = string.Empty;

        public SeatViewModel(PlayerPosition seat)
        {
            this.Seat = seat;
        }

        public PlayerPosition Seat { get; }

        public bool IsUs => Seats.IsUs(this.Seat);

        public string Name
        {
            get => this.name;
            set => this.SetField(ref this.name, value ?? string.Empty, nameof(this.Name));
        }

        /// <summary>Gets the face-down cards of another seat's hand (as many as it holds).</summary>
        public ObservableCollection<CardSlot> Backs { get; } = new();

        public int CardCount
        {
            get => this.cardCount;
            set
            {
                if (!this.SetField(ref this.cardCount, value, nameof(this.CardCount), nameof(this.HasCards)))
                {
                    return;
                }

                while (this.Backs.Count > value)
                {
                    this.Backs.RemoveAt(this.Backs.Count - 1);
                }

                while (this.Backs.Count < value)
                {
                    this.Backs.Add(new CardSlot(null, true));
                }
            }
        }

        public bool HasCards => this.cardCount > 0;

        /// <summary>Gets or sets what the seat said last: its bid during the auction, then its declarations.</summary>
        public string BubbleText
        {
            get => this.bubbleText;
            set => this.SetField(ref this.bubbleText, value ?? string.Empty, nameof(this.BubbleText), nameof(this.HasBubble));
        }

        public bool HasBubble => this.bubbleText.Length > 0;

        /// <summary>Gets or sets the card the seat has on the table in the trick in progress.</summary>
        public CardSlot? PlayedCard
        {
            get => this.playedCard;
            set => this.SetField(ref this.playedCard, value, nameof(this.PlayedCard), nameof(this.HasPlayedCard));
        }

        public bool HasPlayedCard => this.playedCard != null;

        public bool IsToMove
        {
            get => this.isToMove;
            set => this.SetField(ref this.isToMove, value, nameof(this.IsToMove), nameof(this.TurnOpacity));
        }

        public double TurnOpacity => this.isToMove ? 1.0 : 0.55;

        public bool IsDealer
        {
            get => this.isDealer;
            set => this.SetField(ref this.isDealer, value, nameof(this.IsDealer));
        }

        public bool IsDeclarer
        {
            get => this.isDeclarer;
            set => this.SetField(ref this.isDeclarer, value, nameof(this.IsDeclarer));
        }

        /// <summary>Gets or sets this deal's combinations and belotes of the seat ("Tierce, Belote ♥").</summary>
        public string DeclaredText
        {
            get => this.declaredText;
            set => this.SetField(ref this.declaredText, value ?? string.Empty, nameof(this.DeclaredText), nameof(this.HasDeclared));
        }

        public bool HasDeclared => this.declaredText.Length > 0;
    }
}
