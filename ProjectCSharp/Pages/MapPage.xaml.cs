using TourMap.ViewModels;
using TourMap.Services;
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
    private readonly ILocationService _locationService;
    private readonly MapControl _mapControl;

    public MapPage(MainViewModel vm, ILocationService locationService)
    {
        _vm = vm;
        _locationService = locationService;

        // Khởi tạo bản đồ Mapsui (OpenStreetMap)
        _mapControl = new MapControl();
        var tileLayer = OpenStreetMap.CreateTileLayer();
        _mapControl.Map?.Layers.Add(tileLayer);
        
        // Ẩn tất cả các dòng thông tin (FPS, System Logs) hiển thị trên màn hình
        _mapControl.Map?.Widgets.Clear();

        Content = _mapControl;

        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        
        // Cắm cờ các điểm tham quan (POIs) lên bản đồ
        ShowPoisOnMap();

        // Hiển thị vị trí thực tế của người dùng
        await ShowMyLocationAsync();
    }

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
                // Cắm cờ màu Đỏ cho các điểm tham quan / quán ăn
                Styles = new[] { 
                    new SymbolStyle { 
                        Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red), 
                        Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2), 
                        SymbolScale = 0.6 
                    } 
                }
            };
            poiFeatures.Add(feature);
        }

        var poiLayer = new MemoryLayer
        {
            Name = "PoiLayer",
            Features = poiFeatures,
            Style = null
        };

        // Xóa layer cũ nếu người dùng mở lại trang này nhiều lần
        var existing = _mapControl.Map.Layers.FirstOrDefault(l => l.Name == "PoiLayer");
        if (existing != null) _mapControl.Map.Layers.Remove(existing);

        _mapControl.Map.Layers.Add(poiLayer);
    }

    private async Task ShowMyLocationAsync()
    {
        var location = await _locationService.GetCurrentLocationAsync();
        if (location != null && _mapControl.Map != null)
        {
            // Bước 1: Chuyển đổi tọa độ GPS sang hệ thống đồ họa của Mapsui
            var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var mapPoint = new Mapsui.MPoint(x, y);

            // Bước 2: Tạo dấu Marker định vị màu xanh
            var myLocationFeature = new PointFeature(mapPoint)
            {
                Styles = new[] { new SymbolStyle { Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2), SymbolScale = 0.5 } }
            };

            var myLocationLayer = new MemoryLayer
            {
                Name = "MyLocation",
                Features = new[] { myLocationFeature }
            };

            var existing = _mapControl.Map.Layers.FirstOrDefault(l => l.Name == "MyLocation");
            if (existing != null) _mapControl.Map.Layers.Remove(existing);

            _mapControl.Map.Layers.Add(myLocationLayer);

            // Bước 3: Di chuyển ống kính camera (Zoom) đến trung tâm tọa độ vừa đứng
            _mapControl.Map.Navigator.CenterOnAndZoomTo(mapPoint, 2.4);
        }
    }
}
