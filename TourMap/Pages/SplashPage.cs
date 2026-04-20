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
            // Nếu đã chọn ngôn ngữ rồi → kiểm tra auth state
            var savedLang = Preferences.Default.Get("selected_language", string.Empty);
            if (!string.IsNullOrEmpty(savedLang))
            {
                LocalizationService.Current.CurrentLanguage = savedLang;
                await Task.Delay(600);

                // === AUTH CHECK ===
                if (_authService != null)
                {
                    await _authService.InitializeAsync();
                    if (_authService.IsAuthenticated)
                    {
                        // Đã login → vào app chính
                        NavigateToShell();
                        return;
                    }
                    else
                    {
                        // Chưa login → về LoginPage
                        NavigateToLogin();
                        return;
                    }
                }

                // Fallback: no auth service → go to shell directly
                NavigateToShell();
            }
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
            var pending = Preferences.Default.Get("pending_deeplink", string.Empty);
            if (!string.IsNullOrEmpty(pending))
            {
                // Tiêu thụ (xoá) link để các lần sau vào app không bị gán lại
                Preferences.Default.Remove("pending_deeplink");

                string? finalPoiId = null;

                // Xử lý Custom URI Scheme (vd: audiotour://poi/f47ac10b)
                if (pending.StartsWith("audiotour://"))
                {
                    var uri = new Uri(pending);
                    
                    // Host = "poi", AbsolutePath = "/f47ac10b"
                    if (uri.Host.Equals("poi", StringComparison.OrdinalIgnoreCase))
                    {
                        finalPoiId = uri.AbsolutePath.Trim('/');
                    }
                }
                // Xử lý HTTPS Scheme (vd: https://domain.com/Launch/f47ac10b)
                else if (pending.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(pending);
                    var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length > 0)
                    {
                        finalPoiId = segments.Last();
                    }
                }

                // Chờ Routing thiết lập xong UI rồi mới Navigate
                if (!string.IsNullOrEmpty(finalPoiId))
                {
                    // Đẩy quá trình điều hướng sang Thread khác để không block MainThread
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500); // Chờ AppShell khởi tạo Frame
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync($"{nameof(Pages.PoiDetailPage)}?poiId={finalPoiId}");
                        });
                    });
                     // Note: We don't return early here because we want the Shell logic above to finish launching the page first.
                     // The navigation will kick in asynchronously 500ms after.
                }
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
