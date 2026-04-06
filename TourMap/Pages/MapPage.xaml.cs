using TourMap.ViewModels;
using TourMap.Services;
using TourMap.Models;
using Mapsui;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Providers;

namespace TourMap.Pages;

public partial class MapPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly IGpsTrackingService _gpsService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationEngine _narrationEngine;
    private readonly MapControl _mapControl;

    // UI: thanh trạng thái narration ở phía dưới
    private readonly Label _statusLabel;
    private readonly Border _statusBar;
    private readonly Border _poiPreviewCard;
    private readonly Label _poiPreviewTitle;
    private readonly Label _poiPreviewDescription;
    private readonly Button _poiPreviewOpenButton;
    private MemoryLayer? _poiLayer;
    private Poi? _selectedPreviewPoi;

    // ID của POI gần nhất đang highlight (để tránh re-render liên tục)
    private string? _highlightedPoiId;

    public MapPage(
        MainViewModel vm,
        IGpsTrackingService gpsService,
        GeofenceEngine geofenceEngine,
        NarrationEngine narrationEngine)
    {
        _vm = vm;
        _gpsService = gpsService;
        _geofenceEngine = geofenceEngine;
        _narrationEngine = narrationEngine;

        // Ẩn thanh tiêu đề mặc định của ứng dụng trên page này
        Shell.SetNavBarIsVisible(this, false);

        // === Khởi tạo bản đồ Mapsui (OpenStreetMap) ===
        _mapControl = new MapControl();
        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map?.Layers.Add(tileLayer);
        _mapControl.Map?.Widgets.Clear();

        // === Thanh trạng thái narration (bottom bar) ===
        _statusLabel = new Label
        {
            Text = LocalizationService.Current["ReadyToMove"],
            FontSize = 14,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _statusBar = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D2D"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Padding = new Thickness(16, 10),
            Margin = new Thickness(10, 0, 10, 10),
            Shadow = new Shadow { Brush = Microsoft.Maui.Graphics.Colors.Black, Offset = new Point(2, 2), Radius = 4, Opacity = 0.3f },
            Content = _statusLabel
        };

        _poiPreviewTitle = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        };

        _poiPreviewDescription = new Label
        {
            FontSize = 13,
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 3
        };

        _poiPreviewOpenButton = new Button
        {
            Text = "Xem chi tiết",
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1565C0"),
            TextColor = Colors.White,
            CornerRadius = 10
        };
        _poiPreviewOpenButton.Clicked += OnOpenPreviewPoiClicked;

        _poiPreviewCard = new Border
        {
            IsVisible = false,
            BackgroundColor = Colors.White,
            Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#D9E2F2"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14),
            Margin = new Thickness(12),
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill,
            Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(1, 2), Radius = 4, Opacity = 0.15f },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { _poiPreviewTitle, _poiPreviewDescription, _poiPreviewOpenButton }
            }
        };

        var mapHost = new Grid();
        mapHost.Children.Add(_mapControl);
        mapHost.Children.Add(_poiPreviewCard);

        // === Layout: Map + Status Bar ===
        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),     // Map chiếm toàn bộ
                new RowDefinition(GridLength.Auto)       // Status bar bên dưới
            },
            Children =
            {
                mapHost,       // Row 0
                _statusBar     // Row 1
            }
        };
        Grid.SetRow(mapHost, 0);
        Grid.SetRow(_statusBar, 1);

        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        HidePoiPreview();
        _mapControl.MapTapped -= OnMapTapped;
        _mapControl.MapTapped += OnMapTapped;

        // 1. Load danh sách POI từ SQLite
        await _vm.LoadAsync();
        _geofenceEngine.UpdatePois(_vm.Pois.ToList());

        // 2. Hiển thị POI markers trên bản đồ
        ShowPoisOnMap();

        // 3. Subscribe GPS tracking → cập nhật bản đồ + Geofence
        _gpsService.LocationChanged += OnGpsLocationChanged;

        // 4. Subscribe Narration state → cập nhật status bar
        _narrationEngine.StateChanged += OnNarrationStateChanged;

        // 5. Bắt đầu tracking GPS liên tục (runtime-level service đã đảm bảo idempotent).
        await _gpsService.StartTrackingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HidePoiPreview();

        // Gỡ event handlers & dừng tracking
        _gpsService.LocationChanged -= OnGpsLocationChanged;
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
        _mapControl.MapTapped -= OnMapTapped;
        _gpsService.StopTracking();
    }

    // ========== GPS Location Changed ==========

    private void OnGpsLocationChanged(Location location)
    {
        // Cập nhật vị trí user trên bản đồ (real-time)
        UpdateMyLocationOnMap(location.Latitude, location.Longitude);

        // Highlight POI gần nhất
        HighlightNearestPoi(location.Latitude, location.Longitude);
    }

    // ========== Narration State Changed → UI ==========

    private void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case NarrationState.Idle:
                    _statusLabel.Text = LocalizationService.Current["ReadyToMove"];
                    _statusBar.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D2D");
                    break;

                case NarrationState.Playing:
                    _statusLabel.Text = $"{LocalizationService.Current["PlayingAudio"]}: {poi?.Title ?? "..."}";
                    _statusBar.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1B5E20");
                    break;

                case NarrationState.Cooldown:
                    _statusLabel.Text = $"{LocalizationService.Current["Cooldown"]}: {poi?.Title ?? "..."}";
                    _statusBar.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#37474F");
                    break;
            }
        });
    }

    // ========== Map Rendering ==========

    private void ShowPoisOnMap()
    {
        if (_mapControl.Map == null || _vm.Pois == null || !_vm.Pois.Any())
            return;

        var poiFeatures = new List<PointFeature>();

        foreach (var poi in _vm.Pois)
        {
            var (x, y) = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
            var mapPoint = new Mapsui.MPoint(x, y);

            var feature = new PointFeature(mapPoint)
            {
                Data = poi.Id,
                Styles = new[] {
                    new SymbolStyle {
                        Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red),
                        Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2),
                        SymbolScale = 0.5
                    }
                }
            };
            feature["PoiId"] = poi.Id;
            poiFeatures.Add(feature);
        }

        _poiLayer = new MemoryLayer
        {
            Name = "PoiLayer",
            Features = poiFeatures,
            Style = null
        };

        RemoveLayer("PoiLayer");
        _mapControl.Map.Layers.Add(_poiLayer);
    }

    private void UpdateMyLocationOnMap(double lat, double lng)
    {
        if (_mapControl.Map == null) return;

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        var mapPoint = new Mapsui.MPoint(x, y);

        var myLocationFeature = new PointFeature(mapPoint)
        {
            Styles = new[] {
                new SymbolStyle {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2),
                    SymbolScale = 0.5
                }
            }
        };

        var myLocationLayer = new MemoryLayer
        {
            Name = "MyLocation",
            Features = new[] { myLocationFeature }
        };

        RemoveLayer("MyLocation");
        _mapControl.Map.Layers.Add(myLocationLayer);

        // Zoom camera đến vị trí user
        _mapControl.Map.Navigator.CenterOnAndZoomTo(mapPoint, 2.4);
    }

    private void HighlightNearestPoi(double lat, double lng)
    {
        var nearest = _geofenceEngine.GetNearestPoi(lat, lng);
        if (nearest == null || nearest.Id == _highlightedPoiId)
            return;

        _highlightedPoiId = nearest.Id;

        // Tạo marker highlight lớn hơn, màu vàng cho POI gần nhất
        var (x, y) = SphericalMercator.FromLonLat(nearest.Longitude, nearest.Latitude);
        var mapPoint = new Mapsui.MPoint(x, y);

        var highlightFeature = new PointFeature(mapPoint)
        {
            Styles = new[] {
                new SymbolStyle {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Orange),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3),
                    SymbolScale = 0.8  // Lớn hơn marker thường
                }
            }
        };

        var highlightLayer = new MemoryLayer
        {
            Name = "HighlightLayer",
            Features = new[] { highlightFeature },
            Style = null
        };

        RemoveLayer("HighlightLayer");
        _mapControl.Map?.Layers.Add(highlightLayer);
    }

    private void RemoveLayer(string name)
    {
        var existing = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == name);
        if (existing != null) _mapControl.Map?.Layers.Remove(existing);
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        if (_poiLayer == null)
            return;

        var mapInfo = _mapControl.GetMapInfo(e.ScreenPosition, new[] { _poiLayer });
        var poiId = mapInfo?.Feature?.Data?.ToString();

        if (string.IsNullOrWhiteSpace(poiId))
        {
            HidePoiPreview();
            return;
        }

        var poi = _vm.Pois.FirstOrDefault(x => x.Id == poiId);
        if (poi == null)
        {
            HidePoiPreview();
            return;
        }

        _selectedPreviewPoi = poi;
        _poiPreviewTitle.Text = poi.Title;
        _poiPreviewDescription.Text = poi.Description;
        _poiPreviewCard.IsVisible = true;
    }

    private async void OnOpenPreviewPoiClicked(object? sender, EventArgs e)
    {
        if (_selectedPreviewPoi == null)
            return;

        var poiId = _selectedPreviewPoi.Id;
        HidePoiPreview();
        await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={poiId}");
    }

    private void HidePoiPreview()
    {
        _selectedPreviewPoi = null;
        _poiPreviewCard.IsVisible = false;
    }
}

