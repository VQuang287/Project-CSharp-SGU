using SQLite;

namespace TourMap.Models;

public class Poi
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; } = 50;
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? MapLink { get; set; }

    /// <summary>Đường dẫn file audio đã cache trên thiết bị (local).</summary>
    public string? AudioLocalPath { get; set; }

    // AI Multilingual fields — populated from server sync
    public string? DescriptionEn { get; set; }
    public string? AudioUrlEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? AudioUrlZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? AudioUrlKo { get; set; }
    public string? DescriptionJa { get; set; }
    public string? AudioUrlJa { get; set; }
    public string? DescriptionFr { get; set; }
    public string? AudioUrlFr { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlaybackHistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string PoiId { get; set; } = string.Empty;
    public string PoiTitle { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "Unknown";
    public string AudioSource { get; set; } = "TTS";
    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
}

