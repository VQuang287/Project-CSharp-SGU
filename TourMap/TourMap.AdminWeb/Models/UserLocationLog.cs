using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class UserLocationLog
{
    [Key]
    public int Id { get; set; }

    public string? UserAnonId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
