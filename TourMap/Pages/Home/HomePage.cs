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

    // UI Elements
    private Label _welcomeLabel = null!;
    private Label _subtitleLabel = null!;
    private Button _startTourButton = null!;
    private Label _featuredPoisLabel = null!;
    private VerticalStackLayout _featuredPoisList = null!;
    private Label _poiCountLabel = null!;
    private Label _tourCountLabel = null!;
    private Label _syncStatusLabel = null!;

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
        BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F8F9FA");

        // Header labels
        _welcomeLabel = new Label { FontFamily = "InterBold", FontSize = 24, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937") };
        _subtitleLabel = new Label { FontFamily = "InterRegular", FontSize = 14, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280") };

        // Start Tour Button
        _startTourButton = new Button
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"),
            TextColor = Microsoft.Maui.Graphics.Colors.White,
            FontFamily = "InterBold",
            FontSize = 14,
            CornerRadius = 12,
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 12, 0, 0)
        };

        // Featured POIs section
        _featuredPoisLabel = new Label { FontFamily = "InterBold", FontSize = 16, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937"), Margin = new Thickness(0, 8, 0, 0) };
        _featuredPoisList = new VerticalStackLayout { Spacing = 12 };

        // Stats labels
        _poiCountLabel = new Label { FontFamily = "InterBold", FontSize = 24, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"), HorizontalOptions = LayoutOptions.Center };
        _tourCountLabel = new Label { FontFamily = "InterBold", FontSize = 24, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"), HorizontalOptions = LayoutOptions.Center };
        _syncStatusLabel = new Label { FontFamily = "InterBold", FontSize = 24, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E"), HorizontalOptions = LayoutOptions.Center };

        // Stats grid
        var statsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        statsGrid.Children.Add(CreateStatCard(_poiCountLabel, "Địa điểm"));
        statsGrid.Children.Add(CreateStatCard(_tourCountLabel, "Tour").WithColumn(1));
        statsGrid.Children.Add(CreateStatCard(_syncStatusLabel, "Đồng bộ").WithColumn(2));

        // Tour featured card
        var tourCard = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB")),
            Padding = new Thickness(0)
        };

        var tourImage = new Image
        {
            Source = "https://images.unsplash.com/photo-1553621042-f6e147245754?w=800&q=80",
            Aspect = Aspect.AspectFill,
            HeightRequest = 160
        };

        var tourTitle = new Label
        {
            Text = "Tour Ẩm Thực Vĩnh Khánh",
            FontFamily = "InterBold",
            FontSize = 18,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937")
        };

        var tourDesc = new Label
        {
            Text = "Khám phá 10 địa điểm ẩm thực nổi tiếng trên con phố Vĩnh Khánh",
            FontFamily = "InterRegular",
            FontSize = 14,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var tourMeta = new HorizontalStackLayout
        {
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                new Label { Text = "10 địa điểm", FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F") },
                new Label { Text = "•", TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF") },
                new Label { Text = "1 tour", FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F") },
                new Label { Text = "•", TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF") },
                new Label { Text = "Audio tự động", FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F") }
            }
        };

        var tourContent = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 8,
            Children = { tourTitle, tourDesc, tourMeta, _startTourButton }
        };

        var tourLayout = new VerticalStackLayout { Children = { tourImage, tourContent } };
        tourCard.Content = tourLayout;

        // Main layout
        var mainLayout = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 20,
            Children =
            {
                new VerticalStackLayout { Spacing = 8, Children = { _welcomeLabel, _subtitleLabel } },
                tourCard,
                statsGrid,
                _featuredPoisLabel,
                _featuredPoisList
            }
        };

        Content = new ScrollView { Content = mainLayout };
    }

    private Border CreateStatCard(Label valueLabel, string label)
    {
        return new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB")),
            Padding = new Thickness(12),
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 4,
                Children =
                {
                    valueLabel,
                    new Label { Text = label, FontFamily = "InterMedium", FontSize = 11, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center }
                }
            }
        };
    }

    private void SetupLocalization()
    {
        _welcomeLabel.Text = _loc["HomeWelcome"] ?? "Xin chào!";
        _subtitleLabel.Text = _loc["HomeSubtitle"] ?? "Sẵn sàng khám phá Phố Ẩm Thực Vĩnh Khánh?";
        _startTourButton.Text = _loc["StartTour"] ?? "Bắt đầu Tour";
        _featuredPoisLabel.Text = _loc["FeaturedPois"] ?? "Địa điểm nổi bật";
    }

    private void SetupEvents()
    {
        _startTourButton.Clicked += OnStartTourClicked;
        _loc.LanguageChanged += OnLanguageChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
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
                _featuredPoisList.Children.Add(CreatePoiCard(poi));
            }

            _tourCountLabel.Text = "1";

            // Sync status - always show synced for now
            _syncStatusLabel.Text = "✓";
            _syncStatusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HomePage] LoadData failed: {ex.Message}");
        }
    }

    private View CreatePoiCard(Poi poi)
    {
        var card = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB")),
            Padding = new Thickness(12),
            GestureRecognizers =
            {
                new TapGestureRecognizer
                {
                    Command = new Command(() => OnPoiTapped(poi.Id))
                }
            }
        };

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        var imageBorder = new Border
        {
            WidthRequest = 60,
            HeightRequest = 60,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Content = new Image
            {
                Source = string.IsNullOrEmpty(poi.ImageUrl) ? "poi_placeholder.png" : poi.ImageUrl,
                Aspect = Aspect.AspectFill
            }
        };

        var textLayout = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        var titleLabel = new Label
        {
            Text = poi.Title,
            FontFamily = "InterBold",
            FontSize = 14,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937")
        };

        var descLabel = new Label
        {
            Text = poi.Description?.Length > 60 ? poi.Description.Substring(0, 60) + "..." : poi.Description,
            FontFamily = "InterRegular",
            FontSize = 12,
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };

        textLayout.Children.Add(titleLabel);
        textLayout.Children.Add(descLabel);

        layout.Children.Add(imageBorder);
        layout.Children.Add(textLayout);
        Grid.SetColumn(textLayout, 1);

        card.Content = layout;
        return card;
    }

    private async void OnPoiTapped(string poiId)
    {
        await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={poiId}");
    }

    private async void OnStartTourClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Tours.TourListPage));
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(SetupLocalization);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
    }
}
