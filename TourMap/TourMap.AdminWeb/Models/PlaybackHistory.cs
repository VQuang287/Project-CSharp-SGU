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

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }
}
