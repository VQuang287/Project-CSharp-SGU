using TourMap.Services;
using Services = TourMap.Services;

namespace TourMap
{
    public partial class App : Application
    {
        private readonly Pages.SplashPage _splashPage;
        private readonly Services.AutoSyncService _autoSyncService;
        private readonly Services.DeviceTrackingService _deviceTracking;
        private readonly Services.AuthService _authService;
        private readonly Services.DatabaseService _dbService;

        public App(Pages.SplashPage splashPage, Services.AutoSyncService autoSyncService, Services.DeviceTrackingService deviceTracking, Services.AuthService authService, Services.DatabaseService dbService)
        {
            _splashPage = splashPage;
            _autoSyncService = autoSyncService;
            _deviceTracking = deviceTracking;
            _authService = authService;
            _dbService = dbService;
            InitializeComponent();
        }
        // Note: Auth removed - all users are treated as anonymous guests

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_splashPage);

            window.Created += async (_, _) =>
            {
                try
                {
                    // Initialize auth - get anonymous token for SignalR
                    await _authService.InitializeAsync();
                    if (!_authService.IsAuthenticated)
                    {
                        Console.WriteLine("[App] Getting initial anonymous auth token...");
                        var authResult = await _authService.LoginAnonymousAsync();
                        if (authResult.Success)
                        {
                            Console.WriteLine("[App] Anonymous auth token obtained successfully");
                        }
                        else
                        {
                            Console.WriteLine($"[App] Failed to get anonymous token: {authResult.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Auth initialization failed: {ex.Message}");
                }

                try
                {
                    await EnsureDeviceTrackingOnlineAsync("app-created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Initial device tracking failed: {ex.Message}");
                }

                try
                {
                    // Kiểm tra xem có phải là lần đầu chạy app hoặc không có POI nào
                    var hasExistingPois = await _dbService.HasAnyPoiAsync();
                    var isFirstRun = !Preferences.Default.ContainsKey("has_completed_first_sync");
                    var forceFullSync = isFirstRun || !hasExistingPois;
                    
                    if (forceFullSync)
                    {
                        Console.WriteLine("[App] First run or no POIs - forcing full sync from database...");
                        // Xóa last_sync_time để bắt buộc lấy tất cả POI
                        Preferences.Default.Remove("last_sync_time");
                    }
                    
                    // Auth removed - sync works in anonymous mode
                    var syncSuccess = await _autoSyncService.EnsureSyncedAsync("app-created", force: true, forceFullSync: forceFullSync);
                    
                    if (syncSuccess)
                    {
                        Preferences.Default.Set("has_completed_first_sync", true);
                        Console.WriteLine("[App] ✅ First sync completed successfully");
                    }
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

                try
                {
                    await EnsureDeviceTrackingOnlineAsync("app-resumed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Error in resume handler: {ex.Message}");
                }
            };

            // Khi app bị tắt hoàn toàn - gửi trạng thái Offline và ngắt kết nối
            window.Destroying += async (_, _) =>
            {
                try
                {
                    await EnsureDeviceTrackingOfflineAsync("app-destroying");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Error during app shutdown: {ex.Message}");
                }
            };

            return window;
        }

        private async Task EnsureDeviceTrackingOnlineAsync(string reason)
        {
            Console.WriteLine($"[App] Ensure device online ({reason})");

            if (!_authService.IsAuthenticated)
            {
                Console.WriteLine("[App] Getting anonymous auth token...");
                var authResult = await _authService.LoginAnonymousAsync();
                if (!authResult.Success)
                {
                    Console.WriteLine($"[App] Failed to get anonymous token: {authResult.ErrorMessage}");
                }
            }

            if (!_deviceTracking.IsConnected)
            {
                foreach (var hubUrl in Services.BackendEndpoints.GetDeviceHubUrls())
                {
                    try
                    {
                        Console.WriteLine($"[App] Trying to connect to {hubUrl}...");
                        await _deviceTracking.ConnectAsync(hubUrl);
                        if (_deviceTracking.IsConnected)
                        {
                            Services.BackendEndpoints.RememberWorkingServerFromUrl(hubUrl);
                            Console.WriteLine($"[App] Connected to {hubUrl}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[App] Failed at {hubUrl}: {ex.Message}");
                    }
                }
            }

            if (_deviceTracking.IsConnected)
            {
                await _deviceTracking.UpdateStateAsync(DeviceState.Online);
                Console.WriteLine("[App] Online state sent successfully");
            }
            else
            {
                Console.WriteLine("[App] WARNING: Could not connect to any tracking hub");
            }
        }

        private async Task EnsureDeviceTrackingOfflineAsync(string reason)
        {
            Console.WriteLine($"[App] Ensure device offline ({reason})");

            if (!_deviceTracking.IsConnected)
                return;

            try
            {
                await _deviceTracking.UpdateStateAsync(DeviceState.Offline);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Failed to send offline state: {ex.Message}");
            }

            try
            {
                await _deviceTracking.DisconnectAsync();
                Console.WriteLine("[App] Device marked as offline and disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Failed to disconnect: {ex.Message}");
            }
        }
    }
}
