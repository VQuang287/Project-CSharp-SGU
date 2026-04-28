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
    
    // Local cached audio paths for multilingual audio
    public string? AudioLocalPathEn { get; set; }
    public string? AudioLocalPathZh { get; set; }
    public string? AudioLocalPathKo { get; set; }
    public string? AudioLocalPathJa { get; set; }
    public string? AudioLocalPathFr { get; set; }
    
    // TTS Scripts - stored in database for offline TTS generation
    public string? TtsScriptVi { get; set; }
    public string? TtsScriptEn { get; set; }
    public string? TtsScriptZh { get; set; }
    public string? TtsScriptKo { get; set; }
    public string? TtsScriptJa { get; set; }
    public string? TtsScriptFr { get; set; }
    
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

/// <summary>Tour/Food tour definition - a collection of POIs in a specific order.</summary>
[Table("Tours")]
public class Tour
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThumbnailUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Not stored in DB - populated from TourPoiMappings
    [Ignore]
    public List<TourPoiMapping> PoiMappings { get; set; } = new();

    [Ignore]
    public List<Poi> Pois { get; set; } = new();
}

/// <summary>Maps a POI to a Tour with ordering.</summary>
[Table("TourPoiMappings")]
public class TourPoiMapping
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string TourId { get; set; } = string.Empty;

    [Indexed]
    public string PoiId { get; set; } = string.Empty;

    public int OrderIndex { get; set; } = 0;
}

