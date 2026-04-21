using TourMap.Services;

namespace TourMap.Pages;

public class SplashPage : ContentPage
{
    private readonly AuthService? _authService;
    private readonly DeviceTrackingService? _deviceTrackingService;

    public SplashPage() : this(TryResolveAuthService(), TryResolveDeviceTrackingService())
    {
    }

    public SplashPage(AuthService? authService, DeviceTrackingService? deviceTrackingService)
    {
        _authService = authService;
        _deviceTrackingService = deviceTrackingService;
        InitializeUI();
    }

    private void InitializeUI()
    {
        BackgroundColor = Color.FromArgb("#1A237E");

        var logo = new Label
        {
            Text = "🎧",
            FontSize = 80,
            HorizontalOptions = LayoutOptions.Center,
        };

        var title = new Label
        {
            Text = "Audio Guide Tour",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitle = new Label
        {
            Text = "Phố Ẩm thực Vĩnh Khánh, Quận 4",
            FontSize = 14,
            TextColor = Color.FromArgb("#B0BEC5"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 5, 0, 40)
        };

        var langPrompt = new Label
        {
            Text = "Chọn ngôn ngữ / Choose language",
            FontSize = 16,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            ColumnSpacing = 8,
            RowSpacing = 8,
            Margin = new Thickness(20, 0)
        };

        foreach (var (index, lang) in LocalizationService.SupportedLanguages.Select((l, i) => (i, l)))
        {
            var btn = new Button
            {
                Text = $"{lang.Flag} {lang.DisplayName}",
                BackgroundColor = Color.FromArgb("#1565C0"),
                TextColor = Colors.White,
                CornerRadius = 10,
                FontSize = 12,
                HeightRequest = 48
            };
            var capturedCode = lang.Code;
            btn.Clicked += async (s, e) => await SelectLanguageAndProceed(capturedCode);
            grid.Add(btn, index % 3, index / 3);
        }

        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 5,
            Children = { logo, title, subtitle, langPrompt, grid }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var savedLang = Preferences.Default.Get("selected_language", string.Empty);
            var hasPendingPoiDeepLink = !string.IsNullOrEmpty(DeepLinkHelper.PeekPendingPoiId());

            if (string.IsNullOrEmpty(savedLang))
            {
                if (!hasPendingPoiDeepLink)
                {
                    return;
                }

                var fallbackLanguage = LocalizationService.SupportedLanguages
                    .Select(x => x.Code)
                    .FirstOrDefault() ?? "vi";
                Preferences.Default.Set("selected_language", fallbackLanguage);
                LocalizationService.Current.CurrentLanguage = fallbackLanguage;
                await Task.Delay(300);
            }
            else
            {
                LocalizationService.Current.CurrentLanguage = savedLang;
                await Task.Delay(600);
            }

            // === AUTH CHECK ===
            if (_authService != null)
            {
                await _authService.InitializeAsync();
                if (_authService.IsAuthenticated)
                {
                    NavigateToShell();
                    return;
                }

                if (hasPendingPoiDeepLink)
                {
                    var guestSignedIn = await _authService.LoginAnonymousAsync();
                    if (guestSignedIn)
                    {
                        NavigateToShell();
                        return;
                    }
                }

                NavigateToLogin();
                return;
            }

            // Fallback: no auth service → go to shell directly
            NavigateToShell();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SplashPage] Error in OnAppearing: {ex.Message}");
            NavigateToLogin();
        }
    }

    private async Task SelectLanguageAndProceed(string lang)
    {
        Preferences.Default.Set("selected_language", lang);
        LocalizationService.Current.CurrentLanguage = lang;
        await Task.Delay(200);
        NavigateToLogin();
    }

    private async void NavigateToShell()
    {
        // Connect to device tracking hub
        if (_deviceTrackingService != null)
        {
            try
            {
                var hubUrls = BackendEndpoints.GetDeviceHubUrls().ToList();
                foreach (var hubUrl in hubUrls)
                {
                    try
                    {
                        await _deviceTrackingService.ConnectAsync(hubUrl);
                        BackendEndpoints.RememberWorkingServerFromUrl(hubUrl);
                        Console.WriteLine($"[SplashPage] Connected to device tracking hub at {hubUrl}");
                        break;
                    }
                    catch (Exception) when (hubUrl != hubUrls.Last())
                    {
                        Console.WriteLine($"[SplashPage] Failed to connect to {hubUrl}, trying next...");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SplashPage] Failed to connect device tracking: {ex.Message}");
                // Continue anyway - app should work without tracking
            }
        }
        
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            // Always route authenticated users to shell UI.
            // Mark onboarding completed to avoid returning to legacy main menu UI.
            Preferences.Default.Set("onboarding_completed", true);
            window.Page = ServiceHelper.GetService<AppShell>();
        }

        // If app was launched via deeplink (stored by platform MainActivity), navigate to POI detail
        try
        {
            var finalPoiId = DeepLinkHelper.ConsumePendingPoiId();
            if (!string.IsNullOrEmpty(finalPoiId))
            {
                // Chờ Routing thiết lập xong UI rồi mới Navigate
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // Chờ AppShell khởi tạo Frame
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync($"{nameof(Pages.PoiDetailPage)}?poiId={finalPoiId}");
                        }
                        catch (Exception navEx)
                        {
                            Console.WriteLine($"[SplashPage] Deep link navigation failed: {navEx.Message}");
                        }
                    });
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SplashPage] Lỗi Parse Deeplink QR: {ex.Message}");
        }
    }

    private void NavigateToLogin()
    {
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            var authService = _authService ?? TryResolveAuthService();
            if (authService != null)
            {
                window.Page = new LoginPage(authService);
                return;
            }

            window.Page = ServiceHelper.GetService<AppShell>();
        }
    }

    private static AuthService? TryResolveAuthService()
    {
        try
        {
            return ServiceHelper.GetService<AuthService>();
        }
        catch
        {
            return null;
        }
    }
    
    private static DeviceTrackingService? TryResolveDeviceTrackingService()
    {
        try
        {
            return ServiceHelper.GetService<DeviceTrackingService>();
        }
        catch
        {
            return null;
        }
    }
}
