using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class QrCodeEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PoiId { get; set; } = string.Empty;

    [Required]
    public string DeepLink { get; set; } = string.Empty;

    [Required]
    public string QrImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
