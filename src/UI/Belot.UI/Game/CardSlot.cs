namespace Belot.UI.Game
{
    using Belot.Engine.Cards;

    /// <summary>A card on the screen: in the person's hand, on the table, or face down in another seat's hand.</summary>
    public sealed class CardSlot : ObservableObject
    {
        private bool isPlayable = true;

        private bool isFaceDown;

        private bool isHinted;

        private string badgeText = string.Empty;

        public CardSlot(Card? card, bool isFaceDown = false)
        {
            this.Card = card;
            this.isFaceDown = isFaceDown || card == null;
        }

        public Card? Card { get; }

        public string ImageSource => this.IsFaceDown || this.Card == null ? CardImageProvider.BackImage : CardImageProvider.For(this.Card);

        public bool IsFaceDown
        {
            get => this.isFaceDown;
            set => this.SetField(ref this.isFaceDown, value, nameof(this.IsFaceDown), nameof(this.ImageSource));
        }

        /// <summary>Gets or sets a value indicating whether the card may be played now (the others are dimmed on the person's turn).</summary>
        public bool IsPlayable
        {
            get => this.isPlayable;
            set => this.SetField(ref this.isPlayable, value, nameof(this.IsPlayable), nameof(this.IsDimmed));
        }

        public bool IsDimmed => !this.IsPlayable;

        /// <summary>Gets or sets a value indicating whether the hint suggests this card (gold outline).</summary>
        public bool IsHinted
        {
            get => this.isHinted;
            set => this.SetField(ref this.isHinted, value, nameof(this.IsHinted));
        }

        /// <summary>Gets or sets the badge on the card ("Belote" when playing it would make one; assists only).</summary>
        public string BadgeText
        {
            get => this.badgeText;
            set => this.SetField(ref this.badgeText, value ?? string.Empty, nameof(this.BadgeText), nameof(this.HasBadge));
        }

        public bool HasBadge => this.badgeText.Length > 0;
    }
}
