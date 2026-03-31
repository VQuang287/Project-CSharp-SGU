using TourMap.ViewModels;
using TourMap.Services;
using TourMap.Models;
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

        // === Khởi tạo bản đồ Mapsui (OpenStreetMap) ===
        _mapControl = new MapControl();
        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map?.Layers.Add(tileLayer);
        _mapControl.Map?.Widgets.Clear();

        // === Thanh trạng thái narration (bottom bar) ===
        _statusLabel = new Label
        {
            Text = "🎧 Sẵn sàng — Di chuyển đến điểm tham quan",
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
                _mapControl,   // Row 0
                _statusBar     // Row 1
            }
        };
        Grid.SetRow(_mapControl, 0);
        Grid.SetRow(_statusBar, 1);

        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Load danh sách POI từ SQLite
        await _vm.LoadAsync();
        _geofenceEngine.UpdatePois(_vm.Pois.ToList());

        // 2. Hiển thị POI markers trên bản đồ
        ShowPoisOnMap();

        // 3. Subscribe GPS tracking → cập nhật bản đồ + Geofence
        _gpsService.LocationChanged += OnGpsLocationChanged;

        // 4. Subscribe Narration state → cập nhật status bar
        _narrationEngine.StateChanged += OnNarrationStateChanged;

        // 5. Subscribe Geofence trigger → phát narration
        _geofenceEngine.POITriggered += OnPoiTriggered;

        // 6. Bắt đầu tracking GPS liên tục
        await _gpsService.StartTrackingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Gỡ event handlers & dừng tracking
        _gpsService.LocationChanged -= OnGpsLocationChanged;
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
        _geofenceEngine.POITriggered -= OnPoiTriggered;
        _gpsService.StopTracking();
    }

    // ========== GPS Location Changed ==========

    private void OnGpsLocationChanged(Location location)
    {
        // Cập nhật vị trí user trên bản đồ (real-time)
        UpdateMyLocationOnMap(location.Latitude, location.Longitude);

        // Gửi vị trí cho Geofence Engine kiểm tra
        _geofenceEngine.OnLocationChanged(location);

        // Highlight POI gần nhất
        HighlightNearestPoi(location.Latitude, location.Longitude);
    }

    // ========== Geofence Trigger → Narration ==========

    private async void OnPoiTriggered(Poi poi)
    {
        await _narrationEngine.OnPOITriggeredAsync(poi);
    }

    // ========== Narration State Changed → UI ==========

    private void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case NarrationState.Idle:
                    _statusLabel.Text = "🎧 Sẵn sàng — Di chuyển đến điểm tham quan";
                    _statusBar.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D2D");
                    break;

                case NarrationState.Playing:
                    _statusLabel.Text = $"🔊 Đang phát: {poi?.Title ?? "..."}";
                    _statusBar.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1B5E20");
                    break;

                case NarrationState.Cooldown:
                    _statusLabel.Text = $"✅ Đã nghe: {poi?.Title ?? "..."}";
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

        var poiLayer = new MemoryLayer
        {
            Name = "PoiLayer",
            Features = poiFeatures,
            Style = null
        };

        RemoveLayer("PoiLayer");
        _mapControl.Map.Layers.Add(poiLayer);
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
}

