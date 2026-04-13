using System.ComponentModel.DataAnnotations;

namespace TourMap.AdminWeb.Models;

public class Tour
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TourPoiMapping> PoiMappings { get; set; } = new();
}
