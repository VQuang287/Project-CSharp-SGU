using TourMap.Services;
using Services = TourMap.Services;

namespace TourMap
{
    public partial class App : Application
    {
        private readonly Pages.SplashPage _splashPage;
        private readonly Services.AutoSyncService _autoSyncService;
        private readonly Services.DeviceTrackingService _deviceTracking;

        public App(Pages.SplashPage splashPage, Services.AutoSyncService autoSyncService, Services.DeviceTrackingService deviceTracking)
        {
            _splashPage = splashPage;
            _autoSyncService = autoSyncService;
            _deviceTracking = deviceTracking;
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

                // Khi app vào lại từ background, LUÔN reconnect và gửi Online
                try
                {
                    Console.WriteLine("[App] App resumed - ensuring device tracking connected...");
                    
                    // Luôn disconnect trước để reset state
                    if (_deviceTracking.IsConnected)
                    {
                        Console.WriteLine("[App] Disconnecting existing connection...");
                        await _deviceTracking.DisconnectAsync();
                        await Task.Delay(500); // Đợi 500ms để đảm bảo disconnect xong
                    }
                    
                    // Thử kết nối lại
                    bool connected = false;
                    foreach (var hubUrl in Services.BackendEndpoints.GetDeviceHubUrls())
                    {
                        try
                        {
                            Console.WriteLine($"[App] Trying to connect to {hubUrl}...");
                            await _deviceTracking.ConnectAsync(hubUrl);
                            if (_deviceTracking.IsConnected)
                            {
                                Console.WriteLine($"[App] Connected to {hubUrl}, sending Online state...");
                                await _deviceTracking.UpdateStateAsync(DeviceState.Online);
                                Console.WriteLine("[App] Online state sent successfully!");
                                connected = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[App] Failed at {hubUrl}: {ex.Message}");
                        }
                    }
                    
                    if (!connected)
                    {
                        Console.WriteLine("[App] WARNING: Could not connect to any tracking hub!");
                    }
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
                    Console.WriteLine("[App] Window destroying - sending offline state...");
                    // Gửi trạng thái Offline trước khi disconnect
                    await _deviceTracking.UpdateStateAsync(DeviceState.Offline);
                    await _deviceTracking.DisconnectAsync();
                    Console.WriteLine("[App] Device marked as offline and disconnected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] Error during app shutdown: {ex.Message}");
                }
            };

            return window;
        }
    }
}
