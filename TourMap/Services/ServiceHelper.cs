namespace TourMap.Services;

public static class ServiceHelper
{
    public static TService GetService<TService>()
    {
        var provider = Current;
        if (provider == null)
        {
            throw new InvalidOperationException($"Service provider not available. Make sure DI is properly initialized for the current platform.");
        }
        
        var service = provider.GetService<TService>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service {typeof(TService).Name} not found in DI container. Check service registration in MauiProgram.cs");
        }
        
        return service;
    }

    public static IServiceProvider? Current =>
#if WINDOWS10_0_17763_0_OR_GREATER
        MauiWinUIApplication.Current?.Services;
#elif ANDROID
        MauiApplication.Current?.Services;
#elif IOS || MACCATALYST
        MauiUIApplicationDelegate.Current?.Services;
#else
        null;
#endif
}
