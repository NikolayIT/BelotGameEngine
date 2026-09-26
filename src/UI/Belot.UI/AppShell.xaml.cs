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
    }
}
