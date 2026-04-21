namespace TourMap.Services;

/// <summary>
/// GPS tracking liên tục — phát sự kiện mỗi khi vị trí thay đổi.
/// </summary>
public interface IGpsTrackingService
{
    /// <summary>Sự kiện phát ra mỗi khi có vị trí GPS mới.</summary>
    event Action<Location>? LocationChanged;

    /// <summary>Bắt đầu tracking liên tục (mỗi 5 giây).</summary>
    Task StartTrackingAsync();

    /// <summary>Dừng tracking.</summary>
    void StopTracking();

    /// <summary>Đang tracking hay không.</summary>
    bool IsTracking { get; }

    /// <summary>Vị trí mới nhất.</summary>
    Location? LastKnownLocation { get; }
}
