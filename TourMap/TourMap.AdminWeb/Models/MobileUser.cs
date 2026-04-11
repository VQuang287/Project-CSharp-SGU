using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class MobileUser
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    public string? Token { get; set; } // Refresh token hoặc null tuỳ thiết kế

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
