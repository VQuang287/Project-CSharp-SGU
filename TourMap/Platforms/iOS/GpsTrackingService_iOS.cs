using CoreLocation;
using TourMap.Services;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace TourMap;

/// <summary>
/// iOS GPS Tracking Service — dùng CoreLocation để tracking liên tục.
/// </summary>
public class GpsTrackingService_iOS : IGpsTrackingService
{
    private readonly CLLocationManager _locationManager;
    private bool _isTracking;

    public event Action<Location>? LocationChanged;
    
    public bool IsTracking => _isTracking;
    public Location? LastKnownLocation { get; private set; }

    public GpsTrackingService_iOS()
    {
        _locationManager = new CLLocationManager();
        _locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
        _locationManager.DistanceFilter = 10; // Meters - update every 10 meters
        _locationManager.LocationsUpdated += OnLocationsUpdated;
        _locationManager.AuthorizationChanged += OnAuthorizationChanged;
        
        Console.WriteLine("[GpsTracking-iOS] ✅ CLLocationManager initialized");
    }

    public Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            Console.WriteLine("[GpsTracking-iOS] ⚠️ Already tracking");
            return Task.CompletedTask;
        }

        // Request permission nếu chưa có
        if (CLLocationManager.Status != CLAuthorizationStatus.AuthorizedWhenInUse &&
            CLLocationManager.Status != CLAuthorizationStatus.AuthorizedAlways)
        {
            _locationManager.RequestWhenInUseAuthorization();
        }

        _locationManager.StartUpdatingLocation();
        _isTracking = true;
        
        Console.WriteLine("[GpsTracking-iOS] ▶️ Tracking started");
        return Task.CompletedTask;
    }

    public void StopTracking()
    {
        if (!_isTracking) return;
        
        _locationManager.StopUpdatingLocation();
        _isTracking = false;
        
        Console.WriteLine("[GpsTracking-iOS] ⏹️ Tracking stopped");
    }

    private void OnLocationsUpdated(object? sender, CLLocationsUpdatedEventArgs e)
    {
        if (e.Locations.Length == 0) return;
        
        var clLocation = e.Locations[0];
        var location = new Location(
            clLocation.Coordinate.Latitude,
            clLocation.Coordinate.Longitude,
            clLocation.Timestamp.ToDateTime().DateTime);
        
        LastKnownLocation = location;
        
        Console.WriteLine($"[GpsTracking-iOS] 📍 Location update: {location.Latitude:F6}, {location.Longitude:F6}");
        
        LocationChanged?.Invoke(location);
    }

    private void OnAuthorizationChanged(object? sender, CLAuthorizationChangedEventArgs e)
    {
        Console.WriteLine($"[GpsTracking-iOS] 🔐 Authorization changed: {e.Status}");
        
        if (e.Status == CLAuthorizationStatus.AuthorizedWhenInUse ||
            e.Status == CLAuthorizationStatus.AuthorizedAlways)
        {
            if (_isTracking)
            {
                _locationManager.StartUpdatingLocation();
            }
        }
    }
}
