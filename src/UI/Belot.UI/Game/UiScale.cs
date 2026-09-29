namespace Belot.UI.Game
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// How big everything is drawn. The pages are designed for a small phone and every size in
    /// them (fonts, cards, gaps, buttons) is a design size multiplied by a scale that follows the
    /// window, with modest upper limits so tablets retain useful space and readable text. The XAML asks for a design size with
    /// <c>{ui:Size 13}</c> (the scrolling pages) or <c>{ui:TableSize 13}</c> (the game table) and
    /// gets a binding to this object, so everything follows a resize at once.
    /// Live table text uses <c>{ui:TableTextSize 13}</c> to include bounded system font scaling
    /// without changing the card geometry; other page text keeps native font scaling.
    /// <para>No MAUI types here: the UI tests compile this file.</para>
    /// </summary>
    public sealed class UiScale : INotifyPropertyChanged
    {
        /// <summary>The game table's design size (a small phone's page, portrait): the worst case (trick one with declarations at every seat and eight cards each) needs about 606 units of height.</summary>
        public const double TableDesignWidth = 360;

        /// <inheritdoc cref="TableDesignWidth"/>
        public const double TableDesignHeight = 640;

        /// <summary>The widest the table gets, in design units: a wide window keeps the four seats together.</summary>
        public const double TableMaxDesignWidth = 480;

        /// <summary>A tablet gets modestly larger controls, not a magnified phone screen.</summary>
        public const double MaximumTableScale = 1.25;

        public const double MaximumPageScale = 1.1;

        /// <summary>Limits text growth on the fixed game board; scrolling pages keep the full system text scale.</summary>
        public const double MaximumTableTextScale = 1.3;

        public const double TableMaxDesignHeight = 760;

        /// <summary>The minimum metric tile width at normal text size, before switching statistics to one column.</summary>
        public const double StatisticColumnDesignWidth = 140;

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

        /// <summary>Gets the platform's text scale, independent of the window size.</summary>
        public double SystemFontScale { get; private set; } = 1;

        /// <summary>Gets the game board's text scale, including its bounded accessibility enlargement.</summary>
        public double TableText => this.Table * Math.Min(this.SystemFontScale, MaximumTableTextScale);

        /// <summary>Gets the gap between the hand's cards (negative: they overlap), so eight fit the width.</summary>
        public double HandSpacing { get; private set; } = HandSpacingFor(TableDesignWidth, 1);

        /// <summary>The scrolling pages' scale for a window of this size (dp): the design width fills a phone and grows modestly on bigger screens.</summary>
        public static double PageScaleFor(double width, double height) =>
            Math.Clamp(Math.Min(width / PageDesignWidth, height / PageDesignHeight), 1, MaximumPageScale);

        /// <summary>The table's scale for a window of this size (dp): the whole table fits, whichever side is short.</summary>
        public static double TableScaleFor(double width, double height) =>
            Math.Clamp(Math.Min(width / TableDesignWidth, height / TableDesignHeight), 0.8, MaximumTableScale);

        /// <summary>Uses one metric column when two would crowd enlarged numbers; width is in page design units.</summary>
        public static int StatisticColumnsFor(double width, double fontScale)
        {
            if (!double.IsFinite(width) || width <= 0)
            {
                return 1;
            }

            var scale = double.IsFinite(fontScale) && fontScale > 0 ? fontScale : 1;
            return width / scale >= 2 * StatisticColumnDesignWidth ? 2 : 1;
        }

        /// <summary>Keeps short settings choices in one row when they fit; enlarged text gets full-width rows.</summary>
        public static int SettingsChoiceColumnsFor(double width, double fontScale, int choices)
        {
            if (choices is not (2 or 3))
            {
                throw new ArgumentOutOfRangeException(nameof(choices));
            }

            if (!double.IsFinite(width) || width <= 0)
            {
                return 1;
            }

            var scale = double.IsFinite(fontScale) && fontScale > 0 ? fontScale : 1;
            var minimumColumnWidth = choices == 2 ? 140 : 90;
            return width / scale >= choices * minimumColumnWidth ? choices : 1;
        }

        /// <summary>Stacks the home header and seat inputs when enlarged text needs their full width.</summary>
        public static int StartColumnsFor(double width, double fontScale)
        {
            if (!double.IsFinite(width) || width <= 0)
            {
                return 1;
            }

            var scale = double.IsFinite(fontScale) && fontScale > 0 ? fontScale : 1;
            return width / scale >= 280 ? 2 : 1;
        }

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
            if (width <= 0 || height <= 0 || !double.IsFinite(width) || !double.IsFinite(height))
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
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TableText)));
            }

            if (spacing != this.HandSpacing)
            {
                this.HandSpacing = spacing;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.HandSpacing)));
            }
        }

        /// <summary>Follows platform font changes without changing card sizes or the scrolling pages' scale.</summary>
        public void UpdateSystemFontScale(double value)
        {
            var scale = double.IsFinite(value) && value > 0 ? value : 1;
            if (scale == this.SystemFontScale)
            {
                return;
            }

            this.SystemFontScale = scale;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SystemFontScale)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TableText)));
        }
    }
}
