using CoreLocation;
using Microsoft.Maui.Devices.Sensors;
using TourMap.Services;

namespace TourMap;

/// <summary>
/// iOS Location Service — dùng CoreLocation để lấy vị trí hiện tại.
/// </summary>
public class LocationService_iOS : ILocationService
{
    private readonly CLLocationManager _locationManager;

    public LocationService_iOS()
    {
        _locationManager = new CLLocationManager();
        _locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
        Console.WriteLine("[Location-iOS] ✅ CLLocationManager initialized");
    }

    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            // Kiểm tra quyền
            var status = CLLocationManager.Status;
            if (status != CLAuthorizationStatus.AuthorizedWhenInUse && 
                status != CLAuthorizationStatus.AuthorizedAlways)
            {
                Console.WriteLine($"[Location-iOS] ⚠️ Location permission not granted: {status}");
                return null;
            }

            // Request location update
            var tcs = new TaskCompletionSource<CLLocation?>();
            
            _locationManager.LocationsUpdated += (sender, e) =>
            {
                if (e.Locations.Length > 0)
                {
                    tcs.TrySetResult(e.Locations[0]);
                }
            };

            _locationManager.StartUpdatingLocation();
            
            // Timeout sau 10 giây
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            cts.Token.Register(() => tcs.TrySetResult(null));
            
            var clLocation = await tcs.Task;
            _locationManager.StopUpdatingLocation();

            if (clLocation == null)
            {
                Console.WriteLine("[Location-iOS] ⚠️ No location received within timeout");
                return null;
            }

            Console.WriteLine($"[Location-iOS] ✅ Location obtained: {clLocation.Coordinate.Latitude}, {clLocation.Coordinate.Longitude}");
            
            return new Location(
                clLocation.Coordinate.Latitude, 
                clLocation.Coordinate.Longitude,
                clLocation.Timestamp.ToDateTime().DateTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Location-iOS] ❌ Error getting location: {ex.Message}");
            return null;
        }
    }
}
