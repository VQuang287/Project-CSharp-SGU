namespace TourMap.Services
{
    public interface ILocationService
    {
        Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync();
    }
}
