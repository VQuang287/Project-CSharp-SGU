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
    private int _isTrackingFlag = 0; // 0 = false, 1 = true
    private int _currentIntervalMs = MovingIntervalMs;
    private Location? _previousAcceptedLocation;

    public event Action<Location>? LocationChanged;
    public bool IsTracking => _isTrackingFlag == 1;
    public Location? LastKnownLocation { get; private set; }

    public async Task StartTrackingAsync()
    {
        // Thread-safe check to prevent multiple concurrent tracking tasks
        if (Interlocked.CompareExchange(ref _isTrackingFlag, 1, 0) == 1)
            return;

        // ═══════════════════════════════════════════════════════════
        // STEP 1: Xin quyền GPS cơ bản (Foreground)
        // ═══════════════════════════════════════════════════════════
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                Interlocked.Exchange(ref _isTrackingFlag, 0);
                return;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // STEP 2: Xin quyền GPS Background cho Android 12+ (API 31+)
        // Yêu cầu bắt buộc để tracking khi app ở background
        // ═══════════════════════════════════════════════════════════
        if (OperatingSystem.IsAndroidVersionAtLeast(31)) // Android 12+ (API 31)
        {
            var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (bgStatus != PermissionStatus.Granted)
            {
                Console.WriteLine("[GpsTracking] Requesting background location permission (Android 12+)...");
                bgStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                
                if (bgStatus != PermissionStatus.Granted)
                {
                    Console.WriteLine("[GpsTracking] WARNING: Background location permission denied. Tracking may not work when app is in background.");
                    // Vẫn tiếp tục vì foreground tracking vẫn hoạt động
                }
                else
                {
                    Console.WriteLine("[GpsTracking] Background location permission granted.");
                }
            }
        }

        _currentIntervalMs = MovingIntervalMs;

        // Start Foreground Service để giữ app sống
        var intent = new Intent(Platform.AppContext, typeof(Platforms.Android.LocationForegroundService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            Platform.AppContext.StartForegroundService(intent);
        else
            Platform.AppContext.StartService(intent);

        // Cancel existing tracking if any
        _cts?.Cancel();
        _cts?.Dispose();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Chạy vòng lặp tracking trên background thread, không block caller
        _ = Task.Run(async () =>
        {
            try
            {
                await PollLocationAsync();

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(_currentIntervalMs, token);
                    await PollLocationAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Bình thường khi dừng tracking
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GpsTracking] Error in background tracking task: {ex.Message}");
                Interlocked.Exchange(ref _isTrackingFlag, 0);
            }
        }, token);
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
        catch (OperationCanceledException)
        {
            // Ignore cancellation from stop/dispose flow
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
        Interlocked.Exchange(ref _isTrackingFlag, 0);

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
