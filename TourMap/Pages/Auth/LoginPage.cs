using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// Login Page - DEPRECATED: Auth removed from app.
/// This page redirects to main app immediately.
/// </summary>
public class LoginPage : ContentPage
{
    public LoginPage()
    {
        // Auth removed - redirect to main app
        Shell.SetNavBarIsVisible(this, false);
        Content = new VerticalStackLayout 
        { 
            VerticalOptions = LayoutOptions.Center,
            Children = { new Label { Text = "Loading...", HorizontalOptions = LayoutOptions.Center } }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Redirect to main app immediately
        await Task.Delay(100);
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            Preferences.Default.Set("onboarding_completed", true);
            window.Page = ServiceHelper.GetService<AppShell>();
        }
    }
}
