namespace TourMap
{
    public class LocationService_Android : Services.ILocationService
    {
        public LocationService_Android()
        {
            // Implementation for Android
        }

        public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
                
                if (status != PermissionStatus.Granted)
                    return null;

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                using var cancelTokenSource = new CancellationTokenSource();

                var location = await Geolocation.Default.GetLocationAsync(request, cancelTokenSource.Token);
                return location;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Location error: {ex.Message}");
                return null;
            }
        }
    }
}
