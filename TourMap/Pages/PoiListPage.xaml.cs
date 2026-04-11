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
    private string _activeFilter = "all";
    private string _searchQuery = string.Empty;

    // UI refs
    private readonly CollectionView _listView;
    private readonly Entry _searchEntry;
    private readonly HorizontalStackLayout _filterRow;
    private readonly Label _nearbyBadge;
    private readonly Label _visitedBadge;
    private readonly Label _totalLabel;

    // Filter definitions (from Figma POIListScreen)
    private static readonly (string Id, string Label, string Emoji)[] Filters =
    {
        ("all", "Tất cả", "🗺️"),
        ("food", "Ẩm thực", "🍜"),
        ("heritage", "Di tích", "🏛️"),
        ("temple", "Chùa", "⛩️"),
        ("market", "Chợ", "🏪"),
        ("park", "Công viên", "🌿"),
        ("culture", "Văn hóa", "🎭"),
    };

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
        var titleLabel = new Label
        {
            Text = "Địa điểm",
            FontFamily = "InterBold",
            FontSize = 17,
            TextColor = Color.FromArgb("#18181B"),
        };

        // ─── Stats Row ───
        _nearbyBadge = CreateBadge("0 gần bạn", "#E0F5F0", "#0D7A5F", true);
        _visitedBadge = CreateBadge("0 đã nghe", "#DCFCE7", "#22C55E", false);
        _totalLabel = new Label
        {
            Text = "0 tổng cộng",
            FontFamily = "InterRegular",
            FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            VerticalOptions = LayoutOptions.Center,
        };

        var statsRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _nearbyBadge, _visitedBadge, _totalLabel }
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
            Placeholder = "Tìm địa điểm...",
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

        // ─── Filter Chips ───
        _filterRow = new HorizontalStackLayout { Spacing = 8 };
        foreach (var f in Filters)
        {
            var chip = CreateFilterChip(f.Id, f.Label, f.Emoji);
            _filterRow.Children.Add(chip);
        }

        var filterScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = _filterRow,
        };

        // ─── Header Container ───
        var headerContainer = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 4, 16, 12),
            Spacing = 10,
            Children = { titleLabel, statsRow, searchBox, filterScroll }
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
            _allPois = await _dbService.GetPoisAsync();
            UpdateStats();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PoiListPage] Error loading POIs: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Filter & Search Logic
    // ═══════════════════════════════════════════════════════════

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        _searchQuery = e.NewTextValue ?? string.Empty;
        ApplyFilter();
    }

    private void OnFilterTapped(string filterId)
    {
        _activeFilter = filterId;
        UpdateFilterChipStyles();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allPois.Where(p =>
        {
            // Category filter — map seed data tags to categories
            if (_activeFilter != "all")
            {
                // Simple keyword match since Poi model doesn't have Category field
                var matchesFilter = _activeFilter switch
                {
                    "food" => p.Title.Contains("Ốc") || p.Title.Contains("Quán") || p.Description.Contains("ẩm thực", StringComparison.OrdinalIgnoreCase),
                    "heritage" => p.Description.Contains("di tích", StringComparison.OrdinalIgnoreCase) || p.Description.Contains("lịch sử", StringComparison.OrdinalIgnoreCase),
                    _ => true
                };
                if (!matchesFilter) return false;
            }

            // Search
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                return p.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                       p.Description.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);
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
        // Since Poi model doesn't have status field, show simple counts
        _nearbyBadge.Text = $"📍 {total} điểm";
        _totalLabel.Text = $"{total} tổng cộng";
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

    private Border CreateFilterChip(string id, string label, string emoji)
    {
        var isActive = id == _activeFilter;
        var chipLabel = new Label
        {
            Text = $"{emoji} {label}",
            FontFamily = "InterMedium",
            FontSize = 11,
            TextColor = isActive ? Colors.White : Color.FromArgb("#6B7280"),
            VerticalOptions = LayoutOptions.Center,
        };

        var chip = new Border
        {
            BackgroundColor = isActive ? Color.FromArgb("#0D7A5F") : Color.FromArgb("#F6F5F1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(12, 6),
            Content = chipLabel,
        };

        chip.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnFilterTapped(id))
        });

        return chip;
    }

    private void UpdateFilterChipStyles()
    {
        for (int i = 0; i < _filterRow.Children.Count; i++)
        {
            if (_filterRow.Children[i] is Border chip && chip.Content is Label label)
            {
                var filterId = Filters[i].Id;
                var isActive = filterId == _activeFilter;
                chip.BackgroundColor = isActive ? Color.FromArgb("#0D7A5F") : Color.FromArgb("#F6F5F1");
                label.TextColor = isActive ? Colors.White : Color.FromArgb("#6B7280");
            }
        }
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
        titleLabel.SetBinding(Label.TextProperty, "Title");

        // ─── Description ───
        var descLabel = new Label
        {
            FontFamily = "InterRegular",
            FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2,
        };
        descLabel.SetBinding(Label.TextProperty, "Description");

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
            Text = "🎧 TTS",
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
                    Text = "Không tìm thấy",
                    FontFamily = "InterMedium",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#18181B"),
                    HorizontalTextAlignment = TextAlignment.Center,
                },
                new Label
                {
                    Text = "Thử đổi bộ lọc hoặc từ khóa",
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
}

/// <summary>Converts Priority int to readable text.</summary>
public class PriorityToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int priority)
        {
            return priority switch
            {
                >= 8 => "⭐ Ưu tiên cao",
                >= 4 => "📍 Nổi bật",
                _ => "📍 Điểm tham quan"
            };
        }
        return "📍 Điểm tham quan";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
