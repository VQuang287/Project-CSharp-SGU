using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages;

public class HomePage : ContentPage
{
    private readonly LocalizationService _loc;
    private readonly DatabaseService _databaseService;
    private readonly AutoSyncService _autoSyncService;

    // UI Elements - Header
    private Label _welcomeLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _currentTimeLabel = null!;

    // Stats
    private Label _poiCountLabel = null!;
    private Label _syncStatusLabel = null!;
    private Label _poiCountText = null!;
    private Label _syncStatusText = null!;

    // Featured Section
    private Label _featuredPoisLabel = null!;
    private VerticalStackLayout _featuredPoisList = null!;
    private Button _viewAllButton = null!;

    public HomePage()
    {
        _loc = LocalizationService.Current;
        _databaseService = ServiceHelper.GetService<DatabaseService>();
        _autoSyncService = ServiceHelper.GetService<AutoSyncService>();

        BuildUI();
        SetupEvents();
        SetupLocalization();
    }

    private void BuildUI()
    {
        // Modern gradient background
        Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb("#F8F9FA"), 0.0f),
                new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb("#E8F5F0"), 1.0f)
            },
            new Point(0, 0),
            new Point(0, 1));

        Shell.SetNavBarIsVisible(this, false);

        // ═══════════════════════════════════════════
        // HEADER SECTION
        // ═══════════════════════════════════════════
        _welcomeLabel = new Label
        {
            FontFamily = "InterBold",
            FontSize = 28,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937"),
            Opacity = 0
        };

        _subtitleLabel = new Label
        {
            FontFamily = "InterRegular",
            FontSize = 14,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"),
            Opacity = 0
        };

        _currentTimeLabel = new Label
        {
            FontFamily = "InterMedium",
            FontSize = 12,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"),
            HorizontalOptions = LayoutOptions.End
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { _welcomeLabel, _subtitleLabel }
                },
                _currentTimeLabel.WithColumn(1)
            }
        };

        // ═══════════════════════════════════════════
        // STATS SECTION
        // ═══════════════════════════════════════════
        _poiCountLabel = new Label { FontFamily = "InterBold", FontSize = 28, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"), HorizontalOptions = LayoutOptions.Center };
        _poiCountText = new Label { FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center };
        _syncStatusLabel = new Label { FontFamily = "InterBold", FontSize = 28, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E"), HorizontalOptions = LayoutOptions.Center };
        _syncStatusText = new Label { FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center };

        var statsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12,
            Margin = new Thickness(0, 4, 0, 4)
        };

        statsGrid.Children.Add(CreateModernStatCard(_poiCountLabel, _poiCountText, "#E8F5F0"));
        statsGrid.Children.Add(CreateModernStatCard(_syncStatusLabel, _syncStatusText, "#D1FAE5").WithColumn(1));

        // ═══════════════════════════════════════════
        // FEATURED POIS SECTION
        // ═══════════════════════════════════════════
        _featuredPoisLabel = new Label
        {
            FontFamily = "InterBold",
            FontSize = 18,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937"),
            Margin = new Thickness(0, 8, 0, 0)
        };

        _viewAllButton = new Button
        {
            FontFamily = "InterMedium",
            FontSize = 13,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"),
            BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent,
            Padding = new Thickness(8, 4),
            HorizontalOptions = LayoutOptions.End
        };

        var sectionHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children = { _featuredPoisLabel, _viewAllButton.WithColumn(1) }
        };

        _featuredPoisList = new VerticalStackLayout { Spacing = 12 };

        // ═══════════════════════════════════════════
        // MAIN LAYOUT
        // ═══════════════════════════════════════════
        var mainLayout = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16, 20, 32),
            Spacing = 20,
            Children =
            {
                headerGrid,
                statsGrid,
                sectionHeader,
                _featuredPoisList
            }
        };

        Content = new ScrollView
        {
            Content = mainLayout,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never
        };

        // Update time
        UpdateCurrentTime();
    }

    private static HorizontalStackLayout CreateIconLabel(string icon, string text)
    {
        return new HorizontalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = icon, FontSize = 12, VerticalOptions = LayoutOptions.Center },
                new Label { Text = text, FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromRgba(255, 255, 255, 0.9), VerticalOptions = LayoutOptions.Center }
            }
        };
    }

    private static Border CreateModernStatCard(Label valueLabel, Label textLabel, string bgColor)
    {
        return new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb(bgColor),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Shadow = new Shadow
            {
                Brush = Microsoft.Maui.Graphics.Colors.Black,
                Opacity = 0.05f,
                Radius = 8,
                Offset = new Point(0, 2)
            },
            Padding = new Thickness(16, 14),
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 2,
                Children = { valueLabel, textLabel }
            }
        };
    }

    private void UpdateCurrentTime()
    {
        var now = DateTime.Now;
        _currentTimeLabel.Text = now.ToString("HH:mm");
    }

    private void SetupLocalization()
    {
        // Header
        _welcomeLabel.Text = _loc["HomeWelcome"] ?? "Welcome!";
        _subtitleLabel.Text = _loc["HomeSubtitle"] ?? "Ready to explore?";

        // Stats
        _poiCountText.Text = _loc["PoiCountLabel"] ?? "places";
        _syncStatusText.Text = _loc["SyncStatusLabel"] ?? "synced";

        // Section
        _featuredPoisLabel.Text = _loc["FeaturedPois"] ?? "Featured Places";
        _viewAllButton.Text = _loc["ExploreNow"] ?? "Explore →";

        // Fade in animation
        FadeInLabel(_welcomeLabel, 100);
        FadeInLabel(_subtitleLabel, 200);
    }

    private static void FadeInLabel(Label label, uint delay)
    {
        label.Opacity = 0;
        label.TranslationY = 10;
        label.FadeTo(1, 400, Easing.CubicOut);
        label.TranslateTo(0, 0, 400, Easing.CubicOut);
    }

    private void SetupEvents()
    {
        _viewAllButton.Clicked += OnViewAllClicked;
        _loc.LanguageChanged += OnLanguageChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HomePage] OnAppearing error: {ex.Message}");
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var pois = await _databaseService.GetPoisAsync();
            _poiCountLabel.Text = pois.Count.ToString();

            _featuredPoisList.Children.Clear();
            foreach (var poi in pois.Take(5))
            {
                _featuredPoisList.Children.Add(CreateModernPoiCard(poi));
            }

            _syncStatusLabel.Text = "✓";
            _syncStatusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HomePage] LoadData failed: {ex.Message}");
        }
    }

    private View CreateModernPoiCard(Poi poi)
    {
        var card = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Shadow = new Shadow
            {
                Brush = Microsoft.Maui.Graphics.Colors.Black,
                Opacity = 0.08f,
                Radius = 12,
                Offset = new Point(0, 4)
            },
            Padding = new Thickness(0),
            GestureRecognizers =
            {
                new TapGestureRecognizer
                {
                    Command = new Command(async () => await OnPoiTappedSafe(poi.Id))
                }
            }
        };

        // Image with gradient placeholder
        var imageBorder = new Border
        {
            WidthRequest = 80,
            HeightRequest = 80,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb("#E8F5F0"), 0.0f),
                    new GradientStop(Microsoft.Maui.Graphics.Color.FromArgb("#D1FAE5"), 1.0f)
                },
                new Point(0, 0),
                new Point(1, 1)),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Margin = new Thickness(12),
            Content = new Image
            {
                Source = string.IsNullOrEmpty(poi.ImageUrl) ? "poi_placeholder.png" : poi.ImageUrl,
                Aspect = Aspect.AspectFill
            }
        };

        var textLayout = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(0, 12, 12, 12)
        };

        var titleLabel = new Label
        {
            Text = poi.Title,
            FontFamily = "InterBold",
            FontSize = 15,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937")
        };

        var descLabel = new Label
        {
            Text = poi.Description?.Length > 50 ? poi.Description.Substring(0, 50) + "..." : poi.Description,
            FontFamily = "InterRegular",
            FontSize = 12,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };

        // Priority badge
        var badgeText = poi.Priority > 5 ? "⭐ " + (_loc["PoiPriorityHigh"] ?? "High") : (_loc["PoiPriorityFeatured"] ?? "Featured");
        var badge = new Border
        {
            BackgroundColor = poi.Priority > 5
                ? Microsoft.Maui.Graphics.Color.FromArgb("#FEF3C7")
                : Microsoft.Maui.Graphics.Color.FromArgb("#E8F5F0"),
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Padding = new Thickness(6, 2),
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = badgeText,
                FontFamily = "InterMedium",
                FontSize = 10,
                TextColor = poi.Priority > 5
                    ? Microsoft.Maui.Graphics.Color.FromArgb("#92400E")
                    : Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F")
            }
        };

        textLayout.Children.Add(badge);
        textLayout.Children.Add(titleLabel);
        textLayout.Children.Add(descLabel);

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0
        };

        layout.Children.Add(imageBorder);
        layout.Children.Add(textLayout);
        Grid.SetColumn(textLayout, 1);

        card.Content = layout;

        // Add press effect
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                card.ScaleTo(0.98, 100);
                card.ScaleTo(1, 100);
            })
        });

        return card;
    }

    private async Task OnPoiTappedSafe(string poiId)
    {
        try
        {
            await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={poiId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HomePage] Navigation failed: {ex.Message}");
        }
    }

    private async void OnViewAllClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button btn)
            {
                await btn.FadeTo(0.6, 100);
                await btn.FadeTo(1, 100);
            }
            await Shell.Current.GoToAsync(nameof(PoiListPage));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HomePage] View all failed: {ex.Message}");
        }
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            SetupLocalization();
            // Reload POI data to update card badges with new language
            await LoadDataAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
    }
}
