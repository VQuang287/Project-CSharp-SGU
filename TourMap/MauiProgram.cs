using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // === Data & ViewModel ===
            builder.Services.AddSingleton<Services.DatabaseService>();
            builder.Services.AddTransient<ViewModels.MainViewModel>();

            // === Pages ===
            builder.Services.AddTransient<Pages.MapPage>();
            builder.Services.AddTransient<Pages.PoiListPage>();
            builder.Services.AddSingleton<MainPage>();

            // === Phase 1: Core Engines ===
            builder.Services.AddSingleton<Services.GeofenceEngine>();
            builder.Services.AddSingleton<Services.NarrationEngine>();

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
