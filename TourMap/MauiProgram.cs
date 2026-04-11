using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BarcodeScanning;

namespace TourMap
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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
            builder.Services.AddTransient<Pages.SplashPage>(); // BUG-05 fix
            builder.Services.AddTransient<AppShell>(); // BUG-05 fix
            builder.Services.AddSingleton<MainPage>();

            // === Phase 1: Core Engines ===
            builder.Services.AddSingleton<Services.GeofenceEngine>();
            builder.Services.AddSingleton<Services.NarrationEngine>();
            builder.Services.AddSingleton<Services.TourRuntimeService>();

            // === Phase 2 & 3: Audio Player, Sync & Auth ===
            builder.Services.AddSingleton<Services.IAudioPlayerService, Services.AudioPlayerService>();
            builder.Services.AddSingleton<Services.AuthService>();
            builder.Services.AddSingleton<Services.SyncService>();
            
            // === Phase 3.3: Logging Framework ===
            builder.Services.AddSingleton<Services.ILoggerService, Services.LoggerService>();

            // === Platform-specific Services (Android) ===
#if ANDROID
            builder.Services.AddSingleton<Services.ILocationService, LocationService_Android>();
            builder.Services.AddSingleton<Services.IGpsTrackingService, GpsTrackingService_Android>();
            builder.Services.AddSingleton<Services.ITtsService, TtsService_Android>();
#endif

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

