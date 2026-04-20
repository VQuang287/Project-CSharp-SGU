using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class Poi
{
    [Key]
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

    public string? AudioLocalPath { get; set; }

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
    
    // TTS Scripts
    public string? TtsScriptVi { get; set; }
    public string? TtsScriptEn { get; set; }
    public string? TtsScriptZh { get; set; }
    public string? TtsScriptKo { get; set; }
    public string? TtsScriptJa { get; set; }
    public string? TtsScriptFr { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
