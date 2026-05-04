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

            // Tab pages (MapPage, PoiListPage, SettingsPage) are self-registered via TabBar in XAML
            // Only register pages that are navigated to via GoToAsync push-nav
            Routing.RegisterRoute(nameof(Pages.PoiDetailPage), typeof(Pages.PoiDetailPage));
            // Auth pages removed - app works in anonymous mode only
            
            // Tour pages
            Routing.RegisterRoute(nameof(Pages.Tours.TourListPage), typeof(Pages.Tours.TourListPage));
            Routing.RegisterRoute(nameof(Pages.Tours.TourDetailPage), typeof(Pages.Tours.TourDetailPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Auth removed - connect device tracking directly (fire and forget)
                _ = ConnectDeviceTrackingAsync();
                
                // Handle deep link
                _ = NavigatePendingDeepLinkAsync();

                if (_runtimeBootstrapped)
                    return;
                _runtimeBootstrapped = true;

                // Proactively request location permission & start runtime.
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
                Console.WriteLine($"[AppShell] Runtime bootstrap failed: {ex}");
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

        private async Task ConnectDeviceTrackingAsync()
        {
            if (_deviceTrackingConnecting)
                return;

            try
            {
                var trackingService = Services.ServiceHelper.GetService<Services.DeviceTrackingService>();
                if (trackingService == null)
                {
                    Console.WriteLine("[AppShell] DeviceTrackingService is null");
                    return;
                }

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
                Console.WriteLine($"[AppShell] ConnectDeviceTrackingAsync failed: {ex}");
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
            SettingsTab.Title = loc["SettingsTitle"];
        }
    }
}
