namespace Belot.UI.Pages
{
    using System;

    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Scaling;

    using Microsoft.Maui.Controls.Shapes;

    public partial class RulesPage : ContentPage
    {
        // (emoji, titleKey, bodyKey) per section, in reading order.
        private static readonly (string Icon, string TitleKey, string BodyKey)[] Sections =
        {
            ("🂡", "Rules_Cards_Title", "Rules_Cards_Body"),
            ("🗣️", "Rules_Bidding_Title", "Rules_Bidding_Body"),
            ("🎴", "Rules_Play_Title", "Rules_Play_Body"),
            ("✨", "Rules_Combinations_Title", "Rules_Combinations_Body"),
            ("👑", "Rules_Belote_Title", "Rules_Belote_Body"),
            ("🎯", "Rules_Scoring_Title", "Rules_Scoring_Body"),
            ("⚖️", "Rules_Inside_Title", "Rules_Inside_Body"),
            ("🏆", "Rules_Winning_Title", "Rules_Winning_Body"),
        };

        // A double tap on Back goes back once.
        private readonly PageActions actions = new();

        public RulesPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.actions.Activate();
            this.BuildSections();
        }

        protected override void OnDisappearing()
        {
            this.actions.Deactivate();
            base.OnDisappearing();
        }

        // Sizes follow the window (see UiScale); the lists built in code are rebuilt at a new scale.
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            var scale = UiScale.Current.Page;
            UiScale.Current.Update(width, height);
            this.ContentColumn.WidthRequest = PageColumn.Width(width);
            if (UiScale.Current.Page != scale)
            {
                this.BuildSections();
            }
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        // Rebuilt on every appearance so a language switch is reflected.
        private void BuildSections()
        {
            var text = LocalizationManager.Instance;
            this.SectionsHost.Clear();
            foreach (var (icon, titleKey, bodyKey) in Sections)
            {
                var title = new Label
                {
                    TextColor = Color.FromArgb("#F4D586"),
                    FontSize = Ui.Size(16),
                    FontAttributes = FontAttributes.Bold,
                    FormattedText = new FormattedString
                    {
                        Spans =
                        {
                            new Span { Text = icon + "  " },
                            new Span { Text = text[titleKey] },
                        },
                    },
                };

                var body = new Label
                {
                    Text = text[bodyKey],
                    TextColor = Color.FromArgb("#E8EFE2"),
                    FontSize = Ui.Size(13.5),
                    LineHeight = 1.3,
                };

                this.SectionsHost.Add(new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                    BackgroundColor = Color.FromArgb("#26000000"),
                    Padding = Ui.Pad(16, 12),
                    Content = new VerticalStackLayout
                    {
                        Spacing = Ui.Size(6),
                        Children = { title, body },
                    },
                });
            }
        }
    }
}
