namespace TourMap
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Services.LocalizationService.Current.LanguageChanged += OnLanguageChanged;
            ApplyLocalization();

            // Tab pages (MapPage, QrScannerPage, SettingsPage) are self-registered via TabBar in XAML
            // Only register pages that are navigated to via GoToAsync push-nav
            Routing.RegisterRoute(nameof(Pages.PoiDetailPage), typeof(Pages.PoiDetailPage));
        }

        private void OnLanguageChanged()
        {
            MainThread.BeginInvokeOnMainThread(ApplyLocalization);
        }

        private void ApplyLocalization()
        {
            var loc = Services.LocalizationService.Current;
            MapTab.Title = loc["MapBtn"];
            PoiListTab.Title = loc["PoiListBtn"];
            QrTab.Title = loc["QrBtn"];
            OfflineTab.Title = loc["OfflineBtn"];
            SettingsTab.Title = loc["SettingsTitle"];
        }
    }
}
