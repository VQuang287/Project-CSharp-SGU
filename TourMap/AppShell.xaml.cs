using Microsoft.Maui.ApplicationModel;

namespace TourMap
{
    public partial class AppShell : Shell
    {
        private bool _runtimeBootstrapped;
        private bool _deviceTrackingConnecting;

        public AppShell()
        {
            InitializeComponent();
            Services.LocalizationService.Current.LanguageChanged += OnLanguageChanged;
            ApplyLocalization();

            // Tab pages (MapPage, QrScannerPage, SettingsPage) are self-registered via TabBar in XAML
            // Only register pages that are navigated to via GoToAsync push-nav
            Routing.RegisterRoute(nameof(Pages.PoiDetailPage), typeof(Pages.PoiDetailPage));
            Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
            Routing.RegisterRoute(nameof(Pages.RegisterPage), typeof(Pages.RegisterPage));
            Routing.RegisterRoute(nameof(Pages.ProfilePage), typeof(Pages.ProfilePage));
            
            // Tour pages
            Routing.RegisterRoute(nameof(Pages.Tours.TourListPage), typeof(Pages.Tours.TourListPage));
            Routing.RegisterRoute(nameof(Pages.Tours.TourDetailPage), typeof(Pages.Tours.TourDetailPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await EnsureDeviceTrackingConnectedAsync();
            await NavigatePendingDeepLinkAsync();

            if (_runtimeBootstrapped)
                return;
            _runtimeBootstrapped = true;

            try
            {
                // Proactively request location permission & start runtime.
                // This ensures users who already completed onboarding still get GPS prompt.
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                var runtime = Services.ServiceHelper.GetService<Services.TourRuntimeService>();
                await runtime.InitializeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppShell] Runtime bootstrap failed: {ex.Message}");
            }
        }

        private static async Task NavigatePendingDeepLinkAsync()
        {
            try
            {
                var poiId = Services.DeepLinkHelper.ConsumePendingPoiId();
                if (string.IsNullOrWhiteSpace(poiId))
                {
                    return;
                }

                await Shell.Current.GoToAsync($"{nameof(Pages.PoiDetailPage)}?poiId={poiId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppShell] Pending deep-link navigation failed: {ex.Message}");
            }
        }

        private async Task EnsureDeviceTrackingConnectedAsync()
        {
            if (_deviceTrackingConnecting)
                return;

            try
            {
                var authService = Services.ServiceHelper.GetService<Services.AuthService>();
                await authService.InitializeAsync();
                // Guest cũng được connect device tracking (không cần check IsAuthenticated)

                var trackingService = Services.ServiceHelper.GetService<Services.DeviceTrackingService>();
                if (trackingService.IsConnected)
                    return;

                _deviceTrackingConnecting = true;

                foreach (var hubUrl in Services.BackendEndpoints.GetDeviceHubUrls())
                {
                    try
                    {
                        await trackingService.ConnectAsync(hubUrl);
                        if (trackingService.IsConnected)
                        {
                            Services.BackendEndpoints.RememberWorkingServerFromUrl(hubUrl);
                            Console.WriteLine($"[AppShell] Connected device tracking at {hubUrl}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AppShell] Device tracking connect failed at {hubUrl}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppShell] EnsureDeviceTrackingConnectedAsync failed: {ex.Message}");
            }
            finally
            {
                _deviceTrackingConnecting = false;
            }
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
            SettingsTab.Title = loc["SettingsTitle"];
        }
    }
}
