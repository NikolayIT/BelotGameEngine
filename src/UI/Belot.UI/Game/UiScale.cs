namespace Belot.UI.Game
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// How big everything is drawn. The pages are designed for a small phone and every size in
    /// them (fonts, cards, gaps, buttons) is a design size multiplied by a scale that follows the
    /// window: the same picture on a phone, a tablet, an emulator at any density or a desktop
    /// window, never tiny text in a big empty screen. The XAML asks for a design size with
    /// <c>{ui:Size 13}</c> (the scrolling pages) or <c>{ui:TableSize 13}</c> (the game table) and
    /// gets a binding to this object, so everything follows a resize at once.
    /// <para>No MAUI types here: the UI tests compile this file.</para>
    /// </summary>
    public sealed class UiScale : INotifyPropertyChanged
    {
        /// <summary>The game table's design size (a small phone's page, portrait): the worst case (trick one with declarations at every seat and eight cards each) needs about 606 units of height.</summary>
        public const double TableDesignWidth = 360;

        /// <inheritdoc cref="TableDesignWidth"/>
        public const double TableDesignHeight = 640;

        /// <summary>The widest the table gets, in design units: a wide window keeps the four seats together.</summary>
        public const double TableMaxDesignWidth = 600;

        /// <summary>The scrolling pages' design width; their height only limits a landscape window.</summary>
        public const double PageDesignWidth = 400;

        /// <inheritdoc cref="PageDesignWidth"/>
        public const double PageDesignHeight = 640;

        /// <summary>The hand's card width at scale 1 (the cards overlap to fit eight in the width).</summary>
        public const double HandCardDesignWidth = 70;

        /// <summary>The table's side padding at scale 1.</summary>
        public const double TablePaddingDesign = 8;

        private UiScale()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static UiScale Current { get; } = new();

        /// <summary>Gets the scale of the scrolling pages (start, settings, statistics, rules).</summary>
        public double Page { get; private set; } = 1;

        /// <summary>Gets the scale of the game table.</summary>
        public double Table { get; private set; } = 1;

        /// <summary>Gets the gap between the hand's cards (negative: they overlap), so eight fit the width.</summary>
        public double HandSpacing { get; private set; } = HandSpacingFor(TableDesignWidth, 1);

        /// <summary>The scrolling pages' scale for a window of this size (dp): the design width fills a phone and grows on bigger screens.</summary>
        public static double PageScaleFor(double width, double height) =>
            Math.Clamp(Math.Min(width / PageDesignWidth, height / PageDesignHeight), 1, 2);

        /// <summary>The table's scale for a window of this size (dp): the whole table fits, whichever side is short.</summary>
        public static double TableScaleFor(double width, double height) =>
            Math.Clamp(Math.Min(width / TableDesignWidth, height / TableDesignHeight), 0.8, 3);

        /// <summary>The gap between the hand's cards: a comfortable overlap, less when eight would not fit the width.</summary>
        public static double HandSpacingFor(double width, double scale)
        {
            var card = HandCardDesignWidth * scale;
            var room = width - (2 * TablePaddingDesign * scale) - card;
            var step = Math.Min(card * 0.62, Math.Max(room / 7, card * 0.3));
            return Math.Round(step - card, 1);
        }

        /// <summary>Follows the window: called by every page as it is laid out.</summary>
        public void Update(double width, double height)
        {
            if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height))
            {
                return;
            }

            var page = Math.Round(PageScaleFor(width, height), 3);
            var table = Math.Round(TableScaleFor(width, height), 3);
            var spacing = HandSpacingFor(Math.Min(width, TableMaxDesignWidth * table), table);
            if (page != this.Page)
            {
                this.Page = page;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Page)));
            }

            if (table != this.Table)
            {
                this.Table = table;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Table)));
            }

            if (spacing != this.HandSpacing)
            {
                this.HandSpacing = spacing;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HandSpacing)));
            }
        }
    }
}
