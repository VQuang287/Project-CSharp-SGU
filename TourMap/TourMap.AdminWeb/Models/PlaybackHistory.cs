using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourMap.AdminWeb.Models;

public class PlaybackHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PoiId { get; set; } = string.Empty;

    public string? DeviceId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(32)]
    public string TriggerType { get; set; } = "GPS";

    public int DurationSeconds { get; set; }

    public bool IsCompleted { get; set; }

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }
}
