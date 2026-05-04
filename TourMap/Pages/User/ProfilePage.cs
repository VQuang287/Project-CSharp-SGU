using TourMap.Services;
using Microsoft.Maui.Controls.Shapes;

namespace TourMap.Pages;

/// <summary>
/// About Page - No user authentication.
/// Shows app info and basic statistics only.
/// </summary>
public class ProfilePage : ContentPage
{
    private readonly LocalizationService _loc;

    // UI Elements - Simplified for anonymous mode
    private readonly Label _appTitleLabel;
    private readonly Label _appSubtitleLabel;
    
    // Gamification Stats UI
    private readonly Label _statAudioValue;
    private readonly Label _statAudioLabel;
    private readonly Label _statPlacesValue;
    private readonly Label _statPlacesLabel;
    private readonly Label _statBadgesValue;
    private readonly Label _statBadgesLabel;

    private readonly Label _versionLabel;

    public ProfilePage()
    {
        _loc = LocalizationService.Current;
        
        // Hide default nav bar
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F8FAFC");

        // ═══════════════════════════════════════════
        // HEADER - App Logo/Icon only
        // ═══════════════════════════════════════════
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

        var appIcon = new Border
        {
            WidthRequest = 100, HeightRequest = 100,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 50 },
            Stroke = Color.FromArgb("#34D399"),
            StrokeThickness = 4,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.15f, Radius = 10, Offset = new Point(0, 4) },
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 130, 0, 16),
            Content = new Label { Text = "🎧", FontSize = 48, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
        };

        _appTitleLabel = new Label { 
            Text = "Audio Guide Tour", 
            FontSize = 24, 
            FontFamily = "InterBold", 
            TextColor = Color.FromArgb("#18181B"), 
            HorizontalOptions = LayoutOptions.Center, 
            Margin = new Thickness(0, 0, 0, 4) 
        };
        _appSubtitleLabel = new Label { 
            Text = "Phố Ẩm thực Vĩnh Khánh", 
            FontSize = 14, 
            TextColor = Color.FromArgb("#6B7280"), 
            HorizontalOptions = LayoutOptions.Center, 
            Margin = new Thickness(0, 0, 0, 20) 
        };

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
            Children = { headerBg, appIcon, backBtn }
        };

        // ═══════════════════════════════════════════
        // GAMIFICATION STATS
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // APP INFO SECTION - Simplified for anonymous mode
        // ═══════════════════════════════════════════
        var appInfoLabel = new Label { 
            Text = "Thông tin ứng dụng", 
            FontFamily = "InterSemiBold", 
            FontSize = 15, 
            TextColor = Color.FromArgb("#18181B"), 
            Margin = new Thickness(0,0,0,8) 
        };
        var appInfoDesc = new Label { 
            Text = "Ứng dụng Audio Tour Guide cho Phố Ẩm thực Vĩnh Khánh, Quận 4.\nCung cấp thông tin địa điểm và audio thuyết minh đa ngôn ngữ.", 
            FontSize = 14, 
            TextColor = Color.FromArgb("#4B5563"),
            LineHeight = 1.4
        };
        
        var appInfoCard = new Border
        {
            BackgroundColor = Colors.White, 
            StrokeShape = new RoundRectangle { CornerRadius = 16 }, 
            Stroke = Color.FromArgb("#E5E7EB"), 
            StrokeThickness = 1,
            Padding = new Thickness(16), 
            Margin = new Thickness(20, 0),
            Content = new VerticalStackLayout { Spacing = 6, Children = { appInfoLabel, appInfoDesc } }
        };

        // Favorites section - kept for future use
        var favoritesTitle = new Label { 
            Text = "Địa điểm yêu thích", 
            FontFamily = "InterBold", 
            FontSize = 16, 
            TextColor = Color.FromArgb("#18181B"), 
            Margin = new Thickness(20, 24, 20, 12) 
        };
        
        var scroller = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never };
        var favStack = new HorizontalStackLayout { Spacing = 12, Padding = new Thickness(20, 0) };
        var emptyFav = new Border { 
            BackgroundColor = Color.FromArgb("#E0F5F0"), 
            Stroke = Colors.Transparent, 
            StrokeShape = new RoundRectangle { CornerRadius=12 }, 
            Padding = new Thickness(16), 
            Content = new Label { Text = "Chưa có địa điểm yêu thích", TextColor = Color.FromArgb("#0D7A5F"), FontSize = 12 } 
        };
        favStack.Children.Add(emptyFav);
        scroller.Content = favStack;

        var favoritesSection = new VerticalStackLayout { Children = { favoritesTitle, scroller } };

        // ═══════════════════════════════════════════
        // FOOTER
        // ═══════════════════════════════════════════
        _versionLabel = new Label { 
            FontSize = 11, 
            TextColor = Color.FromArgb("#D1D5DB"), 
            HorizontalOptions = LayoutOptions.Center, 
            Margin = new Thickness(0, 24, 0, 40) 
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children = { topHeaderGrid, _appTitleLabel, _appSubtitleLabel, statsCard, appInfoCard, favoritesSection, _versionLabel }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _loc.LanguageChanged += OnLanguageChanged;
        RefreshStats();
        OnLanguageChanged(); // Apply Initial Translation
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
            // Title overrides shell text
            Title = _loc["ProfileTitle"] ?? "Hồ sơ";

            // Gamification
            _statAudioLabel.Text = _loc["GamificationAudio"] ?? "Thời gian nghe";
            _statPlacesLabel.Text = _loc["GamificationPlaces"] ?? "Địa điểm đã đến";
            _statBadgesLabel.Text = _loc["GamificationBadges"] ?? "Danh hiệu";

            // Version - app works in anonymous mode
            _versionLabel.Text = string.Format(_loc["AppVersionFormat"] ?? "TourMap {0} • Audio Tour Guide", "1.0");

            RefreshStats();
        });
    }

    private void RefreshStats()
    {
        // Anonymous mode - show mock stats or load from local preferences
        // In a real implementation, these would be loaded from local storage
        var prefsPlaces = Preferences.Default.Get("UserPlacesVisited", 0);
        _statPlacesValue.Text = prefsPlaces.ToString();
        _statAudioValue.Text = "0h";
        _statBadgesValue.Text = "-";
    }
}
