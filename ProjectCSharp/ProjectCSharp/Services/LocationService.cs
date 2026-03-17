using Microsoft.Maui.Devices.Sensors;

namespace ProjectCSharp.Services
{
    public class LocationService : ILocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                // Yêu cầu lấy vị trí với độ chính xác cao nhất, timeout sau 10s
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

                var location = await Geolocation.Default.GetLocationAsync(request);

                return location;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Thiết bị không hỗ trợ GPS
                Console.WriteLine($"Thiết bị không hỗ trợ GPS: {fnsEx.Message}");
                return null;
            }
            catch (PermissionException pEx)
            {
                // Chưa được cấp quyền GPS
                Console.WriteLine($"Lỗi quyền truy cập: {pEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Lỗi không xác định khác
                Console.WriteLine($"Lỗi lấy GPS: {ex.Message}");
                return null;
            }
        }
    }
}