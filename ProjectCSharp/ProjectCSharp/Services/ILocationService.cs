using Microsoft.Maui.Devices.Sensors;

namespace ProjectCSharp.Services
{
    public interface ILocationService
    {
        // Hàm lấy vị trí hiện tại của người dùng
        Task<Location?> GetCurrentLocationAsync();
    }
}
