using Microsoft.Extensions.Logging;
using ProjectCSharp.Services;
using ProjectCSharp.ViewModels;
using ProjectCSharp.Views;

namespace ProjectCSharp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 1. Đăng ký Services
            builder.Services.AddSingleton<ILocationService, LocationService>();

            // 2. Đăng ký ViewModels
            builder.Services.AddTransient<TourViewModel>();

            // 3. Đăng ký Views
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
