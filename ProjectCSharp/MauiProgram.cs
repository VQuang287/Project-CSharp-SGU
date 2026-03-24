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

            // Register app services and viewmodels for frontend
            builder.Services.AddSingleton<Services.DatabaseService>();
            builder.Services.AddTransient<ViewModels.MainViewModel>();
            builder.Services.AddTransient<Pages.MapPage>();
            builder.Services.AddTransient<Pages.PoiListPage>();
            builder.Services.AddSingleton<MainPage>();
            // Register location service (platform implementation on Android)
#if ANDROID
            builder.Services.AddSingleton<TourMap.Services.ILocationService, TourMap.LocationService_Android>();
#endif

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
