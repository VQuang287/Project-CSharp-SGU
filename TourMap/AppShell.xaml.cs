namespace TourMap
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Tab pages (MapPage, QrScannerPage, SettingsPage) are self-registered via TabBar in XAML
            // Only register pages that are navigated to via GoToAsync push-nav
            Routing.RegisterRoute(nameof(Pages.PoiDetailPage), typeof(Pages.PoiDetailPage));
        }
    }
}
