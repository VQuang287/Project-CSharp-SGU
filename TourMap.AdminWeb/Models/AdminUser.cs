using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class AdminUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Administrator";

    public bool IsActive { get; set; } = true;

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public DateTime? LastLoginUtc { get; set; }
}
