using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class MobileUser
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    // === Auth fields ===
    [MaxLength(256)]
    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    [MaxLength(100)]
    public string DisplayName { get; set; } = "Khách";

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Guest"; // Guest, User, Premium

    [MaxLength(20)]
    public string AuthProvider { get; set; } = "local"; // local, google, apple

    public bool IsEmailVerified { get; set; } = false;

    // === Token fields ===
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // === Timestamps ===
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
