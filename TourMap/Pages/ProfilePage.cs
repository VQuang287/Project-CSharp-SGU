using TourMap.Services;
using Microsoft.Maui.Controls.Shapes;

namespace TourMap.Pages;

public class ProfilePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly LocalizationService _loc;

    private readonly Label _nameLabel;
    private readonly Label _emailLabel;
    private readonly Label _roleLabel;
    private readonly Label _memberSinceLabel;
    private readonly Label _avatarInitials;
    private readonly Border _avatarBorder;

    private readonly Label _statAudioValue;
    private readonly Label _statAudioLabel;
    private readonly Label _statPlacesValue;
    private readonly Label _statPlacesLabel;
    private readonly Label _statBadgesValue;
    private readonly Label _statBadgesLabel;

    private readonly Border _guestInfoCard;
    private readonly Label _guestBenefitsHeader;
    private readonly Label _guestBenefit1;
    private readonly Label _guestBenefit2;
    private readonly Label _guestBenefit3;
    private readonly Button _registerBtn;

    private readonly VerticalStackLayout _userDetailsCard;
    private readonly Label _authEmailLabel;
    private readonly Label _authEmailValue;
    private readonly Label _authRoleLabel;
    private readonly Label _authRoleValue;
    private readonly Label _authMethodLabel;
    private readonly Label _authMethodValue;

    private readonly VerticalStackLayout _favoritesSection;
    private readonly Label _favoritesTitle;

    private readonly Button _changePasswordBtn;
    private readonly Button _logoutBtn;
    private readonly Label _versionLabel;

    public ProfilePage(AuthService authService)
    {
        _authService = authService;
        _loc = LocalizationService.Current;

        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFC");

        var headerBg = new Border
        {
            HeightRequest = 180,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,
            Stroke = Colors.Transparent,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#0D7A5F"), Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#044E3B"), Offset = 1.0f }
                }
            }
        };

        _avatarInitials = new Label
        {
            FontSize = 36,
            FontFamily = "InterBold",
            TextColor = Color.FromArgb("#0D7A5F"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        _avatarBorder = new Border
        {
            WidthRequest = 100,
            HeightRequest = 100,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 50 },
            Stroke = Color.FromArgb("#34D399"),
            StrokeThickness = 4,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.15f, Radius = 10, Offset = new Point(0, 4) },
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 130, 0, 16),
            Content = _avatarInitials
        };

        _nameLabel = new Label { FontSize = 24, FontFamily = "InterBold", TextColor = Color.FromArgb("#18181B"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 4) };
        _emailLabel = new Label { FontSize = 14, TextColor = Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 8) };
        _roleLabel = new Label { FontSize = 12, FontFamily = "InterSemiBold", HorizontalOptions = LayoutOptions.Center, Padding = new Thickness(12, 4), Margin = new Thickness(0, 0, 0, 4) };
        _memberSinceLabel = new Label { FontSize = 12, TextColor = Color.FromArgb("#9CA3AF"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 0, 20) };

        var backBtn = new Button
        {
            Text = "←",
            FontSize = 24,
            FontFamily = "InterBold",
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 50,
            HeightRequest = 50,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(10, 40, 0, 0)
        };
        backBtn.Clicked += async (s, e) => await Shell.Current.GoToAsync("..");

        var topHeaderGrid = new Grid
        {
            Children = { headerBg, _avatarBorder, backBtn }
        };

        _statAudioValue = new Label { Text = "0h", FontSize = 20, FontFamily = "InterBold", TextColor = Color.FromArgb("#0D7A5F"), HorizontalTextAlignment = TextAlignment.Center };
        _statAudioLabel = new Label { FontSize = 11, TextColor = Color.FromArgb("#6B7280"), HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1 };

        _statPlacesValue = new Label { Text = "0", FontSize = 20, FontFamily = "InterBold", TextColor = Color.FromArgb("#0D7A5F"), HorizontalTextAlignment = TextAlignment.Center };
        _statPlacesLabel = new Label { FontSize = 11, TextColor = Color.FromArgb("#6B7280"), HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1 };

        _statBadgesValue = new Label { Text = "-", FontSize = 20, FontFamily = "InterBold", TextColor = Color.FromArgb("#0D7A5F"), HorizontalTextAlignment = TextAlignment.Center };
        _statBadgesLabel = new Label { FontSize = 11, TextColor = Color.FromArgb("#6B7280"), HorizontalTextAlignment = TextAlignment.Center, MaxLines = 1 };

        var statsGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(1), new ColumnDefinition(GridLength.Star), new ColumnDefinition(1), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                new VerticalStackLayout { Spacing = 2, Children = { _statAudioValue, _statAudioLabel } }.WithColumn(0),
                new BoxView { WidthRequest = 1, BackgroundColor = Color.FromArgb("#E5E7EB"), VerticalOptions = LayoutOptions.Fill, Margin = new Thickness(0, 4) }.WithColumn(1),
                new VerticalStackLayout { Spacing = 2, Children = { _statPlacesValue, _statPlacesLabel } }.WithColumn(2),
                new BoxView { WidthRequest = 1, BackgroundColor = Color.FromArgb("#E5E7EB"), VerticalOptions = LayoutOptions.Fill, Margin = new Thickness(0, 4) }.WithColumn(3),
                new VerticalStackLayout { Spacing = 2, Children = { _statBadgesValue, _statBadgesLabel } }.WithColumn(4)
            }
        };

        var statsCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.05f, Radius = 8, Offset = new Point(0, 4) },
            Padding = new Thickness(16),
            Margin = new Thickness(20, 0, 20, 24),
            Content = statsGrid
        };

        _guestBenefitsHeader = new Label { FontFamily = "InterSemiBold", FontSize = 15, TextColor = Color.FromArgb("#18181B"), Margin = new Thickness(0, 0, 0, 8) };
        _guestBenefit1 = new Label { FontSize = 14, TextColor = Color.FromArgb("#4B5563") };
        _guestBenefit2 = new Label { FontSize = 14, TextColor = Color.FromArgb("#4B5563") };
        _guestBenefit3 = new Label { FontSize = 14, TextColor = Color.FromArgb("#4B5563") };
        _registerBtn = new Button { FontFamily = "InterSemiBold", BackgroundColor = Color.FromArgb("#1565C0"), TextColor = Colors.White, CornerRadius = 10, HeightRequest = 44, Margin = new Thickness(0, 16, 0, 0) };
        _registerBtn.Clicked += OnAuthActionClicked;

        _guestInfoCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Color.FromArgb("#E5E7EB"),
            StrokeThickness = 1,
            Padding = new Thickness(20),
            Margin = new Thickness(20, 0),
            Content = new VerticalStackLayout { Spacing = 4, Children = { _guestBenefitsHeader, _guestBenefit1, _guestBenefit2, _guestBenefit3, _registerBtn } }
        };

        _authEmailLabel = new Label { FontSize = 14, TextColor = Color.FromArgb("#6B7280"), WidthRequest = 100 };
        _authEmailValue = new Label { FontSize = 14, FontFamily = "InterSemiBold", TextColor = Color.FromArgb("#18181B") };
        _authRoleLabel = new Label { FontSize = 14, TextColor = Color.FromArgb("#6B7280"), WidthRequest = 100 };
        _authRoleValue = new Label { FontSize = 14, FontFamily = "InterSemiBold", TextColor = Color.FromArgb("#18181B") };
        _authMethodLabel = new Label { FontSize = 14, TextColor = Color.FromArgb("#6B7280"), WidthRequest = 100 };
        _authMethodValue = new Label { FontSize = 14, FontFamily = "InterSemiBold", TextColor = Color.FromArgb("#18181B") };

        _userDetailsCard = new VerticalStackLayout
        {
            Spacing = 16,
            Margin = new Thickness(20, 0),
            Children =
            {
                new Border { BackgroundColor = Colors.White, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 20, Content = new VerticalStackLayout { Spacing = 12, Children = {
                    new HorizontalStackLayout { Spacing = 8, Children = { _authEmailLabel, _authEmailValue } },
                    new HorizontalStackLayout { Spacing = 8, Children = { _authRoleLabel, _authRoleValue } },
                    new HorizontalStackLayout { Spacing = 8, Children = { _authMethodLabel, _authMethodValue } }
                }}}
            }
        };

        _favoritesTitle = new Label { FontFamily = "InterBold", FontSize = 16, TextColor = Color.FromArgb("#18181B"), Margin = new Thickness(20, 24, 20, 12) };

        var scroller = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
        var favStack = new HorizontalStackLayout { Spacing = 12, Padding = new Thickness(20, 0) };
        var emptyFav = new Border { BackgroundColor = Color.FromArgb("#E0F5F0"), Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = new Thickness(16), Content = new Label { Text = "Chưa có địa điểm yêu thích", TextColor = Color.FromArgb("#0D7A5F"), FontSize = 12 } };
        favStack.Children.Add(emptyFav);
        scroller.Content = favStack;

        _favoritesSection = new VerticalStackLayout { Children = { _favoritesTitle, scroller } };

        _changePasswordBtn = new Button
        {
            FontFamily = "InterSemiBold",
            FontSize = 15,
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#1F2937"),
            CornerRadius = 12,
            HeightRequest = 48,
            Margin = new Thickness(20, 24, 20, 0)
        };
        _changePasswordBtn.Clicked += OnChangePasswordClicked;

        _logoutBtn = new Button
        {
            FontFamily = "InterSemiBold",
            FontSize = 15,
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            TextColor = Color.FromArgb("#DC2626"),
            CornerRadius = 12,
            HeightRequest = 48,
            Margin = new Thickness(20, 12, 20, 0)
        };
        _logoutBtn.Clicked += async (s, e) => await LogoutAsync();

        _versionLabel = new Label { FontSize = 11, TextColor = Color.FromArgb("#D1D5DB"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 24, 0, 40) };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { topHeaderGrid, _nameLabel, _emailLabel, _roleLabel, _memberSinceLabel, statsCard, _guestInfoCard, _userDetailsCard, _favoritesSection, _changePasswordBtn, _logoutBtn, _versionLabel }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_authService.IsGuest)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..");
                if (Application.Current?.Windows.FirstOrDefault() is Window window)
                    window.Page = new LoginPage(_authService);
            });
            return;
        }

        _loc.LanguageChanged += OnLanguageChanged;
        RefreshProfile();
        OnLanguageChanged();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Title = _loc["ProfileTitle"] ?? "Hồ sơ";
            _statAudioLabel.Text = _loc["GamificationAudio"] ?? "Thời gian nghe";
            _statPlacesLabel.Text = _loc["GamificationPlaces"] ?? "Địa điểm đã đến";
            _statBadgesLabel.Text = _loc["GamificationBadges"] ?? "Danh hiệu";
            _favoritesTitle.Text = _loc["MyFavorites"] ?? "Yêu thích của tôi";
            if (_favoritesSection.IsVisible && ((HorizontalStackLayout)((ScrollView)_favoritesSection.Children[1]).Content).Children[0] is Border b && b.Content is Label l)
                l.Text = _loc["NoFavorites"] ?? "Chưa có địa điểm";
            _guestBenefitsHeader.Text = _loc["GuestBenefitsHeader"] ?? "💡 Đăng ký tài khoản để:";
            _guestBenefit1.Text = _loc["GuestBenefit1"] ?? "• Lưu lịch sử tour";
            _guestBenefit2.Text = _loc["GuestBenefit2"] ?? "• Đồng bộ dữ liệu";
            _guestBenefit3.Text = _loc["GuestBenefit3"] ?? "• Trải nghiệm đầy đủ";
            _registerBtn.Text = _loc["RegisterNowBtn"] ?? "Đăng ký ngay";
            _authEmailLabel.Text = _loc["EmailLabel"] ?? "📧 Email";
            _authRoleLabel.Text = _loc["RoleLabel"] ?? "🏷️ Vai trò";
            _authMethodLabel.Text = _loc["AuthMethodLabel"] ?? "🔐 Xác thực";
            _changePasswordBtn.Text = _loc["ChangePasswordBtn"] ?? "🔐 Đổi mật khẩu";
            _logoutBtn.Text = _loc["LogoutBtn"] ?? "🚪 Đăng xuất";
            _versionLabel.Text = string.Format(_loc["AppVersionFormat"] ?? "TourMap {0} • Audio Tour Guide", "1.0");
            RefreshProfile();
        });
    }

    private void RefreshProfile()
    {
        var user = _authService.CurrentUser;
        var isGuest = _authService.IsGuest;

        _guestInfoCard.IsVisible = isGuest;
        _userDetailsCard.IsVisible = !isGuest;
        _favoritesSection.IsVisible = !isGuest;

        string name = user?.DisplayName ?? (_loc["GuestRole"] ?? "👤 Khách");
        _nameLabel.Text = name;
        _emailLabel.Text = user?.Email ?? (_loc["EmailUnregistered"] ?? "Chưa đăng ký email");

        _avatarInitials.Text = !isGuest && name.Length > 0 ? name.Substring(0, Math.Min(2, name.Length)).ToUpper() : "👤";

        if (user != null)
        {
            var dateStr = user.CreatedAt.ToString("dd/MM/yyyy");
            var formatStr = _loc["MemberSinceFormat"] ?? "Thành viên từ {0}";
            _memberSinceLabel.Text = string.Format(formatStr, dateStr);
        }
        else
        {
            _memberSinceLabel.Text = string.Empty;
        }

        var role = user?.Role ?? "Guest";
        _roleLabel.Text = role switch
        {
            "Guest" => _loc["GuestRole"] ?? "👤 Khách",
            "User" => _loc["MemberRole"] ?? "✅ Thành viên",
            "Premium" => _loc["PremiumRole"] ?? "⭐ Premium",
            _ => role
        };
        _roleLabel.BackgroundColor = role switch
        {
            "Guest" => Color.FromArgb("#F3F4F6"),
            "User" => Color.FromArgb("#DBEAFE"),
            "Premium" => Color.FromArgb("#FEF3C7"),
            _ => Color.FromArgb("#F3F4F6")
        };
        _roleLabel.TextColor = role switch
        {
            "Guest" => Color.FromArgb("#6B7280"),
            "User" => Color.FromArgb("#1D4ED8"),
            "Premium" => Color.FromArgb("#B45309"),
            _ => Color.FromArgb("#6B7280")
        };

        if (!isGuest)
        {
            _authEmailValue.Text = user?.Email ?? "-";
            _authRoleValue.Text = user?.Role ?? "-";
            _authMethodValue.Text = user?.AuthProvider ?? "local";
            _changePasswordBtn.IsVisible = user?.AuthProvider == "local";

            var prefsPlaces = Preferences.Default.Get("UserPlacesVisited", 0);
            _statPlacesValue.Text = prefsPlaces.ToString();
        }
        else
        {
            _changePasswordBtn.IsVisible = false;
            _statAudioValue.Text = "0h";
            _statPlacesValue.Text = "0";
            _statBadgesValue.Text = "-";
        }
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        string? oldPassword = await DisplayPromptAsync(
            _loc["ChangePasswordTitle"] ?? "Đổi mật khẩu",
            _loc["EnterOldPassword"] ?? "Nhập mật khẩu hiện tại:",
            _loc["ConfirmBtn"] ?? "Xác nhận",
            _loc["CancelBtn"] ?? "Hủy");

        if (string.IsNullOrEmpty(oldPassword)) return;

        string? newPassword = await DisplayPromptAsync(
            _loc["ChangePasswordTitle"] ?? "Đổi mật khẩu",
            _loc["EnterNewPassword"] ?? "Nhập mật khẩu mới (Tối thiểu 6 ký tự):",
            _loc["ConfirmBtn"] ?? "Xác nhận",
            _loc["CancelBtn"] ?? "Hủy");

        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
        {
            await DisplayAlertAsync("Lỗi", _loc["PasswordTooShort"] ?? "Mật khẩu phải có ít nhất 6 ký tự.", "OK");
            return;
        }

        var result = await _authService.ChangePasswordAsync(oldPassword, newPassword);
        if (result.Success)
        {
            await DisplayAlertAsync("Thành công", _loc["ChangePasswordSuccess"] ?? "Đã đổi mật khẩu thành công.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Thất bại", result.ErrorMessage ?? "Không thể đổi mật khẩu.", "OK");
        }
    }

    private async void OnAuthActionClicked(object? sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new LoginPage(_authService);
    }

    private async Task LogoutAsync()
    {
        var title = _loc["LogoutConfirmTitle"] ?? "Đăng xuất";
        var msg = _loc["LogoutConfirmMsg"] ?? "Bạn có chắc chắn muốn đăng xuất?";
        var ok = _loc["LogoutBtn"] != null ? _loc["LogoutBtn"].Replace("🚪 ", "") : "Đăng xuất";
        var cancel = _loc["LogoutCancelBtn"] ?? "Hủy";

        var confirm = await DisplayAlertAsync(title, msg, ok, cancel);
        if (!confirm) return;

        await _authService.LogoutAsync();

        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new LoginPage(_authService);
    }
}