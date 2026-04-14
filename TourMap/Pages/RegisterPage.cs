using TourMap.Services;

namespace TourMap.Pages;

public class RegisterPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly Entry _nameEntry;
    private readonly Entry _emailEntry;
    private readonly Entry _passwordEntry;
    private readonly Entry _confirmPasswordEntry;
    private readonly Label _errorLabel;
    private readonly Button _registerBtn;
    private readonly ActivityIndicator _spinner;

    public RegisterPage(AuthService authService)
    {
        _authService = authService;
        BackgroundColor = Color.FromArgb("#F8FAFC");
        try { Shell.SetNavBarIsVisible(this, false); } catch { }

        var logo = new Label
        {
            Text = "🎧",
            FontSize = 60,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var title = new Label
        {
            Text = "Tạo tài khoản",
            FontSize = 28,
            FontFamily = "InterBold",
            TextColor = Color.FromArgb("#18181B"),
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitle = new Label
        {
            Text = "Đăng ký để lưu lịch sử tour",
            FontSize = 14,
            TextColor = Color.FromArgb("#6B7280"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 30)
        };

        _nameEntry = new Entry
        {
            Placeholder = "Tên hiển thị",
            FontFamily = "InterRegular",
            FontSize = 16,
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _emailEntry = new Entry
        {
            Placeholder = "Email",
            Keyboard = Keyboard.Email,
            FontFamily = "InterRegular",
            FontSize = 16,
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _passwordEntry = new Entry
        {
            Placeholder = "Mật khẩu (≥ 6 ký tự)",
            IsPassword = true,
            FontFamily = "InterRegular",
            FontSize = 16,
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _confirmPasswordEntry = new Entry
        {
            Placeholder = "Xác nhận mật khẩu",
            IsPassword = true,
            FontFamily = "InterRegular",
            FontSize = 16,
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _confirmPasswordEntry.Completed += async (s, e) => await RegisterAsync();

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
            Color = Color.FromArgb("#2E7D32"),
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };

        _registerBtn = new Button
        {
            Text = "Đăng ký",
            FontFamily = "InterSemiBold",
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 50,
            Margin = new Thickness(0, 0, 0, 16)
        };
        _registerBtn.Clicked += async (s, e) => await RegisterAsync();

        var loginLink = new Label
        {
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Đã có tài khoản? ", TextColor = Color.FromArgb("#6B7280"), FontSize = 14 },
                    new Span { Text = "Đăng nhập", TextColor = Color.FromArgb("#1565C0"), FontSize = 14, FontFamily = "InterSemiBold", TextDecorations = TextDecorations.Underline }
                }
            },
            HorizontalOptions = LayoutOptions.Center
        };
        var loginTap = new TapGestureRecognizer();
        loginTap.Tapped += (s, e) =>
        {
            try
            {
                if (Application.Current?.Windows.FirstOrDefault() is Window window)
                    window.Page = new LoginPage(_authService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterPage] Error navigating to Login: {ex.Message}");
            }
        };
        loginLink.GestureRecognizers.Add(loginTap);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(30),
                VerticalOptions = LayoutOptions.Center,
                Spacing = 0,
                Children = { logo, title, subtitle, _nameEntry, _emailEntry, _passwordEntry, _confirmPasswordEntry, _errorLabel, _spinner, _registerBtn, loginLink }
            }
        };
    }

    private async Task RegisterAsync()
    {
        var name = _nameEntry.Text?.Trim();
        var email = _emailEntry.Text?.Trim();
        var password = _passwordEntry.Text;
        var confirm = _confirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Vui lòng nhập email.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Vui lòng nhập mật khẩu.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
            return;
        }

        if (password != confirm)
        {
            ShowError("Mật khẩu xác nhận không khớp.");
            return;
        }

        SetLoading(true);
        ShowError(null);

        var result = await _authService.RegisterAsync(email, password, name ?? email.Split('@')[0]);

        SetLoading(false);

        if (result.Success)
        {
            if (Application.Current?.Windows.FirstOrDefault() is Window window)
                window.Page = ServiceHelper.GetService<AppShell>();
        }
        else
        {
            ShowError(result.ErrorMessage ?? "Đăng ký thất bại.");
        }
    }

    private void ShowError(string? message)
    {
        _errorLabel.Text = message ?? string.Empty;
        _errorLabel.IsVisible = !string.IsNullOrEmpty(message);
    }

    private void SetLoading(bool loading)
    {
        _spinner.IsRunning = loading;
        _spinner.IsVisible = loading;
        _registerBtn.IsEnabled = !loading;
        _registerBtn.Text = loading ? "Đang xử lý..." : "Đăng ký";
    }
}