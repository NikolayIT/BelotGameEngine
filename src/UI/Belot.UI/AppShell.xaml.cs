namespace Belot.UI
{
    using Belot.UI.Pages;

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            this.InitializeComponent();

            Routing.RegisterRoute("GamePage", typeof(GamePage));
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("StatisticsPage", typeof(StatisticsPage));
            Routing.RegisterRoute("RulesPage", typeof(RulesPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // iOS's swipe from the edge of the screen pops a page without a word, and there it is
            // easily done by accident beside West's cards. A game is only left through its own
            // way out, which asks first (Android's Back button comes to the same question).
            if (args.Source == ShellNavigationSource.Pop && args.CanCancel && this.CurrentPage is GamePage { IsLeaving: false })
            {
                args.Cancel();
            }
        }
    }
}
