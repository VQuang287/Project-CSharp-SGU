using Android.Content;
using TourMap.Services;

namespace TourMap;

/// <summary>
/// Android: GPS tracking liên tục dùng Timer + Geolocation API.
/// Cập nhật vị trí mỗi 5 giây, lọc nhiễu accuracy > 50m.
/// </summary>
public class GpsTrackingService_Android : IGpsTrackingService, IDisposable
{
    private const int MovingIntervalMs = 5000;
    private const int FastMovingIntervalMs = 3000;
    private const int StationaryIntervalMs = 10000;
    private const double MaxAccuracyMeters = 50.0; // Lọc GPS noise
    private const double StationaryDistanceMeters = 15.0;

    private CancellationTokenSource? _cts;
    private bool _isTracking;
    private int _currentIntervalMs = MovingIntervalMs;
    private Location? _previousAcceptedLocation;

    public event Action<Location>? LocationChanged;
    public bool IsTracking => _isTracking;
    public Location? LastKnownLocation { get; private set; }

    public async Task StartTrackingAsync()
    {
        if (_isTracking) return;

        // Xin quyền GPS
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;
        }

        _isTracking = true;
        _currentIntervalMs = MovingIntervalMs;

        // Start Foreground Service để giữ app sống
        var intent = new Intent(Platform.AppContext, typeof(Platforms.Android.LocationForegroundService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            Platform.AppContext.StartForegroundService(intent);
        else
            Platform.AppContext.StartService(intent);

        _cts = new CancellationTokenSource();

        // Chạy vòng lặp tracking trên background thread
        _ = Task.Run(async () =>
        {
            try
            {
                await PollLocationAsync();

                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(_currentIntervalMs, _cts.Token);
                    await PollLocationAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Bình thường khi dừng tracking
            }
        });
    }

    private async Task PollLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(4));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null) return;

            // Lọc nhiễu: bỏ vị trí có accuracy kém
            if (location.Accuracy.HasValue && location.Accuracy.Value > MaxAccuracyMeters)
                return;

            UpdatePollingInterval(location);
            _previousAcceptedLocation = location;
            LastKnownLocation = location;

            // Phát sự kiện trên Main Thread để UI có thể cập nhật
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LocationChanged?.Invoke(location);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GPS] Lỗi lấy vị trí: {ex.Message}");
        }
    }

    private void UpdatePollingInterval(Location currentLocation)
    {
        var speedMetersPerSecond = currentLocation.Speed ?? 0;

        if (speedMetersPerSecond >= 5)
        {
            _currentIntervalMs = FastMovingIntervalMs;
            return;
        }

        if (_previousAcceptedLocation == null)
        {
            _currentIntervalMs = MovingIntervalMs;
            return;
        }

        var distanceMeters = GeofenceEngine.Haversine(
            _previousAcceptedLocation.Latitude,
            _previousAcceptedLocation.Longitude,
            currentLocation.Latitude,
            currentLocation.Longitude);

        _currentIntervalMs = distanceMeters < StationaryDistanceMeters && speedMetersPerSecond < 1.2
            ? StationaryIntervalMs
            : MovingIntervalMs;
    }

    public void StopTracking()
    {
        _isTracking = false;

        // Tắt Foreground Service
        var intent = new Intent(Platform.AppContext, typeof(Platforms.Android.LocationForegroundService));
        intent.SetAction("STOP_SERVICE");
        Platform.AppContext.StartService(intent);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        StopTracking();
    }
}
