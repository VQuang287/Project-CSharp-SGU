using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BarcodeScanning;

namespace TourMap
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Ensure SQLite provider is initialized before any DB access.
            SQLitePCL.Batteries_V2.Init();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseBarcodeScanning()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                    fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("Inter-Bold.ttf", "InterBold");
                });

            // === Data & ViewModel ===
            builder.Services.AddSingleton<Services.DatabaseService>();
            builder.Services.AddTransient<ViewModels.MainViewModel>();

            // === Pages ===
            builder.Services.AddTransient<Pages.MapPage>();
            builder.Services.AddTransient<Pages.PoiListPage>();
            builder.Services.AddTransient<Pages.PoiDetailPage>();
            builder.Services.AddTransient<Pages.QrScannerPage>();
            builder.Services.AddTransient<Pages.SettingsPage>();
            builder.Services.AddTransient<Pages.OfflinePacksPage>();
            builder.Services.AddTransient<Pages.SplashPage>();
            // Auth pages removed - app works in anonymous mode only
            builder.Services.AddTransient<AppShell>();
            // SYS-H02 fix: MainPage registered for compatibility
            builder.Services.AddTransient<MainPage>();
            
            // === Tour Pages ===
            builder.Services.AddTransient<Pages.Tours.TourListPage>();
            builder.Services.AddTransient<Pages.Tours.TourDetailPage>();

            // === Phase 1: Core Engines ===
            builder.Services.AddSingleton<Services.GeofenceEngine>();
            builder.Services.AddSingleton<Services.NarrationEngine>();
            builder.Services.AddSingleton<Services.TourRuntimeService>();

            // === Phase 2 & 3: Audio Player, Sync ===
            builder.Services.AddSingleton<Services.IAudioPlayerService, Services.AudioPlayerService>();
            
            // BUG-W01 fix: Use HttpClient from DI instead of manual new HttpClient()
            builder.Services.AddHttpClient();
            // Auth service needed for SignalR authentication
            builder.Services.AddSingleton<Services.AuthService>();
            builder.Services.AddSingleton<Services.SyncService>();
            builder.Services.AddSingleton<Services.AutoSyncService>();
            builder.Services.AddSingleton<Services.DeviceTrackingService>();
            
            // === Phase 3.3: Logging Framework ===
            builder.Services.AddSingleton<Services.ILoggerService, Services.LoggerService>();

            // === Phase 3.4: Auto Download ===
            builder.Services.AddSingleton<Services.AutoDownloadService>();

            // === Platform-specific Services (Android) ===
#if ANDROID
            builder.Services.AddSingleton<Services.ILocationService, LocationService_Android>();
            builder.Services.AddSingleton<Services.IGpsTrackingService, GpsTrackingService_Android>();
            builder.Services.AddSingleton<Services.ITtsService, TtsService_Android>();
#endif

            // === Platform-specific Services (iOS) ===
#if IOS
            builder.Services.AddSingleton<Services.ILocationService, LocationService_iOS>();
            builder.Services.AddSingleton<Services.IGpsTrackingService, GpsTrackingService_iOS>();
            builder.Services.AddSingleton<Services.ITtsService, TtsService_iOS>();
#endif

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
