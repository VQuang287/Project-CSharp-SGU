using TourMap.Models;

namespace TourMap.Services;

/// <summary>
/// Geofence Engine — trái tim của ứng dụng.
/// Nhận vị trí GPS → tính khoảng cách Haversine → kiểm tra POI trong bán kính
/// → debounce + cooldown → phát sự kiện POITriggered.
/// </summary>
public class GeofenceEngine
{
    // === Thông số kỹ thuật (theo PRD) ===
    private const double DebounceDurationSeconds = 30;   // 30 giây giữa 2 trigger bất kỳ
    private const double CooldownDurationMinutes = 10;   // 10 phút per POI
    private const double MaxScanRadiusMeters = 500;      // Chỉ xét POI trong 500m
    private const int MaxPoisInRange = 3;                // Top 3 POI gần nhất

    // Thread-safety lock for GPS thread + UI thread access
    private readonly object _lock = new();

    // Cooldown per POI: lưu thời điểm trigger gần nhất
    private readonly Dictionary<string, DateTime> _cooldowns = new();

    // Debounce: thời điểm trigger gần nhất (bất kỳ POI nào)
    private DateTime _lastTriggerTime = DateTime.MinValue;

    // Danh sách POI để kiểm tra (immutable snapshot pattern)
    private IReadOnlyList<Poi> _pois = Array.Empty<Poi>();

    /// <summary>Sự kiện phát ra khi user đi vào vùng POI (đã qua debounce + cooldown).</summary>
    public event Action<Poi>? POITriggered;

    /// <summary>Cập nhật danh sách POI từ database.</summary>
    public void UpdatePois(List<Poi> pois)
    {
        lock (_lock)
        {
            _pois = (pois ?? new List<Poi>()).AsReadOnly();
        }
    }

    /// <summary>
    /// Gọi mỗi khi GPS cập nhật vị trí mới. Kiểm tra tất cả POI.
    /// </summary>
    public void OnLocationChanged(Location userLocation)
    {
        // Take a thread-safe snapshot of POIs and cooldown state
        IReadOnlyList<Poi> pois;
        lock (_lock) { pois = _pois; }

        if (pois.Count == 0) return;

        var now = DateTime.UtcNow;

        // Bước 1: Debounce toàn cục — không trigger liên tục
        if ((now - _lastTriggerTime).TotalSeconds < DebounceDurationSeconds)
            return;

        // Bước 2: Lọc POI trong vùng R_max = 500m và tính khoảng cách
        var nearbyPois = pois
            .Where(p => p.IsActive && p.RadiusMeters > 0)
            .Select(poi => new
            {
                Poi = poi,
                Distance = Haversine(userLocation.Latitude, userLocation.Longitude,
                                     poi.Latitude, poi.Longitude)
            })
            .Where(x => x.Distance <= MaxScanRadiusMeters)
            .OrderBy(x => x.Distance)  // Gần nhất trước
            .Take(MaxPoisInRange)
            .ToList();

        if (nearbyPois.Count == 0) return;

        // Bước 3: Kiểm tra từng POI (ưu tiên theo Priority cao nhất, trong nhóm gần nhất)
        var candidatePoi = nearbyPois
            .Where(x => x.Distance <= x.Poi.RadiusMeters) // Trong bán kính kích hoạt
            .OrderByDescending(x => x.Poi.Priority)        // Ưu tiên cao nhất
            .ThenBy(x => x.Distance)                       // Nếu cùng priority → gần nhất
            .FirstOrDefault();

        if (candidatePoi == null) return;

        // Bước 4: Cooldown per POI — không phát lại cùng POI trong 10 phút
        var poiId = candidatePoi.Poi.Id;
        lock (_lock)
        {
            if (_cooldowns.TryGetValue(poiId, out var lastTriggered))
            {
                if ((now - lastTriggered).TotalMinutes < CooldownDurationMinutes)
                    return; // Còn trong cooldown
            }

            // ✅ Trigger thành công!
            _lastTriggerTime = now;
            _cooldowns[poiId] = now;
        }

        Console.WriteLine($"[Geofence] 🎯 Triggered POI: {candidatePoi.Poi.Title} " +
                          $"(distance={candidatePoi.Distance:F0}m, " +
                          $"radius={candidatePoi.Poi.RadiusMeters}m, " +
                          $"priority={candidatePoi.Poi.Priority})");

        POITriggered?.Invoke(candidatePoi.Poi);
    }

    /// <summary>
    /// Tìm POI gần nhất với vị trí hiện tại (để highlight trên bản đồ).
    /// </summary>
    public Poi? GetNearestPoi(double latitude, double longitude)
    {
        IReadOnlyList<Poi> pois;
        lock (_lock) { pois = _pois; }

        if (pois.Count == 0) return null;

        return pois
            .Select(poi => new
            {
                Poi = poi,
                Distance = Haversine(latitude, longitude, poi.Latitude, poi.Longitude)
            })
            .Where(x => x.Distance <= MaxScanRadiusMeters)
            .OrderBy(x => x.Distance)
            .FirstOrDefault()?.Poi;
    }

    /// <summary>
    /// Công thức Haversine — tính khoảng cách (m) giữa 2 điểm GPS trên bề mặt Trái Đất.
    /// </summary>
    public static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000; // Bán kính Trái Đất (m)
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
