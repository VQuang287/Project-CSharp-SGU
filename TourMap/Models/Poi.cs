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
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
}
