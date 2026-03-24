using TourMap.Models;

namespace TourMap.Services;

public class SampleDataService
{
    private readonly List<Poi> _pois = new()
    {
        new Poi { Title = "City Hall", Description = "Historic city hall.", Latitude = 10.762622, Longitude = 106.660172, RadiusMeters = 80, Priority = 10 },
        new Poi { Title = "River Walk", Description = "Nice riverside.", Latitude = 10.763, Longitude = 106.662, RadiusMeters = 60, Priority = 5 },
        new Poi { Title = "Old Bridge", Description = "Old bridge built 1920.", Latitude = 10.761, Longitude = 106.661, RadiusMeters = 50, Priority = 7 }
    };

    public Task<List<Poi>> GetPoisAsync()
    {
        return Task.FromResult(_pois.ToList());
    }
}
