namespace Belot.UI.Game
{
    using Belot.Engine.GameMechanics;

    /// <summary>A combination the person may declare, with its check box.</summary>
    public sealed class AnnounceOption : ObservableObject
    {
        private bool isSelected;

        private bool isHinted;

        public AnnounceOption(BelotAnnounce announce)
        {
            this.Announce = announce;
        }

        public BelotAnnounce Announce { get; }

        public string Text => BelotTexts.CombinationText(this.Announce.Type, this.Announce.Card);

        public int Points => this.Announce.Value;

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetField(ref this.isSelected, value, nameof(this.IsSelected), nameof(this.Mark));
        }

        public string Mark => this.isSelected ? "☑" : "☐";

        public bool IsHinted
        {
            get => this.isHinted;
            set => this.SetField(ref this.isHinted, value, nameof(this.IsHinted));
        }
    }
}
