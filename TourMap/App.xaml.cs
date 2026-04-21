namespace TourMap
{
    public partial class App : Application
    {
        private readonly Pages.SplashPage _splashPage;
        private readonly Services.AutoSyncService _autoSyncService;

        public App(Pages.SplashPage splashPage, Services.AutoSyncService autoSyncService)
        {
            _splashPage = splashPage;
            _autoSyncService = autoSyncService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_splashPage);

            window.Created += async (_, _) =>
            {
                try
                {
                    await _autoSyncService.EnsureSyncedAsync("app-created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Initial auto-sync failed: {ex.Message}");
                }
            };

            window.Resumed += async (_, _) =>
            {
                try
                {
                    await _autoSyncService.EnsureSyncedAsync("app-resumed", force: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Resume auto-sync failed: {ex.Message}");
                }
            };

            return window;
        }
    }
}
