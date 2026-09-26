namespace Belot.UI.Game
{
    using Belot.Engine.Game;

    /// <summary>A button of the bidding panel.</summary>
    public sealed class BidOption : ObservableObject
    {
        private bool isEnabled;

        private bool isHinted;

        private string text = string.Empty;

        public BidOption(BidType bid)
        {
            this.Bid = bid;
        }

        public BidType Bid { get; }

        public string Text
        {
            get => this.text;
            set => this.SetField(ref this.text, value ?? string.Empty, nameof(this.Text));
        }

        /// <summary>Gets the colour of the text: red for hearts and diamonds.</summary>
        public string TextColor => this.Bid == BidType.Hearts || this.Bid == BidType.Diamonds ? BelotTexts.Red : BelotTexts.Dark;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetField(ref this.isEnabled, value, nameof(this.IsEnabled), nameof(this.IsVisible), nameof(this.Opacity));
        }

        /// <summary>Gets a value indicating whether the button shows: Double and Redouble only when they are open.</summary>
        public bool IsVisible => this.isEnabled || (this.Bid != BidType.Double && this.Bid != BidType.ReDouble);

        public double Opacity => this.isEnabled ? 1.0 : 0.35;

        public bool IsHinted
        {
            get => this.isHinted;
            set => this.SetField(ref this.isHinted, value, nameof(this.IsHinted));
        }
    }
}
