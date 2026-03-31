using TourMap.Services;

namespace TourMap;

/// <summary>
/// Android: GPS tracking liên tục dùng Timer + Geolocation API.
/// Cập nhật vị trí mỗi 5 giây, lọc nhiễu accuracy > 50m.
/// </summary>
public class GpsTrackingService_Android : IGpsTrackingService, IDisposable
{
    private const int IntervalMs = 5000; // 5 giây
    private const double MaxAccuracyMeters = 50.0; // Lọc GPS noise

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _isTracking;

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
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(IntervalMs));

        // Chạy vòng lặp tracking trên background thread
        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await PollLocationAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Bình thường khi dừng tracking
            }
        });

        // Lấy vị trí đầu tiên ngay lập tức
        await PollLocationAsync();
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

    public void StopTracking()
    {
        _isTracking = false;
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        StopTracking();
    }
}
