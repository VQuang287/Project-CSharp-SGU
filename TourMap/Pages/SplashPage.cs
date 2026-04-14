using TourMap.Services;

namespace TourMap.Pages;

public class SplashPage : ContentPage
{
    private readonly AuthService? _authService;

    public SplashPage() : this(TryResolveAuthService())
    {
    }

    public SplashPage(AuthService? authService)
    {
        _authService = authService;
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

    private void NavigateToShell()
    {
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = ServiceHelper.GetService<AppShell>();
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
}
