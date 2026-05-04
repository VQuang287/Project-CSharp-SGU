using TourMap.ViewModels;
using TourMap.Services;
using TourMap.Models;
using Mapsui;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;

namespace TourMap.Pages;

/// <summary>
/// Map Page — Figma-faithful UI implementation.
/// Full-screen map with floating header, search bar, GPS badge,
/// bottom sheet POI preview card, and active audio player bar.
/// </summary>
public partial class MapPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly TourRuntimeService _tourRuntimeService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationEngine _narrationEngine;
    private readonly AutoSyncService _autoSyncService;
    private readonly MapControl _mapControl;
    private readonly LocalizationService _loc;

    // UI Overlays
    private readonly Label _gpsBadgeLabel;
    private readonly Border _gpsBadge;
    private readonly Label _headerSubtitle;
    
    // Audio Player Bar
    private readonly Border _audioPlayerBar;
    private readonly Label _audioTitle;
    private readonly Label _audioSubtitle;
    private readonly Button _audioPlayBtn;
    
    // POI Preview Card
    private readonly Border _poiPreviewCard;
    private readonly Label _previewTitle;
    private readonly Label _previewDesc;
    private readonly Label _previewCategory;
    private readonly Button _openBtn;
    private Poi? _selectedPreviewPoi;

    // Map Layers
    private MemoryLayer? _poiLayer;
    private string? _highlightedPoiId;
    private bool _runtimeInitialized;
    private bool _hasGpsFix;
    private bool _hasCenteredInitialViewport;

    public MapPage() : this(
        ServiceHelper.GetService<MainViewModel>(),
        ServiceHelper.GetService<TourRuntimeService>(),
        ServiceHelper.GetService<GeofenceEngine>(),
        ServiceHelper.GetService<NarrationEngine>(),
        ServiceHelper.GetService<AutoSyncService>())
    {
    }

    public MapPage(
        MainViewModel vm,
        TourRuntimeService tourRuntimeService,
        GeofenceEngine geofenceEngine,
        NarrationEngine narrationEngine,
        AutoSyncService autoSyncService)
    {
        InitializeComponent();
        _vm = vm;
        _tourRuntimeService = tourRuntimeService;
        _geofenceEngine = geofenceEngine;
        _narrationEngine = narrationEngine;
        _autoSyncService = autoSyncService;
        _loc = LocalizationService.Current;

        Shell.SetNavBarIsVisible(this, false);

        // ═══════════════════════════════════════════
        // MAP CONTROL
        // ═══════════════════════════════════════════
        _mapControl = new MapControl { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map?.Layers.Add(tileLayer);
        _mapControl.Map?.Widgets.Clear();

        // ═══════════════════════════════════════════
        // FLOATING HEADER
        // ═══════════════════════════════════════════
        // Note: Profile navigation removed - app works in anonymous mode
        // Avatar replaced with simple decorative element
        var headerIcon = new Border
        {
            WidthRequest = 40, HeightRequest = 40,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0F5F0"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Content = new Label { Text = "�️", FontSize = 20, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
        };

        _headerSubtitle = new Label { 
            Text = _loc["MapSubtitle"] ?? "Khánh Hội", 
            FontFamily = "InterBold", FontSize = 20, 
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#18181B") 
        };
        
        var titleStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = _loc["MapTitle"] ?? "Audio Tour", FontFamily = "InterMedium", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280") },
                _headerSubtitle
            }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children = { titleStack, headerIcon.WithColumn(1) }
        };

        var headerCard = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromRgba(1.0, 1.0, 1.0, 0.9),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Shadow = new Shadow { Brush = Microsoft.Maui.Graphics.Colors.Black, Opacity = 0.05f, Radius = 8, Offset = new Point(0, 4) },
            Padding = new Thickness(16),
            Margin = new Thickness(16, 44, 16, 0),
            VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout { Children = { headerGrid } }
        };

        // ═══════════════════════════════════════════
        // GPS BADGE
        // ═══════════════════════════════════════════
        _gpsBadgeLabel = new Label { Text = _loc["GpsOff"] ?? "GPS đang tắt", FontFamily = "InterMedium", FontSize = 10, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        _gpsBadge = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Shadow = new Shadow { Brush = Microsoft.Maui.Graphics.Colors.Black, Opacity = 0.1f, Radius = 4, Offset = new Point(0, 2) },
            Padding = new Thickness(8, 4),
            Margin = new Thickness(16, 12, 0, 0),
            HorizontalOptions = LayoutOptions.Start,
            Content = new HorizontalStackLayout
            {
                Spacing = 4,
                Children = { new Label { Text = "●", FontSize = 10, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center }, _gpsBadgeLabel }
            }
        };

        var topOverlay = new VerticalStackLayout { Children = { headerCard, _gpsBadge }, InputTransparent = true, CascadeInputTransparent = false };

        // ═══════════════════════════════════════════
        // POI PREVIEW CARD
        // ═══════════════════════════════════════════
        var previewThumb = new BoxView { WidthRequest = 72, HeightRequest = 72, BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0F5F0"), CornerRadius = 12 };
        _previewCategory = new Label { Text = "📍 ĐIỂM THAM QUAN", FontFamily = "InterBold", FontSize = 9, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F") };
        _previewTitle = new Label { FontFamily = "InterBold", FontSize = 14, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#18181B"), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
        _previewDesc = new Label { FontFamily = "InterRegular", FontSize = 11, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"), MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation };

        _openBtn = new Button
        {
            Text = _loc["SeeDetails"] ?? "Xem chi tiết",
            FontFamily = "InterMedium", FontSize = 12,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"),
            TextColor = Microsoft.Maui.Graphics.Colors.White,
            HeightRequest = 36,
            CornerRadius = 12,
            Padding = 0,
        };
        _openBtn.Clicked += OnOpenPreviewPoiClicked;

        var previewInfo = new VerticalStackLayout { Spacing = 2, Children = { _previewCategory, _previewTitle, _previewDesc } };
        
        var previewGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(72), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
            Children = { previewThumb, previewInfo.WithColumn(1) }
        };

        _poiPreviewCard = new Border
        {
            IsVisible = false,
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Microsoft.Maui.Graphics.Colors.Transparent,
            Shadow = new Shadow { Brush = Microsoft.Maui.Graphics.Colors.Black, Opacity = 0.15f, Radius = 8, Offset = new Point(0, -2) },
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0, 16, 16),
            Content = new VerticalStackLayout { Spacing = 12, Children = { previewGrid, _openBtn } }
        };

        // ═══════════════════════════════════════════
        // AUDIO PLAYER BAR
        // ═══════════════════════════════════════════
        var audioIcon = new Border { WidthRequest = 32, HeightRequest = 32, BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0F5F0"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }, Stroke = Microsoft.Maui.Graphics.Colors.Transparent, Content = new Label { Text = "🎧", FontSize = 16, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        _audioTitle = new Label { Text = _loc["Ready"] ?? "Sẵn sàng", FontFamily = "InterBold", FontSize = 12, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#18181B"), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
        _audioSubtitle = new Label { Text = _loc["TapPoiHint"] ?? "Chạm điểm tham quan", FontFamily = "InterRegular", FontSize = 10, TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF") };
        
        _audioPlayBtn = new Button
        {
            Text = "▶", FontSize = 16,
            WidthRequest = 40, HeightRequest = 40,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F"),
            TextColor = Microsoft.Maui.Graphics.Colors.White,
            CornerRadius = 20, Padding = 0,
        };
        _audioPlayBtn.Clicked += OnPlayPauseClicked;

        _audioPlayerBar = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Colors.White,
            Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#E4E4E0"),
            StrokeThickness = 1,
            Padding = new Thickness(16, 10),
            Content = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 12,
                Children =
                {
                    audioIcon,
                    new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _audioTitle, _audioSubtitle } }.WithColumn(1),
                    _audioPlayBtn.WithColumn(2)
                }
            }
        };

        var bottomContainer = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            Spacing = 0,
            Children = { _poiPreviewCard, _audioPlayerBar }
        };

        // ═══════════════════════════════════════════
        // ROOT LAYOUT
        // ═══════════════════════════════════════════
        Content = new Grid
        {
            Children = { _mapControl, topOverlay, bottomContainer }
        };

        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // Subscribe to language changes
            _loc.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            
            HidePoiPreview();
            _mapControl.MapTapped -= OnMapTapped;
            _mapControl.MapTapped += OnMapTapped;

            await EnsureRuntimeInitializedAsync();
            await _vm.LoadAsync();
            ShowPoisOnMap();
            EnsureInitialViewport();

            _tourRuntimeService.LocationUpdated -= OnGpsLocationChanged;
            _tourRuntimeService.LocationUpdated += OnGpsLocationChanged;
            _narrationEngine.StateChanged += OnNarrationStateChanged;
            _autoSyncService.SyncCompleted -= OnAutoSyncCompleted;
            _autoSyncService.SyncCompleted += OnAutoSyncCompleted;

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            UpdateGpsBadge(status == PermissionStatus.Granted);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapPage] Error in OnAppearing: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HidePoiPreview();

        _tourRuntimeService.LocationUpdated -= OnGpsLocationChanged;
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
        _autoSyncService.SyncCompleted -= OnAutoSyncCompleted;
        _mapControl.MapTapped -= OnMapTapped;
        
        // Unsubscribe from language changes
        _loc.LanguageChanged -= OnLanguageChanged;
        
        UpdateGpsBadge(false);
    }

    private async Task EnsureRuntimeInitializedAsync()
    {
        if (!_runtimeInitialized)
        {
            // Auth removed - proceed directly to location permission

            // Always request location permission BEFORE starting GPS tracking/runtime.
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            UpdateGpsBadge(status == PermissionStatus.Granted);

            // Start runtime first (non-blocking GPS loop) so UI can continue.
            await _tourRuntimeService.InitializeAsync();
            _runtimeInitialized = true;

            // Sync in background (do not block first render)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _autoSyncService.EnsureSyncedAsync("map-initial-load", force: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MapPage] Background sync failed: {ex.Message}");
                }
            });
        }

        await _tourRuntimeService.RefreshPoisAsync();
    }

    private async void OnAutoSyncCompleted(object? sender, AutoSyncCompletedEventArgs e)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _vm.LoadAsync();
                ShowPoisOnMap();
                EnsureInitialViewport();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapPage] UI refresh after auto-sync failed: {ex.Message}");
        }
    }

    private void EnsureInitialViewport()
    {
        if (_hasGpsFix || _hasCenteredInitialViewport)
            return;

        if (_mapControl?.Map == null)
            return;

        double centerLat;
        double centerLng;

        if (_vm?.Pois != null && _vm.Pois.Any())
        {
            centerLat = _vm.Pois.Average(p => p.Latitude);
            centerLng = _vm.Pois.Average(p => p.Longitude);
        }
        else
        {
            // Fallback center near Vinh Khanh street so map is never stuck at world view.
            centerLat = 10.7612;
            centerLng = 106.7046;
        }

        var (x, y) = SphericalMercator.FromLonLat(centerLng, centerLat);
        var mapPoint = new Mapsui.MPoint(x, y);

        // Use neighborhood-level resolution for mobile UX.
        _mapControl.Map.Navigator.CenterOnAndZoomTo(mapPoint, 2.4);
        _hasCenteredInitialViewport = true;
    }

    private void UpdateGpsBadge(bool active)
    {
        _gpsBadgeLabel.Text = active ? (_loc["GpsOn"] ?? "GPS đang bật") : (_loc["GpsOff"] ?? "GPS đang tắt");
        _gpsBadgeLabel.TextColor = active ? Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F") : Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF");
        if (_gpsBadge.Content is HorizontalStackLayout h)
            if (h.Children[0] is Label dot)
                dot.TextColor = active ? Microsoft.Maui.Graphics.Color.FromArgb("#22C55E") : Microsoft.Maui.Graphics.Color.FromArgb("#9CA3AF");
    }

    private void OnGpsLocationChanged(Location location)
    {
        _hasGpsFix = true;
        UpdateMyLocationOnMap(location.Latitude, location.Longitude);
        HighlightNearestPoi(location.Latitude, location.Longitude);
    }

    // ========== NARRATION STATE ==========
    private void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case NarrationState.Idle:
                    _audioTitle.Text = _loc["ReadyToMove"];
                    _audioSubtitle.Text = _loc["AutoPlayHint"] ?? "Chuẩn bị phát tự động";
                    _audioPlayBtn.Text = "▶";
                    break;
                case NarrationState.Playing:
                    _audioTitle.Text = poi?.Title ?? _loc["AudioTour"] ?? "Audio Tour";
                    _audioSubtitle.Text = _loc["Playing"] ?? "Đang phát...";
                    _audioPlayBtn.Text = "⏸";
                    break;
                case NarrationState.Cooldown:
                    _audioTitle.Text = poi?.Title ?? _loc["AudioTour"] ?? "Audio Tour";
                    _audioSubtitle.Text = _loc["Finished"] ?? "Đã phát xong";
                    _audioPlayBtn.Text = "▶";
                    break;
            }
        });
    }
    
    // ========== LANGUAGE CHANGE HANDLER ==========
    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Content == null) return;
                
                // Update header
                _headerSubtitle.Text = _loc["MapSubtitle"] ?? "Khánh Hội";

                // Update POI Card Labels
                _previewCategory.Text = _loc["PoiCategoryDefault"] ?? "📍 ĐIỂM THAM QUAN";
                if (_openBtn != null)
                {
                    _openBtn.Text = _loc["SeeDetails"] ?? "Xem chi tiết";
                }
                
                // Update audio bar based on current narration state
                switch (_narrationEngine.CurrentState)
                {
                    case NarrationState.Idle:
                        _audioTitle.Text = _loc["Ready"] ?? "Sẵn sàng";
                        _audioSubtitle.Text = _loc["TapPoiHint"] ?? "Chạm điểm tham quan";
                        break;
                    case NarrationState.Playing:
                        _audioSubtitle.Text = _loc["Playing"] ?? "Đang phát...";
                        break;
                    case NarrationState.Cooldown:
                        _audioSubtitle.Text = _loc["Finished"] ?? "Đã phát xong";
                        break;
                }
                
                // Update GPS badge
                var isGpsActive = _gpsBadgeLabel.TextColor?.ToArgbHex() == Microsoft.Maui.Graphics.Color.FromArgb("#0D7A5F")?.ToArgbHex();
                _gpsBadgeLabel.Text = isGpsActive ? (_loc["GpsOn"] ?? "GPS đang bật") : (_loc["GpsOff"] ?? "GPS đang tắt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapPage] Error in OnLanguageChanged: {ex.Message}");
            }
        });
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_audioPlayBtn.Text == "⏸")
            _narrationEngine.Stop(); // Placeholder for actual stop/pause logic
    }

    // ========== MAP RENDERING ==========
    private void ShowPoisOnMap()
    {
        if (_mapControl?.Map == null)
            return;

        RemoveLayer("PoiLayer");
        if (_vm?.Pois == null || !_vm.Pois.Any())
        {
            _mapControl.Refresh();
            return;
        }

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
                        Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#0D7A5F")), // Teal Default
                        Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2),
                        SymbolScale = 0.9
                    }
                }
            };
            feature["PoiId"] = poi.Id;
            poiFeatures.Add(feature);
        }

        _poiLayer = new MemoryLayer { Name = "PoiLayer", Features = poiFeatures, Style = null };
        _mapControl.Map.Layers.Add(_poiLayer);
        _mapControl.Refresh();
    }

    private void UpdateMyLocationOnMap(double lat, double lng)
    {
        if (_mapControl.Map == null) return;

        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        var mapPoint = new Mapsui.MPoint(x, y);

        var myLocFeature = new PointFeature(mapPoint)
        {
            Styles = new[] {
                new SymbolStyle {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#3B82F6")), // Blue
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3),
                    SymbolScale = 1.05
                }
            }
        };

        var myLocLayer = new MemoryLayer { Name = "MyLocation", Features = new[] { myLocFeature } };
        RemoveLayer("MyLocation");
        _mapControl.Map.Layers.Add(myLocLayer);
        _mapControl.Map.Navigator.CenterOnAndZoomTo(mapPoint, 2.4);
        _mapControl.Refresh();
    }

    private void HighlightNearestPoi(double lat, double lng)
    {
        if (_geofenceEngine == null) return;
        
        var nearest = _geofenceEngine.GetNearestPoi(lat, lng);
        if (nearest == null || nearest.Id == _highlightedPoiId) return;

        _highlightedPoiId = nearest.Id;
        var (x, y) = SphericalMercator.FromLonLat(nearest.Longitude, nearest.Latitude);
        var mapPoint = new Mapsui.MPoint(x, y);

        var highlightFeature = new PointFeature(mapPoint)
        {
            Styles = new[] {
                new SymbolStyle {
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#F5A623")), // Amber
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3),
                    SymbolScale = 0.8
                }
            }
        };

        var highlightLayer = new MemoryLayer { Name = "HighlightLayer", Features = new[] { highlightFeature }, Style = null };
        RemoveLayer("HighlightLayer");
        _mapControl.Map?.Layers.Add(highlightLayer);
        _mapControl.Refresh();
    }

    private void RemoveLayer(string name)
    {
        var layer = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == name);
        if (layer != null) _mapControl.Map?.Layers.Remove(layer);
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        try
        {
            // Debounce to prevent rapid tapping issues
            if (DateTime.Now - _lastTapTime < _tapDebounce)
            {
                Console.WriteLine("[MapPage] Tap debounced - ignoring");
                return;
            }
            _lastTapTime = DateTime.Now;
            
            Console.WriteLine($"[MapPage] OnMapTapped triggered at: {e.ScreenPosition}");
            
            if (_poiLayer == null) 
            {
                Console.WriteLine("[MapPage] OnMapTapped: _poiLayer is null");
                HidePoiPreview();
                return;
            }
            
            if (_mapControl?.Map == null)
            {
                Console.WriteLine("[MapPage] OnMapTapped: MapControl or Map is null");
                HidePoiPreview();
                return;
            }

            var mapInfo = _mapControl.GetMapInfo(e.ScreenPosition, new[] { _poiLayer });
            if (mapInfo == null || mapInfo.Feature == null)
            {
                Console.WriteLine("[MapPage] OnMapTapped: No feature found at tap location");
                HidePoiPreview();
                return;
            }

            var poiId = mapInfo.Feature.Data?.ToString();
            if (string.IsNullOrWhiteSpace(poiId))
            {
                Console.WriteLine("[MapPage] OnMapTapped: POI Id is null or empty");
                HidePoiPreview();
                return;
            }

            Console.WriteLine($"[MapPage] OnMapTapped: Looking for POI with id: {poiId}");
            var poi = _vm.Pois.FirstOrDefault(x => x.Id == poiId);
            if (poi == null)
            {
                Console.WriteLine($"[MapPage] OnMapTapped: POI with id {poiId} not found in ViewModel");
                HidePoiPreview();
                return;
            }

            Console.WriteLine($"[MapPage] OnMapTapped: Found POI - {poi.Title}");
            _selectedPreviewPoi = poi;
            _previewTitle.Text = poi.Title ?? "Không có tiêu đề";
            _previewDesc.Text = poi.Description ?? "Không có mô tả";
            _poiPreviewCard.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapPage] OnMapTapped ERROR: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[MapPage] Stack trace: {ex.StackTrace}");
            HidePoiPreview();
        }
    }

    private DateTime _lastTapTime = DateTime.MinValue;
    private readonly TimeSpan _tapDebounce = TimeSpan.FromMilliseconds(300);
    private bool _isNavigating = false;

    private async void OnOpenPreviewPoiClicked(object? sender, EventArgs e)
    {
        Console.WriteLine($"[MapPage] OnOpenPreviewPoiClicked called. _selectedPreviewPoi: {_selectedPreviewPoi?.Id ?? "NULL"}");
        
        // Prevent double-click
        if (_isNavigating)
        {
            Console.WriteLine("[MapPage] Navigation already in progress - ignoring");
            return;
        }
        
        try
        {
            if (_selectedPreviewPoi == null) 
            {
                Console.WriteLine("[MapPage] ERROR: _selectedPreviewPoi is null");
                return;
            }
            
            if (Shell.Current == null)
            {
                Console.WriteLine("[MapPage] ERROR: Shell.Current is null");
                await DisplayAlertAsync("Lỗi", "Không thể điều hướng. Vui lòng thử lại.", "OK");
                return;
            }
            
            var id = _selectedPreviewPoi.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("[MapPage] ERROR: POI Id is empty");
                return;
            }
            
            _isNavigating = true;
            Console.WriteLine($"[MapPage] Navigating to PoiDetailPage with id: {id}");
            HidePoiPreview();
            
            await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={id}");
            Console.WriteLine("[MapPage] Navigation completed successfully");
        }
        catch (InvalidOperationException ioe)
        {
            Console.WriteLine($"[MapPage] Navigation error (InvalidOperation): {ioe.Message}");
            await DisplayAlertAsync("Lỗi", "Không thể mở chi tiết địa điểm. Vui lòng thử lại sau.", "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapPage] CRITICAL ERROR navigating to POI detail: {ex.GetType().Name}");
            Console.WriteLine($"[MapPage] Message: {ex.Message}");
            Console.WriteLine($"[MapPage] Stack Trace: {ex.StackTrace}");
            await DisplayAlertAsync("Lỗi", $"Không thể mở chi tiết địa điểm: {ex.Message}", "OK");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void HidePoiPreview()
    {
        _selectedPreviewPoi = null;
        _poiPreviewCard.IsVisible = false;
    }
}

public static class GridExtensions
{
    public static T WithColumn<T>(this T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
