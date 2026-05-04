using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// POI List Screen — Figma-faithful implementation.
/// Stats row + Search + Filter chips + POI cards with thumbnail.
/// </summary>
public partial class PoiListPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly LocalizationService _loc;

    // Data
    private List<Poi> _allPois = new();
    private string _searchQuery = string.Empty;

    // UI refs
    private readonly CollectionView _listView;
    private readonly Entry _searchEntry;
    private readonly Label _totalLabel;
    private readonly Label _titleLabel;

    public PoiListPage() : this(ServiceHelper.GetService<DatabaseService>())
    {
    }

    public PoiListPage(DatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        _loc = LocalizationService.Current;
        Shell.SetNavBarIsVisible(this, false);

        // ─── Header ───
        _titleLabel = new Label
        {
            Text = _loc["PoiListTitle"] ?? "Địa điểm",
            FontFamily = "InterBold",
            FontSize = 17,
            TextColor = Color.FromArgb("#18181B"),
        };

        // ─── Stats Row ─── (Chỉ hiển thị tổng số POI)
        _totalLabel = new Label
        {
            Text = "Số POI đang có: 0",
            FontFamily = "InterRegular",
            FontSize = 12,
            TextColor = Color.FromArgb("#0D7A5F"),
            VerticalOptions = LayoutOptions.Center,
        };

        var statsRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _totalLabel }
        };

        // ─── Search ───
        var searchIcon = new Label
        {
            Text = "🔍",
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        _searchEntry = new Entry
        {
            Placeholder = _loc["SearchPlaceholder"] ?? "Tìm điểm tham quan...",
            FontFamily = "InterRegular",
            FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            BackgroundColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
        };
        _searchEntry.TextChanged += OnSearchChanged;

        var searchBox = new Border
        {
            BackgroundColor = Color.FromArgb("#F6F5F1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(12, 0),
            HeightRequest = 40,
            Content = new HorizontalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = { searchIcon, _searchEntry }
            }
        };

        // ─── Header Container ─── (Không có filter chips)
        var headerContainer = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 4, 16, 12),
            Spacing = 10,
            Children = { _titleLabel, statsRow, searchBox }
        };

        // ─── POI List ───
        _listView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemTemplate = new DataTemplate(() => CreatePoiCard()),
            EmptyView = CreateEmptyView(),
        };
        _listView.SelectionChanged += OnPoiSelected;

        var listContainer = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 12, 16, 16),
                Children = { _listView }
            }
        };

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
            Children =
            {
                headerContainer,
            }
        };
        Grid.SetRow(headerContainer, 0);

        // CollectionView directly in grid row 1
        _listView.Margin = new Thickness(16, 12, 16, 16);
        ((Grid)Content).Children.Add(_listView);
        Grid.SetRow(_listView, 1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // Subscribe to language changes
            _loc.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            
            _allPois = await _dbService.GetPoisAsync();
            UpdateStats();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PoiListPage] Error loading POIs: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe from language changes to prevent memory leaks
        _loc.LanguageChanged -= OnLanguageChanged;
    }

    // ═══════════════════════════════════════════════════════════
    // Filter & Search Logic
    // ═══════════════════════════════════════════════════════════

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        _searchQuery = e.NewTextValue ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allPois.Where(p =>
        {
            // Search only - no category filter
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var searchableText = GetSearchableText(p);
                return searchableText.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
            }
            return true;
        })
        .OrderByDescending(p => p.Priority)
        .ToList();

        _listView.ItemsSource = filtered;
    }

    private void UpdateStats()
    {
        var total = _allPois.Count;
        _totalLabel.Text = $"Số POI đang có: {total}";
    }

    // ═══════════════════════════════════════════════════════════
    // UI Factory Methods
    // ═══════════════════════════════════════════════════════════

    private Label CreateBadge(string text, string bgHex, string textHex, bool showPulse)
    {
        return new Label
        {
            Text = text,
            FontFamily = "InterMedium",
            FontSize = 11,
            TextColor = Color.FromArgb(textHex),
            BackgroundColor = Color.FromArgb(bgHex),
            Padding = new Thickness(10, 4),
            VerticalOptions = LayoutOptions.Center,
        };
    }

    // Filter chips removed - app now shows all POIs with search only

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Content == null) return;

                _titleLabel.Text = _loc["PoiListTitle"] ?? "Địa điểm";
                _searchEntry.Placeholder = _loc["SearchPlaceholder"] ?? "Tìm điểm tham quan...";
                
                // Refresh stats labels
                UpdateStats();
                _listView.EmptyView = CreateEmptyView();
                _listView.ItemTemplate = new DataTemplate(() => CreatePoiCard());
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PoiListPage] Error in OnLanguageChanged: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Creates a single POI card matching Figma POIListItem design.
    /// White card, thumbnail 72px, title, description snippet, priority dots.
    /// </summary>
    private View CreatePoiCard()
    {
        // ─── Image placeholder ───
        var thumbnail = new BoxView
        {
            WidthRequest = 72,
            HeightRequest = 72,
            BackgroundColor = Color.FromArgb("#E0F5F0"),
            CornerRadius = 12,
        };
        // If POI has ImageUrl, show image instead
        var image = new Image
        {
            WidthRequest = 72,
            HeightRequest = 72,
            Aspect = Aspect.AspectFill,
        };
        image.SetBinding(Image.SourceProperty, "ImageUrl");

        var imageContainer = new Grid
        {
            WidthRequest = 72,
            HeightRequest = 72,
            Children = { thumbnail, image }
        };
        // Clip to rounded rect
        imageContainer.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry
        {
            CornerRadius = 12,
            Rect = new Rect(0, 0, 72, 72)
        };

        // ─── Title ───
        var titleLabel = new Label
        {
            FontFamily = "InterMedium",
            FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
        };
        titleLabel.SetBinding(Label.TextProperty, ".", converter: new LocalizedPoiTitleConverter());

        // ─── Description ───
        var descLabel = new Label
        {
            FontFamily = "InterRegular",
            FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2,
        };
        descLabel.SetBinding(Label.TextProperty, ".", converter: new LocalizedPoiDescriptionConverter());

        // ─── Bottom row: priority indicator ───
        var priorityLabel = new Label
        {
            FontFamily = "InterMedium",
            FontSize = 10,
            TextColor = Color.FromArgb("#0D7A5F"),
        };
        priorityLabel.SetBinding(Label.TextProperty, "Priority",
            converter: new PriorityToTextConverter());

        var audioIndicator = new Label
        {
            Text = _loc["AudioTts"] ?? "🎧 TTS",
            FontFamily = "InterMedium",
            FontSize = 10,
            TextColor = Color.FromArgb("#0D7A5F"),
        };

        var bottomRow = new HorizontalStackLayout
        {
            Spacing = 12,
            Children = { priorityLabel, audioIndicator }
        };

        // ─── Info column ───
        var infoColumn = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children = { titleLabel, descLabel, bottomRow }
        };

        // ─── Chevron ───
        var chevron = new Label
        {
            Text = "›",
            FontSize = 20,
            TextColor = Color.FromArgb("#9CA3AF"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
        };

        // ─── Card layout ───
        var cardGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(72),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            ColumnSpacing = 12,
            Padding = new Thickness(12),
        };
        cardGrid.Add(imageContainer, 0, 0);
        cardGrid.Add(infoColumn, 1, 0);
        cardGrid.Add(chevron, 2, 0);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(0, 1),
                Radius = 4,
                Opacity = 0.06f,
            },
            Content = cardGrid,
        };

        return card;
    }

    private View CreateEmptyView()
    {
        return new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8,
            Padding = new Thickness(0, 64),
            Children =
            {
                new Label { Text = "🔍", FontSize = 48, HorizontalTextAlignment = TextAlignment.Center },
                new Label
                {
                    Text = _loc["PoiEmptyTitle"] ?? "Không tìm thấy",
                    FontFamily = "InterMedium",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#18181B"),
                    HorizontalTextAlignment = TextAlignment.Center,
                },
                new Label
                {
                    Text = _loc["PoiEmptyHint"] ?? "Thử đổi bộ lọc hoặc từ khóa",
                    FontFamily = "InterRegular",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#9CA3AF"),
                    HorizontalTextAlignment = TextAlignment.Center,
                }
            }
        };
    }

    private async void OnPoiSelected(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is Poi selectedPoi)
            {
                _listView.SelectedItem = null;
                await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={selectedPoi.Id}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PoiListPage] Error navigating to POI detail: {ex.Message}");
        }
    }

    private string GetFilterLabel(string id)
    {
        return id switch
        {
            "all" => _loc["PoiFilterAll"] ?? "Tất cả",
            "food" => _loc["PoiFilterFood"] ?? "Ẩm thực",
            "heritage" => _loc["PoiFilterHeritage"] ?? "Di tích",
            "temple" => _loc["PoiFilterTemple"] ?? "Chùa",
            "market" => _loc["PoiFilterMarket"] ?? "Chợ",
            "park" => _loc["PoiFilterPark"] ?? "Công viên",
            "culture" => _loc["PoiFilterCulture"] ?? "Văn hóa",
            _ => id,
        };
    }

    private static string GetSearchableText(Poi poi)
    {
        var segments = new[]
        {
            poi.Title,
            poi.Description,
            poi.DescriptionEn,
            poi.DescriptionZh,
            poi.DescriptionKo,
            poi.DescriptionJa,
            poi.DescriptionFr,
            poi.TtsScriptEn,
            poi.TtsScriptZh,
            poi.TtsScriptKo,
            poi.TtsScriptJa,
            poi.TtsScriptFr
        };

        return string.Join(" ", segments.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}

/// <summary>Converts Priority int to readable text.</summary>
public class PriorityToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var loc = LocalizationService.Current;
        if (value is int priority)
        {
            return priority switch
            {
                >= 8 => loc["PoiPriorityHigh"] ?? "⭐ Ưu tiên cao",
                >= 4 => loc["PoiPriorityFeatured"] ?? "📍 Nổi bật",
                _ => loc["PoiPriorityDefault"] ?? "📍 Điểm tham quan"
            };
        }
        return loc["PoiPriorityDefault"] ?? "📍 Điểm tham quan";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public class LocalizedPoiTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not Poi poi) return string.Empty;
        return poi.Title;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public class LocalizedPoiDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not Poi poi) return string.Empty;

        var lang = LocalizationService.Current.CurrentLanguage;
        return lang switch
        {
            "en" => poi.DescriptionEn ?? poi.TtsScriptEn ?? poi.Description,
            "zh" => poi.DescriptionZh ?? poi.TtsScriptZh ?? poi.DescriptionEn ?? poi.TtsScriptEn ?? poi.Description,
            "ko" => poi.DescriptionKo ?? poi.TtsScriptKo ?? poi.DescriptionEn ?? poi.TtsScriptEn ?? poi.Description,
            "ja" => poi.DescriptionJa ?? poi.TtsScriptJa ?? poi.DescriptionEn ?? poi.TtsScriptEn ?? poi.Description,
            "fr" => poi.DescriptionFr ?? poi.TtsScriptFr ?? poi.DescriptionEn ?? poi.TtsScriptEn ?? poi.Description,
            _ => poi.Description,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
