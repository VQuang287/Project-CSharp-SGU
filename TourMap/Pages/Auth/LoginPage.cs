using TourMap.Services;

namespace TourMap.Pages;

public class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly Entry _emailEntry;
    private readonly Entry _passwordEntry;
    private readonly Label _errorLabel;
    private readonly Button _loginBtn;
    private readonly ActivityIndicator _spinner;

    public LoginPage(AuthService authService)
    {
        _authService = authService;
        BackgroundColor = Color.FromArgb("#F8FAFC");
        // Guard: Shell.SetNavBarIsVisible may throw outside Shell hierarchy
        try { Shell.SetNavBarIsVisible(this, false); } catch { }

        // ═══════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════
        var logo = new Label
        {
            Text = "🎧",
            FontSize = 60,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var title = new Label
        {
            Text = "Đăng nhập",
            FontSize = 28,
            FontFamily = "InterBold",
            TextColor = Color.FromArgb("#18181B"),
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitle = new Label
        {
            Text = "Audio Tour Guide - Phố Ẩm thực",
            FontSize = 14,
            TextColor = Color.FromArgb("#6B7280"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };

        // ═══════════════════════════════════════════
        // FORM
        // ═══════════════════════════════════════════
        _emailEntry = new Entry
        {
            Placeholder = "Email",
            Keyboard = Keyboard.Email,
            FontFamily = "InterRegular",
            FontSize = 16,
            TextColor = Color.FromArgb("#18181B"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _passwordEntry = new Entry
        {
            Placeholder = "Mật khẩu",
            IsPassword = true,
            FontFamily = "InterRegular",
            FontSize = 16,
            TextColor = Color.FromArgb("#18181B"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _passwordEntry.Completed += async (s, e) => await LoginAsync();

        _errorLabel = new Label
        {
            TextColor = Color.FromArgb("#DC2626"),
            FontSize = 13,
            FontFamily = "InterRegular",
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _spinner = new ActivityIndicator
        {
            Color = Color.FromArgb("#1565C0"),
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };

        _loginBtn = new Button
        {
            Text = "Đăng nhập",
            FontFamily = "InterSemiBold",
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#1565C0"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 50,
            Margin = new Thickness(0, 0, 0, 12)
        };
        _loginBtn.Clicked += async (s, e) => await LoginAsync();

        var forgotPasswordLink = new Label
        {
            Text = "Quên mật khẩu?",
            TextColor = Color.FromArgb("#1565C0"),
            FontSize = 13,
            FontFamily = "InterSemiBold",
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 16)
        };
        var forgotTap = new TapGestureRecognizer();
        forgotTap.Tapped += async (s, e) => await ForgotPasswordAsync();
        forgotPasswordLink.GestureRecognizers.Add(forgotTap);

        var registerLink = new Label
        {
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Chưa có tài khoản? ", TextColor = Color.FromArgb("#6B7280"), FontSize = 14 },
                    new Span { Text = "Đăng ký", TextColor = Color.FromArgb("#1565C0"), FontSize = 14, FontFamily = "InterSemiBold",
                               TextDecorations = TextDecorations.Underline }
                }
            },
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        var registerTap = new TapGestureRecognizer();
        registerTap.Tapped += (s, e) =>
        {
            try
            {
                if (Application.Current?.Windows.FirstOrDefault() is Window window)
                    window.Page = new RegisterPage(_authService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoginPage] Error navigating to Register: {ex.Message}");
            }
        };
        registerLink.GestureRecognizers.Add(registerTap);

        // ═══════════════════════════════════════════
        // DIVIDER
        // ═══════════════════════════════════════════
        var dividerStack = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20),
            Children =
            {
                new BoxView { Color = Color.FromArgb("#E5E7EB"), HeightRequest = 1, WidthRequest = 80, VerticalOptions = LayoutOptions.Center },
                new Label { Text = "  hoặc  ", TextColor = Color.FromArgb("#9CA3AF"), FontSize = 13 },
                new BoxView { Color = Color.FromArgb("#E5E7EB"), HeightRequest = 1, WidthRequest = 80, VerticalOptions = LayoutOptions.Center }
            }
        };

        // ═══════════════════════════════════════════
        // GUEST MODE
        // ═══════════════════════════════════════════
        var guestBtn = new Button
        {
            Text = "👤 Tiếp tục như khách",
            FontFamily = "InterRegular",
            FontSize = 14,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6B7280"),
            BorderColor = Color.FromArgb("#D1D5DB"),
            BorderWidth = 1,
            CornerRadius = 12,
            HeightRequest = 48
        };
        guestBtn.Clicked += async (s, e) => await LoginAsGuestAsync();

        // ═══════════════════════════════════════════
        // LAYOUT
        // ═══════════════════════════════════════════
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(30),
                VerticalOptions = LayoutOptions.Center,
                Spacing = 0,
                Children = { logo, title, subtitle, _emailEntry, _passwordEntry, forgotPasswordLink, _errorLabel, _spinner, _loginBtn, registerLink, dividerStack, guestBtn }
            }
        };
    }

    private async Task LoginAsync()
    {
        var email = _emailEntry.Text?.Trim();
        var password = _passwordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Vui lòng nhập email và mật khẩu.");
            return;
        }

        SetLoading(true);
        ShowError(null);

        var result = await _authService.LoginAsync(email, password);

        SetLoading(false);

        if (result.Success)
        {
            await NavigateToMainApp();
        }
        else
        {
            ShowError(result.ErrorMessage ?? "Đăng nhập thất bại.");
        }
    }

    private async Task LoginAsGuestAsync()
    {
        SetLoading(true);
        ShowError(null);

        await _authService.LoginAnonymousAsync();

        SetLoading(false);
        await NavigateToMainApp();
    }

    private Task NavigateToMainApp()
    {
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            // Force modern shell UI after successful auth.
            // Prevent fallback to legacy MainPage menu on fresh installs.
            Preferences.Default.Set("onboarding_completed", true);
            window.Page = ServiceHelper.GetService<AppShell>();
        }
        return Task.CompletedTask;
    }

    private void ShowError(string? message)
    {
        _errorLabel.Text = message ?? "";
        _errorLabel.IsVisible = !string.IsNullOrEmpty(message);
    }

    private void SetLoading(bool loading)
    {
        _spinner.IsRunning = loading;
        _spinner.IsVisible = loading;
        _loginBtn.IsEnabled = !loading;
        _loginBtn.Text = loading ? "Đang đăng nhập..." : "Đăng nhập";
    }

    private async Task ForgotPasswordAsync()
    {
        string? email = await DisplayPromptAsync(
            "Khôi phục mật khẩu",
            "Vui lòng nhập Email của bạn:",
            "Gửi",
            "Hủy",
            null,
            -1,
            Keyboard.Email,
            _emailEntry.Text?.Trim() ?? "");

        if (string.IsNullOrWhiteSpace(email)) return;

        SetLoading(true);
        ShowError(null);

        var result = await _authService.ForgotPasswordAsync(email);

        SetLoading(false);

        if (result.Success)
        {
            await DisplayAlertAsync("Thành công", "Đã gửi Email khôi phục mật khẩu, vui lòng kiểm tra hộp thư.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Thất bại", result.ErrorMessage ?? "Yêu cầu khôi phục mật khẩu thất bại.", "OK");
        }
    }
}
